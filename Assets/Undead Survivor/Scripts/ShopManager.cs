using UnityEngine;
using UnityEngine.UI;

// 상점 UI 표시, 영구 강화 구매 및 가격과 레벨 갱신을 관리하는 스크립트.

public class ShopManager : MonoBehaviour
{
    // 상점 항목 하나의 구매 규칙과 UI 참조를 보관
    [System.Serializable]
    public class ShopItem
    {
        public GameManager.ShopUpgradeType type;   // 이 상점 항목이 어떤 강화 타입인지 지정.
        public int maxLevel = 10;  // 구매 가능한 최대 레벨.
        public int baseCost = 10;  // 0레벨 기준 기본 가격.
        public int costIncrease = 5;   // 레벨이 오를 때마다 추가되는 가격.
        public Text levelText; // 현재 레벨 / 최대 레벨을 표시할 Text.
        public Text costText;  // 다음 구매 비용 또는 MAX를 표시할 Text.
    }

    public GameObject shopPanel;   // 상점 전체 패널 오브젝트.
    public Text currencyText;  // 현재 보유 상점 재화를 표시할 Text.
    public ShopItem[] shopItems;   // 상점에 표시될 모든 강화 항목.

    void OnEnable()
    {
        Refresh();  // 상점 매니저가 켜질 때 UI 값을 최신 상태로 갱신.
    }

    // 상점 버튼을 눌렀을 때 상점 패널을 열고 UI 값을 갱신하는 함수.
    public void OpenShop()
    {
        if (shopPanel != null)  // 상점 패널이 연결되어 있으면 활성화.
        {
            shopPanel.SetActive(true);
        }

        Refresh();  // 패널을 연 직후 재화, 레벨, 비용 표시 갱신.
    }

    // 닫기 버튼을 눌렀을 때 상점 패널을 숨기는 함수.
    public void CloseShop()
    {
        if (shopPanel != null)  // 상점 패널이 연결되어 있으면 비활성화.
        {
            shopPanel.SetActive(false);
        }
    }

    // ShopItems 배열의 index에 해당하는 상점 항목을 구매하는 함수.
    public void Buy(int index)
    {
        if (shopItems == null || index < 0 || index >= shopItems.Length)    // 배열이 없거나 범위 밖 인덱스면 실행 중단.
            return;

        ShopItem item = shopItems[index];   // 클릭한 버튼에 대응되는 상점 항목.
        if (GameManager.instance.TryBuyShopUpgrade(item.type, item.maxLevel, item.baseCost, item.costIncrease))  // GameManager에 구매 가능 여부와 저장 처리를 맡김.
        {
            Refresh();  // 구매 성공 시 UI 값 갱신.
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
        if (shopItems == null)  // 상점 항목 배열이 없으면 실행 중단.
            return;

        for (int i = 0; i < shopItems.Length; i++) // 배열을 순회하며 요청한 강화 타입과 같은 항목 검색.
        {
            if (shopItems[i].type == type)
            {
                Buy(i); // 해당 타입을 찾으면 공통 구매 함수 호출.
                return;
            }
        }
    }

    // 상점 재화, 각 항목의 레벨과 비용 텍스트를 최신 상태로 갱신하는 함수.
    public void Refresh()
    {
        if (GameManager.instance == null)   // GameManager가 없으면 상점 수치를 가져올 수 없으므로 중단.
            return;

        if (currencyText != null)   // 재화 텍스트가 연결되어 있으면 현재 상점 재화 표시.
        {
            currencyText.text = GameManager.instance.shopCurrency.ToString();
        }

        if (shopItems == null)  // 표시할 상점 항목이 없으면 종료.
            return;

        for (int i = 0; i < shopItems.Length; i++) // 모든 상점 항목 UI 갱신.
        {
            ShopItem item = shopItems[i];  // 현재 갱신할 항목.
            int level = GameManager.instance.GetShopUpgradeLevel(item.type);    // GameManager에 저장된 현재 강화 레벨.

            if (item.levelText != null) // 레벨 표시 Text가 연결되어 있으면 현재 레벨 / 최대 레벨 표시.
            {
                item.levelText.text = level + " / " + item.maxLevel;
            }

            if (item.costText != null)  // 비용 Text가 연결되어 있으면 구매 비용 또는 MAX 표시.
            {
                item.costText.text = level >= item.maxLevel ? "MAX" : "Cost " + GameManager.instance.GetShopUpgradeCost(item.type, item.baseCost, item.costIncrease);
            }
        }
    }
}
