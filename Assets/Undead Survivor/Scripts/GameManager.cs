using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;  // 장면관리를 사용하기위해 SceneManagement 네임스페이스 추가
using UnityEngine.UI;


/// 게임 시작과 종료, 경험치, 상점 저장값, 일시정지 및 보스전의 흐름 등. 게임의 전반적인 관리를 하는 스크립트.

public class GameManager : MonoBehaviour
{
    // 상점 강화 항목을 배열 인덱스로 관리하기 위한 구분값. ShopManager와 PauseUI에서 같은 순서로 참조한다.
    public enum ShopUpgradeType { LevelUpCost, MoveSpeed, Damage, AttackSpeed, MaxHealth, EnemySpawnTime, RandomBoxChance, PickupRange }

    public static GameManager instance;  // static : 정적으로 사용하겠다는 키워드, 바로 메모리에 얹어버림, 인스펙터에 나타나지않음, 정적변수는 즉시 클래스에서 호출가능
    [Header("# Game Control")]    // Header : 인스펙터의 속성들을 깔끔하게 구분시켜주는 타이틀
    public bool isLive;
    public bool isBossBattle;
    public bool isBossCleared;
    public bool isPaused;
    public float gameTime;
    public float maxGameTime = 2* 10;
    public float bossBattleTime = 180f;
    public float bossTime;
    [Header("# Player Info")]
    public int playerId;    // 캐릭터 ID를 저장할 변수 선언
    public float health;
    public float baseMaxHealth = 100;
    public float maxHealth = 100;
    public int level;   // 게임매니저에 레벨, 킬수, 경험치 변수 선언
    public int kill;
    public int exp;
    public int[] nextExp = { 5, 8, 10, 13, 15, 18, 20, 23, 25, 28, 30 };   // 각 레벨의 필요 경험치를 보관할 배열 변수 선언
    [Header("# Shop")]
    public int shopCurrency;
    public int bossShopCurrencyReward = 100;
    public int[] shopUpgradeLevels = new int[8];
    public float shopLevelUpCostDiscount = 0.03f;
    public float shopMoveSpeedBonus = 0.03f;
    public float shopDamageBonus = 0.03f;
    public float shopAttackSpeedBonus = 0.03f;
    public float shopMaxHealthBonus = 0.1f;
    public float shopEnemySpawnTimeReduction = 0.03f;
    public float shopRandomBoxChanceBonus = 0.01f;
    public float shopPickupRangeBonus = 0.25f;
    [Header("# Game Object")]
    public Player player;
    public PoolManager pool;
    public LevelUp uiLevelUp;
    public BossHPUI uiBossHP;
    public PauseUI uiPause;
    public Result uiResult;    // 게임 결과 UI 오브젝트를 저장할 변수 선언, 타입을 스크립트로 변경(영상13 28:30) 
    public GameObject uiHealth;
    public GameObject shopButton;
    public GameObject pauseButton;
    public GameObject enemyCleaner; // 게임 승리시 적을 정리하는 클리너 변수 선언
    public Spawner spawner;
    [Header("# Boss Info")]
    public float bossZoomSize = 4.5f;
    public float bossZoomDuration = 1.2f;
    public float bossZoomHoldTime = 0.6f;
    public float bossZoomReturnDuration = 0.8f;
    public Image bossWarningOverlay;
    public Image bossWarningImage;
    public Sprite bossWarningSprite;
    public Vector2 bossWarningImageSize = new Vector2(520f, 110f);
    public float bossWarningDuration = 1.2f;
    public float bossWarningBlinkInterval = 0.15f;
    public int bossDeathExplosionFxPrefabId = -1;
    public int bossDeathExplosionCount = 8;
    public float bossDeathExplosionRadius = 2.5f;
    public float bossDeathExplosionSize = 2.5f;
    public float bossDeathSlowScale = 0.25f;
    public float bossDeathSlowDuration = 1.6f;
    public float bossDeathShakePower = 0.25f;
    public float bossDeathShakeDuration = 0.8f;
    public int bossRewardCoinPrefabId = 5;
    public float bossRewardCoinRadius = 2.8f;
    public float bossRewardCoinBounceDuration = 0.8f;
    public Sprite bossRewardCoinSprite;

    float defaultCameraSize; // 보스 줌인 연출 후 되돌아갈 기본 카메라 크기. 게임 시작 시 현재 카메라 크기를 저장.
    bool isBossDeathRoutine; // 보스 사망 연출이 중복 실행되지 않도록 막는 변수. 보스가 여러 탄에 동시에 죽어도 연출은 한 번만 실행.


    void Awake()
    {
        instance = this;    // 인스턴스 변수를 자기자신으로 초기화
        LoadShopData();    // 저장된 상점 재화와 강화 레벨을 불러와 게임 시작 전 보정값 준비.

        // 인스펙터 연결이 비어 있을 때 필요한 참조를 자동으로 보완.
        if (spawner == null)   // Spawner가 인스펙터에 연결되지 않았다면 씬에서 자동 검색.
        {
            spawner = FindObjectOfType<Spawner>();
        }
        if (uiHealth == null)  // 보스 등장 연출 중 잠시 숨길 Health UI를 자동 검색.
        {
            uiHealth = GameObject.Find("Canvas/HUD/Health");
        }
        EnsureBossWarningUI();  // 보스 경고 UI가 없으면 런타임에 자동 생성.
        SetShopButtonActive(true);  // 시작 화면에서는 상점 버튼 활성화.
        SetPauseButtonActive(false);    // 게임 시작 전에는 일시정지 버튼 비활성화.
    }

