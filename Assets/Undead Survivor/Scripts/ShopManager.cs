using UnityEngine;
using UnityEngine.UI;

// 상점 UI 표시, 영구 강화 구매 및 가격과 레벨 갱신을 관리하는 스크립트.

public class ShopManager : MonoBehaviour
{
    // 상점 항목 하나의 구매 규칙과 UI 참조를 보관
    [System.Serializable]
    public class ShopItem
    {
        public GameManager.ShopUpgradeType type;
        public int maxLevel = 10;
        public int baseCost = 10;
        public int costIncrease = 5;
        public Text levelText;
        public Text costText;
    }

    public GameObject shopPanel;
    public Text currencyText;
    public ShopItem[] shopItems;

    void OnEnable()
    {
        Refresh();
    }

    public void OpenShop()
    {
        if (shopPanel != null)
        {
            shopPanel.SetActive(true);
        }

        Refresh();
    }

    public void CloseShop()
    {
        if (shopPanel != null)
        {
            shopPanel.SetActive(false);
        }
    }

    public void Buy(int index)
    {
        if (shopItems == null || index < 0 || index >= shopItems.Length)
            return;

        ShopItem item = shopItems[index];
        if (GameManager.instance.TryBuyShopUpgrade(item.type, item.maxLevel, item.baseCost, item.costIncrease))
        {
            Refresh();
        }
    }

    public void BuyLevelUpCost() { BuyType(GameManager.ShopUpgradeType.LevelUpCost); }
    public void BuyMoveSpeed() { BuyType(GameManager.ShopUpgradeType.MoveSpeed); }
    public void BuyDamage() { BuyType(GameManager.ShopUpgradeType.Damage); }
    public void BuyAttackSpeed() { BuyType(GameManager.ShopUpgradeType.AttackSpeed); }
    public void BuyMaxHealth() { BuyType(GameManager.ShopUpgradeType.MaxHealth); }
    public void BuyEnemySpawnTime() { BuyType(GameManager.ShopUpgradeType.EnemySpawnTime); }
    public void BuyRandomBoxChance() { BuyType(GameManager.ShopUpgradeType.RandomBoxChance); }
    public void BuyPickupRange() { BuyType(GameManager.ShopUpgradeType.PickupRange); }

    // 버튼에 대응하는 강화 데이터를 찾아 공통 구매 로직으로 전달
    void BuyType(GameManager.ShopUpgradeType type)
    {
        if (shopItems == null)
            return;

        for (int i = 0; i < shopItems.Length; i++)
        {
            if (shopItems[i].type == type)
            {
                Buy(i);
                return;
            }
        }
    }

    public void Refresh()
    {
        if (GameManager.instance == null)
            return;

        if (currencyText != null)
        {
            currencyText.text = GameManager.instance.shopCurrency.ToString();
        }

        if (shopItems == null)
            return;

        for (int i = 0; i < shopItems.Length; i++)
        {
            ShopItem item = shopItems[i];
            int level = GameManager.instance.GetShopUpgradeLevel(item.type);

            if (item.levelText != null)
            {
                item.levelText.text = level + " / " + item.maxLevel;
            }

            if (item.costText != null)
            {
                item.costText.text = level >= item.maxLevel ? "MAX" : "Cost " + GameManager.instance.GetShopUpgradeCost(item.type, item.baseCost, item.costIncrease);
            }
        }
    }
}
