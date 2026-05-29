using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Weapon : MonoBehaviour
{
    // 무기ID, 프리팹ID, 데미지, 개수, 속도 변수 선언
    public int id;
    public int prefabId;
    public float damage;
    public int count;
    public float speed;

    float timer;
    Player player;  // Player의 자식 오브젝트를 편히 불러오기 위한 player변수 선언
    public int masterUpgradeCount = 0; // [추가] 무기 한계돌파(최대 레벨 이후 추가 강화) 횟수 기록

    // [초월 진화용 추가] 무기 진화 단계 스프라이트 및 데이터
    public SpriteRenderer mySpriteRenderer;
    public ItemData originData;

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

            case 4 :    // [추가] 무기ID 4 : 폭탄 던지기 무기 (신규 무기)
                timer += Time.deltaTime;
                if (timer > speed)
                {
                    timer = 0f;
                    FireBomb();
                }
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

    // [수정] 무기 초월 및 이미지 스프라이트 단계적 진화 시스템 탑재
    public void MasterUpgrade(float damageMultiplier, int extraCount)
    {
        masterUpgradeCount++;
        damage += damage * damageMultiplier; // 데미지 증가율 누적
        count += extraCount;                 // 발사 수 증가
        speed *= 0.95f;                      // 쿨타임 5% 단축

        // [진화 이미지 교체] 1차 초월(M1)시 갈퀴/라이플, 2차 초월(M2)시 낫/샷건 등으로 진화
        UpdateEvolutionVisual();

        // 회전형 무기의 경우 즉시 재배치 필요
        if (id == 0)
        {
            Batch();
        }
    }

    // [추가] 초월 레벨에 근거하여 무기와 탄환의 외형 스프라이트를 변경해 주는 함수
    void UpdateEvolutionVisual()
    {
        if (originData == null) return;

        // 초월 차수에 매칭되는 진화 스프라이트가 존재하는지 판별
        // index 0: M1 진화형, index 1: M2 진화형 등 (ItemData의 customEvolutions 배열 참고)
        int evolutionIndex = masterUpgradeCount - 1;

        if (originData.customEvolutions != null && evolutionIndex < originData.customEvolutions.Length)
        {
            Sprite nextSprite = originData.customEvolutions[evolutionIndex];
            if (nextSprite != null)
            {
                // 1. 플레이어가 장착하고 조준하는 Hand 스프라이트 이미지 진화
                Hand hand = player.hands[id == 0 ? 0 : 1];
                if (hand != null && hand.spriter != null)
                {
                    hand.spriter.sprite = nextSprite;
                }

                // 2. 만약 무기 오브젝트 자체에 렌더러가 직접 달린 형태라면 이미지 변경
                if (mySpriteRenderer != null)
                {
                    mySpriteRenderer.sprite = nextSprite;
                }
            }
        }
    }

    public void LevelUp(float damage, int count)
    {
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
            if (data.projectile == GameManager.instance.pool.prefabs[index])    // 프리팹 아이디는 풀링매니저의 변수에서 찾아내 초기화
            {
                prefabId = index;
                break;
            }
        }


        switch (id)  // 무기ID에 따라 로직을 분리할 switch생성
        {   
            case 0 :    // 무기ID 하나씩 case ~ break로 감싸기
                // Player의 근접무기 회전속도를 정하는 구간
                speed = 150 * Character.WeaponSpeed;
                Batch();
                break;

            default :   // 그외 나머지 경우가 있다면 default ~ break으로 감싸기, 이경우에는 원거리무기
            // Player의 원거리무기 연사속도를 정하는 구간
                speed = 0.5f * Character.WeaponRate;   // speed = 연사속도
                break;
        }

        // Hand set
        Hand hand = player.hands[(int)data.itemType];   // enum값 itemType 값 앞에 int타입을 작성해 강제 형변환, 근거리(Melee)=0, 원거리(Range)=1
        hand.spriter.sprite = data.hand;    // 스크립트블 오브젝트의 데이터로 스프라이트 적용
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
            // [초월 비주얼 반영] 사출되는 투사체 이미지도 진화형으로 교체
            int evolutionIndex = masterUpgradeCount - 1;
            if (originData.customEvolutions != null && evolutionIndex >= 0 && evolutionIndex < originData.customEvolutions.Length)
            {
                Sprite projectileSprite = originData.customEvolutions[evolutionIndex];
                SpriteRenderer bRenderer = bullet.GetComponent<SpriteRenderer>();
                if (bRenderer != null && projectileSprite != null)
                {
                    bRenderer.sprite = projectileSprite;
                }
            }
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

        Transform bullet = GameManager.instance.pool.Get(prefabId).transform;
        bullet.position = transform.position;   // 기존 근접무기 생성 로직을 그대로 활용하면서 위치는 Player위치로 지정
        bullet.rotation = Quaternion.FromToRotation(Vector3.up,dir); //Quaternion.FromToRotation(시작방향,목표방향) : 지정된 축을 중심으로 목표를 향해 회전하는 함수
        // bullet의 위쪽(Vector3.up)을 target 방향으로 회전, Vector3.up을 쓰는 이유 : 2D게임에서는 보통 스프라이트의 위쪽 방향이 발사방향
        
        // Bullet.cs 에 플레이어 위치를 전달하여 최대 사거리 제한을 연산하도록 초기화
        bullet.GetComponent<Bullet>().Init(damage, count, dir, transform.position); 
        
        // [초월 비주얼 반영] 사출되는 원거리 총알 이미지도 진화형으로 교체
        int evolutionIndex = masterUpgradeCount - 1;
        if (originData.customEvolutions != null && evolutionIndex >= 0 && evolutionIndex < originData.customEvolutions.Length)
        {
            Sprite projectileSprite = originData.customEvolutions[evolutionIndex];
            SpriteRenderer bRenderer = bullet.GetComponent<SpriteRenderer>();
            if (bRenderer != null && projectileSprite != null)
            {
                bRenderer.sprite = projectileSprite;
            }
        }

        AudioManager.instance.PlaySfx(AudioManager.Sfx.Range); // 원거리 무기 사출시 효과음 재생
    }

    void FireBomb() // [추가] 폭탄 무기 발사 메커니즘
    {
        Vector3 targetPos;
        // 타겟이 있으면 타겟 방향, 없으면 플레이어가 바라보는 앞 방향으로 투척
        if (player.scanner.nearestTarget)
        {
            targetPos = player.scanner.nearestTarget.position;
        }
        else
        {
            targetPos = transform.position + (player.inputVec != Vector2.zero ? (Vector3)player.inputVec.normalized * 3f : Vector3.up * 3f);
        }

        Vector3 dir = targetPos - transform.position;
        float distance = Mathf.Min(dir.magnitude, 5f); // 던지는 최대 거리 제한 (5유닛)
        dir = dir.normalized;

        Transform bomb = GameManager.instance.pool.Get(prefabId).transform;
        bomb.position = transform.position;

        // Bullet.cs 의 Init 시점에 폭발물 플래그가 동작하도록 설정
        Bullet bombBullet = bomb.GetComponent<Bullet>();
        if (bombBullet != null)
        {
            bombBullet.Init(damage, 1, dir, transform.position); // 1번 관통/충돌 시 폭발
            bombBullet.isBomb = true;
            bombBullet.explosionRadius = 2.5f; // 폭발 스플래시 데미지 반경 설정
            bombBullet.maxRange = distance;    // 목표 사거리에 도달하면 즉시 폭발하도록 설정
        }

        AudioManager.instance.PlaySfx(AudioManager.Sfx.Range);
    }
        
}