    // 캐릭터 선택 후 게임 상태를 초기화하고 플레이어, 기본 무기, UI, 시간 흐름을 시작.
    public void GameStart(int id)   // 게임 시작 함수에 Player의 ID 매개변수 추가
    {
        playerId = id;  // 선택한 캐릭터 id 저장.
        SetShopButtonActive(false); // 게임 시작 후에는 상점 버튼 숨기기.
        SetPauseButtonActive(true); // 게임 시작 후에는 일시정지 버튼 표시.
        isPaused = false;   // 새 게임 시작 시 일시정지 상태 초기화.
        if (uiPause != null)    // 이전 게임에서 일시정지 UI가 켜져 있었다면 숨기기.
        {
            uiPause.Hide();
        }
        ApplyShopStats();   // 상점에서 구매한 최대체력 보정값 적용.
        health = maxHealth; // 게임 시작시 현재체력을 최대체력으로 초기화
        kill = 0;   // 킬 수도 0으로 초기화
        gameTime = 0f;  // gameTime도 0으로 초기화
        bossTime = bossBattleTime;  // 보스전 제한시간을 기본값으로 초기화.
        isBossBattle = false;   // 게임 시작 시점은 일반 웨이브 상태.
        isBossCleared = false;  // 보스 처치 업적 조건 초기화.

        player.gameObject.SetActive(true);  // 게임 시작시 Player 활성화 후 기본무기 지급
        if (uiBossHP != null)   // 게임 시작시 BOSS HP UI가 활성화 되있다면 Hide.
        {
            uiBossHP.Hide();
        }

        if (Camera.main != null)    // 보스 줌인 연출 후 원래 크기로 복귀하기 위해 기본 카메라 사이즈 저장.
        {
            defaultCameraSize = Camera.main.orthographicSize;
        }
        HideBossWarningUI();    // 혹시 남아 있는 보스 경고 UI 비활성화.

        uiLevelUp.Select(playerId % 2); // 선택한 캐릭터에 맞는 기본 무기 지급.
        Resume();   // 게임 시간을 정상 속도로 진행.

        AudioManager.instance.PlayBgm(true);    // 게임 시작부분에 PlayBgm 함수 호출
        AudioManager.instance.PlaySfx(AudioManager.Sfx.Select); // 캐릭터 선택버튼 클릭시 효과음 재생
    }

    // 패배 처리 코루틴을 시작.
    public void GameOver()  // 코루틴 없이 바로 Stop함수 실행 시, Player의 Dead 애니메이션 출력 전에 멈춰버림
    {
        StartCoroutine(GameOverRoutine());  // 패배 UI와 사운드 처리를 코루틴으로 실행.
    }

    IEnumerator GameOverRoutine()   // 게임오버의 딜레이를 위해 코루틴 작성
    {
        PrepareGameEnd();   // 게임 종료에 공통으로 필요한 상태값과 UI 정리.

        yield return new WaitForSeconds(0.5f);  // 0.5초 기다리기

        uiResult.gameObject.SetActive(true);   // 게임결과 UI 활성화
        uiResult.Lose();    // 이미지 오브젝트를 활성화하는 패배 함수 호출
        SetShopButtonActive(true);  // 게임 종료시 ShopButtom 활성화.
        Stop(); // 결과창 상태에서 게임 시간 정지.

        AudioManager.instance.PlayBgm(false);   // 게임 종료시 Bgm종료
        AudioManager.instance.PlaySfx(AudioManager.Sfx.Lose); // 게임 오버시 효과음 재생
    }

    // 승리 처리 코루틴을 시작.
    public void GameVictory()  // 코루틴 없이 바로 Stop함수 실행 시, Player의 Dead 애니메이션 출력 전에 멈춰버림
    {
        StartCoroutine(GameVictoryRoutine());   // 승리 UI와 사운드 처리를 코루틴으로 실행.
    }

    // 보스가 사망했을 때 보상과 사망 연출을 한 번만 실행.
    public void BossDead(Vector3 bossPosition)
    {
        if (isBossDeathRoutine) // 보스 사망 연출이 이미 진행 중이면 중복 호출 방지.
            return;

        isBossCleared = true;   // 업적 해금 조건에서 사용할 보스 클리어 플래그.
        StartCoroutine(BossDeathRoutine(bossPosition));    // 보스 사망 연출 코루틴 실행.
    }

    // 보스 사망 시 슬로우, 폭발, 화면 흔들림과 보상 생성을 진행하는 코루틴.
    IEnumerator BossDeathRoutine(Vector3 bossPosition)
    {
        isBossDeathRoutine = true;  // 보스 사망 연출 코루틴 실행.

        if (uiBossHP != null)   // 보스 체력 UI가 있으면 숨기기.
        {
            uiBossHP.Hide();
        }

        float prevTimeScale = Time.timeScale;   // 기존 시간 배율을 저장. 추후에 시간배율을 복구하기 위함.
        Time.timeScale = bossDeathSlowScale;    // bossDeathSlowScale만큼 게임 시간을 느리게 감속.

        StartCoroutine(BossDeathShakeRoutine());    // 카메라를 짧게 흔드는 보스 사망 연출 코루틴 실행.
        SpawnBossRewardCoins(bossPosition); // 보스 위치 기준으로 보상 코인 생성.
        for (int i = 0; i < bossDeathExplosionCount; i++)   // 보스 사망 시 여러 개의 폭발 이펙트를 생성.
        {
            Vector2 randomPos = Random.insideUnitCircle * bossDeathExplosionRadius; // Random.insideUnitCircle : 반지름 1짜리 원 내부의 랜덤좌표 반환, 보스 위치 주변 원형 범위 안에서 랜덤 폭발.
            SpawnBossDeathExplosion(bossPosition + new Vector3(randomPos.x, randomPos.y, 0f));  // 보스위치에 랜덤 오프셋을 더해서 폭팔 이펙트 생성.
            yield return new WaitForSecondsRealtime(bossDeathSlowDuration / Mathf.Max(1, bossDeathExplosionCount)); // WaitForSecondsRealtime : 타임스케일의 영향을 받지않는 실제시간 기준으로 대기.
        }

        yield return new WaitForSecondsRealtime(bossDeathSlowDuration); // 실제시간(Realtime) 기준 bossDeathSlowDuration 값만큼 추가 대기 시간.

        Time.timeScale = prevTimeScale > 0f ? prevTimeScale : 1f;   // 시간 배율 복구.
        isBossDeathRoutine = false; // 보스 사망 연출 코루틴 종료.
        GameVictory();  // 게임 승리 처리.
    }

