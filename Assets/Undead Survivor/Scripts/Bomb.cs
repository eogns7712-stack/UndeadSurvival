using UnityEngine;

// 수류탄 무기의 발사 주기와 투사체 이동, 폭발 및 초월 효과를 처리.
// 무기 오브젝트와 풀에서 꺼낸 투사체가 같은 컴포넌트를 공유.
// 1.중복 폭발 방지
// 2.폭발 범위 안 적 검색
// 3.적에게 데미지
// 4.폭발 이펙트 재생
// 5.초월 단계에 따라 파편 생성
// 6.초월 단계에 따라 화염 장판 생성
// 7.사운드 재생
// 8.풀 반환
public class Bomb : MonoBehaviour
{   // 하나의 Bomb 컴포넌트가 장착 무기와 날아가는 수류탄 역할을 모두 맡기 때문에 현재 역할을 구분.
    enum BombMode
    {
        Idle,
        Weapon,
        Grenade
    }

    [Header("# Weapon")]    // 풀 매니저에서 꺼낼 프리팹 ID와 장착 무기의 현재 공격 수치를 보관.
    public int prefabId;
    public int fragmentPrefabId;
    public int explosionFxPrefabId = -1;
    public int fireZonePrefabId = -1;
    public float damage;
    public float speed;
    public int masterUpgradeCount;
    public ItemData originData;

    [Header("# Projectile")]    // 풀에서 꺼낸 투사체가 얼마나 날아가고, 폭발 범위와 초월 단계를 어떻게 적용할지 결정.
    public float maxRange = 5f;
    public float explosionRadius = 2.5f;
    public int bombStage;

    [Header("# Balance")]   // 수류탄의 발사 간격, 투척 거리, 파편과 화염지대의 세부 값.
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

    [HideInInspector] public Sprite shardSprite;    // 1단계 초월 파편에 적용할 스프라이트는 ItemData에서 받아 투사체로 전달. 
    //  [HideInInspector] : Inspector 창에 표시하지 않지만, 직렬화(Serialize)는 유지하도록 하는 속성

    BombMode mode = BombMode.Idle;
    Player player;
    Rigidbody2D rigid;

    // 장착 무기 상태에서 사용하는 발사 쿨타임과 투척 상태를 보관.
    float baseCooldown;
    float gloveRate;
    float cooldown;
    float timer;
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
            // 수류탄은 masterUpgradeCount를 ItemData의 레벨 데이터 길이로 나눠 현재 초월 단계를 계산.
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

