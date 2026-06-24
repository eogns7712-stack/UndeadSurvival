using UnityEngine;
using UnityEngine.UI;

// 일시정지 화면의 장비 강화 단계와 영구 상점 보너스를 표시 및 관리하는 스크립트.
public class PauseUI : MonoBehaviour
{
    /// 장비 한 종류의 아이콘과 강화 단계 표시를 묶는다.
    [System.Serializable]
    public class EquipmentSlot
    {
        public ItemData.ItemType itemType; // 이 슬롯이 표시할 아이템 종류.
        public GameObject root;    // 슬롯 전체 오브젝트. 아이템이 없으면 숨김.
        public Image icon; // 장착 아이템 아이콘.
        public Text levelText; // 장착 아이템의 레벨/초월 단계 텍스트.
    }

    /// 상점 강화 한 종류의 이름과 적용 수치 표시를 묶는다.
    [System.Serializable]
    public class ShopBonusSlot
    {
        public GameManager.ShopUpgradeType type;   // 이 슬롯이 표시할 상점 강화 종류.
        public GameObject root;    // 상점 보너스 한 줄 전체 오브젝트.
        public Text nameText;  // 상점 보너스 이름 텍스트.
        public Text valueText; // 현재 적용 중인 보너스 수치 텍스트.
    }

    public GameObject pausePanel;  // 일시정지 전체 패널.
    public Text titleText; // 상단 PAUSE 타이틀 텍스트.
    public Text shopBonusText; // 상점 보너스 섹션 제목 텍스트.
    public EquipmentSlot[] equipmentSlots; // 장착 장비 표시 슬롯 배열.
    public ShopBonusSlot[] shopBonusSlots; // 상점 보너스 표시 슬롯 배열.

    // 일시정지 패널을 열기 전 현재 장비와 상점 보너스 표시를 갱신하는 함수.
    public void Show()
    {
        Refresh();  // 패널을 켜기 전에 최신 상태로 갱신.

        if (pausePanel != null) // 패널 참조가 연결되어 있으면 활성화.
        {
            pausePanel.SetActive(true);
        }
    }

    // 일시정지 패널을 숨기는 함수.
    public void Hide()
    {
        if (pausePanel != null) // 패널 참조가 연결되어 있으면 비활성화.
        {
            pausePanel.SetActive(false);
        }
    }

    // 장비 슬롯과 상점 보너스 표시를 한 번에 다시 계산하는 함수.
    public void Refresh()
    {
        if (titleText != null)  // 타이틀 텍스트가 연결되어 있으면 고정 문구 표시.
        {
            titleText.text = "PAUSE";
        }

        Item[] items = GetLevelUpItems();  // LevelUp UI 하위 Item 컴포넌트들을 가져와 현재 장착/강화 상태 확인.
        RefreshEquipmentSlots(items);  // 장비 아이콘과 레벨 표시 갱신.
        RefreshShopBonus();    // 상점 보너스 표시 갱신.
    }

    // 장착된 아이템을 슬롯 종류와 연결하고 아이콘 및 초월 단계를 갱신
    void RefreshEquipmentSlots(Item[] items)
    {
        if (equipmentSlots == null || equipmentSlots.Length == 0)   // 표시할 슬롯이 없으면 실행 중단.
            return;

        for (int i = 0; i < equipmentSlots.Length; i++)    // 인스펙터에 등록된 슬롯을 순서대로 갱신.
        {
            EquipmentSlot slot = equipmentSlots[i]; // 현재 갱신할 장비 슬롯.
            Item item = FindEquippedItem(items, slot.itemType);    // 슬롯 타입에 맞는 장착 아이템 검색.
            bool hasItem = item != null && item.data != null && item.level > 0; // 실제로 획득한 아이템인지 확인.

            if (slot.root != null)  // 아이템이 없으면 슬롯 자체를 숨김.
            {
                slot.root.SetActive(hasItem);
            }

            ClearRootText(slot);    // 루트 오브젝트에 남아있는 기본 Text가 있으면 비워 UI 겹침 방지.

            if (!hasItem)
            {
                if (slot.icon != null)  // 아이템이 없으면 아이콘 숨김.
                {
                    slot.icon.enabled = false;
                }

                if (slot.levelText != null) // 아이템이 없으면 레벨 텍스트 비움.
                {
                    slot.levelText.text = "";
                }

                continue;
            }

            if (slot.icon != null)  // 장착 아이템 아이콘 적용.
            {
                slot.icon.sprite = item.data.itemIcon;
                slot.icon.enabled = item.data.itemIcon != null;
            }

            if (slot.levelText != null) // 현재 레벨 또는 초월 단계 표시.
            {
                slot.levelText.text = GetLevelText(item);
            }
        }
    }

    // 슬롯 루트에 남아 있는 임시 Text를 비워 아이콘/레벨 텍스트와 겹치지 않게 하는 함수.
    void ClearRootText(EquipmentSlot slot)
    {
        if (slot.root == null)  // 루트가 없으면 실행 중단.
            return;

        Text rootText = slot.root.GetComponent<Text>();    // 루트 오브젝트에 직접 붙은 Text 검사.
        if (rootText != null && rootText != slot.levelText)
        {
            rootText.text = "";
        }
    }

