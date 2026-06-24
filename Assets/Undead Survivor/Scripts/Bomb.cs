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
    Player player; // 장착 무기 상태에서 스캐너와 입력 방향을 읽기 위한 Player 참조.
    Rigidbody2D rigid; // 풀에서 꺼낸 수류탄 투사체를 이동시키는 Rigidbody2D.

    // 장착 무기 상태에서 사용하는 발사 쿨타임과 투척 상태를 보관.
    float baseCooldown;    // 수류탄 자체 레벨에 따른 기본 발사 간격.
    float gloveRate;   // 장갑 장비에서 전달받은 공격속도 보너스.
    float cooldown;    // 기본 쿨타임에 캐릭터/상점/장갑 보정을 모두 적용한 최종 발사 간격.
    float timer;   // 장착 무기 상태에서 다음 수류탄 투척까지 시간을 재는 타이머.
    int fragmentBonusCount; // 수류탄 레벨업/초월로 누적된 파편 개수 보너스.
    bool hasExploded;  // 같은 수류탄이 충돌과 거리 도달로 중복 폭발하지 않게 막는 값.
    Vector3 startPosition; // 투척된 수류탄이 출발한 위치. 최대 사거리 계산에 사용.

    public float Cooldown
    {
        get { return cooldown; }   // 인스펙터나 다른 UI에서 현재 수류탄 발사 간격을 확인할 때 사용.
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
        hasExploded = false;   // 풀에서 다시 꺼낼 때 이전 폭발 상태 초기화.
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
        timer += Time.deltaTime;   // 장착 무기 상태에서만 투척 쿨타임 누적.
        if (timer <= cooldown) // 아직 쿨타임이 남아 있으면 투척하지 않음.
            return;

        timer = 0f;    // 투척 후 다음 쿨타임 계산을 위해 초기화.
        ThrowGrenade();    // 실제 수류탄 투척.
    }

    void UpdateProjectile() // 투척된 수류탄을 목표 지점까지 이동시키고 도착하면 폭발.
    {
        CacheReferences();

        if (rigid == null)
            return;

        float travelDistance = Vector3.Distance(startPosition, transform.position); // 출발 지점부터 현재 위치까지 이동 거리 계산.
        if (travelDistance < maxRange) // 아직 투척 가능 거리 안이면 계속 비행.
            return;

        Explode();  // 최대 사거리에 도달하면 충돌하지 않아도 폭발.
    }

    public void Init(ItemData data)
    {
        CacheReferences();

        // 플레이어가 수류탄 아이템을 처음 얻었을 때, 이 오브젝트는 발사 담당 무기로 동작.
        mode = BombMode.Weapon;
        hasExploded = false;
        timer = 0f;

        name = "Bomb" + data.itemId;   // 하이어라키에서 구분하기 쉽도록 이름 지정.
        transform.parent = player.transform;   // 장착 무기는 Player 하위 오브젝트로 배치.
        transform.localPosition = Vector3.zero;    // Player 중심에 위치 초기화.

        damage = data.baseDamage * Character.Damage;   // 기본 데미지에 캐릭터/상점 공격력 보정 적용.
        fragmentBonusCount = data.baseCount + Character.Count; // 기본 파편 보너스에 캐릭터 개수 보정 적용.
        originData = data; // 이후 레벨업/초월 계산과 파편 스프라이트 참조를 위해 원본 데이터 저장.

        // ItemData에 연결된 projectile 프리팹을 기준으로 풀 ID와 밸런스 값을 가져온다.
        prefabId = FindPrefabId(data.projectile);
        CopyProjectileSettings(data.projectile);

        baseCooldown = defaultCooldown; // ItemData에 속도 데이터가 없을 경우 사용할 기본 쿨타임.
        if (data.speeds != null && data.speeds.Length > 0) // ItemData에 수류탄 전용 발사 간격이 있으면 우선 사용.
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

        fragmentPrefabId = projectileBomb.fragmentPrefabId;    // 1단계 초월 파편 프리팹 풀 ID 복사.
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
        masterUpgradeCount++;  // 초월 누적 횟수 증가. CurrentStage 계산의 기준.

        damage += damage * damageMultiplier;   // 현재 데미지를 기준으로 초월 데미지 증가율 곱 적용.
        fragmentBonusCount += extraCount;  // 초월로 추가되는 파편 개수 보너스 누적.

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

        return 0;   // 매칭되는 프리팹을 찾지 못하면 기본 0번으로 반환. 인스펙터 연결 오류 방지용 fallback.
    }

    // 풀에서 재사용된 수류탄의 상태를 매번 새 값으로 갱신(수류탄이 레벨업 되었을 수 있기때문)
    void InitProjectile(float damage, Vector3 dir, Vector3 startPos, int stage, int prefabId, Sprite fragmentSprite)
    {
        CacheReferences();

        mode = BombMode.Grenade;   // 이 오브젝트는 이제 장착 무기가 아니라 날아가는 수류탄으로 동작.
        originData = null; // 투사체는 원본 ItemData를 직접 들고 있을 필요가 없으므로 비움.
        hasExploded = false;   // 재사용된 투사체의 이전 폭발 상태 초기화.

        this.damage = damage;  // 이번 투척 수류탄의 데미지 적용.
        this.prefabId = prefabId;  // 풀 반환/참조용 프리팹 ID 저장.
        bombStage = stage; // 현재 초월 단계 저장. 1단계 파편, 2단계 화염지대 분기에 사용.
        shardSprite = fragmentSprite;  // 1단계 초월 파편에 적용할 스프라이트 전달.

        startPosition = startPos == default ? transform.position : startPos;   // 최대 사거리 계산용 시작 위치 저장.

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.enabled = true;  // 풀에서 재사용될 때 꺼져 있을 수 있는 렌더러 활성화.
        }

        if (rigid != null)
        {
            rigid.velocity = dir.normalized * throwSpeed;  // 투척 방향으로 수류탄 이동 시작.
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

        Vector3 targetPos = GetTargetPosition();   // 스캐너 목표 또는 입력 방향 기반 투척 목표 위치.
        Vector3 dir = targetPos - transform.position;   // 현재 위치에서 목표 위치로 향하는 방향.
        if (dir.sqrMagnitude <= 0.0001f)
        {   // 플레이어와 목표가 같은 위치일 때 방향 벡터가 0이 되는 상황을 막는다.
            dir = Vector3.up;
        }

        // 실제 투척 거리는 적이 멀리 있어도 최대 투척 거리 안으로 제한한다.
        float throwRange = Mathf.Min(dir.magnitude, maxThrowRange);
        dir = dir.normalized;

        Transform grenade = GameManager.instance.pool.Get(prefabId).transform; // 수류탄 투사체를 풀에서 꺼내기.
        grenade.position = transform.position; // 플레이어 위치에서 투척 시작.
        grenade.rotation = Quaternion.FromToRotation(Vector3.up, dir); // 수류탄 스프라이트가 날아가는 방향을 바라보게 회전.

        Bomb grenadeBomb = grenade.GetComponent<Bomb>();
        if (grenadeBomb == null)
        {   // 풀 ID가 잘못 연결된 경우 게임이 멈추지 않도록 꺼낸 오브젝트를 바로 되돌림(오류방지)
            grenade.gameObject.SetActive(false);
            return;
        }

        grenadeBomb.maxRange = throwRange; // 이번 투척의 실제 도달 거리 적용.
        grenadeBomb.fragmentPrefabId = fragmentPrefabId;   // 장착 무기에서 복사해둔 파편 풀 ID 전달.
        grenadeBomb.explosionFxPrefabId = explosionFxPrefabId; // 폭발 이펙트 풀 ID 전달.
        grenadeBomb.fireZonePrefabId = fireZonePrefabId;   // 화염지대 풀 ID 전달.
        grenadeBomb.fragmentBonusCount = fragmentBonusCount;   // 현재 레벨/초월로 누적된 파편 수 전달.
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

        if (!collision.CompareTag("Enemy")) // Enemy가 아닌 오브젝트와 닿으면 폭발하지 않음.
            return;

        // 수류탄 본체는 적에게 닿거나 최대 거리에 도달하면 한 번만 폭발한다.
        Explode();
    }

    void Explode()  // 초월 단계에 따라 기본 폭발 뒤 파편 또는 화염지대를 추가.
    {
        if (hasExploded)   // 충돌과 사거리 도달이 같은 프레임에 겹쳐도 한 번만 폭발.
            return;

        hasExploded = true;    // 이후 중복 폭발 차단.

        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(transform.position, explosionRadius, LayerMask.GetMask("Enemy")); // 폭발 반경 안의 적 검색.
        foreach (Collider2D enemyCollider in hitEnemies)
        {
            Enemy enemy = enemyCollider.GetComponent<Enemy>(); // Enemy 컴포넌트가 있는 대상에게만 데미지 적용.
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
            }
        }

        PlayExplosionFx(); // 폭발 VFX 재생.

        if (bombStage >= 1) // 1단계 이상 초월 수류탄은 파편 생성.
        {
            SpawnFragments();
        }

        if (bombStage >= 2) // 2단계 이상 초월 수류탄은 화염지대 추가 생성.
        {
            SpawnFireZone();
        }

        AudioManager.instance.PlaySfx(AudioManager.Sfx.Dead); // 폭발 사운드 재생.
        ReturnToPool(); // 폭발이 끝난 수류탄 본체는 풀로 반환.
    }

    void SpawnFragments()
    {   // 기본 파편 수에 수류탄 레벨/초월에서 누적된 보너스를 더해 원형으로 배치.        
        int fragmentCount = Mathf.Max(1, baseFragmentCount + fragmentBonusCount);

        for (int i = 0; i < fragmentCount; i++)
        {   // 각 파편은 360도를 동일 간격으로 나눠 바깥 방향으로 발사.
            Vector3 dir = Quaternion.Euler(0, 0, i * (360f / fragmentCount)) * Vector3.up;
            GameObject fragment = GameManager.instance.pool.Get(fragmentPrefabId); // 파편 오브젝트를 풀에서 꺼내기.
            fragment.transform.position = transform.position + dir * fragmentSpawnOffset;  // 폭발 중심에서 살짝 떨어진 위치에 생성.
            fragment.transform.rotation = Quaternion.FromToRotation(Vector3.up, dir);   // 파편이 날아갈 방향으로 회전.
            fragment.transform.localScale = Vector3.one * fragmentVisualScale;  // 파편 시각 크기 적용.

            SpriteRenderer sourceRenderer = GetComponent<SpriteRenderer>(); // 파편 스프라이트 fallback용 수류탄 본체 렌더러.
            SpriteRenderer fragmentRenderer = fragment.GetComponent<SpriteRenderer>(); // 파편 렌더러.
            if (fragmentRenderer != null)
            {   // 초월 스프라이트가 있으면 우선 적용하고, 없으면 수류탄 스프라이트를 사용.
                fragmentRenderer.sprite = shardSprite != null ? shardSprite : (sourceRenderer != null ? sourceRenderer.sprite : null);
                fragmentRenderer.sortingOrder = fragmentSortingOrder;
                fragmentRenderer.color = Color.white;
                fragmentRenderer.enabled = true;
            }

            BombFragment fragmentLogic = fragment.GetComponent<BombFragment>(); // 파편 이동/충돌을 담당하는 스크립트 가져오기.
            if (fragmentLogic == null)  // 프리팹에 스크립트가 빠졌더라도 기능이 작동하도록 런타임에 보강.
            {
                fragmentLogic = fragment.AddComponent<BombFragment>();
            }

            // 파편 데미지는 폭발 데미지의 일정 비율이며, 별도 BombFragment.cs가 이동과 반환을 담당.
            fragmentLogic.Init(damage * fragmentDamageRate, fragmentPer + 1, fragmentRange, fragmentHitDelay, fragmentSpeed, dir);
        }
    }

    void SpawnFireZone()
    {   // 2단계 초월 화염지대는 폭발 데미지와 반경을 기준으로 별도 장판 데미지를 계산.
        float fireDamage = damage * fireDamageRate;    // 화염지대 tick 데미지.
        float fireRadius = explosionRadius * fireRadiusRate;   // 폭발 반경을 기준으로 화염지대 반경 계산.

        if (fireZonePrefabId < 0 || GameManager.instance.pool == null || fireZonePrefabId >= GameManager.instance.pool.prefabs.Length) // 풀 ID가 잘못되어 있으면 생성 중단.
            return;

        GameObject fireZone = GameManager.instance.pool.Get(fireZonePrefabId); // 화염지대 오브젝트를 풀에서 꺼내기.
        fireZone.transform.position = transform.position;  // 폭발 위치에 화염지대 배치.
        fireZone.transform.rotation = Quaternion.identity;  // 장판은 회전이 필요 없으므로 기본 회전값 적용.

        BombFireZone pooledZone = fireZone.GetComponent<BombFireZone>();
        if (pooledZone == null)
        {
            // 프리팹에 컴포넌트를 못 붙였더라도 플레이 중 기능이 빠지지 않도록 보강.
            pooledZone = fireZone.AddComponent<BombFireZone>();
        }

        pooledZone.SetupZone(fireDamage, fireRadius, fireDuration); // 데미지, 범위, 지속시간 전달 후 장판 시작.
    }

    void PlayExplosionFx()
    {
        // 폭발 이펙트도 풀 ID가 연결된 경우에만 꺼내서 재생.
        if (explosionFxPrefabId < 0 || GameManager.instance.pool == null || explosionFxPrefabId >= GameManager.instance.pool.prefabs.Length)
            return;

        GameObject fx = GameManager.instance.pool.Get(explosionFxPrefabId); // 폭발 VFX 오브젝트를 풀에서 꺼내기.
        fx.transform.position = transform.position; // 현재 수류탄 위치에 VFX 배치.

        BombExplosionFX pooledFx = fx.GetComponent<BombExplosionFX>(); // 폭발 VFX 전용 스크립트 가져오기.
        if (pooledFx == null)
        {
            // 이펙트 프리팹에 전용 스크립트가 없으면 런타임에 붙여 최소 동작.
            pooledFx = fx.AddComponent<BombExplosionFX>();
        }

        pooledFx.PlayExplosion(explosionRadius); // 폭발 반경을 기준으로 VFX 크기 재생.
    }

    void ReturnToPool()
    {
        // 풀로 되돌릴 때 이전 속도가 남아 다음 재사용에 섞이지 않도록 정지.
        if (rigid != null)
        {
            rigid.velocity = Vector2.zero;
        }

        mode = BombMode.Idle;  // 다음 재사용 전까지 아무 동작도 하지 않는 상태로 변경.
        gameObject.SetActive(false);   // 오브젝트 풀로 반환.
    }
}