    IEnumerator GameVictoryRoutine()   // 게임오버의 딜레이를 위해 코루틴 작성
    {
        PrepareGameEnd();   // 승리 처리 전에 게임 진행 상태와 보스 UI 정리.
        if (enemyCleaner != null)   // enemyCleaner가 아직 활성화 되지 않았다면
        {
            enemyCleaner.SetActive(true);   //게임 승리 코루틴 전반부에 적 클리너 활성화
        }

        yield return new WaitForSeconds(0.5f);  // 0.5초 기다리기

        uiResult.gameObject.SetActive(true);   // 게임결과 UI 활성화
        uiResult.Win();    // 이미지 오브젝트를 활성화하는 승리 함수 호출
        SetShopButtonActive(true);  // 게임 종료 연출 시 ShopButton 활성화.
        Stop(); // 결과창 상태에서 게임 시간 정지.

        
        AudioManager.instance.PlayBgm(false);   // 게임 종료시 Bgm종료
        AudioManager.instance.PlaySfx(AudioManager.Sfx.Win); // 게임 승리시 효과음 재생
    }

    public void GameRetry() // 게임 재시작 함수 작성
    {
        SceneManager.LoadScene(0);    // LoadScene이름 혹은 인덱스로 장면을 새롭게 부르는 함수
    }

    // 캐릭터 선택 화면에서만 상점 버튼을 보이게 제어하는 함수.
    void SetShopButtonActive(bool active)
    {
        if (shopButton != null) // ShopButton이 인스펙터에 연결되어 있을 때만 활성화 상태 변경.
        {
            shopButton.SetActive(active);
        }
    }

    // 게임 진행 중에만 일시정지 버튼을 보이게 제어하는 함수.
    void SetPauseButtonActive(bool active)
    {
        if (pauseButton != null)    // PauseButton이 인스펙터에 연결되어 있을 때만 활성화 상태 변경.
        {
            pauseButton.SetActive(active);
        }
    }

    // 승리와 패배에 공통으로 필요한 UI 및 시간 상태를 정리하는 함수.
    void PrepareGameEnd()
    {
        isLive = false; // 게임 진행 상태 종료.
        isBossBattle = false;   // 보스전 상태 해제.
        isPaused = false;   // 결과창에서는 일시정지 상태가 아니므로 false로 초기화.
        SetPauseButtonActive(false);    // 게임 종료 후 일시정지 버튼 숨기기.

        if (uiPause != null)    // 일시정지 UI가 열려 있었다면 숨기기.
        {
            uiPause.Hide();
        }

        if (uiBossHP != null)   // 보스 체력 UI가 열려 있었다면 숨기기.
        {
            uiBossHP.Hide();
        }
    }

    // 몬스터 처치나 보스 보상으로 얻은 상점 재화를 저장하는 함수.
    public void AddShopCurrency(int amount)
    {
        shopCurrency += Mathf.Max(0, amount);  // 음수 보상이 들어오더라도 재화가 깎이지 않도록 0 이상으로 보정.
        PlayerPrefs.SetInt("ShopCurrency", shopCurrency);  // 상점 재화를 즉시 PlayerPrefs에 저장.
    }

    // 상점 버튼을 눌렀을 때 재화와 최대 레벨을 확인하고 강화 수치를 저장하는 함수.
    public bool TryBuyShopUpgrade(ShopUpgradeType type, int maxLevel, int baseCost, int costIncrease)
    {
        int index = (int)type;  // enum 값을 배열 인덱스로 변환.
        EnsureShopUpgradeArray();   // 상점 항목 수와 배열 길이가 맞는지 확인.

        if (shopUpgradeLevels[index] >= maxLevel)   // 이미 최대 레벨이면 구매 실패.
            return false;

        int cost = GetShopUpgradeCost(type, baseCost, costIncrease);    // 현재 레벨 기준 구매 비용 계산.
        if (shopCurrency < cost)    // 보유 재화가 부족하면 구매 실패.
            return false;

        shopCurrency -= cost;   // 구매 비용 차감.
        shopUpgradeLevels[index]++; // 해당 상점 항목 레벨 증가.
        SaveShopData(); // 변경된 재화와 강화 레벨 저장.
        ApplyShopStats();   // 구매 직후 현재 게임에 적용 가능한 수치 갱신.
        return true;    // 구매 성공.
    }

    // 지정한 상점 강화 항목의 현재 레벨을 반환하는 함수.
    public int GetShopUpgradeLevel(ShopUpgradeType type)
    {
        EnsureShopUpgradeArray();   // 상점 배열 길이 보정.
        return shopUpgradeLevels[(int)type];    // enum 값을 인덱스로 사용해서 레벨 반환.
    }

    // 상점 강화 레벨에 따라 다음 구매 비용을 계산하는 함수.
    public int GetShopUpgradeCost(ShopUpgradeType type, int baseCost, int costIncrease)
    {
        return baseCost + GetShopUpgradeLevel(type) * costIncrease; // 기본 가격 + 현재 레벨 * 증가 가격.
    }

