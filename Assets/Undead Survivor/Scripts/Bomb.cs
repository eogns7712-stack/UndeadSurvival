using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
    public float damage;
    public int masterUpgradeCount;
    public ItemData originData;

    [Header("# Projectile")]
    public float maxRange = 5f;
    public float explosionRadius = 2.5f;
    public int bombStage;
    public GameObject fireZonePrefab;

    [HideInInspector] public Sprite shardSprite;

    const float DefaultCooldown = 1.8f;
    const float MaxThrowRange = 3.2f;
    const float ThrowSpeed = 7.5f;
    const float FragmentSpeed = 15f;
    const float FragmentRange = 4.5f;
    const float FragmentDamageRate = 0.5f;
    const float FragmentSpawnOffset = 0.8f;
    const float FragmentHitDelay = 0.08f;
    const float FragmentVisualScale = 0.45f;
    const int FragmentSortingOrder = 20;
    const int BaseFragmentCount = 8;
    const int FragmentPer = 1;

    BombMode mode = BombMode.Idle;
    Player player;
    Rigidbody2D rigid;

    float baseCooldown = DefaultCooldown;
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

    void UpdateWeapon()
    {
        timer += Time.deltaTime;
        if (timer <= cooldown)
            return;

        timer = 0f;
        ThrowGrenade();
    }

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

        baseCooldown = DefaultCooldown;
        if (data.speeds != null && data.speeds.Length > 0)
        {
            baseCooldown = data.speeds[0];
        }

        prefabId = FindPrefabId(data.projectile);
        ApplyGear();

        player.BroadcastMessage("ApplyGear", SendMessageOptions.DontRequireReceiver);
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
        hitDelayTimer = mode == BombMode.Fragment ? FragmentHitDelay : 0f;

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
            float moveSpeed = mode == BombMode.Grenade ? ThrowSpeed : FragmentSpeed;
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

        float throwRange = Mathf.Min(dir.magnitude, MaxThrowRange);
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
        int fragmentCount = Mathf.Max(1, BaseFragmentCount + fragmentBonusCount);

        for (int i = 0; i < fragmentCount; i++)
        {
            Vector3 dir = Quaternion.Euler(0, 0, i * (360f / fragmentCount)) * Vector3.up;
            GameObject fragment = new GameObject("BombFragment");
            fragment.transform.position = transform.position + dir * FragmentSpawnOffset;
            fragment.transform.rotation = Quaternion.FromToRotation(Vector3.up, dir);
            fragment.transform.localScale = Vector3.one * FragmentVisualScale;

            SpriteRenderer sourceRenderer = GetComponent<SpriteRenderer>();
            SpriteRenderer fragmentRenderer = fragment.AddComponent<SpriteRenderer>();
            fragmentRenderer.sprite = shardSprite != null ? shardSprite : (sourceRenderer != null ? sourceRenderer.sprite : null);
            fragmentRenderer.sortingOrder = FragmentSortingOrder;
            fragmentRenderer.color = Color.white;

            Rigidbody2D fragmentRigid = fragment.AddComponent<Rigidbody2D>();
            fragmentRigid.gravityScale = 0f;
            fragmentRigid.velocity = dir.normalized * FragmentSpeed;

            CircleCollider2D fragmentCollider = fragment.AddComponent<CircleCollider2D>();
            fragmentCollider.isTrigger = true;
            fragmentCollider.radius = 0.18f;

            BombFragment fragmentLogic = fragment.AddComponent<BombFragment>();
            fragmentLogic.Init(damage * FragmentDamageRate, FragmentPer + 1, FragmentRange, FragmentHitDelay);
        }
    }

    void SpawnFireZone()
    {
        if (fireZonePrefab != null)
        {
            GameObject fireZone = Instantiate(fireZonePrefab, transform.position, Quaternion.identity);
            Destroy(fireZone, 3f);
            return;
        }

        GameObject tempFireZone = new GameObject("TemporaryFireZone");
        tempFireZone.transform.position = transform.position;
        TemporaryFireZone zoneComp = tempFireZone.AddComponent<TemporaryFireZone>();
        zoneComp.SetupZone(damage * 0.25f, explosionRadius * 0.85f, 3f);
    }

    void PlayExplosionFx()
    {
        GameObject fxObject = new GameObject("TemporaryExplosionVFX");
        fxObject.transform.position = transform.position;
        TemporaryExplosionFX fxComp = fxObject.AddComponent<TemporaryExplosionFX>();
        fxComp.PlayExplosion(explosionRadius);
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

public class TemporaryExplosionFX : MonoBehaviour
{
    SpriteRenderer sr;
    float maxDuration = 0.22f;
    float elapsed = 0f;
    Vector3 maxScale;

    public void PlayExplosion(float radius)
    {
        sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sprite = CreateCircleSprite();
        sr.sortingOrder = 10;

        transform.localScale = Vector3.one * 0.1f;
        maxScale = Vector3.one * radius * 1.8f;
    }

    Sprite CreateCircleSprite()
    {
        int size = 128;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float center = size / 2f;
        float radius = size / 2f - 4f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                if (dist <= radius)
                {
                    float alpha = Mathf.Clamp01((radius - dist) / 4.0f);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
                else
                {
                    texture.SetPixel(x, y, Color.clear);
                }
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    void Update()
    {
        elapsed += Time.deltaTime;
        float t = elapsed / maxDuration;

        transform.localScale = Vector3.Lerp(Vector3.one * 0.1f, maxScale, Mathf.Sin(t * Mathf.PI * 0.5f));

        if (sr != null)
        {
            float alpha = Mathf.Lerp(0.85f, 0f, t);
            sr.color = new Color(1f, Mathf.Lerp(0.45f, 0.15f, t), 0.05f, alpha);
        }

        if (elapsed >= maxDuration)
        {
            Destroy(gameObject);
        }
    }
}

public class BombFragment : MonoBehaviour
{
    float damage;
    int remainingHits;
    float maxRange;
    float hitDelayTimer;
    Vector3 startPosition;
    Rigidbody2D rigid;

    public void Init(float damage, int remainingHits, float maxRange, float hitDelay)
    {
        this.damage = damage;
        this.remainingHits = remainingHits;
        this.maxRange = maxRange;
        hitDelayTimer = hitDelay;
        startPosition = transform.position;
        rigid = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        if (!GameManager.instance.isLive)
            return;

        if (hitDelayTimer > 0f)
        {
            hitDelayTimer -= Time.deltaTime;
        }

        if (Vector3.Distance(startPosition, transform.position) >= maxRange)
        {
            Destroy(gameObject);
        }
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (hitDelayTimer > 0f)
            return;

        if (!collision.CompareTag("Enemy"))
            return;

        Enemy enemy = collision.GetComponent<Enemy>();
        if (enemy != null)
        {
            enemy.TakeDamage(damage);
        }

        remainingHits--;
        if (remainingHits <= 0)
        {
            if (rigid != null)
            {
                rigid.velocity = Vector2.zero;
            }
            Destroy(gameObject);
        }
    }
}

public class TemporaryFireZone : MonoBehaviour
{
    SpriteRenderer sr;
    float damage;
    float radius;
    float duration;
    float timer = 0f;
    float tickTimer = 0f;
    float tickRate = 0.5f;

    public void SetupZone(float damage, float radius, float duration)
    {
        this.damage = damage;
        this.radius = radius;
        this.duration = duration;

        sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sprite = CreateFireZoneSprite();
        sr.sortingOrder = 4;

        transform.localScale = Vector3.one * radius * 2f;
    }

    Sprite CreateFireZoneSprite()
    {
        int size = 128;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float center = size / 2f;
        float r = size / 2f - 4f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                if (dist <= r)
                {
                    float alpha = Mathf.Clamp01((r - dist) / 12.0f) * 0.5f;
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
                else
                {
                    texture.SetPixel(x, y, Color.clear);
                }
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= duration)
        {
            Destroy(gameObject);
            return;
        }

        float beat = Mathf.PingPong(timer * 5f, 1f);
        float scaleMultiplier = 1f + beat * 0.1f;
        transform.localScale = Vector3.one * radius * 2f * scaleMultiplier;

        if (sr != null)
        {
            sr.color = Color.Lerp(new Color(1f, 0.82f, 0f, 0.45f), new Color(1f, 0.35f, 0f, 0.3f), beat);
        }

        tickTimer += Time.deltaTime;
        if (tickTimer < tickRate)
            return;

        tickTimer = 0f;
        Collider2D[] targets = Physics2D.OverlapCircleAll(transform.position, radius, LayerMask.GetMask("Enemy"));
        foreach (Collider2D target in targets)
        {
            Enemy enemy = target.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
            }
        }
    }
}
