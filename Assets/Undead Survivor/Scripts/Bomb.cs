using UnityEngine;

/// 수류탄 무기의 발사 주기와 투사체 이동, 폭발 및 초월 효과를 처리하는 스크립트.
/// 무기 오브젝트와 풀에서 꺼낸 투사체가 같은 컴포넌트를 공유

public class Bomb : MonoBehaviour
{
    enum BombMode
    {
        Idle,
        Weapon,
        Grenade,
        Fragment
    }

    [Header("# Weapon")]
    public int prefabId;
    public int fragmentPrefabId;
    public int explosionFxPrefabId = -1;
    public int fireZonePrefabId = -1;
    public float damage;
    public float speed;
    public int masterUpgradeCount;
    public ItemData originData;

    [Header("# Projectile")]
    public float maxRange = 5f;
    public float explosionRadius = 2.5f;
    public int bombStage;

    [Header("# Balance")]
    public float defaultCooldown = 1.8f;
    public float maxThrowRange = 3.2f;
    public float throwSpeed = 7.5f;
    public float fragmentSpeed = 15f;
    public float fragmentRange = 4.5f;
    public float fragmentDamageRate = 0.5f;
    public float fragmentSpawnOffset = 0.8f;
    public float fragmentHitDelay = 0.08f;
    public float fragmentVisualScale = 0.45f;
    public int fragmentSortingOrder = 20;
    public int baseFragmentCount = 8;
    public int fragmentPer = 1;
    public float fireDamageRate = 0.25f;
    public float fireRadiusRate = 0.85f;
    public float fireDuration = 3f;

    [HideInInspector] public Sprite shardSprite;

    BombMode mode = BombMode.Idle;
    Player player;
    Rigidbody2D rigid;

    float baseCooldown;
    float gloveRate;
    float cooldown;
    float timer;
    float hitDelayTimer;
    int remainingHits;
    int fragmentBonusCount;
    bool hasExploded;
    Vector3 startPosition;

    public float Cooldown
    {
        get { return cooldown; }
    }

    public int CurrentStage
    {
        get
        {
            if (originData == null || originData.damages == null || originData.damages.Length == 0)
                return 0;

            if (masterUpgradeCount <= 0)
                return 0;

            return 1 + (masterUpgradeCount - 1) / originData.damages.Length;
        }
    }

    void Awake()
    {
        CacheReferences();
    }

    void OnEnable()
    {
        hasExploded = false;
    }

    void Update()
    {
        if (!GameManager.instance.isLive)
            return;

        switch (mode)
        {
            case BombMode.Weapon:
                UpdateWeapon();
                break;
            case BombMode.Grenade:
            case BombMode.Fragment:
                UpdateProjectile();
                break;
        }
    }

    void CacheReferences()
    {
        if (player == null && GameManager.instance != null)
        {
            player = GameManager.instance.player;
        }

        if (rigid == null)
        {
            rigid = GetComponent<Rigidbody2D>();
        }
    }

    // 플레이어에게 장착된 수류탄 무기의 발사 주기를 갱신
    void UpdateWeapon()
    {
        timer += Time.deltaTime;
        if (timer <= cooldown)
            return;

        timer = 0f;
        ThrowGrenade();
    }

    // 투척된 수류탄을 목표 지점까지 이동시키고 도착하면 폭발
    void UpdateProjectile()
    {
        CacheReferences();

        if (rigid == null)
            return;

        if (hitDelayTimer > 0f)
        {
            hitDelayTimer -= Time.deltaTime;
        }

        float travelDistance = Vector3.Distance(startPosition, transform.position);
        if (travelDistance < maxRange)
            return;

        if (mode == BombMode.Grenade)
        {
            Explode();
            return;
        }

        ReturnToPool();
    }

    public void Init(ItemData data)
    {
        CacheReferences();

        mode = BombMode.Weapon;
        hasExploded = false;
        timer = 0f;

        name = "Bomb" + data.itemId;
        transform.parent = player.transform;
        transform.localPosition = Vector3.zero;

        damage = data.baseDamage * Character.Damage;
        fragmentBonusCount = data.baseCount + Character.Count;
        originData = data;

        prefabId = FindPrefabId(data.projectile);
        CopyProjectileSettings(data.projectile);

        baseCooldown = defaultCooldown;
        if (data.speeds != null && data.speeds.Length > 0)
        {
            baseCooldown = data.speeds[0];
        }

        ApplyGear();

        player.BroadcastMessage("ApplyGear", SendMessageOptions.DontRequireReceiver);
    }

