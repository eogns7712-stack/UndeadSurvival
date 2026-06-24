using UnityEngine;

/// 장갑과 신발 등 패시브 장비의 레벨과 능력치 적용을 관리하는 스크립트.

public class Gear : MonoBehaviour
{
    public ItemData.ItemType type;  // 장비 타입. Glove면 공격속도, Shoe면 이동속도 적용.
    public float rate;  // 현재 장비 레벨에서 적용할 증가율.
    // Weapon 스크립트와 구조 동일
    public void Init(ItemData data)
    {
        // Basic Set
        name = "Gear " + data.itemId;  // 하이어라키에서 구분하기 쉽도록 아이템 ID로 이름 설정.
        transform.parent = GameManager.instance.player.transform;  // 장비는 Player 하위 오브젝트로 배치.
        transform.localPosition = Vector3.zero;    // Player 중심 위치로 초기화.

        // Property Set
        type = data.itemType;  // ItemData에 설정된 장비 타입 저장.
        rate = data.damages[0];    // 1레벨 장비 효과 수치 적용.
        ApplyGear();   // 타입에 맞는 능력치 즉시 적용.
    }

    // 장비 레벨업 시 증가율을 갱신하고 효과를 다시 적용하는 함수.
    public void LevelUp(float rate)
    {
        this.rate = rate;  // 레벨업 선택지에서 전달받은 새 증가율 저장.
        ApplyGear();   // 새 증가율을 현재 무기나 플레이어 이동속도에 반영.
    }

    void ApplyGear()    // 아이템 타입에 따라 적절하게 로직을 적용시켜주는 함수 추가
    {
        switch (type)
        {
            case ItemData.ItemType.Glove:
                RateUp();
                break;
            case ItemData.ItemType.Shoe:
                SpeedUp();
                break;
        }
    }

    void RateUp()   // 장갑의 기능인 연사력을 올려주는 함수 작성
    {
        Weapon[] weapons = transform.parent.GetComponentsInChildren<Weapon>();    // 부모 오브젝트인 Player로 올라가서 모든 Weapon을 가져오기
        
        foreach(Weapon weapon in weapons)   // foreach문으로 Weapon배열에 들어있는 weapons를 하나씩 순회하면서 타입에 따라 속도올리기
        {
            weapon.ApplyGear(rate); // 일반 근접/원거리 무기에 장갑 공격속도 효과 적용.
        }

        Bomb[] bombs = transform.parent.GetComponentsInChildren<Bomb>();  // 수류탄은 Bomb 스크립트로 분리되어 있으므로 별도로 가져오기.

        foreach(Bomb bomb in bombs) // 수류탄 무기에도 장갑 공격속도 효과 적용.
        {
            bomb.ApplyGear(rate);
        }
    }

    // 신발 장비 효과로 플레이어 이동속도를 갱신하는 함수.
    void SpeedUp()
    {
        float speed = 5 * Character.Speed; // 기본 이동속도에 캐릭터/상점 이동속도 보정 적용.
        GameManager.instance.player.speed = speed + speed * rate;  // 장비 증가율을 더해 최종 이동속도 반영.
    }
}
