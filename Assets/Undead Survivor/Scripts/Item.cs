using UnityEngine;
using UnityEngine.UI;

/// 레벨업 선택지의 표시 내용과 무기, 장비, 수류탄 강화 적용을 담당하는 스크립트.

public class Item : MonoBehaviour
{
    // 아이템 관리에 필요한 변수 선언
    public ItemData data;   // 이 선택지가 참조하는 ItemData 스크립터블 오브젝트.
    public int level;   // 현재 아이템 강화 레벨. 일반 레벨과 초월 누적 레벨을 함께 관리.
    public Weapon weapon;   // 근접/원거리 무기 선택지일 때 생성된 Weapon 참조.
    public Bomb bomb;   // 수류탄 선택지일 때 생성된 Bomb 참조.
    public Gear gear;   // 장갑/신발 선택지일 때 생성된 Gear 참조.
    public float masterDamageMultiplier = 0.12f;   // 무기 초월 시 적용할 기본 데미지 증가율.
    public int masterExtraCount = 1; // 무기 초월 시 추가될 기본 개수/관통 보너스.
    public float passiveMasterRatePerLevel = 0.04f; // 패시브 장비 초월 시 레벨당 추가 증가율.

    Image icon; // 레벨업 선택지에 표시할 아이템 아이콘.
    Text textLevel;    // 선택지 왼쪽 레벨 표시 텍스트.
    Text textName; // 선택지 상단 이름/초월명 표시 텍스트.
    Text textDesc; // 선택지 설명 텍스트.

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
            int L = data.damages.Length;   // 한 초월 단계 안에서 사용하는 기본 레벨 길이.
            int targetTotalLevel = level + 1; // 업그레이드 수락 시 달성하게 될 최종 레벨
            
            // 레벨 주기를 순환하는 초월 단계 산출 공식 적용 (1 -> 5 순환 구조 구현)
            int dL = targetTotalLevel - L - 1; 
            int stage = 1 + (dL / L);         // 1단계(M1), 2단계(M2)...
            int displayLevel = 1 + (dL % L);  // 각 단계 내부의 순환 레벨 1 -> 5

            // [초월 설명 텍스트 보완 : 설명 잘림 최적화]
            icon.sprite = GetTranscendenceIcon(stage); // 초월 단계에 맞는 선택지 아이콘으로 교체.
            string evolutionStepName = "";
            string customDescription = GetTranscendenceDescription(stage, displayLevel);

            if (data.itemId == 0) // 근접무기 삽
            {
                evolutionStepName = (stage == 1) ? " [M1 갈퀴]" : " [M2 낫]";
            }
            else if (data.itemId == 1) // 원거리무기 총
            {
                evolutionStepName = (stage == 1) ? " [M1 라이플]" : " [M2 샷건]";
            }
            else if (data.itemId == 4) // [버그 수정] 원거리무기 수류탄 분기 연동.
            {
                evolutionStepName = (stage == 1) ? " [M1 파편 수류탄]" : " [M2 소이 수류탄]";
            }
            else
            {
                // 패시브 장비류(장갑, 신발) 초월
                evolutionStepName = (data.itemType == ItemData.ItemType.Glove) ? " [공속 초월]" : " [이동 초월]";
            }

            // [레이아웃 겹침 해결] 좁은 왼쪽 레벨칸에는 아주 심플하게 "M1 L.1" 형태로 표기하여 겹침을 방지.
            textLevel.text = $"M{stage} L.{displayLevel}";
            
