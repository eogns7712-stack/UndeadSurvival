using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    // [보상 다양화] 보석, 랜덤박스, 동전(경험치 다량), 힐팩(10% 체력 회복), 자석(필드 자성 끌림화)
    public enum PickupType { Exp, RandomBox, Coin, HealPack, Magnet }
    public PickupType type;

    public float expValue = 1f;
    public float magnetSpeed = 8f;
    public float magnetDistance = 3.5f; // 자석 흡입 적용 거리

    // 상자에서 나올 수 있는 아이템들의 스프라이트 (유니티 인스펙터에서 등록)
    [Header("# Box Item Visuals")]
    public Sprite coinSprite;
    public Sprite healPackSprite;
    public Sprite magnetSprite;

    Transform playerTransform;
    bool isBeingAttracted = false;

    void OnEnable()
    {
        isBeingAttracted = false;
        if (GameManager.instance != null && GameManager.instance.player != null)
        {
            playerTransform = GameManager.instance.player.transform;
        }
    }

    public void InitPickup(PickupType pickupType, float value)
    {
        this.type = pickupType;
        if (pickupType == PickupType.Exp)
        {
            this.expValue = value;
        }
        
        // 타입별 프리셋 스프라이트 강제 교체 설정
        SpriteRenderer spr = GetComponent<SpriteRenderer>();
        if (spr != null)
        {
            if (type == PickupType.Coin && coinSprite != null) spr.sprite = coinSprite;
            else if (type == PickupType.HealPack && healPackSprite != null) spr.sprite = healPackSprite;
            else if (type == PickupType.Magnet && magnetSprite != null) spr.sprite = magnetSprite;
        }
    }

    void Update()
    {
        if (playerTransform == null || !GameManager.instance.isLive)
            return;

        // 상자(Box)의 경우 애니메이션을 보며 제자리에서 먹어야 하므로 자석 당김 연산 제외
        if (type == PickupType.RandomBox)
            return;

        float distance = Vector3.Distance(transform.position, playerTransform.position);

        // 플레이어에 가까이 다가가면 끌림 플래그 가동
        if (!isBeingAttracted && distance <= magnetDistance)
        {
            isBeingAttracted = true;
        }

        if (isBeingAttracted)
        {
            // 부드러운 자석 흡입 이동
            transform.position = Vector3.MoveTowards(transform.position, playerTransform.position, magnetSpeed * Time.deltaTime);
        }
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            // 상자를 제외한 일반 픽업류는 즉시 획득
            if (type != PickupType.RandomBox)
            {
                CollectReward();
            }
        }
    }

    public void CollectReward()
    {
        switch (type)
        {
            case PickupType.Exp:
                // 일반 경험치 보석 수집
                GameManager.instance.GetExp(); 
                break;

            case PickupType.Coin:
                // 동전: 보석 5개 분량의 럭키 경험치 증폭
                for (int i = 0; i < 5; i++)
                {
                    GameManager.instance.GetExp();
                }
                break;

            case PickupType.HealPack:
                // 힐팩: 플레이어 체력 10% 즉시 보충
                GameManager.instance.health = Mathf.Min(GameManager.instance.health + (GameManager.instance.maxHealth * 0.1f), GameManager.instance.maxHealth);
                break;

            case PickupType.Magnet:
                // 자석: 필드 상의 모든 보석들을 플레이어 방향으로 자력 끌림 가동!
                ActivateFullMagnet();
                break;
        }

        AudioManager.instance.PlaySfx(AudioManager.Sfx.Select);
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

    // [수정] 상자 오픈 시 지정된 확률로 3종의 추가 물리 아이템(코인, 힐팩, 자석) 중 하나를 필드에 드롭!
    void SpawnBoxItemReward()
    {
        // 상자를 획득했으므로, 필드의 보석 프리팹 풀을 재사용하여 코인/힐팩/자석 인스턴스를 바닥에 떨굽니다.
        GameObject boxItem = GameManager.instance.pool.Get(5); // 보석 풀 재활용 (스프라이트가 동적으로 세팅됨)
        if (boxItem != null)
        {
            boxItem.transform.position = transform.position;
            ItemPickup pickup = boxItem.GetComponent<ItemPickup>();
            if (pickup != null)
            {
                // 랜덤 3종 보너스 타입 결정
                int luckyRoll = Random.Range(2, 5); // Coin (2), HealPack (3), Magnet (4)
                pickup.InitPickup((PickupType)luckyRoll, 0);
            }
        }

        AudioManager.instance.PlaySfx(AudioManager.Sfx.LevelUp);
    }

    // 전 맵의 보석을 강제로 끌어오는 자석 함수
    void ActivateFullMagnet()
    {
        ItemPickup[] allPickups = FindObjectsOfType<ItemPickup>();
        foreach (ItemPickup item in allPickups)
        {
            if (item.gameObject.activeSelf && item.type == PickupType.Exp)
            {
                item.isBeingAttracted = true; // 자성 원격 작동
            }
        }
    }
}