    // 프리팹에 설정된 투척 및 초월 밸런스 값을 장착 무기에 복사
    void CopyProjectileSettings(GameObject projectile)
    {
        if (projectile == null)
            return;

        Bomb projectileBomb = projectile.GetComponent<Bomb>();
        if (projectileBomb == null)
            return;

        fragmentPrefabId = projectileBomb.fragmentPrefabId;
        explosionFxPrefabId = projectileBomb.explosionFxPrefabId;
        fireZonePrefabId = projectileBomb.fireZonePrefabId;
        explosionRadius = projectileBomb.explosionRadius;
        maxRange = projectileBomb.maxRange;
        defaultCooldown = projectileBomb.defaultCooldown;
        maxThrowRange = projectileBomb.maxThrowRange;
        throwSpeed = projectileBomb.throwSpeed;
        fragmentSpeed = projectileBomb.fragmentSpeed;
        fragmentRange = projectileBomb.fragmentRange;
        fragmentDamageRate = projectileBomb.fragmentDamageRate;
        fragmentSpawnOffset = projectileBomb.fragmentSpawnOffset;
        fragmentHitDelay = projectileBomb.fragmentHitDelay;
        fragmentVisualScale = projectileBomb.fragmentVisualScale;
        fragmentSortingOrder = projectileBomb.fragmentSortingOrder;
        baseFragmentCount = projectileBomb.baseFragmentCount;
        fragmentPer = projectileBomb.fragmentPer;
        fireDamageRate = projectileBomb.fireDamageRate;
        fireRadiusRate = projectileBomb.fireRadiusRate;
        fireDuration = projectileBomb.fireDuration;
    }

    public void LevelUp(float damage, int count)
    {
        this.damage = damage;
        fragmentBonusCount += count;

        if (originData != null && originData.speeds != null && masterUpgradeCount < originData.speeds.Length)
        {
            baseCooldown = originData.speeds[masterUpgradeCount];
        }

        ApplyGear();
        player.BroadcastMessage("ApplyGear", SendMessageOptions.DontRequireReceiver);
    }

    public void MasterUpgrade(float damageMultiplier, int extraCount)
    {
        masterUpgradeCount++;

        damage += damage * damageMultiplier;
        fragmentBonusCount += extraCount;

        ApplyGear();
    }

    public void ApplyGear()
    {
        float rate = Mathf.Clamp(gloveRate, 0f, 0.95f);
        cooldown = baseCooldown * Character.WeaponRate * (1f - rate);
        speed = cooldown;
    }

    public void ApplyGear(float rate)
    {
        gloveRate = rate;
        ApplyGear();
    }

    public void Init(float damage, int per, Vector3 dir, Vector3 startPos = default, int bombStage = 0, int prefabId = 0, bool isBomb = false)
    {
        BombMode projectileMode = isBomb ? BombMode.Grenade : BombMode.Fragment;
        int hitLimit = isBomb ? 0 : Mathf.Max(1, per + 1);
        Sprite sprite = projectileMode == BombMode.Fragment ? shardSprite : null;
        InitProjectile(projectileMode, damage, dir, startPos, bombStage, prefabId, hitLimit, sprite);
    }

    int FindPrefabId(GameObject projectile)
    {
        for (int index = 0; index < GameManager.instance.pool.prefabs.Length; index++)
        {
            if (projectile == GameManager.instance.pool.prefabs[index])
            {
                return index;
            }
        }

        return 0;
    }

    // 풀에서 재사용된 수류탄과 파편의 상태를 매번 새 값으로 초기화
    void InitProjectile(BombMode projectileMode, float damage, Vector3 dir, Vector3 startPos, int stage, int prefabId, int hitLimit, Sprite fragmentSprite)
    {
        CacheReferences();

        mode = projectileMode;
        originData = null;
        hasExploded = false;

        this.damage = damage;
        this.prefabId = prefabId;
        bombStage = stage;
        remainingHits = hitLimit;
        shardSprite = fragmentSprite;

        startPosition = startPos == default ? transform.position : startPos;
        hitDelayTimer = mode == BombMode.Fragment ? fragmentHitDelay : 0f;

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.enabled = true;
            if (mode == BombMode.Fragment && fragmentSprite != null)
            {
                sr.sprite = fragmentSprite;
            }
        }