            // 넓고 남는 공간이 많은 상단 이름칸 우측에 "[M1 라이플]" 등의 진화 단계를 표기.
            textName.text = evolutionStepName.Trim();
            textDesc.text = customDescription;
    }

    void ShowNormalDescription()
    {
        icon.sprite = data.itemIcon;    // 일반 레벨업 선택지는 기본 아이템 아이콘으로 복구.
        textName.text = data.itemName; // 초월 해제 시 원래 이름 복원
        textLevel.text = "Lv." + (level + 1); // level이 1부터 시작하기 위함

        switch (data.itemType)  // 아이템 타입에 따라 설명이 두개가 되는경우가 있기 때문에 switch문으로 구분
        {
            case ItemData.ItemType.Melee:   // 무기 타입의 경우
            case ItemData.ItemType.Range:
            case ItemData.ItemType.Bomb:
                textDesc.text = GetWeaponDescription();    // 실제 선택 시 적용되는 무기/수류탄 강화값 기준으로 표시.
                break;

            case ItemData.ItemType.Glove:   // 장비 타입의 경우
            case ItemData.ItemType.Shoe:
                textDesc.text = GetGearDescription();   // 장비는 누적 증가량이 아니라 해당 레벨의 최종 rate를 표시.
                break;
            
            default:    // 일회성 아이템의 경우
                textDesc.text = string.Format(data.itemDesc);
                break;
        }
    }

    string GetWeaponDescription()
    {
        if (data.itemId == 4)   // 예전 데이터에서 수류탄이 Range 타입으로 남아 있어도 설명은 Bomb 기준으로 표시.
        {
            if (level == 0)
                return $"수류탄 획득\n폭발 공격력 {FormatNumber(data.baseDamage)}\n스캐너 목표로 투척";

            float bombDamagePercent = data.damages[level] * 100f;
            int fragmentBonus = data.counts[level];
            if (fragmentBonus > 0)
                return $"폭발 공격력 +{FormatNumber(bombDamagePercent)}%\n파편 보너스 +{fragmentBonus}\n초월 후 파편에 적용";

            return $"폭발 공격력 +{FormatNumber(bombDamagePercent)}%\n파편 보너스 변화 없음";
        }

        if (level == 0) // 처음 획득하는 선택지는 강화가 아니라 장착/해금 효과.
        {
            switch (data.itemType)
            {
                case ItemData.ItemType.Melee:
                    return $"무기 획득\n공격력 {FormatNumber(data.baseDamage)}\n무기 개수 {data.baseCount}";
                case ItemData.ItemType.Range:
                    return $"무기 획득\n공격력 {FormatNumber(data.baseDamage)}\n관통 {data.baseCount}";
                case ItemData.ItemType.Bomb:
                    return $"수류탄 획득\n폭발 공격력 {FormatNumber(data.baseDamage)}\n스캐너 목표로 투척";
            }
        }

        float damagePercent = data.damages[level] * 100f;  // 일반 레벨업은 baseDamage 기준 증가율을 사용.
        int countBonus = data.counts[level];   // 근접은 개수, 원거리는 관통, 수류탄은 초월 후 파편 보너스로 사용.
        switch (data.itemType)
        {
            case ItemData.ItemType.Melee:
                if (countBonus > 0)
                    return $"공격력 +{FormatNumber(damagePercent)}%\n무기 개수 +{countBonus}";
                return $"공격력 +{FormatNumber(damagePercent)}%\n무기 개수 변화 없음";

            case ItemData.ItemType.Range:
                if (countBonus > 0)
                    return $"공격력 +{FormatNumber(damagePercent)}%\n관통 +{countBonus}";
                return $"공격력 +{FormatNumber(damagePercent)}%\n관통 변화 없음";

            case ItemData.ItemType.Bomb:
                if (countBonus > 0)
                    return $"폭발 공격력 +{FormatNumber(damagePercent)}%\n파편 보너스 +{countBonus}\n초월 후 파편에 적용";
                return $"폭발 공격력 +{FormatNumber(damagePercent)}%\n파편 보너스 변화 없음";
        }

        return string.Format(data.itemDesc, damagePercent, countBonus);
    }

    string GetGearDescription()
    {
        float rate = data.damages[level] * 100f;    // Gear.LevelUp에 그대로 전달되는 최종 장비 효과율.
        if (data.itemType == ItemData.ItemType.Glove)
            return $"공격속도 +{FormatNumber(rate)}%\n무기와 수류탄에 적용";

        return $"이동속도 +{FormatNumber(rate)}%\n플레이어 이동에 적용";
    }

    string GetTranscendenceDescription(int stage, int displayLevel)
    {
        float masterDamagePct = masterDamageMultiplier * 100f; // Weapon/Bomb.MasterUpgrade에 전달되는 실제 데미지 증가율.
        if (data.itemId == 0)
        {
            return $"현재 공격력 +{FormatNumber(masterDamagePct)}%\n무기 외형 진화\n개수 증가는 없음";
        }

        if (data.itemId == 1)
        {
            if (stage == 1 && displayLevel == 1)
                return "라이플로 진화\n현재 공격력 -30%\n발사 간격 감소";

            if (stage == 1)
                return $"라이플 강화\n현재 공격력 +{FormatNumber(masterDamagePct)}%\n관통 +{masterExtraCount}";

            if (displayLevel == 1)
                return "샷건으로 진화\n현재 공격력 +120%\n3발 분산, 발사 간격 증가";

            return $"샷건 강화\n현재 공격력 +{FormatNumber(masterDamagePct)}%\n3발 분산 유지";
        }

        if (data.itemId == 4)
        {
            Bomb balance = GetBombBalanceSource();
            float fragmentDamagePct = balance != null ? balance.fragmentDamageRate * 100f : 0f;
            float fireDamagePct = balance != null ? balance.fireDamageRate * 100f : 0f;

            if (stage == 1)
                return $"파편 수류탄 진화\n폭발 공격력 +{FormatNumber(masterDamagePct)}%, 파편 +{masterExtraCount}\n파편 피해 {FormatNumber(fragmentDamagePct)}%";

            return $"소이 수류탄 진화\n폭발 공격력 +{FormatNumber(masterDamagePct)}%, 파편 +{masterExtraCount}\n화염 피해 {FormatNumber(fireDamagePct)}%";
        }

        if (data.itemType == ItemData.ItemType.Glove || data.itemType == ItemData.ItemType.Shoe)
        {
            int nextMasterLevel = level - data.damages.Length + 1;  // 선택 후 적용될 패시브 초월 누적 레벨.
            float nextRate = data.damages[data.damages.Length - 1] + passiveMasterRatePerLevel * nextMasterLevel;
            string targetName = data.itemType == ItemData.ItemType.Glove ? "공격속도" : "이동속도";
            return $"{targetName} 초월 강화\n현재 효과 총 +{FormatNumber(nextRate * 100f)}%\n레벨당 +{FormatNumber(passiveMasterRatePerLevel * 100f)}%";
        }

        return $"현재 공격력 +{FormatNumber(masterDamagePct)}%\n추가 수량 +{masterExtraCount}";
    }

    Bomb GetBombBalanceSource()
    {
        if (bomb != null)
            return bomb;

        if (data.projectile == null)
            return null;

        return data.projectile.GetComponent<Bomb>();
    }

    Sprite GetTranscendenceIcon(int stage)
    {
        int evolutionIndex = stage - 1; // stage 1은 배열 0번, stage 2는 배열 1번과 연결.
        if (data.customChoiceIcons != null && evolutionIndex >= 0 && evolutionIndex < data.customChoiceIcons.Length && data.customChoiceIcons[evolutionIndex] != null)
        {
            return data.customChoiceIcons[evolutionIndex];   // LevelUp 선택지 전용 초월 아이콘 사용.
        }

        return data.itemIcon;   // 초월 전용 아이콘이 없으면 기존 아이콘 유지.
    }

    string FormatNumber(float value)
    {
        return value.ToString("0.#");
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
        if (data.itemId == 4)   // 예전 데이터에서 수류탄이 원거리 무기처럼 들어온 경우 Bomb 로직으로 우회.
        {
            SelectBomb();
            return;
        }

        if (level == 0) // 처음 획득한 무기라면 새 Weapon 오브젝트 생성.
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
            float nextDamage = data.baseDamage * Character.Damage;
            int nextCount = 0;
            
            // 이 아래부분에서 스크랩터블 오브젝트에서 작성한 아이템 데이터의 레벨당 증가값을 + 혹은 * 로 지정할 수 있음
            nextDamage += data.baseDamage * data.damages[level] * Character.Damage;
            nextCount += data.counts[level];

            weapon.LevelUp(nextDamage,nextCount);
        }
        level ++;   // 선택 처리가 끝난 뒤 아이템 레벨 증가.
    }

    // 장갑/신발 같은 패시브 장비 선택지를 처리하는 함수.
    void SelectGear()
    {
        if (level == 0) // 처음 획득한 장비라면 새 Gear 오브젝트 생성.
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
        level ++;   // 선택 처리가 끝난 뒤 아이템 레벨 증가.
    }

    // 수류탄 선택지를 처리하는 함수. 일반 무기와 분리된 Bomb 스크립트를 사용.
    void SelectBomb()
    {
        if (level == 0) // 처음 획득한 수류탄이라면 새 Bomb 무기 오브젝트 생성.
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
            float nextDamage = data.baseDamage * Character.Damage;
            int nextCount = 0;
            
            // 이 아래부분에서 스크랩터블 오브젝트에서 작성한 아이템 데이터의 레벨당 증가값을 + 혹은 * 로 지정할 수 있음
            nextDamage += data.baseDamage * data.damages[level] * Character.Damage;
            nextCount += data.counts[level];

            bomb.LevelUp(nextDamage,nextCount);
        }
        level ++;   // 선택 처리가 끝난 뒤 수류탄 레벨 증가.
    }
}
