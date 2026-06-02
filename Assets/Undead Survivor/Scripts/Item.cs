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
        // [수정] 만렙에 도달한 무기 혹은 장비라면 순환 한계 초월 텍스트 및 전용 연출 상세 가이드 설명 완벽 보완!
        if (level >= data.damages.Length)
        {
            int L = data.damages.Length;
            int targetTotalLevel = level + 1; // 업그레이드 수락 시 달성하게 될 최종 레벨
            
            // L레벨 주기를 순환하는 초월 단계 산출 공식 적용 (1 -> 5 순환 구조 구현)
            int dL = targetTotalLevel - L - 1; 
            int stage = 1 + (dL / L);         // 1단계(M1), 2단계(M2)...
            int displayLevel = 1 + (dL % L);  // 각 단계 내부의 순환 레벨 1 -> 5

            // [초월 진화 비주얼 가이드 설명 텍스트 보완 - 설명 잘림 최적화]
            string evolutionStepName = "";
            string customDescription = "";

            if (data.itemId == 0) // 근접무기 삽
            {
                if (stage == 1)
                {
                    evolutionStepName = " [M1 갈퀴]";
                    customDescription = "사방의 몹을 쓸어 모으는 <color=yellow>갈퀴</color>로 진화합니다.\n화력 +15% 및 무기 슬롯 개수가 증가합니다.";
                }
                else if (stage >= 2)
                {
                    evolutionStepName = " [M2 낫]";
                    customDescription = "거대한 사신의 <color=orange>낫</color>으로 최종 진화합니다.\n회전 속도가 증가하며 범위 스플래시가 가중됩니다.";
                }
            }
            else if (data.itemId == 1) // 원거리무기 총
            {
                if (stage == 1)
                {
                    evolutionStepName = " [M1 라이플]";
                    customDescription = "고속 연속 점사가 보장되는 <color=yellow>라이플</color>로 진화합니다.\n정교한 탄환이 고속 점사 발사됩니다.";
                }
                else if (stage >= 2)
                {
                    evolutionStepName = " [M2 샷건]";
                    customDescription = "산탄을 사방으로 난사하는 <color=orange>샷건</color>으로 진화합니다.\n광범위 스플래시 제압 사격이 펼쳐집니다.";
                }
            }
            else
            {
                customDescription = $"{data.itemName}의 구조를 고도화하여 영구 강화합니다.\n공격 효율성이 추가 누적 가산됩니다.";
            }

            // [레이아웃 겹침 해결] 좁은 왼쪽 레벨칸에는 아주 심플하게 "M1 L.1" 형태로 표기하여 겹침을 방지합니다.
            textLevel.text = $"M{stage} L.{displayLevel}";
            
            // 넓고 남는 공간이 많은 상단 이름칸 우측에 "[M1 라이플]" 등의 진화 단계를 조화롭게 결합시킵니다.
            textName.text = data.itemName + evolutionStepName;
            textDesc.text = customDescription;
        }
        else
        {
            textName.text = data.itemName; // 초월 해제 시 원래 이름 복원
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
        switch (data.itemType)
        {
            case ItemData.ItemType.Melee:
            case ItemData.ItemType.Range:
                if (level == 0)
                {
                    GameObject newWeapon = new GameObject();
                    weapon = newWeapon.AddComponent<Weapon>();
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
    }
}