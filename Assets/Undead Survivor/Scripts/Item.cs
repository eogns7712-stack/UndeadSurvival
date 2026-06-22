using UnityEngine;
using UnityEngine.UI;

/// 레벨업 선택지의 표시 내용과 무기, 장비, 수류탄 강화 적용을 담당하는 스크립트.

public class Item : MonoBehaviour
{
    // 아이템 관리에 필요한 변수 선언
    public ItemData data;   
    public int level;
    public Weapon weapon;
    public Bomb bomb;
    public Gear gear;
    public float masterDamageMultiplier = 0.12f;
    public int masterExtraCount = 1;
    public float passiveMasterRatePerLevel = 0.04f;

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
        textDesc = texts[2];    // GetComponents of hierarchy order
        textName.text = data.itemName;
    }

    void OnEnable() // 활성화 될 때 자동으로 실행되는 함수
    {
        // [수정] 만렙에 도달한 장비에 초월 텍스트 및 전용 연출 가이드 설명.
        if (level >= data.damages.Length)
        {
            ShowTranscendenceDescription();
        }
        else
        {
            ShowNormalDescription();
        }
    }

    // 현재 초월 단계에 맞는 이름, 아이콘과 전용 설명을 표시한다.
    void ShowTranscendenceDescription()
    {
            int L = data.damages.Length;
            int targetTotalLevel = level + 1; // 업그레이드 수락 시 달성하게 될 최종 레벨
            
            // 레벨 주기를 순환하는 초월 단계 산출 공식 적용 (1 -> 5 순환 구조 구현)
            int dL = targetTotalLevel - L - 1; 
            int stage = 1 + (dL / L);         // 1단계(M1), 2단계(M2)...
            int displayLevel = 1 + (dL % L);  // 각 단계 내부의 순환 레벨 1 -> 5

            // [초월 설명 텍스트 보완 : 설명 잘림 최적화]
            string evolutionStepName = "";
            string customDescription = "";

            // 초월 단계에 맞춰 실시간으로 대입할 초월 가산 비율을 연산.
            float displayDamagePct = masterDamageMultiplier * 100f; // 무기 기본 초월 대미지 (12%)
            int displayCount = masterExtraCount;         // 무기 기본 초월 추가 개수 (1개)

            if (data.itemId == 0) // 근접무기 삽
            {
                evolutionStepName = (stage == 1) ? " [M1 갈퀴]" : " [M2 낫]";
                displayCount = 0;
            }
            else if (data.itemId == 1) // 원거리무기 총
            {
                evolutionStepName = (stage == 1) ? " [M1 라이플]" : " [M2 샷건]";
                
                // 총기류 초월 특성에 걸맞는 전용 수치를 계산하여 기입.
                if (stage == 1)
                {
                    displayDamagePct = -30f; // 라이플 대미지 30% 감소
                }
                else
                {
                    displayDamagePct = 120f; // 샷건 대미지 120% 증가.
                    displayCount = 3;        // 3발 분산 사격화
                }
            }
            else if (data.itemId == 4) // [버그 수정] 원거리무기 수류탄 분기 연동.
            {
                evolutionStepName = (stage == 1) ? " [M1 파편 수류탄]" : " [M2 소이 수류탄]";
                
                // 수류탄 초월 기획 전용 대칭 데이터 연산 기입
                if (stage == 1)
                {
                    displayDamagePct = 35f;  // 파편 데미지 연산
                    displayCount = 5;        // 파편 개수 가산
                }
                else
                {
                    displayDamagePct = 80f;   // 소이탄 지옥불 중심 데미지
                    displayCount = 1;        // 지옥불 화염 영역 개수
                }
            }
            else
            {
                // 패시브 장비류(장갑, 신발) 초월
                evolutionStepName = (data.itemType == ItemData.ItemType.Glove) ? " [공속 초월]" : " [이동 초월]";
                displayDamagePct = passiveMasterRatePerLevel * 100f; // 장갑/신발 속도 4% 가산
                displayCount = 0;
            }

            // ItemData의 'Transcendence Descriptions' 배열에 적혀 있는 "{0}", "{1}" 포맷 양식을 읽어와 수치를 동적 맵핑.
            int descIndex = stage - 1;
            if (data.transcendenceDescs != null && descIndex < data.transcendenceDescs.Length && !string.IsNullOrEmpty(data.transcendenceDescs[descIndex]))
            {
                // 에셋에 수치 포맷({0}, {1})을 포함해 적어둔 규칙이 있다면 실시간 동적 가동.
                customDescription = string.Format(data.transcendenceDescs[descIndex], displayDamagePct, displayCount);
            }
            else
            {
                // 에셋에 초월 설명이 아예 비어있을 때 오작동을 차단하는 기본 가이드문(Fallback 장치)
                if (data.itemId == 4)
                {
                    if (stage == 1)
                        customDescription = $"수류탄이 공중에서 분열해 <color=yellow>{displayCount}개</color>의 소형 파편으로 쪼개집니다.\n기본 대미지가 <color=yellow>+{displayDamagePct}%</color> 가산됩니다.";
                    else
                        customDescription = $"폭발지에 <color=orange>지옥불 소이 지대</color>를 <color=yellow>{displayCount}개</color> 잔류시킵니다.\n중심부 데미지가 <color=orange>+{displayDamagePct}%</color> 폭증합니다.";
                }
                else
                {
                    customDescription = "돌파 강화를 가동합니다.\n성능 효율성이 추가 누적 가산됩니다.";
                }
            }

            // [레이아웃 겹침 해결] 좁은 왼쪽 레벨칸에는 아주 심플하게 "M1 L.1" 형태로 표기하여 겹침을 방지.
            textLevel.text = $"M{stage} L.{displayLevel}";
            
            // 넓고 남는 공간이 많은 상단 이름칸 우측에 "[M1 라이플]" 등의 진화 단계를 표기.
            textName.text = evolutionStepName.Trim();
            textDesc.text = customDescription;
    }

    void ShowNormalDescription()
    {
        textName.text = data.itemName; // 초월 해제 시 원래 이름 복원
        textLevel.text = "Lv." + (level + 1); // level이 1부터 시작하기 위함

        switch (data.itemType)  // 아이템 타입에 따라 설명이 두개가 되는경우가 있기 때문에 switch문으로 구분
        {
            case ItemData.ItemType.Melee:   // 무기 타입의 경우
            case ItemData.ItemType.Range:
            case ItemData.ItemType.Bomb:
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

    // 선택된 아이템 종류에 맞는 강화 로직을 실행한 뒤 선택창을 닫는다.
    public void OnClick()
    {
        switch (data.itemType)
        {
            case ItemData.ItemType.Melee:
            case ItemData.ItemType.Range:
                SelectWeapon();
                break;

            case ItemData.ItemType.Bomb:
                SelectBomb();
                break;

            case ItemData.ItemType.Glove:
            case ItemData.ItemType.Shoe:
                SelectGear();
                break;

            case ItemData.ItemType.Heal:    // 일회성 아이템의 로직은 바로 case문에서 작성
                GameManager.instance.health = GameManager.instance.maxHealth;
                break;
        }
        
        // 효과음 재생
        AudioManager.instance.PlaySfx(AudioManager.Sfx.Select);
    }

    void SelectWeapon()
    {
        if (data.itemId == 4)
        {
            SelectBomb();
            return;
        }

        if (level == 0)
        {
            GameObject newWeapon = new GameObject();
            weapon = newWeapon.AddComponent<Weapon>();
            weapon.Init(data);
        }
        else if (level >= data.damages.Length)
        {   // [추가] 무기 최대 레벨에 도달했을 시, 추가 무한 성장 초월 강화 로직 적용
            if (weapon != null)
            {
                weapon.MasterUpgrade(masterDamageMultiplier, masterExtraCount); // 대미지 12% 누적 합산 및 수량 1개 증가
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
    }

    void SelectGear()
    {
        if (level == 0)
        {
            GameObject newGear = new GameObject();    // 새로운 패시브 장비 기어 생성
            gear = newGear.AddComponent<Gear>();    
            gear.Init(data);
        }
        else if (level >= data.damages.Length)
        {
            // [수정] 패시브 장비 만렙 이후 초월 적용 (장갑의 공격속도, 신발 이동속도를 소폭 추가 강화하며 무기 공격력에는 영향 없음)
            if (gear != null)
            {
                float nextRate = data.damages[data.damages.Length - 1] + (passiveMasterRatePerLevel * (level - data.damages.Length + 1));
                gear.LevelUp(nextRate);
            }
        }
        else
        {
            float nextRate = data.damages[level];
            gear.LevelUp(nextRate);
        }
        level ++;
    }

    void SelectBomb()
    {
        if (level == 0)
        {
            GameObject newBomb = new GameObject();
            bomb = newBomb.AddComponent<Bomb>();
            bomb.Init(data);
            Hand.SetBombHandActive(GameManager.instance.player, true);
        }
        else if (level >= data.damages.Length)
        {
            // [추가] 무기 최대 레벨에 도달했을 시 추가 무한 성장 초월 강화 로직 적용
            if (bomb != null)
            {
                bomb.MasterUpgrade(masterDamageMultiplier, masterExtraCount); // 대미지 12% 누적 합산 및 수량 1개 증가
            }
        }
        else
        {
            float nextDamage = data.baseDamage;
            int nextCount = 0;
            
            // 이 아래부분에서 스크랩터블 오브젝트에서 작성한 아이템 데이터의 레벨당 증가값을 + 혹은 * 로 지정할 수 있음
            nextDamage += data.baseDamage * data.damages[level];
            nextCount += data.counts[level];

            bomb.LevelUp(nextDamage,nextCount);
        }
        level ++;
    }
}
