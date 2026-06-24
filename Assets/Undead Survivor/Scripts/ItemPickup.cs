using System.Collections;
using UnityEngine;

/// 경험치, 코인, 회복, 자석 및 랜덤박스 보상의 이동과 획득을 처리하는 스크립트.

public class ItemPickup : MonoBehaviour
{
    // EXPGem, 랜덤박스, 동전(경험치 다량), 힐팩(10% 체력 회복), 자석
    public enum PickupType { Exp, RandomBox, Coin, HealPack, Magnet }
    public PickupType type; // 현재 픽업 오브젝트가 어떤 보상인지 저장.

    public float expValue = 1f; // 경험치 보석이 지급할 경험치 양.
    public float magnetSpeed = 8f;  // 플레이어에게 빨려 들어가는 이동속도.
    public float magnetDistance = 3.5f; // 자석 흡입 적용 거리
    public float autoCollectDistance = 0.2f;    // 충돌 판정이 빗나가도 자동 획득되는 최소 거리.
    public int coinExpValue = 10;   // 금색 코인이 지급할 경험치 양.
    public float healRate = 0.1f;   // 힐팩이 회복할 최대 체력 비율.
    public int boxItemPrefabId = 5; // 랜덤박스 보상으로 재사용할 풀 인덱스.
    public float boxItemBounceMinDistance = 1.2f; // 랜덤박스 보상 튕김 최소 거리.
    public float boxItemBounceMaxDistance = 1.8f; // 랜덤박스 보상 튕김 최대 거리.
    public float boxItemBounceDuration = 0.6f;    // 랜덤박스 보상 튕김 시간.
    public float postBounceCollectDelay = 0.3f;   // 튕김이 끝난 뒤 바로 먹히지 않게 하는 추가 대기 시간.
    public float bounceHeight = 1.5f; // 튕김 포물선의 최고 높이.

    // 상자에서 나올 수 있는 아이템들의 스프라이트
    [Header("# Box Item Visuals")]
    public Sprite coinSprite;
    public Sprite healPackSprite;
    public Sprite magnetSprite;

    Transform playerTransform; // 아이템이 따라갈 플레이어 Transform.
    bool isBeingAttracted = false;  // 자석 범위에 들어와 플레이어에게 끌리는 중인지 확인.

    // [버그 방지] 오브젝트 풀 재사용 시 원본 이미지를 유지하기 위한 캐싱 변수
    Sprite originalSprite;
    SpriteRenderer spr;

    // [비주얼 개선] 아이템 획득 유예 타이머 (상자 오프닝 연출용)
    float collectCooldown = 0f;

    void Awake()
    {
        spr = GetComponent<SpriteRenderer>();
        if (spr != null)
        {
            originalSprite = spr.sprite; // EXPGem 이미지 보관
        }
    }

    void OnEnable()
    {
        isBeingAttracted = false;
        collectCooldown = 0f; // 오브젝트 활성화 시 쿨다운 리셋
        if (GameManager.instance != null && GameManager.instance.player != null)
        {
            playerTransform = GameManager.instance.player.transform;
        }
    }

    public bool CanCollect()
    {
        return collectCooldown <= 0f;   // 튕김 연출 보호 시간이 끝났을 때만 수집 가능.
    }

    // 풀에서 꺼낸 픽업 오브젝트의 타입, 값, 스프라이트를 초기화하는 함수.
    public void InitPickup(PickupType pickupType, float value)
    {
        this.type = pickupType; // 이번에 사용할 보상 타입 저장.
        if (pickupType == PickupType.Exp)
        {
            this.expValue = value;  // 경험치 보석의 지급량 저장.
            
            // [버그 수정] 일반 몹 드롭 보석으로 활성화될 때 원본 보석 이미지로 리셋
            if (spr != null && originalSprite != null)
            {
                spr.sprite = originalSprite;
            }
        }
        
        // 상자에서 나온 특수 보상일 때만 지정된 전용 이미지로 실시간 가로채기 변경
        if (spr != null)
        {
            if (type == PickupType.Coin && coinSprite != null) spr.sprite = coinSprite;
            else if (type == PickupType.HealPack && healPackSprite != null) spr.sprite = healPackSprite;
            else if (type == PickupType.Magnet && magnetSprite != null) spr.sprite = magnetSprite;
        }
    }

    void Update()
    {
        if (playerTransform == null || !GameManager.instance.isLive)   // 플레이어가 없거나 게임이 멈췄으면 이동 처리 중단.
            return;

        // [추가] 획득 및 끌림 쿨다운이 돌고 있다면 대기 (포물선 튕김 연출 보호용)
        if (collectCooldown > 0f)
        {
            collectCooldown -= Time.deltaTime;
            return;
        }

        // 상자(Box)의 경우 애니메이션을 보며 제자리에서 먹어야 하므로 자석 당김 연산 제외
        if (type == PickupType.RandomBox)
            return;

        float distance = Vector3.Distance(transform.position, playerTransform.position);    // 플레이어와 현재 픽업 사이 거리.
        float pickupDistance = magnetDistance + GameManager.instance.ShopPickupRangeBonus;  // 기본 흡수 거리 + 상점 보너스.

        // 플레이어에 가까이 다가가면 끌림 플래그 가동
        if (!isBeingAttracted && distance <= pickupDistance)
        {
            isBeingAttracted = true;
        }

        if (isBeingAttracted)
            {
            // 부드러운 자석 흡입 이동
            transform.position = Vector3.MoveTowards(transform.position, playerTransform.position, magnetSpeed * Time.deltaTime);

            // [안전장치 추가] 자성 상태에서 플레이어와의 거리가 극도로 가까워지면 충돌 판정 유실 없이 자동 즉시 획득
            if (distance < autoCollectDistance)    // 충분히 가까워졌으면 충돌 여부와 상관없이 보상 획득 처리.
            {
                CollectReward();
            }
        }
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {   // [추가] 공중에서 튕기는 연출 중에는 플레이어와 닿아도 수집하지 않음
            if (collectCooldown > 0f)
                return;

            if (type != PickupType.RandomBox)   // 상자를 제외한 일반 픽업류는 즉시 획득
            {
                CollectReward();
            }
        }
    }