    // 상점의 경험치 비용 감소 효과를 적용하되 최소 요구치는 1로 보정하는 함수.
    public int GetRequiredExp(int levelIndex)
    {
        int baseExp = nextExp[Mathf.Min(levelIndex, nextExp.Length - 1)];   // 레벨이 배열 길이를 넘어가도 마지막 필요 경험치를 사용.
        float discount = ShopLevelUpCostDiscount;   // 상점에서 구매한 레벨업 필요 경험치 감소율.
        return Mathf.Max(1, Mathf.CeilToInt(baseExp * (1f - discount)));    // 할인 적용 후 최소 필요 경험치는 1로 보정.
    }

    // 상점에서 구매한 이동속도 증가율.
    public float ShopMoveSpeedRate
    {
        get { return GetShopUpgradeLevel(ShopUpgradeType.MoveSpeed) * shopMoveSpeedBonus; }
    }

    // 상점에서 구매한 공격력 증가율.
    public float ShopDamageRate
    {
        get { return GetShopUpgradeLevel(ShopUpgradeType.Damage) * shopDamageBonus; }
    }

    // 상점에서 구매한 공격속도 증가율. 공격 주기가 과하게 낮아지지 않도록 상한을 둔다.
    public float ShopAttackSpeedRate
    {
        get { return Mathf.Clamp(GetShopUpgradeLevel(ShopUpgradeType.AttackSpeed) * shopAttackSpeedBonus, 0f, 0.45f); }
    }

    // 몬스터 리젠 시간에 곱해지는 상점 보정값.
    public float ShopEnemySpawnRate
    {
        get { return Mathf.Clamp(1f - GetShopUpgradeLevel(ShopUpgradeType.EnemySpawnTime) * shopEnemySpawnTimeReduction, 0.5f, 1f); }
    }

    // 랜덤박스 드랍 확률에 더해지는 상점 보정값.
    public float ShopBoxDropChanceBonus
    {
        get { return Mathf.Clamp(GetShopUpgradeLevel(ShopUpgradeType.RandomBoxChance) * shopRandomBoxChanceBonus, 0f, 0.15f); }
    }

    // 아이템 흡수 거리에 더해지는 상점 보정값.
    public float ShopPickupRangeBonus
    {
        get { return GetShopUpgradeLevel(ShopUpgradeType.PickupRange) * shopPickupRangeBonus; }
    }

    // 레벨업 필요 경험치에 적용되는 상점 할인율.
    public float ShopLevelUpCostDiscount
    {
        get { return Mathf.Clamp(GetShopUpgradeLevel(ShopUpgradeType.LevelUpCost) * shopLevelUpCostDiscount, 0f, 0.3f); }
    }

    // 최대 체력에 곱해지는 상점 증가율.
    public float ShopMaxHealthRate
    {
        get { return GetShopUpgradeLevel(ShopUpgradeType.MaxHealth) * shopMaxHealthBonus; }
    }

    // 일시정지 상태창 표시용 몬스터 리젠 감소율.
    public float ShopEnemySpawnTimeReductionRate
    {
        get { return 1f - ShopEnemySpawnRate; }
    }

    // 저장된 상점 강화 수치를 현재 게임에 사용하는 보정값으로 변환하는 함수.
    public void ApplyShopStats()
    {
        maxHealth = baseMaxHealth * (1f + ShopMaxHealthRate);  // 기본 최대체력에 상점 최대체력 증가율 적용.
    }

    // PlayerPrefs에 저장된 상점 재화와 강화 레벨을 불러오는 함수.
    void LoadShopData()
    {
        EnsureShopUpgradeArray();   // 저장값을 불러오기 전 상점 배열 길이 보정.
        shopCurrency = PlayerPrefs.GetInt("ShopCurrency", 0);  // 저장된 상점 재화를 불러오고, 없으면 0으로 시작.
        for (int i = 0; i < shopUpgradeLevels.Length; i++) // 상점 항목 수만큼 저장된 레벨 불러오기.
        {
            shopUpgradeLevels[i] = PlayerPrefs.GetInt("ShopUpgrade_" + i, 0);
        }
        ApplyShopStats();   // 불러온 강화 레벨을 현재 능력치에 반영.
    }

    // 상점 재화와 강화 레벨을 PlayerPrefs에 저장하는 함수.
    void SaveShopData()
    {
        EnsureShopUpgradeArray();   // 저장 전 상점 배열 길이 보정.
        PlayerPrefs.SetInt("ShopCurrency", shopCurrency); // 현재 상점 재화 저장.
        for (int i = 0; i < shopUpgradeLevels.Length; i++) // 모든 상점 강화 레벨 저장.
        {
            PlayerPrefs.SetInt("ShopUpgrade_" + i, shopUpgradeLevels[i]);
        }
        PlayerPrefs.Save(); // PlayerPrefs 저장 내용 즉시 디스크에 반영.
    }

    // 상점 항목이 추가되어도 기존 저장값을 유지하며 배열 길이를 맞추는 함수.
    void EnsureShopUpgradeArray()
    {
        int count = System.Enum.GetValues(typeof(ShopUpgradeType)).Length;  // enum에 등록된 상점 항목 개수 계산.
        if (shopUpgradeLevels == null || shopUpgradeLevels.Length != count) // 배열이 없거나 항목 수와 맞지 않으면 새 배열 생성.
        {
            int[] oldLevels = shopUpgradeLevels;   // 기존 강화 레벨을 임시 저장.
            shopUpgradeLevels = new int[count];
            if (oldLevels != null)
            {
                for (int i = 0; i < Mathf.Min(oldLevels.Length, shopUpgradeLevels.Length); i++)    // 기존에 있던 항목의 레벨은 유지.
                {
                    shopUpgradeLevels[i] = oldLevels[i];
                }
            }
        }
    }

