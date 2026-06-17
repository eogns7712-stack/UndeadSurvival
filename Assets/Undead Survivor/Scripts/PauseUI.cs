using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PauseUI : MonoBehaviour
{
    [System.Serializable]
    public class EquipmentSlot
    {
        public ItemData.ItemType itemType;
        public GameObject root;
        public Image icon;
        public Text levelText;
    }

    public GameObject pausePanel;
    public Text titleText;
    public Text statusText;
    public Text shopBonusText;
    public EquipmentSlot[] equipmentSlots;

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

        RefreshStatus();
        RefreshEquipmentSlots();
        RefreshShopBonus();
    }

    void RefreshStatus()
    {
        if (statusText == null)
            return;

        if (GameManager.instance == null || GameManager.instance.uiLevelUp == null)
        {
            statusText.text = "";
            return;
        }

        Item[] items = GameManager.instance.uiLevelUp.GetComponentsInChildren<Item>(true);
        List<string> lines = new List<string>();

        for (int i = 0; i < items.Length; i++)
        {
            Item item = items[i];
            if (item == null || item.data == null || item.level <= 0)
                continue;

            switch (item.data.itemType)
            {
                case ItemData.ItemType.Melee:
                case ItemData.ItemType.Range:
                case ItemData.ItemType.Glove:
                case ItemData.ItemType.Shoe:
                case ItemData.ItemType.Bomb:
                    lines.Add(item.data.itemName + " : " + GetLevelText(item));
                    break;
            }
        }

        statusText.text = lines.Count > 0 ? string.Join("\n", lines.ToArray()) : "장착 장비 없음";
    }

    void RefreshEquipmentSlots()
    {
        if (equipmentSlots == null || equipmentSlots.Length == 0)
            return;

        Item[] items = GetLevelUpItems();

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
        if (shopBonusText == null || GameManager.instance == null)
            return;

        GameManager manager = GameManager.instance;
        shopBonusText.text =
            "상점 보너스\n" +
            "레벨업 필요 경험치 -" + FormatPercent(manager.ShopLevelUpCostDiscount) + "\n" +
            "이동속도 +" + FormatPercent(manager.ShopMoveSpeedRate) + "\n" +
            "공격력 +" + FormatPercent(manager.ShopDamageRate) + "\n" +
            "공격속도 +" + FormatPercent(manager.ShopAttackSpeedRate) + "\n" +
            "최대체력 +" + FormatPercent(manager.ShopMaxHealthRate) + "\n" +
            "몹 리젠시간 -" + FormatPercent(manager.ShopEnemySpawnTimeReductionRate) + "\n" +
            "랜덤박스 확률 +" + FormatPercent(manager.ShopBoxDropChanceBonus) + "\n" +
            "아이템 흡수 거리 +" + manager.ShopPickupRangeBonus.ToString("0.##");
    }

    string FormatPercent(float value)
    {
        return (value * 100f).ToString("0.#") + "%";
    }
}
