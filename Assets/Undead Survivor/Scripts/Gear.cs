using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gear : MonoBehaviour
{
    public ItemData.ItemType type;
    public float rate;
    // Weapon 스크립트와 구조 동일
    public void Init(ItemData data)
    {
        // Basic Set
        name = "Gear " + data.itemId;
        transform.parent = GameManager.instance.player.transform;
        transform.localPosition = Vector3.zero;

        // Property Set
        type = data.itemType;
        rate = data.damages[0];
        ApplyGear();
    }

    public void LevelUp(float rate)
    {
        this.rate = rate;
        ApplyGear();
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
            switch (weapon.id)
            {
                case 0 :
                    float speed = 150 * Character.WeaponSpeed;  //Gear.cs 의 장갑 획득으로 인한 회전속도와 연사력에도 Player의 특성이 반영하도록 설정
                    weapon.speed = speed + (speed * rate);
                    break;
                default :
                    speed = 0.5f * Character.WeaponRate;
                    weapon.speed = speed * (1f - rate);
                    break;
            }
        }
    }

    void SpeedUp()
    {
        float speed = 5 * Character.Speed;
        GameManager.instance.player.speed = speed + speed * rate;
    }
}