    public void CollectReward() // 보상 종류에 따라 즉시 적용하거나 랜덤박스 보상을 생성.
    {
        switch (type)
        {
            case PickupType.Exp:
                // [버그 예방] GameManager 직접 주입 대신 플레이어 Exp Buffer에 우회 적립.
                if (GameManager.instance.player != null)
                {
                    GameManager.instance.player.pendingExp += (int)expValue;
                }
                break;

            case PickupType.Coin:
                // [버그 예방] 금색 코인 대량 경험치도 플레이어 Exp Buffer에 안전 우회하여 경험치 유실 방지.
                if (GameManager.instance.player != null)
                {
                    GameManager.instance.player.pendingExp += coinExpValue;
                }
                break;

            case PickupType.HealPack:
                // 힐팩: 플레이어 체력 10% 즉시 보충
                GameManager.instance.health = Mathf.Min(GameManager.instance.health + (GameManager.instance.maxHealth * healRate), GameManager.instance.maxHealth);
                break;

            case PickupType.Magnet:
                // 자석: 필드 상의 모든 보석들을 플레이어 방향으로 자력 끌림 가동!
                ActivateFullMagnet();
                break;
        }

        AudioManager.instance.PlaySfx(AudioManager.Sfx.Select); // 보상 획득 효과음 재생.
        gameObject.SetActive(false); // 수집 완료 후 풀 복귀
    }

    // BoxOpen.cs 애니메이션 완료 지점에서 호출되는 안전 보상 연결 통로
    public void CollectRewardDirectly()
    {
        if (type == PickupType.RandomBox)
        {
            SpawnBoxItemReward();
        }
    }

    // [수정] 상자 오픈 시 지정된 확률로 3종의 추가 물리 아이템(코인, 힐팩, 자석) 중 하나를 필드에 드롭
    void SpawnBoxItemReward()
    {
        // 상자를 획득했으므로, 필드의 보석 프리팹 풀을 재사용하여 코인/힐팩/자석 인스턴스를 바닥에 떨구기
        GameObject boxItem = GameManager.instance.pool.Get(boxItemPrefabId); // 보석 풀 재활용 (오브젝트 풀링)
        if (boxItem != null)
        {
            boxItem.transform.position = transform.position;
            ItemPickup pickup = boxItem.GetComponent<ItemPickup>();
            if (pickup != null)
            {
                // 랜덤 3종 보너스 타입 결정: Coin (2), HealPack (3), Magnet (4)
                int luckyRoll = Random.Range(2, 5); 
                
                // 에디터 상에 설정해놓은 Sprite 연결을 유지하기 위해 해당 정보들을 넘겨주며 초기화합니다.
                pickup.coinSprite = this.coinSprite;
                pickup.healPackSprite = this.healPackSprite;
                pickup.magnetSprite = this.magnetSprite;

                pickup.InitPickup((PickupType)luckyRoll, 0);

                // 소환 시 사방 360도 무작위 방향으로 튕겨 나가는 포물선 연출
                Vector3 randomOffset = Quaternion.Euler(0, 0, Random.Range(0f, 360f)) * Vector3.up * Random.Range(boxItemBounceMinDistance, boxItemBounceMaxDistance);
                pickup.StartBounce(transform.position, transform.position + randomOffset, boxItemBounceDuration);
            }
        }

        AudioManager.instance.PlaySfx(AudioManager.Sfx.LevelUp);
    }

    // [추가] RandomBox Open 시 포물선 튕기기 연출
    public void StartBounce(Vector3 start, Vector3 end, float duration)
    {
        collectCooldown = duration + postBounceCollectDelay; // 튕기는 시간(0.6초) + 땅에 안착 후 여유 대기시간(0.3초) 동안 무적 보호막
        StartCoroutine(BounceRoutine(start, end, duration)); // 포물선 이동 코루틴 시작.
    }

    // 드롭 아이템을 포물선으로 튕겨낸 뒤 일정 시간 후 획득 가능하게 만듦.
    IEnumerator BounceRoutine(Vector3 start, Vector3 end, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // X, Y축 선형 보간 이동
            Vector3 currentPos = Vector3.Lerp(start, end, t);

            // 포물선을 그리기 위한 삼각함수 Sin 연산 적용
            float height = Mathf.Sin(t * Mathf.PI) * bounceHeight; // 높이 가중치 설정
            currentPos.y += height;

            transform.position = currentPos;
            yield return null;
        }
        transform.position = end; // 연출 완료 후 목푯값에 정확히 수렴 고정
    }

    // 전 맵의 경험치를 끌어오는 자석 아이템을 구현하는 함수
    void ActivateFullMagnet()
    {
        ItemPickup[] allPickups = FindObjectsOfType<ItemPickup>(); // 씬에 활성화된 모든 ItemPickup 검색.
        foreach (ItemPickup item in allPickups)
        {
            if (item.gameObject.activeSelf && (item.type == PickupType.Exp || item.type == PickupType.Coin)) // 경험치 보석과 금색 코인만 자석 대상.
            {
                item.isBeingAttracted = true; // 자성 작동
            }
        }
    }
}