    // 일반 게임 시간과 보스전 제한 시간을 갱신하고, 시간이 끝나면 보스전 또는 패배를 처리하는 함수.
    void Update()
    {
        if (!isLive)    // 게임 진행 상태가 아니면 시간 갱신 중단.
            return;

        gameTime += Time.deltaTime;    // 타이머 변수에 deltaTime 계속 더하기
        if (isBossBattle)   // 보스전 중이라면 일반 게임 시간이 아니라 보스전 제한시간을 감소.
        {
            bossTime -= Time.deltaTime;
            if (bossTime <= 0f) // 보스전 제한시간이 끝나면 패배 처리.
            {
                bossTime = 0f;  // HUD에 음수 시간이 나오지 않도록 0으로 고정.
                GameOver();
            }
            return;
        }

        if (gameTime > maxGameTime) // 일반 생존 시간이 끝나면 보스전으로 전환.
        {
            gameTime = maxGameTime; // HUD에 남은 시간이 음수로 표시되지 않도록 최대 시간으로 고정.
            StartBossBattle();
        }
    }

    // 일반 생존 시간이 끝났을 때 보스전 시작 코루틴을 실행하는 함수.
    void StartBossBattle()
    {
        if (isBossBattle)   // 이미 보스전 상태라면 중복 실행 방지.
            return;

        StartCoroutine(BossBattleRoutine());
    }

    // 보스 소환 연출 : 잡몹 정리, 경고문구, 보스 소환, 카메라 줌과 UI 표시를 순서대로 연출.
    IEnumerator BossBattleRoutine()
    {
        isBossBattle = true;    // HUD, Spawner, BossAttack 등에서 보스전 상태를 확인할 수 있게 true로 변경.
        bossTime = bossBattleTime;  // 보스전 제한시간을 인스펙터 설정값으로 초기화.

        // 기존 잡몹을 정리한 뒤 보스 등장 연출 동안 게임 시간을 멈춘다.
        if (enemyCleaner != null)
        {
            enemyCleaner.SetActive(true);   // EnemyCleaner 활성화, 필드에 남아있는 잡몹 정리.
            yield return new WaitForSeconds(0.2f);  // 정리 로직이 한 번 실행될 짧은 시간 대기.
            enemyCleaner.SetActive(false);  // 보스전 중 계속 켜져있지 않도록 다시 비활성화.
        }

        float prevTimeScale = Time.timeScale;   // 보스 등장 연출 후 원래 시간 배율로 복구하기 위해 저장.
        Time.timeScale = 0f;    // 보스 등장 연출 동안 게임 일시정지.
        bool wasHealthActive = uiHealth != null && uiHealth.activeSelf; // Health UI가 원래 켜져 있었는지 저장.
        if (uiHealth != null)   // 보스 등장 연출 중 Health UI가 화면을 가리지 않도록 숨기기.
        {
            uiHealth.SetActive(false);
        }

        // 경고 연출 후 보스를 생성하고 BossHP UI와 카메라 줌인을 함께 진행.
        yield return StartCoroutine(BossWarningRoutine());

        Enemy boss = null;
        if (spawner != null)    // Spawner가 연결되어 있으면 보스 프리팹을 풀에서 생성.
        {
            boss = spawner.SpawnBossMonster();
        }

        if (uiBossHP != null)   // 보스 HP UI를 활성화하고 체력바 채우기 연출 시작.
        {
            uiBossHP.Show(boss);
        }

        yield return StartCoroutine(BossZoomRoutine(boss != null ? boss.transform : null)); // 보스 위치로 줌인 후 플레이어 위치로 복귀.

        if (uiHealth != null)   // 보스 등장 연출 전 Health UI 상태로 복구.
        {
            uiHealth.SetActive(wasHealthActive);
        }
        Time.timeScale = prevTimeScale > 0f ? prevTimeScale : 1f;   // 저장해둔 시간 배율로 복구. 0 이하라면 1로 보정.
    }

