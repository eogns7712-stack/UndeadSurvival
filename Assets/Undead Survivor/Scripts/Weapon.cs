using UnityEngine;

/// 근접,원거리 무기의 레벨, 초월, 장비 보정과 공격 실행을 관리하는 스크립트.

public class Weapon : MonoBehaviour
{
    // 무기ID, 프리팹ID, 데미지, 개수 변수 선언
    public int id;
    public int prefabId;
    public float damage;
    public int count;
    public float speed;
    public float minFireCooldown = 0.02f;   // 상점/장갑 보너스가 겹쳐도 발사 간격이 이 값보다 낮아지지 않도록 제한.

    const float MeleeRotationSpeed = 150f;  // 기본 근접무기 회전 속도.
    const float PistolCooldown = 0.5f;  // 기본 총 발사 간격.
    const float RifleCooldown = 0.22f;  // 1단계 초월 라이플 발사 간격.
    const float ShotgunCooldown = 1.35f;    // 2단계 초월 샷건 발사 간격.
    const float MaxGearRate = 0.95f;    // 장갑 보너스로 쿨타임이 0 이하가 되지 않도록 제한할 최대 비율.
    static readonly float[] ShotgunAngles = { -15f, 0f, 15f };  // 샷건 3발 분산 각도.

    float timer;    // 원거리 무기 발사 주기 계산용 타이머.
    float gearRate; // 장갑 장비에서 전달받은 공격속도 보너스.
    Player player;  // Player의 자식 오브젝트를 편히 불러오기 위한 player변수 선언
    public int masterUpgradeCount = 0; // [추가] 무기 초월 횟수 기록

    // [초월 진화용 추가] 무기 진화 단계 스프라이트 및 데이터
    public SpriteRenderer mySpriteRenderer;
    public ItemData originData;

    // masterUpgradeCount의 단순 누적이 아닌, 데이터 배열 길이에 기인한 초월 단계(Stage) 연산기.
    public int CurrentStage
    {
        get
        {
            if (originData == null || originData.damages.Length == 0) return 1;
            if (masterUpgradeCount <= 0) return 0;
            // ex) 배열 크기가 5일 때, 1~5회 강화는 1단계(M1), 6~10회 강화는 2단계(M2)로 정확히 구분
            return 1 + (masterUpgradeCount - 1) / originData.damages.Length;
        }
    }

    void Awake()
    {
        player = GameManager.instance.player;   // 게임매니저에 들어있는 player를 이용해 변수 초기화
    }

    void Update()
    {
        if (!GameManager.instance.isLive)
            return;
        switch (id)  // 무기ID에 따라 로직을 분리할 switch생성
        {
            case 0 :    // 무기ID 하나씩 case ~ break로 감싸기
                transform.Rotate(Vector3.back * speed * Time.deltaTime);   // Vector3.back = (0,0,-1)

                break;

            case 4 :
                break;

            default :   // 그외 나머지 경우가 있다면 default ~ break으로 감싸기
                timer += Time.deltaTime;    // Update에서 deltaTime 계속 더하기 (timer = 게임시간)

                if (timer > speed)
                {
                    timer = 0f;
                    Fire();
                }
                break;
        }
    }

    // [수정] 무기 초월 및 이미지 스프라이트 단계적 진화 시스템
    public void MasterUpgrade(float damageMultiplier, int extraCount)
    {
        masterUpgradeCount++;
        
        if (id == 1)
        {
            // 총은 별도 맞춤 함수를 통해 밸런스를 적용하므로 누적 가산 연산 분리
            UpdateGunTranscendenceBalance(damageMultiplier, extraCount);
        }
        else if (id == 0)
        {
            damage += damage * damageMultiplier;
        }
        else
        {
            damage += damage * damageMultiplier; // 일반 무기는 기존 규칙대로 데미지 증가율 누적
            count += extraCount;                 // 발사 수 증가
            speed *= 0.95f;                      // 쿨타임 5% 단축
        }

        // [진화 이미지 교체] 1차 초월(M1)시 갈퀴/라이플, 2차 초월(M2)시 낫/샷건 등으로 진화
        UpdateEvolutionVisual();

        // 회전형 무기인 경우 즉시 재배치 필요
        if (id == 0)
        {
            Batch();
        }
    }