    // LevelUp UI 하위의 Item 컴포넌트들을 가져오는 함수.
    Item[] GetLevelUpItems()
    {
        if (GameManager.instance == null || GameManager.instance.uiLevelUp == null) // GameManager나 LevelUp UI가 없으면 빈 배열 반환.
            return new Item[0];

        return GameManager.instance.uiLevelUp.GetComponentsInChildren<Item>(true);
    }

    // 슬롯 타입과 일치하면서 이미 획득한 Item을 찾는 함수.
    Item FindEquippedItem(Item[] items, ItemData.ItemType itemType)
    {
        for (int i = 0; i < items.Length; i++) // 모든 레벨업 Item 후보를 순회.
        {
            Item item = items[i];
            if (item != null && item.data != null && item.level > 0 && IsMatchingSlot(item, itemType))
            {
                return item;
            }
        }

        return null;    // 해당 슬롯에 표시할 장착 아이템이 없을 때.
    }

    // 수류탄이 과거 원거리 무기 ID와 겹치는 경우까지 고려해 슬롯을 구분
    bool IsMatchingSlot(Item item, ItemData.ItemType itemType)
    {
        if (itemType == ItemData.ItemType.Bomb)
        {
            return item.data.itemType == ItemData.ItemType.Bomb || item.data.itemId == 4;
        }

        if (itemType == ItemData.ItemType.Range && item.data.itemId == 4)
            return false;

        return item.data.itemType == itemType;
    }

    string GetLevelText(Item item)
    {
        int maxLevel = item.data.damages != null ? item.data.damages.Length : 0;
        if (maxLevel <= 0)  // 레벨 데이터가 없는 예외 아이템은 현재 레벨만 표시.
            return "Lv." + item.level;

        if (item.level <= maxLevel) // 기본 1~5레벨 구간.
            return "Lv." + item.level + " / " + maxLevel;

        int extraLevel = item.level - maxLevel;    // 만렙 이후 초월 누적 레벨.
        int stage = 1 + (extraLevel - 1) / maxLevel;   // M1, M2 같은 초월 단계.
        int stageLevel = 1 + (extraLevel - 1) % maxLevel; // 초월 단계 내부 레벨.
        return "M" + stage + " Lv." + stageLevel + " / " + maxLevel;
    }

    void RefreshShopBonus()
    {
        if (GameManager.instance == null)
            return;

        if (shopBonusText != null)
        {
            shopBonusText.text = "상점 보너스";
        }

        RefreshShopBonusSlots();
    }

    // 상점 강화 단계로부터 실제 적용 중인 증감 수치를 계산해 표시
    void RefreshShopBonusSlots()
    {
        if (shopBonusSlots == null)
            return;

        GameManager manager = GameManager.instance;

        for (int i = 0; i < shopBonusSlots.Length; i++)
        {
            ShopBonusSlot slot = shopBonusSlots[i];
            if (slot.root != null)
            {
                slot.root.SetActive(true);
            }

            if (slot.nameText != null)
            {
                slot.nameText.text = GetShopBonusName(slot.type);
            }

            if (slot.valueText != null)
            {
                slot.valueText.text = GetShopBonusValue(manager, slot.type);
            }
        }
    }

    string GetShopBonusName(GameManager.ShopUpgradeType type)
    {
        switch (type)
        {
            case GameManager.ShopUpgradeType.LevelUpCost: return "필요 경험치";
            case GameManager.ShopUpgradeType.MoveSpeed: return "이동속도";
            case GameManager.ShopUpgradeType.Damage: return "공격력";
            case GameManager.ShopUpgradeType.AttackSpeed: return "공격속도";
            case GameManager.ShopUpgradeType.MaxHealth: return "최대체력";
            case GameManager.ShopUpgradeType.EnemySpawnTime: return "리젠시간";
            case GameManager.ShopUpgradeType.RandomBoxChance: return "박스 확률";
            case GameManager.ShopUpgradeType.PickupRange: return "흡수 거리";
        }

        return type.ToString();
    }

    string GetShopBonusValue(GameManager manager, GameManager.ShopUpgradeType type)
    {
        switch (type)
        {
            case GameManager.ShopUpgradeType.LevelUpCost: return "-" + FormatPercent(manager.ShopLevelUpCostDiscount);
            case GameManager.ShopUpgradeType.MoveSpeed: return "+" + FormatPercent(manager.ShopMoveSpeedRate);
            case GameManager.ShopUpgradeType.Damage: return "+" + FormatPercent(manager.ShopDamageRate);
            case GameManager.ShopUpgradeType.AttackSpeed: return "+" + FormatPercent(manager.ShopAttackSpeedRate);
            case GameManager.ShopUpgradeType.MaxHealth: return "+" + FormatPercent(manager.ShopMaxHealthRate);
            case GameManager.ShopUpgradeType.EnemySpawnTime: return "-" + FormatPercent(manager.ShopEnemySpawnTimeReductionRate);
            case GameManager.ShopUpgradeType.RandomBoxChance: return "+" + FormatPercent(manager.ShopBoxDropChanceBonus) + "p";
            case GameManager.ShopUpgradeType.PickupRange: return "+" + manager.ShopPickupRangeBonus.ToString("0.##");
        }

        return "0";
    }

    string FormatPercent(float value)
    {
        return (value * 100f).ToString("0.#") + "%";
    }
}
