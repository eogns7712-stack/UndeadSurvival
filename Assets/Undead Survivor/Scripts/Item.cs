using System.Collections;
using System.Collections.Generic;
using System.Net;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class Item : MonoBehaviour
{
    // 아이템 관리에 필요한 변수 선언
    public ItemData data;   
    public int level;
    public Weapon weapon;
    public Gear gear;

    Image icon;
    Text textLevel;
    Text textName;
    Text textDesc;

    void Awake()
    {   //변수 초기화
        icon = GetComponentsInChildren<Image>()[1];    // 자식 오브젝트의 컴포넌트가 필요하므로 GetComponentsInChildren사용
        // GetComponentsInChildren에서 두번째 값으로 가져오기 (첫번째는 자기자신(버튼), 두번째가 아이콘)
        icon.sprite = data.itemIcon;

        Text[] texts = GetComponentsInChildren<Text>();
        textLevel = texts[0];
        textName = texts[1];
        textDesc = texts[2];    // GetComponents의 순서는 계층구조의 순서를 따라감
        textName.text = data.itemName;
    }

    void OnEnable() // 활성화 될 때 자동으로 실행되는 함수
    {
        // [수정] 만렙에 도달한 무기 혹은 장비라면 한계 초월 텍스트로 보완하여 기입
        if (level >= data.damages.Length)
        {
            int overLevel = level - data.damages.Length + 1;
            textLevel.text = "초월 M" + overLevel;
            textDesc.text = data.itemName + "의 한계를 돌파하여 추가 속도와 파괴력을 무한 가중시킵니다.";
        }
        else
        {

            textLevel.text = "Lv." + (level + 1); // level이 1부터 시작하기 위함

            switch (data.itemType)  // 아이템 타입에 따라 설명이 두개가 되는경우가 있기 때문에 switch문으로 구분
            {
                case ItemData.ItemType.Melee:   // 무기 타입의 경우
                case ItemData.ItemType.Range:
                    textDesc.text = string.Format(data.itemDesc, data.damages[level] * 100, data.counts[level]);    // 데미지 상승량을 보여주기 위해 *100
                    break;

                case ItemData.ItemType.Glove:   // 장비 타입의 경우
                case ItemData.ItemType.Shoe:
                    textDesc.text = string.Format(data.itemDesc, data.damages[level] * 100 );
                    break;
                
                default:    // 일회성 아이템의 경우
                    textDesc.text = string.Format(data.itemDesc);
                    break;
            }
        }
    }

    public void OnClick()
    {
        Debug.Log($"Clicked Item: {data.itemName}, itemId: {data.itemId}, playerId: {GameManager.instance.playerId}");
        switch (data.itemType)
        {
            case ItemData.ItemType.Melee:   // 여러 case를 붙여서 로직을 실행할 수 있음
            case ItemData.ItemType.Range:   // 근접공격과 원거리공격은 같은로직으로 실행
                if (level == 0)
                {
                    GameObject newWeapon = new GameObject();    // 게임 오브젝트를 스크립트로 만들 수 있음, 새로운 게임 오브젝트를 코드로 작성
                    weapon = newWeapon.AddComponent<Weapon>();    // AddComponent<T> : 게임 오브젝트에 T컴포넌트를 추가하는 함수
                    // AddComponent 함수 반환값을 미리 선언한 변수에 저장
                    weapon.Init(data);
                }

                else if (level >= data.damages.Length)
                {
                    // [추가] 무기 최대 레벨에 도달했을 시 추가 무한 성장 초월 강화 로직 적용
                    if (weapon != null)
                    {
                        weapon.MasterUpgrade(0.12f, 1); // 대미지 12% 누적 합산 및 수량 1개 증가
                    }
                }

                else
                {
                    float nextDamage = data.baseDamage;
                    int nextCount = 0;
                    
                    // 이 아래부분에서 스크랩터블 오브젝트에서 작성한 아이템 데이터의 레벨당 증가값을 + 혹은 * 로 지정할 수 있음
                    nextDamage += data.baseDamage * data.damages[level];
                    nextCount += data.counts[level];

                    weapon.LevelUp(nextDamage,nextCount);
                }
                // [수정] 여러 분기에서 중복되어 상승하여 레벨을 더블 가산시키던 level++ 연산을 case 마지막 1개로 단일화하여 정렬합니다.
                level ++;
                break;

            case ItemData.ItemType.Glove:
            case ItemData.ItemType.Shoe:
                if (level == 0)
                {
                    GameObject newGear = new GameObject();    // 게임 오브젝트를 스크립트로 만들 수 있음, 새로운 게임 오브젝트를 코드로 작성
                    gear = newGear.AddComponent<Gear>();    // AddComponent<T> : 게임 오브젝트에 T컴포넌트를 추가하는 함수
                    // AddComponent 함수 반환값을 미리 선언한 변수에 저장
                    gear.Init(data);
                }

                else if (level >= data.damages.Length)
                {
                    // 패시브 장비 만렙 이후 초월 적용 (장갑의 공격속도, 신발 이동속도를 소폭 추가 강화)
                    if (gear != null)
                    {
                        float nextRate = data.damages[data.damages.Length - 1] + (0.04f * (level - data.damages.Length + 1));
                        gear.LevelUp(nextRate);
                    }
                }

                else
                {
                    float nextRate = data.damages[level];
                    gear.LevelUp(nextRate);
                }
                level ++;
                break;

            case ItemData.ItemType.Heal:    // 일회성 아이템의 로직은 바로 case문에서 작성
                GameManager.instance.health = GameManager.instance.maxHealth;
                break;
        }
        
        // 효과음 재생
        AudioManager.instance.PlaySfx(AudioManager.Sfx.Select);

        // [수정] 초월(한계 돌파) 강화를 무한대로 계속해서 선택할 수 있도록, 만렙 도달 시 버튼을 완전히 꺼버려 상호작용을 못 하게 막던 로직을 주석 처리합니다.
        // if (level == data.damages.Length)
        // {
        //     GetComponent<Button>().interactable = false;
        // }
    }
}