    // [추가] 총(itemId == 1)의 초월 단계별(M1 라이플, M2 샷건) 전용 밸런스
    // [버그수정]총기 초월 단계별 피해량, 발사 간격과 탄환 수를 현재 강화값에서 재계산
    void UpdateGunTranscendenceBalance(float damageMultiplier, int extraCount)
    {
        if (originData == null || originData.damages.Length == 0)
            return;

        int stage = CurrentStage;
        int stageLevel = ((masterUpgradeCount - 1) % originData.damages.Length) + 1;
        if (stage == 1) // 1단계 초월: 라이플 진화
        {
            // 공격력은 줄어들고 연사속도는 비약적으로 상승
            if (stageLevel == 1)
            {
                damage *= 0.7f;
            }
            else
            {
                damage += damage * damageMultiplier;
                count += extraCount;
            }
            ApplyGear();
        }
        else if (stage >= 2) // 2단계 초월: 샷건 최종 단계
        {
            // 공격력이 대폭 상승하고 연사속도 감소 + 격발 시 3발 분산 구현을 위한 쿨타임 증가
            if (stageLevel == 1)
            {
                damage *= 2.2f;
            }
            else
            {
                damage += damage * damageMultiplier;
            }
            count = 3;  // 발사 수 강제 3발 (산탄 구현용)
            ApplyGear();
        }
    }

    // [추가] 초월 레벨에 근거하여 무기와 탄환의 외형 스프라이트를 변경해 주는 함수
    void UpdateEvolutionVisual()
    {
        if (originData == null) return;

        // 초월 차수에 매칭되는 진화 스프라이트가 존재하는지 판별
        // index 0: M1 진화형, index 1: M2 진화형 등 (ItemData의 customEvolutions 배열 참고)
        int evolutionIndex = CurrentStage - 1;

        if (originData.customEvolutions != null && evolutionIndex >= 0 && evolutionIndex < originData.customEvolutions.Length)
        {
            Sprite nextSprite = originData.customEvolutions[evolutionIndex];
            if (nextSprite != null)
            {
                // 1. 플레이어가 장착하고 조준하는 Hand 스프라이트 이미지 진화
                Hand hand = player.hands[(int)originData.itemType];
                if (hand != null && hand.spriter != null)
                {
                    hand.spriter.sprite = nextSprite;
                    hand.spriter.transform.localScale = Vector3.one;
                }

                // 2. 만약 무기 오브젝트 자체에 렌더러가 직접 달린 형태라면 이미지 변경
                if (mySpriteRenderer != null)
                {
                    mySpriteRenderer.sprite = nextSprite;
                }
            }
        }
    }

    // [추가] 장갑(Glove) 등 기어 업그레이드 수신부 연동
    // BroadcastMessage 수신 시 각 무기군 고유의 연사 속도 기획 비율을 고정
    public void ApplyGear()
    {
        switch (id)
        {
            case 0: // 근접 공전 삽
                speed = MeleeRotationSpeed * Character.WeaponSpeed;
                speed += speed * gearRate;
                Batch();
                break;
            case 1: // 원거리 총
                ApplyFireCooldown(GetGunBaseCooldown());
                break;
            default:
                ApplyFireCooldown(PistolCooldown);
                break;
        }
    }

    float GetGunBaseCooldown()
    {
        int stage = CurrentStage;  // 현재 초월 단계에 따라 총기 기본 쿨타임을 선택.
        if (stage == 1) // M1 라이플
            return RifleCooldown;   // 연사 쿨타임 대폭 감소

        if (stage >= 2) // M2 샷건
            return ShotgunCooldown; // 연사 속도 대폭 감소

        return PistolCooldown; // 기본 권총
    }

    // 장갑과 상점 공속을 반영하되 발사 간격이 최소값 아래로 내려가지 않게 한다.
    void ApplyFireCooldown(float baseCooldown)
    {
        speed = baseCooldown * Character.WeaponRate;
        speed *= 1f - Mathf.Clamp(gearRate, 0f, MaxGearRate);
        speed = Mathf.Max(minFireCooldown, speed);
    }

    public void ApplyGear(float rate)
    {
        gearRate = rate;   // Gear.cs에서 받은 장갑 공속 증가율 저장.
        ApplyGear();   // 저장된 장비 수치를 즉시 무기 속도에 반영.
    }