    // 카메라 추적 스크립트를 잠시 끄고 보스 위치로 줌인했다가 플레이어 중심으로 복귀하는 코루틴.
    IEnumerator BossZoomRoutine(Transform bossTarget)
    {
        Camera mainCamera = Camera.main;   // 현재 씬의 메인 카메라 가져오기.
        if (mainCamera == null || bossTarget == null)  // 카메라 또는 보스 타겟이 없으면 줌 연출 생략.
            yield break;

        if (defaultCameraSize <= 0f)    // 기본 카메라 크기가 저장되지 않았다면 현재 값을 기본값으로 사용.
        {
            defaultCameraSize = mainCamera.orthographicSize;
        }

        Transform cameraTransform = mainCamera.transform;  // 카메라 위치 이동을 위한 Transform.
        Transform originalParent = cameraTransform.parent; // 연출이 끝난 뒤 원래 부모 오브젝트로 되돌리기 위해 저장.
        Vector3 originalLocalPosition = cameraTransform.localPosition;  // 원래 localPosition 저장.
        Quaternion originalLocalRotation = cameraTransform.localRotation;   // 원래 localRotation 저장.
        Behaviour[] cameraBehaviours = mainCamera.GetComponents<Behaviour>();  // 카메라에 붙은 추적 스크립트들을 가져오기.
        List<Behaviour> disabledCameraBehaviours = new List<Behaviour>();  // 줌 연출 중 껐다가 다시 켤 스크립트 목록.

        for (int i = 0; i < cameraBehaviours.Length; i++)  // 카메라에 붙은 Behaviour 컴포넌트 검사.
        {
            Behaviour behaviour = cameraBehaviours[i];
            if (behaviour == null || !behaviour.enabled || behaviour == mainCamera || behaviour is AudioListener) // Camera와 AudioListener는 끄면 안되므로 제외.
                continue;

            disabledCameraBehaviours.Add(behaviour);   // 나중에 다시 켜기 위해 목록에 저장.
            behaviour.enabled = false; // 카메라 추적 스크립트가 줌인 연출을 방해하지 않도록 잠시 비활성화.
        }

        cameraTransform.SetParent(null, true);   // 부모 오브젝트의 이동 영향을 받지 않도록 카메라를 임시로 분리.

        float startSize = mainCamera.orthographicSize; // 줌 시작 시점의 카메라 크기.
        float targetSize = bossZoomSize;   // 인스펙터에서 설정한 보스 줌인 목표 크기.
        if (targetSize >= startSize)   // 목표 크기가 현재보다 크면 줌인이 아니라 줌아웃이 되므로 보정.
        {
            targetSize = startSize * 0.55f;
        }
        Vector3 startPos = cameraTransform.position;   // 줌인 시작 위치 저장.
        Vector3 bossPos = new Vector3(bossTarget.position.x, bossTarget.position.y, startPos.z);   // 카메라 z값은 유지하고 x,y만 보스 위치로 이동.
        float zoomDuration = uiBossHP != null ? Mathf.Max(0.1f, uiBossHP.fillDuration) : bossZoomDuration;  // BossHP 채우기 시간과 줌인 시간을 맞춤.
        float elapsed = 0f; // 줌인 진행 시간.

        while (elapsed < zoomDuration)  // zoomDuration 동안 보스 위치로 카메라 이동 및 확대.
        {
            elapsed += Time.unscaledDeltaTime;  // Time.timeScale이 0인 상태에서도 연출이 진행되도록 unscaledDeltaTime 사용.
            float t = Mathf.Clamp01(elapsed / zoomDuration);   // 0~1 사이 보간값.
            mainCamera.orthographicSize = Mathf.Lerp(startSize, targetSize, t); // 카메라 크기를 목표 크기로 보간.
            cameraTransform.position = Vector3.Lerp(startPos, bossPos, t);  // 카메라 위치를 보스 위치로 보간.
            yield return null;
        }

        yield return new WaitForSecondsRealtime(bossZoomHoldTime);  // 보스를 잠시 보여주는 유지 시간.

        Vector3 playerPos = new Vector3(player.transform.position.x, player.transform.position.y, startPos.z); // 복귀할 플레이어 중심 위치.
        elapsed = 0f;   // 복귀 연출 시간 초기화.

        while (elapsed < bossZoomReturnDuration)    // bossZoomReturnDuration 동안 플레이어 위치로 복귀.
        {
            elapsed += Time.unscaledDeltaTime;  // Time.timeScale이 0인 상태에서도 복귀 연출 진행.
            float t = Mathf.Clamp01(elapsed / bossZoomReturnDuration);  // 0~1 사이 보간값.
            mainCamera.orthographicSize = Mathf.Lerp(targetSize, defaultCameraSize, t); // 카메라 크기를 기본 크기로 복구.
            cameraTransform.position = Vector3.Lerp(bossPos, playerPos, t);    // 카메라 위치를 플레이어 위치로 복구.
            yield return null;
        }

        mainCamera.orthographicSize = defaultCameraSize;   // 오차 방지를 위해 최종 카메라 크기 고정.
        cameraTransform.SetParent(originalParent, true);   // 원래 부모 오브젝트로 복귀.
        cameraTransform.localPosition = originalLocalPosition;  // 원래 localPosition 복구.
        cameraTransform.localRotation = originalLocalRotation;  // 원래 localRotation 복구.

        for (int i = 0; i < disabledCameraBehaviours.Count; i++)   // 줌 연출 중 꺼둔 카메라 스크립트들을 다시 활성화.
        {
            if (disabledCameraBehaviours[i] != null)
            {
                disabledCameraBehaviours[i].enabled = true;
            }
        }
    }

    // 보스 등장 전 빨간 오버레이와 경고 이미지를 깜빡이게 출력하는 코루틴.
    IEnumerator BossWarningRoutine()
    {
        EnsureBossWarningUI();  // 보스 경고 UI가 없으면 생성하고, 있으면 숨김 상태로 초기화.

        if (bossWarningOverlay == null) // Canvas가 없어서 오버레이를 만들 수 없다면 경고 연출 생략.
            yield break;

        if (bossWarningImage != null)   // 경고 이미지 오브젝트가 있으면 스프라이트와 크기 갱신.
        {
            bossWarningImage.sprite = bossWarningSprite;   // 인스펙터에 넣은 WARNING 스프라이트 적용.
            bossWarningImage.SetNativeSize();  // 원본 이미지 크기 기준으로 먼저 맞춤.
            RectTransform warningRect = bossWarningImage.GetComponent<RectTransform>(); // UI 크기 조절용 RectTransform 가져오기.
            warningRect.sizeDelta = bossWarningImageSize;  // 인스펙터에서 지정한 최종 출력 크기 적용.
            bossWarningImage.gameObject.SetActive(true);   // 깜빡임 시작 전 이미지 활성화.
        }

        float elapsed = 0f; // 경고 연출 진행 시간.
        bool visible = false;   // 오버레이와 경고 이미지의 현재 표시 상태.

        while (elapsed < bossWarningDuration)   // bossWarningDuration 동안 경고 UI 깜빡임 반복.
        {
            visible = !visible; // 표시 상태 반전.
            bossWarningOverlay.gameObject.SetActive(visible);  // 빨간 오버레이 표시/숨김.
            if (bossWarningImage != null)   // 경고 이미지도 오버레이와 같은 타이밍으로 표시/숨김.
            {
                bossWarningImage.gameObject.SetActive(visible);
            }

            yield return new WaitForSecondsRealtime(bossWarningBlinkInterval);  // Time.timeScale이 0이어도 실제 시간 기준으로 대기.
            elapsed += bossWarningBlinkInterval;    // 깜빡임 간격만큼 진행 시간 증가.
        }

        HideBossWarningUI();    // 경고 연출이 끝나면 UI 숨기기.
    }