        if (rigid != null)
        {
            float moveSpeed = mode == BombMode.Grenade ? throwSpeed : fragmentSpeed;
            rigid.velocity = dir.normalized * moveSpeed;
        }
    }

    Sprite GetFragmentSprite()
    {
        if (originData == null)
            return null;

        if (originData.customProjectileEvolutions != null && originData.customProjectileEvolutions.Length > 0)
        {
            return originData.customProjectileEvolutions[0];
        }

        return null;
    }

    void ThrowGrenade()
    {
        CacheReferences();

        if (player == null)
            return;

        Vector3 targetPos = GetTargetPosition();
        Vector3 dir = targetPos - transform.position;
        if (dir.sqrMagnitude <= 0.0001f)
        {
            dir = Vector3.up;
        }

        float throwRange = Mathf.Min(dir.magnitude, maxThrowRange);
        dir = dir.normalized;

        Transform grenade = GameManager.instance.pool.Get(prefabId).transform;
        grenade.position = transform.position;
        grenade.rotation = Quaternion.FromToRotation(Vector3.up, dir);

        Bomb grenadeBomb = grenade.GetComponent<Bomb>();
        if (grenadeBomb == null)
        {
            grenade.gameObject.SetActive(false);
            return;
        }

        grenadeBomb.maxRange = throwRange;
        grenadeBomb.fragmentPrefabId = fragmentPrefabId;
        grenadeBomb.explosionFxPrefabId = explosionFxPrefabId;
        grenadeBomb.fireZonePrefabId = fireZonePrefabId;
        grenadeBomb.InitProjectile(BombMode.Grenade, damage, dir, transform.position, CurrentStage, prefabId, 0, GetFragmentSprite());

        AudioManager.instance.PlaySfx(AudioManager.Sfx.Range);
    }

    Vector3 GetTargetPosition()
    {
        if (player.scanner != null && player.scanner.nearestTarget != null)
        {
            return player.scanner.nearestTarget.position;
        }

        Vector3 fallbackDir = player.inputVec != Vector2.zero ? (Vector3)player.inputVec.normalized : Vector3.up;
        return transform.position + fallbackDir * 3f;
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (mode == BombMode.Weapon || mode == BombMode.Idle)
            return;

        if (hitDelayTimer > 0f)
            return;

        if (!collision.CompareTag("Enemy"))
            return;

        if (mode == BombMode.Grenade)
        {
            Explode();
            return;
        }

        Enemy enemy = collision.GetComponent<Enemy>();
        if (enemy != null)
        {
            enemy.TakeDamage(damage);
        }

        remainingHits--;
        if (remainingHits <= 0)
        {
            ReturnToPool();
        }
    }

    // 초월 단계에 따라 기본 폭발 뒤 파편 또는 화염지대를 추가.
    void Explode()
    {
        if (hasExploded)
            return;

        hasExploded = true;

        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(transform.position, explosionRadius, LayerMask.GetMask("Enemy"));
        foreach (Collider2D enemyCollider in hitEnemies)
        {
            Enemy enemy = enemyCollider.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
            }
        }

        PlayExplosionFx();

        if (bombStage >= 1)
        {
            SpawnFragments();
        }

        if (bombStage >= 2)
        {
            SpawnFireZone();
        }

        AudioManager.instance.PlaySfx(AudioManager.Sfx.Dead);
        ReturnToPool();
    }

    void SpawnFragments()
    {
        int fragmentCount = Mathf.Max(1, baseFragmentCount + fragmentBonusCount);

        for (int i = 0; i < fragmentCount; i++)
        {
            Vector3 dir = Quaternion.Euler(0, 0, i * (360f / fragmentCount)) * Vector3.up;
            GameObject fragment = GameManager.instance.pool.Get(fragmentPrefabId);
            fragment.transform.position = transform.position + dir * fragmentSpawnOffset;
            fragment.transform.rotation = Quaternion.FromToRotation(Vector3.up, dir);
            fragment.transform.localScale = Vector3.one * fragmentVisualScale;

            SpriteRenderer sourceRenderer = GetComponent<SpriteRenderer>();
            SpriteRenderer fragmentRenderer = fragment.GetComponent<SpriteRenderer>();
            if (fragmentRenderer != null)
            {
                fragmentRenderer.sprite = shardSprite != null ? shardSprite : (sourceRenderer != null ? sourceRenderer.sprite : null);
                fragmentRenderer.sortingOrder = fragmentSortingOrder;
                fragmentRenderer.color = Color.white;
                fragmentRenderer.enabled = true;
            }

            BombFragment fragmentLogic = fragment.GetComponent<BombFragment>();
            if (fragmentLogic == null)
            {
                fragmentLogic = fragment.AddComponent<BombFragment>();
            }

            if (fragmentLogic != null)
            {
                fragmentLogic.Init(damage * fragmentDamageRate, fragmentPer + 1, fragmentRange, fragmentHitDelay, dir);
            }
        }
    }

    void SpawnFireZone()
    {
        float fireDamage = damage * fireDamageRate;
        float fireRadius = explosionRadius * fireRadiusRate;

        if (fireZonePrefabId < 0 || GameManager.instance.pool == null || fireZonePrefabId >= GameManager.instance.pool.prefabs.Length)
            return;

        GameObject fireZone = GameManager.instance.pool.Get(fireZonePrefabId);
        fireZone.transform.position = transform.position;
        fireZone.transform.rotation = Quaternion.identity;

        BombFireZone pooledZone = fireZone.GetComponent<BombFireZone>();
        if (pooledZone == null)
        {
            pooledZone = fireZone.AddComponent<BombFireZone>();
        }

        pooledZone.SetupZone(fireDamage, fireRadius, fireDuration);
    }

    void PlayExplosionFx()
    {
        if (explosionFxPrefabId < 0 || GameManager.instance.pool == null || explosionFxPrefabId >= GameManager.instance.pool.prefabs.Length)
            return;

        GameObject fx = GameManager.instance.pool.Get(explosionFxPrefabId);
        fx.transform.position = transform.position;

        BombExplosionFX pooledFx = fx.GetComponent<BombExplosionFX>();
        if (pooledFx == null)
        {
            pooledFx = fx.AddComponent<BombExplosionFX>();
        }

        pooledFx.PlayExplosion(explosionRadius);
    }

    void ReturnToPool()
    {
        if (rigid != null)
        {
            rigid.velocity = Vector2.zero;
        }

        mode = BombMode.Idle;
        gameObject.SetActive(false);
    }
}