    public void LevelUp(float damage, int count)
    {
        // 만렙 이전 일반 성장의 연사 속도 세팅
        this.damage = damage;
        this.count += count;

        if (id == 0)    // 속성 변경과 동시에 근접무기의 경우 배치도 필요하니 함수호출
            Batch();

        player.BroadcastMessage("ApplyGear",SendMessageOptions.DontRequireReceiver);   // 나중에 추가된 무기에도 강화된 값을 적용하기 위함
    }

    public void Init(ItemData data) // Weapon 초기화 함수에 스크립트블 오브젝트를 매개변수로 받아 활용 (영상11 33:30)
    {
        // Basic Set
        name = "Weapon" + data.itemId;
        transform.parent = player.transform;
        transform.localPosition = Vector3.zero; // 지역 위치인 localPosition을 원점으로 변경

        // Property set
        id = data.itemId;
        damage = data.baseDamage * Character.Damage;    // Player의 무기 데미지를 정하는 구간
        count = data.baseCount + Character.Count;   // Player의 원거리 관통력과 근접무기 갯수를 정하는 구간
        originData = data; // 초월 진화 데이터 추적용 보관

        for (int index = 0; index < GameManager.instance.pool.prefabs.Length; index++)
        {
            if (data.projectile == GameManager.instance.pool.prefabs[index])    // 프리팹 id는 풀링매니저의 변수에서 찾아내 초기화
            {
                prefabId = index;
                break;
            }
        }

        // 수량 및 관통 초기화 완료 후 수치 반영
        ApplyGear();

        // Hand set
        Hand hand = player.hands[(int)data.itemType];   // enum값 itemType 값 앞에 int타입을 작성해 강제 형변환, 근거리(Melee)=0, 원거리(Range)=1
        hand.spriter.sprite = data.hand;    // 스크립트블 오브젝트의 데이터로 스프라이트 적용
        hand.spriter.transform.localScale = Vector3.one;
        hand.gameObject.SetActive(true);

        player.BroadcastMessage("ApplyGear",SendMessageOptions.DontRequireReceiver); // BroadcastMessage : 특정 함수 호출을 모든 자식에게 방송하는 함수, 나중에 추가된 무기에도 강화된 값을 적용하기 위함
        // BroadcastMessage의 두번째 인자값으로 SendMessageOptions.DontRequireReceiver 추가, 반드시 답변을 할 필요가 없다
    }

    void Batch()    // 근접무기 재배치용 함수
    {
        for (int index = 0; index < count; index++) // for문으로 count만큼 풀링에서 가져오기
        {
            Transform bullet;

            if (index < transform.childCount)   // childCount : 자신의 자식 오브젝트 개수 확인
            {
                bullet = transform.GetChild(index); // index가 아직 childCount 범위 내라면 GetChild함수로 가져오기
            }
            else    // 가져올 오브젝트가 없다면 오브젝트 풀링에서 가져오기 [ 최적화 ]
            {
                bullet = GameManager.instance.pool.Get(prefabId).transform;   // GameObject에서 Transform을 가져오기 때문에 Transform bullet
            }
            bullet.parent = transform;  // parent 속성을 통해 bullet의 부모를 현재 오브젝트로 설정, Weapon이 회전하면 Bullet도 회전하게 하기위함

            bullet.localPosition = Vector3.zero;    // 부모 기준 위치를 (0,0,0)으로 설정
            bullet.localRotation = Quaternion.identity;     // 부모 기준 회전값을 0도로 초기화
            Vector3 rotVec = Vector3.forward * 360 * index / count; //회전 : 순서대로 360을 나누값을 Z축에 적용
            bullet.Rotate(rotVec);
            bullet.Translate(bullet.up * 1.5f, Space.World);  // Translate()함수로 자신의 위쪽으로 이동
            // Space.World의 의미 : 월드좌표 기준으로 이동, 즉 부모 회전에 영향을 받지않아 정확한 위치에 배치

            bullet.GetComponent<Bullet>().Init(damage, -100, Vector3.zero); // -100 is Infinity Per.
            
            // [초월 투사체 분리] 사출되는 투사체 전용 이미지 등록 체크
            ApplyProjectileEvolutionSprite(bullet, true);
        }
    }

