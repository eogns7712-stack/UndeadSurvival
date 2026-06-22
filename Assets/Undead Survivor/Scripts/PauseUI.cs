using UnityEngine;
using UnityEngine.UI;

// 일시정지 화면의 장비 강화 단계와 영구 상점 보너스를 표시 및 관리하는 스크립트.
public class PauseUI : MonoBehaviour
{
    /// 장비 한 종류의 아이콘과 강화 단계 표시를 묶는다.
    [System.Serializable]
    public class EquipmentSlot
    {
        public ItemData.ItemType itemType;
        public GameObject root;
        public Image icon;
        public Text levelText;
    }

    /// 상점 강화 한 종류의 이름과 적용 수치 표시를 묶는다.
    [System.Serializable]
    public class ShopBonusSlot
    {
        public GameManager.ShopUpgradeType type;
        public GameObject root;
        public Text nameText;
        public Text valueText;
    }

    public GameObject pausePanel;
    public Text titleText;
    public Text shopBonusText;
    public EquipmentSlot[] equipmentSlots;
    public ShopBonusSlot[] shopBonusSlots;

    public void Show()
    {
        Refresh();

        if (pausePanel != null)
        {
            pausePanel.SetActive(true);
        }
    }

    public void Hide()
    {
        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }
    }

    public void Refresh()
    {
        if (titleText != null)
        {
            titleText.text = "PAUSE";
        }

        Item[] items = GetLevelUpItems();
        RefreshEquipmentSlots(items);
        RefreshShopBonus();
    }

    // 장착된 아이템을 슬롯 종류와 연결하고 아이콘 및 초월 단계를 갱신
    void RefreshEquipmentSlots(Item[] items)
    {
        if (equipmentSlots == null || equipmentSlots.Length == 0)
            return;

        for (int i = 0; i < equipmentSlots.Length; i++)
        {
            EquipmentSlot slot = equipmentSlots[i];
            Item item = FindEquippedItem(items, slot.itemType);
            bool hasItem = item != null && item.data != null && item.level > 0;

            if (slot.root != null)
            {
                slot.root.SetActive(hasItem);
            }

            ClearRootText(slot);

            if (!hasItem)
            {
                if (slot.icon != null)
                {
                    slot.icon.enabled = false;
                }

                if (slot.levelText != null)
                {
                    slot.levelText.text = "";
                }

                continue;
            }

            if (slot.icon != null)
            {
                slot.icon.sprite = item.data.itemIcon;
                slot.icon.enabled = item.data.itemIcon != null;
            }

            if (slot.levelText != null)
            {
                slot.levelText.text = GetLevelText(item);
            }
        }
    }

    void ClearRootText(EquipmentSlot slot)
    {
        if (slot.root == null)
            return;

        Text rootText = slot.root.GetComponent<Text>();
        if (rootText != null && rootText != slot.levelText)
        {
            rootText.text = "";
        }
    }

    Item[] GetLevelUpItems()
    {
        if (GameManager.instance == null || GameManager.instance.uiLevelUp == null)
            return new Item[0];

        return GameManager.instance.uiLevelUp.GetComponentsInChildren<Item>(true);
    }

    Item FindEquippedItem(Item[] items, ItemData.ItemType itemType)
    {
        for (int i = 0; i < items.Length; i++)
        {
            Item item = items[i];
            if (item != null && item.data != null && item.level > 0 && IsMatchingSlot(item, itemType))
            {
                return item;
            }
        }

        return null;
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
        if (maxLevel <= 0)
            return "Lv." + item.level;

        if (item.level <= maxLevel)
            return "Lv." + item.level + " / " + maxLevel;

        int extraLevel = item.level - maxLevel;
        int stage = 1 + (extraLevel - 1) / maxLevel;
        int stageLevel = 1 + (extraLevel - 1) % maxLevel;
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