        // 장착된 무기는 발사 주기만 계산하고, 풀에서 꺼낸 투사체는 이동 거리와 충돌을 처리한다.
        switch (mode)
        {
            case BombMode.Weapon:
                UpdateWeapon();
                break;
            case BombMode.Grenade:
                UpdateProjectile();
                break;
        }
    }

    void CacheReferences()
    {
        // 오브젝트 풀링된 비활성화 오브젝트가 다시 활성화될 때 참조가 비어 있는 경우. 필요할 때마다 보강.
        if (player == null && GameManager.instance != null)
        {
            player = GameManager.instance.player;
        }

        if (rigid == null)
        {
            rigid = GetComponent<Rigidbody2D>();
        }
    }

    void UpdateWeapon() // 플레이어에게 장착된 수류탄 무기의 발사 주기를 갱신.
    {
        timer += Time.deltaTime;
        if (timer <= cooldown)
            return;

        timer = 0f;
        ThrowGrenade();
    }

    void UpdateProjectile() // 투척된 수류탄을 목표 지점까지 이동시키고 도착하면 폭발.
    {
        CacheReferences();

        if (rigid == null)
            return;

        float travelDistance = Vector3.Distance(startPosition, transform.position);
        if (travelDistance < maxRange)
            return;

        Explode();
    }

    public void Init(ItemData data)
    {
        CacheReferences();

        // 플레이어가 수류탄 아이템을 처음 얻었을 때, 이 오브젝트는 발사 담당 무기로 동작.
        mode = BombMode.Weapon;
        hasExploded = false;
        timer = 0f;

        name = "Bomb" + data.itemId;
        transform.parent = player.transform;
        transform.localPosition = Vector3.zero;

        damage = data.baseDamage * Character.Damage;
        fragmentBonusCount = data.baseCount + Character.Count;
        originData = data;

        // ItemData에 연결된 projectile 프리팹을 기준으로 풀 ID와 밸런스 값을 가져온다.
        prefabId = FindPrefabId(data.projectile);
        CopyProjectileSettings(data.projectile);

        baseCooldown = defaultCooldown;
        if (data.speeds != null && data.speeds.Length > 0)
        {
            baseCooldown = data.speeds[0];
        }

        ApplyGear();

        // 새 무기가 장착되었음을 다른 장비/손 표시 스크립트에도 알려준다.
        player.BroadcastMessage("ApplyGear", SendMessageOptions.DontRequireReceiver);
    }

    void CopyProjectileSettings(GameObject projectile)  // 프리팹에 설정된 투척 및 초월 밸런스 값을 장착 무기에 복사
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
    {   // 미초월된 수류탄의 레벨업은 외부에서 계산된 누적 데미지를 받아오고, 파편 수 보너스만 추가로 누적.
        this.damage = damage;
        fragmentBonusCount += count;

        // 수류탄 자체 레벨업으로만 수류탄 기본 쿨타임을 교체.
        if (originData != null && originData.speeds != null && masterUpgradeCount < originData.speeds.Length)
        {
            baseCooldown = originData.speeds[masterUpgradeCount];
        }

        ApplyGear();
        player.BroadcastMessage("ApplyGear", SendMessageOptions.DontRequireReceiver);
    }

    public void MasterUpgrade(float damageMultiplier, int extraCount)
    {   // 초월 단계(masterUpgradeCount)를 올린 뒤 현재 데미지 기준으로 곱 적용.
        masterUpgradeCount++;

        damage += damage * damageMultiplier;
        fragmentBonusCount += extraCount;

        ApplyGear();
    }

    public void ApplyGear()
    {   // 장갑과 상점 공속 보너스가 겹쳐도 연사속도가 0 이하로 내려가지 않게 제한.
        float rate = Mathf.Clamp(gloveRate, 0f, 0.95f);
        cooldown = baseCooldown * Character.WeaponRate * (1f - rate);
        speed = cooldown;
    }

    public void ApplyGear(float rate)
    {   // Gear에서 전달하는 장갑 공속 보너스는 저장해 두고, 이후 수류탄 레벨업에도 다시 적용.
        gloveRate = rate;
        ApplyGear();
    }

    int FindPrefabId(GameObject projectile)
    {   // ItemData는 프리팹 참조만 가지고 있으므로 풀 배열에서 같은 프리팹의 인덱스를 찾는다.
        for (int index = 0; index < GameManager.instance.pool.prefabs.Length; index++)
        {
            if (projectile == GameManager.instance.pool.prefabs[index])
            {
                return index;
            }
        }

        return 0;
    }

    // 풀에서 재사용된 수류탄의 상태를 매번 새 값으로 갱신(수류탄이 레벨업 되었을 수 있기때문)
    void InitProjectile(float damage, Vector3 dir, Vector3 startPos, int stage, int prefabId, Sprite fragmentSprite)
    {
        CacheReferences();

        mode = BombMode.Grenade;
        originData = null;
        hasExploded = false;

        this.damage = damage;
        this.prefabId = prefabId;
        bombStage = stage;
        shardSprite = fragmentSprite;

        startPosition = startPos == default ? transform.position : startPos;

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.enabled = true;
        }

        if (rigid != null)
        {
            rigid.velocity = dir.normalized * throwSpeed;
        }
    }

    Sprite GetFragmentSprite()
    {   // 1단계 초월 파편 스프라이트는 ItemData의 Custom Projectile Evolutions 첫 번째 칸을 사용.
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
        {   // 플레이어와 목표가 같은 위치일 때 방향 벡터가 0이 되는 상황을 막는다.
            dir = Vector3.up;
        }

        // 실제 투척 거리는 적이 멀리 있어도 최대 투척 거리 안으로 제한한다.
        float throwRange = Mathf.Min(dir.magnitude, maxThrowRange);
        dir = dir.normalized;

        Transform grenade = GameManager.instance.pool.Get(prefabId).transform;
        grenade.position = transform.position;
        grenade.rotation = Quaternion.FromToRotation(Vector3.up, dir);

        Bomb grenadeBomb = grenade.GetComponent<Bomb>();
        if (grenadeBomb == null)
        {   // 풀 ID가 잘못 연결된 경우 게임이 멈추지 않도록 꺼낸 오브젝트를 바로 되돌림(오류방지)
            grenade.gameObject.SetActive(false);
            return;
        }

        grenadeBomb.maxRange = throwRange;
        grenadeBomb.fragmentPrefabId = fragmentPrefabId;
        grenadeBomb.explosionFxPrefabId = explosionFxPrefabId;
        grenadeBomb.fireZonePrefabId = fireZonePrefabId;
        grenadeBomb.fragmentBonusCount = fragmentBonusCount;
        grenadeBomb.InitProjectile(damage, dir, transform.position, CurrentStage, prefabId, GetFragmentSprite());

        AudioManager.instance.PlaySfx(AudioManager.Sfx.Range);
    }

    Vector3 GetTargetPosition()
    {   // 원거리 무기처럼 Scanner가 찾은 가장 가까운 적을 우선 목표 지정.
        if (player.scanner != null && player.scanner.nearestTarget != null)
        {
            return player.scanner.nearestTarget.position;
        }

        // 목표가 없으면 플레이어 입력 방향, 입력도 없으면 위쪽으로 투척.
        Vector3 fallbackDir = player.inputVec != Vector2.zero ? (Vector3)player.inputVec.normalized : Vector3.up;
        return transform.position + fallbackDir * 3f;
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        // 장착 무기 상태의 Bomb은 충돌 판정 대상이 아니다.
        if (mode == BombMode.Weapon || mode == BombMode.Idle)
            return;

        if (!collision.CompareTag("Enemy"))
            return;

        // 수류탄 본체는 적에게 닿거나 최대 거리에 도달하면 한 번만 폭발한다.
        Explode();
    }

    void Explode()  // 초월 단계에 따라 기본 폭발 뒤 파편 또는 화염지대를 추가.
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
    {   // 기본 파편 수에 수류탄 레벨/초월에서 누적된 보너스를 더해 원형으로 배치.        
        int fragmentCount = Mathf.Max(1, baseFragmentCount + fragmentBonusCount);

        for (int i = 0; i < fragmentCount; i++)
        {   // 각 파편은 360도를 동일 간격으로 나눠 바깥 방향으로 발사.
            Vector3 dir = Quaternion.Euler(0, 0, i * (360f / fragmentCount)) * Vector3.up;
            GameObject fragment = GameManager.instance.pool.Get(fragmentPrefabId);
            fragment.transform.position = transform.position + dir * fragmentSpawnOffset;
            fragment.transform.rotation = Quaternion.FromToRotation(Vector3.up, dir);
            fragment.transform.localScale = Vector3.one * fragmentVisualScale;

            SpriteRenderer sourceRenderer = GetComponent<SpriteRenderer>();
            SpriteRenderer fragmentRenderer = fragment.GetComponent<SpriteRenderer>();
            if (fragmentRenderer != null)
            {   // 초월 스프라이트가 있으면 우선 적용하고, 없으면 수류탄 스프라이트를 사용.
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

            // 파편 데미지는 폭발 데미지의 일정 비율이며, 별도 BombFragment.cs가 이동과 반환을 담당.
            fragmentLogic.Init(damage * fragmentDamageRate, fragmentPer + 1, fragmentRange, fragmentHitDelay, fragmentSpeed, dir);
        }
    }

    void SpawnFireZone()
    {   // 2단계 초월 화염지대는 폭발 데미지와 반경을 기준으로 별도 장판 데미지를 계산.
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
            // 프리팹에 컴포넌트를 못 붙였더라도 플레이 중 기능이 빠지지 않도록 보강.
            pooledZone = fireZone.AddComponent<BombFireZone>();
        }

        pooledZone.SetupZone(fireDamage, fireRadius, fireDuration);
    }

    void PlayExplosionFx()
    {
        // 폭발 이펙트도 풀 ID가 연결된 경우에만 꺼내서 재생.
        if (explosionFxPrefabId < 0 || GameManager.instance.pool == null || explosionFxPrefabId >= GameManager.instance.pool.prefabs.Length)
            return;

        GameObject fx = GameManager.instance.pool.Get(explosionFxPrefabId);
        fx.transform.position = transform.position;

        BombExplosionFX pooledFx = fx.GetComponent<BombExplosionFX>();
        if (pooledFx == null)
        {
            // 이펙트 프리팹에 전용 스크립트가 없으면 런타임에 붙여 최소 동작.
            pooledFx = fx.AddComponent<BombExplosionFX>();
        }

        pooledFx.PlayExplosion(explosionRadius);
    }

    void ReturnToPool()
    {
        // 풀로 되돌릴 때 이전 속도가 남아 다음 재사용에 섞이지 않도록 정지.
        if (rigid != null)
        {
            rigid.velocity = Vector2.zero;
        }

        mode = BombMode.Idle;
        gameObject.SetActive(false);
    }
}