    // 보스 경고 UI가 인스펙터에 연결되어 있지 않으면 런타임에 자동 생성하는 함수.
    void EnsureBossWarningUI()
    {
        if (bossWarningOverlay == null) // 빨간 오버레이 이미지가 없다면 Canvas 아래에 새로 생성.
        {
            GameObject canvas = GameObject.Find("Canvas"); // UI를 붙일 Canvas 오브젝트 검색.
            if (canvas == null) // Canvas가 없으면 UI 생성 불가, 함수 종료.
                return;

            GameObject overlayObject = new GameObject("BossWarningOverlay");   // 빨간색 화면 깜빡임용 오브젝트 생성.
            overlayObject.transform.SetParent(canvas.transform, false);    // Canvas 하위 UI로 배치.
            bossWarningOverlay = overlayObject.AddComponent<Image>();  // Image 컴포넌트를 붙여 화면 전체 색상 표시.
            bossWarningOverlay.color = new Color(1f, 0f, 0f, 0.25f);   // 투명한 빨간색 오버레이.

            RectTransform overlayRect = overlayObject.GetComponent<RectTransform>();    // 오버레이 크기 조절용 RectTransform.
            overlayRect.anchorMin = Vector2.zero;   // 화면 좌하단 기준으로 앵커 설정.
            overlayRect.anchorMax = Vector2.one;    // 화면 우상단 기준으로 앵커 설정.
            overlayRect.offsetMin = Vector2.zero;   // 좌하단 여백 0.
            overlayRect.offsetMax = Vector2.zero;   // 우상단 여백 0, Canvas 전체를 덮도록 설정.
        }

        if (bossWarningImage != null)   // 경고 이미지가 이미 있으면 중복 생성하지 않고 숨김 처리만.
        {
            HideBossWarningUI();
            return;
        }

        GameObject imageObject = new GameObject("BossWarningImage");   // WARNING 스프라이트를 표시할 오브젝트 생성.
        imageObject.transform.SetParent(bossWarningOverlay.transform, false);   // 오버레이 하위에 배치해서 함께 켜고 끄기 쉽게 구성.
        bossWarningImage = imageObject.AddComponent<Image>();  // 경고 이미지를 표시할 Image 컴포넌트 추가.
        bossWarningImage.sprite = bossWarningSprite;   // 인스펙터에 설정된 경고 스프라이트 적용.
        bossWarningImage.preserveAspect = true;    // 이미지 비율 유지.

        RectTransform imageRect = imageObject.GetComponent<RectTransform>();    // 경고 이미지 위치와 크기 조절용 RectTransform.
        imageRect.anchorMin = new Vector2(0.5f, 0.5f);   // 화면 중앙에 고정.
        imageRect.anchorMax = new Vector2(0.5f, 0.5f);   // 화면 중앙에 고정.
        imageRect.pivot = new Vector2(0.5f, 0.5f);   // 이미지 중심 기준으로 배치.
        imageRect.anchoredPosition = Vector2.zero;   // 중앙 위치.
        imageRect.sizeDelta = bossWarningImageSize;  // 인스펙터에서 지정한 경고 이미지 크기 적용.

        HideBossWarningUI();    // 생성 직후에는 보이지 않게 숨김 상태로 초기화.
    }

    // 보스 경고 오버레이와 이미지를 비활성화하는 함수.
    void HideBossWarningUI()
    {
        if (bossWarningOverlay != null) // 빨간 오버레이가 있으면 숨기기.
        {
            bossWarningOverlay.gameObject.SetActive(false);
        }

        if (bossWarningImage != null)   // 경고 이미지가 있으면 숨기기.
        {
            bossWarningImage.gameObject.SetActive(false);
        }
    }

    // 보스 사망 연출용, 보스 위치 주변에 폭발 VFX를 풀에서 꺼내 생성하고 재생하는 함수.
    void SpawnBossDeathExplosion(Vector3 position)
    {
        if (bossDeathExplosionFxPrefabId < 0 || pool == null)   // 풀 ID 체크. 폭발VFX 프리팹이 설정되지 않거나 or pollManager가 없으면 실행 중단.
            return;

        GameObject fx = pool.Get(bossDeathExplosionFxPrefabId); // 풀에서 폭발 이펙트 꺼내기(오브젝트 풀링)
        if (fx == null) // 풀에서 오브젝트를 못가져오면 실행 중단.
            return;

        fx.transform.position = position;   // 폭발 위치 지정. 폭발 이펙트의 위치 이동.
        BombExplosionFX explosion = fx.GetComponent<BombExplosionFX>(); // BombExplosionFX 가져오기
        if (explosion != null)      // 폭발 컴포넌트가 있으면 재생.
        {
            explosion.PlayExplosion(bossDeathExplosionSize);    // bossDeathExplosionSize값이 높을수록 이펙트가 커짐.
        }
    }

