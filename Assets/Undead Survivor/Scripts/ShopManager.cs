using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ShopManager : MonoBehaviour
{
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