    void ApplyProjectileEvolutionSprite(Transform bullet, bool useWeaponSpriteFallback)
    {
        if (originData == null)
            return;

        int evolutionIndex = CurrentStage - 1;
        Sprite projectileSprite = null;

        if (originData.customProjectileEvolutions != null && evolutionIndex >= 0 && evolutionIndex < originData.customProjectileEvolutions.Length)
        {
            projectileSprite = originData.customProjectileEvolutions[evolutionIndex];
        }
        else if (useWeaponSpriteFallback && originData.customEvolutions != null && evolutionIndex >= 0 && evolutionIndex < originData.customEvolutions.Length)
        {
            // 발사체 전용이 없을 시, 무기 스프라이트(Melee)로 대체
            projectileSprite = originData.customEvolutions[evolutionIndex];
        }

        SpriteRenderer bRenderer = bullet.GetComponent<SpriteRenderer>();
        if (bRenderer != null && projectileSprite != null)
        {
            bRenderer.sprite = projectileSprite;
        }
    }

    void Fire() // 총알 사출 구현함수
    {
        if (!player.scanner.nearestTarget)  // 지정한 목표가 없으면 넘어가는 조건의 로직
            return;
            
        // 총알이 나가는 방향 계산
        Vector3 targetPos = player.scanner.nearestTarget.position;  // 타겟(Enemy)의 위치는 Player의 하위오브젝트 scanner가 nearestTarget변수로 들고있음
        Vector3 dir = targetPos - transform.position;   // 크기가 포함된 방향 : 목표위치 - 나의위치
        dir = dir.normalized;   // normalized : 현재 벡터의 방향은 유지하고 크기를 1로 변환한 속성(정규화)

        // masterUpgradeCount 누적수가 아닌, 정밀 검사식 CurrentStage가 2단계 이상일 때에만 샷건 사격을 발사합니다.
        if (id == 1 && CurrentStage >= 2)
        {
            // 중심 조준 방향(dir)을 기준으로 좌우 15도씩 틀어 총 3발 발사
            for (int i = 0; i < ShotgunAngles.Length; i++)
            {
                // 각도만큼 방향 벡터 회전 연산
                Vector3 rotDir = Quaternion.AngleAxis(ShotgunAngles[i], Vector3.forward) * dir;

            Transform bullet = GameManager.instance.pool.Get(prefabId).transform;  // 샷건 탄환을 오브젝트 풀에서 가져오기.
            bullet.position = transform.position;
                bullet.rotation = Quaternion.FromToRotation(Vector3.up, rotDir);

                Bullet bulletComponent = bullet.GetComponent<Bullet>();
                if (bulletComponent == null)
                {
                    bullet.gameObject.SetActive(false);
                    continue;
                }

                bulletComponent.Init(damage, 1, rotDir, transform.position); // 관통력 1 보정

                // 탄환 이미지 진화형 샷건 스프라이트로 치환 적용
                ApplyProjectileEvolutionSprite(bullet, false);
            }
        }
        else
        {   // 기본 총 및 1단계 초월(라이플) 격발 로직.
            Transform bullet = GameManager.instance.pool.Get(prefabId).transform;  // 기본 총/라이플 탄환을 오브젝트 풀에서 가져오기.
            bullet.position = transform.position;   // 기존 근접무기 생성 로직을 그대로 활용하면서 위치는 Player위치로 지정
            bullet.rotation = Quaternion.FromToRotation(Vector3.up, dir); // Quaternion.FromToRotation(시작방향, 목표방향) : 지정된 축을 중심으로 목표를 향해 회전하는 함수
            
            // Bullet.cs 에 플레이어 위치를 전달하여 최대 사거리 제한을 연산하도록 초기화
            Bullet bulletComponent = bullet.GetComponent<Bullet>();
            if (bulletComponent == null)
            {
                bullet.gameObject.SetActive(false);
                return;
            }

            bulletComponent.Init(damage, count, dir, transform.position); 

            // [초월 비주얼 반영] 사출되는 원거리 총알 이미지도 진화형으로 교체
            ApplyProjectileEvolutionSprite(bullet, false);
        }

        AudioManager.instance.PlaySfx(AudioManager.Sfx.Range); // 원거리 무기 사출시 효과음 재생
    }
}