    // 보스 처치 보상 코인을 보스 주변에서 사방으로 튕겨나가게 생성하는 함수.
    void SpawnBossRewardCoins(Vector3 bossPosition)
    {
        if (bossRewardCoinPrefabId < 0 || pool == null) // coin프리팹이 없거나 pollManager가 없으면 실행 중단(오류방지)
            return;

        int coinCount = Mathf.Max(0, bossShopCurrencyReward);   // 생성될 코인의 갯수 설정(bossShopCurrencyReward의 값만큼 반복)
        for (int i = 0; i < coinCount; i++) // 코인 생성루프
        {
            GameObject coin = pool.Get(bossRewardCoinPrefabId); // 오브젝트 풀에서 코인 꺼내기.
            if (coin == null)   // 만약 코인이 없더라도(풀 오류 발생 시) 다음 코인 생성 진행 (오류방지)
                continue;

            coin.transform.position = bossPosition; // 코인의 위치는 보스의 위치에서 생성
            ItemPickup pickup = coin.GetComponent<ItemPickup>();    // 코인 이동과 획득 처리를 담당하는 컴포넌트 ItemPickup 가져오기.
            if (pickup != null)
            {
                if (bossRewardCoinSprite != null)
                {
                    pickup.coinSprite = bossRewardCoinSprite;   // 코인 스프라이트 교체, 일반코인과 보스 전용코인 스프라이트를 다르게 사용 가능하도록 조정.
                }

                pickup.InitPickup(ItemPickup.PickupType.Coin, 0);   // pickup 초기화, 생성된 오브젝트의 타입은 Coin이다.
                Vector3 randomOffset = Quaternion.Euler(0, 0, Random.Range(0f, 360f)) * Vector3.up * Random.Range(bossRewardCoinRadius * 0.5f, bossRewardCoinRadius);   // 랜덤 방향 계산, 360도 랜덤 방향 * bossRewardCoinRadius/2 ~ bossRewardCoinRadius 사이거리 랜덤 거리 설정.
                pickup.StartBounce(bossPosition, bossPosition + randomOffset, bossRewardCoinBounceDuration);    // 코인이 튕겨나가는 연출.(시작점,도착점,시간)
            }
        }
    }

    // 보스 사망 연출 중 카메라를 짧게 흔든 뒤 원래 위치로 복귀하는 코루틴.
    IEnumerator BossDeathShakeRoutine()
    {
        Camera mainCamera = Camera.main;    // 메인 카메라 가져오기.
        if (mainCamera == null) // 만약 카메라가 없으면, 코루틴 즉시종료(오류방지)
            yield break;

        Transform cameraTransform = mainCamera.transform;   // camera의 위치 저장.
        Vector3 origin = cameraTransform.position;  // 원래 위치 저장. 흔들림이 끝났을 때 원래 위치로 복구하기 위함.
        float elapsed = 0f; // 화면 흔들림 진행시간을 측정.

        while (elapsed < bossDeathShakeDuration)    // 루프, bossDeathShakeDuration 의 값(시간)만큼 화면 흔들림 지속.
        {
            Vector2 shake = Random.insideUnitCircle * bossDeathShakePower;  // bossDeathShakePower의 범위내 랜덤값만큼 매 프레임마다 화면 흔들림.
            cameraTransform.position = origin + new Vector3(shake.x, shake.y, 0f);  // 원래 위치(origine)에서 shake에서 생성된 랜덤값 만큼 카메라 이동(흔들림 연출)
            elapsed += Time.unscaledDeltaTime;  // 보스 사망 연출에서 타임스케일 감소(게임시간 저배속)를 사용중이라 Time.deltaTime 사용 안함. Time.unscaledDeltaTime : 슬로우 모션 영향 안 받음
            yield return null;  // 다음 프레임까지 대기. 매 프레임 카메라 위치 랜덤 변경.
        }

        cameraTransform.position = origin;  // bossDeathShakeDuration 의 값만큼 루프가 끝난 후 원래 위치로 카메라 이동.
    }

    // 경험치를 획득하고, 필요 경험치를 넘긴 만큼 레벨업 UI를 반복 호출.
    public void GetExp(int amount = 1)    // 경험치 증가함수 작성
    {
        if (!isLive)    // Player가 isLive상태가 아니라면 무시.
            return;

        exp += amount;

        while (exp >= GetRequiredExp(level))  // exp가 현재 레벨의 최대경험치와 같아지면
        // Mathf.Min 함수를 사용해 최고 경험치를 그대로 사용하도록 변경 (영상12 40:40)
        {
            exp -= GetRequiredExp(level);
            level ++;   // level + 1
            uiLevelUp.Show();   // LevelUp UI 출력
            if (!isLive)
                break;
        }
    }

    public void Stop()  // 시간을 정지시키는 함수.
    {
        isLive = false;
        Time.timeScale = 0;  // timeScale : 유니티의 시간속도(배율)
    }

    public void Resume()    // 시간을 작동시키는 함수.
    {
        isLive = true;
        Time.timeScale = 1;
    }

    // 일시정지 버튼에서 호출되는 토글 함수.
    public void TogglePause()
    {
        if (isPaused)
        {
            ResumePause();
        }
        else
        {
            PauseGame();
        }
    }

    // 게임 진행 중 일시정지 UI를 열고 시간을 정지시키는 함수.
    public void PauseGame()
    {
        if (!isLive || Time.timeScale == 0f)    // player가 isLive 상태가 아니거나, 이미 일시정지 상태(timeScale==0)라면 실행중단.
            return;

        isPaused = true;    // GameManager에서 일시정지 상태를 관리하는 값 true로 변경.
        if (uiPause != null)    // 일시정지 UI가 null 상태가 아니라면 일시정지 UI 표시.
        {
            uiPause.Show();
        }
        Stop(); // 실제 시간정지 함수 호출 (timeScale == 0)
    }

    // 일시정지 UI를 닫고 게임 시간을 다시 진행시키는 함수.
    public void ResumePause()
    {
        if (!isPaused)  // 일시정지 상태가 아니면 실행중단.
            return;

        isPaused = false;   // GameManager에서 일시정지 상태를 관리하는 값 false로 변경.
        if (uiPause != null)    // 일시정지 UI가 null 상태가 아니라면 일시정지 UI 숨기기.
        {
            uiPause.Hide();
        }
        Resume();   // 실제 시간 재개 함수 호출 (timeScale == 1)
    }
}
