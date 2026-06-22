using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;  // 장면관리를 사용하기위해 SceneManagement 네임스페이스 추가
using UnityEngine.UI;


/// 게임 시작과 종료, 경험치, 상점 저장값, 일시정지 및 보스전의 흐름 등. 게임의 전반적인 관리를 하는 스크립트.

public class GameManager : MonoBehaviour
{
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

    float defaultCameraSize;
    bool isBossDeathRoutine;


    void Awake()
    {
        instance = this;    // 인스턴스 변수를 자기자신으로 초기화
        LoadShopData();
        if (spawner == null)
        {
            spawner = FindObjectOfType<Spawner>();
        }
        if (uiHealth == null)
        {
            uiHealth = GameObject.Find("Canvas/HUD/Health");
        }
        EnsureBossWarningUI();
        SetShopButtonActive(true);
        SetPauseButtonActive(false);
    }

    public void GameStart(int id)   // 게임 시작 함수에 Player의 ID 매개변수 추가
    {
        playerId = id;
        SetShopButtonActive(false);
        SetPauseButtonActive(true);
        isPaused = false;
        if (uiPause != null)
        {
            uiPause.Hide();
        }
        ApplyShopStats();
        health = maxHealth; // 게임 시작시 현재체력을 최대체력으로 초기화
        kill = 0;
        gameTime = 0f;
        bossTime = bossBattleTime;
        isBossBattle = false;
        isBossCleared = false;

        player.gameObject.SetActive(true);  // 게임 시작시 Player 활성화 후 기본무기 지급
        if (uiBossHP != null)
        {
            uiBossHP.Hide();
        }

        if (Camera.main != null)
        {
            defaultCameraSize = Camera.main.orthographicSize;
        }
        HideBossWarningUI();

        uiLevelUp.Select(playerId % 2);
        Resume();

        AudioManager.instance.PlayBgm(true);    // 게임 시작부분에 PlayBgm 함수 호출
        AudioManager.instance.PlaySfx(AudioManager.Sfx.Select); // 캐릭터 선택버튼 클릭시 효과음 재생
    }

    public void GameOver()  // 코루틴 없이 바로 Stop함수 실행 시, Player의 Dead 애니메이션 출력 전에 멈춰버림
    {
        StartCoroutine(GameOverRoutine());
    }

    IEnumerator GameOverRoutine()   // 게임오버의 딜레이를 위해 코루틴 작성
    {
        PrepareGameEnd();

        yield return new WaitForSeconds(0.5f);  // 0.5초 기다리기

        uiResult.gameObject.SetActive(true);   // 게임결과 UI 활성화
        uiResult.Lose();    // 이미지 오브젝트를 활성화하는 패배 함수 호출
        SetShopButtonActive(true);
        Stop();

        AudioManager.instance.PlayBgm(false);   // 게임 종료시 Bgm종료
        AudioManager.instance.PlaySfx(AudioManager.Sfx.Lose); // 게임 오버시 효과음 재생
    }

    public void GameVictory()  // 코루틴 없이 바로 Stop함수 실행 시, Player의 Dead 애니메이션 출력 전에 멈춰버림
    {
        StartCoroutine(GameVictoryRoutine());
    }

    public void BossDead(Vector3 bossPosition)
    {
        if (isBossDeathRoutine)
            return;

        isBossCleared = true;
        StartCoroutine(BossDeathRoutine(bossPosition));
    }

    // 보스 사망 시 슬로우, 폭발, 화면 흔들림과 보상 생성을 진행한다.
    IEnumerator BossDeathRoutine(Vector3 bossPosition)
    {
        isBossDeathRoutine = true;

        if (uiBossHP != null)
        {
            uiBossHP.Hide();
        }

        float prevTimeScale = Time.timeScale;
        Time.timeScale = bossDeathSlowScale;

        StartCoroutine(BossDeathShakeRoutine());
        SpawnBossRewardCoins(bossPosition);

        for (int i = 0; i < bossDeathExplosionCount; i++)
        {
            Vector2 randomPos = Random.insideUnitCircle * bossDeathExplosionRadius;
            SpawnBossDeathExplosion(bossPosition + new Vector3(randomPos.x, randomPos.y, 0f));
            yield return new WaitForSecondsRealtime(bossDeathSlowDuration / Mathf.Max(1, bossDeathExplosionCount));
        }

        yield return new WaitForSecondsRealtime(bossDeathSlowDuration);

        Time.timeScale = prevTimeScale > 0f ? prevTimeScale : 1f;
        isBossDeathRoutine = false;
        GameVictory();
    }

    IEnumerator GameVictoryRoutine()   // 게임오버의 딜레이를 위해 코루틴 작성
    {
        PrepareGameEnd();
        if (enemyCleaner != null)
        {
            enemyCleaner.SetActive(true);   //게임 승리 코루틴 전반부에 적 클리너 활성화
        }

        yield return new WaitForSeconds(0.5f);  // 0.5초 기다리기

        uiResult.gameObject.SetActive(true);   // 게임결과 UI 활성화
        uiResult.Win();    // 이미지 오브젝트를 활성화하는 승리 함수 호출
        SetShopButtonActive(true);
        Stop();

        
        AudioManager.instance.PlayBgm(false);   // 게임 종료시 Bgm종료
        AudioManager.instance.PlaySfx(AudioManager.Sfx.Win); // 게임 승리시 효과음 재생
    }

    public void GameRetry() // 게임 재시작 함수 작성
    {
        SceneManager.LoadScene(0);    // LoadScene이름 혹은 인덱스로 장면을 새롭게 부르는 함수
    }

    void SetShopButtonActive(bool active)
    {
        if (shopButton != null)
        {
            shopButton.SetActive(active);
        }
    }

    void SetPauseButtonActive(bool active)
    {
        if (pauseButton != null)
        {
            pauseButton.SetActive(active);
        }
    }

    // 승리와 패배에 공통으로 필요한 UI 및 시간 상태를 정리.
    void PrepareGameEnd()
    {
        isLive = false;
        isBossBattle = false;
        isPaused = false;
        SetPauseButtonActive(false);

        if (uiPause != null)
        {
            uiPause.Hide();
        }

        if (uiBossHP != null)
        {
            uiBossHP.Hide();
        }
    }

    public void AddShopCurrency(int amount)
    {
        shopCurrency += Mathf.Max(0, amount);
        PlayerPrefs.SetInt("ShopCurrency", shopCurrency);
    }

    public bool TryBuyShopUpgrade(ShopUpgradeType type, int maxLevel, int baseCost, int costIncrease)
    {
        int index = (int)type;
        EnsureShopUpgradeArray();

        if (shopUpgradeLevels[index] >= maxLevel)
            return false;

        int cost = GetShopUpgradeCost(type, baseCost, costIncrease);
        if (shopCurrency < cost)
            return false;

        shopCurrency -= cost;
        shopUpgradeLevels[index]++;
        SaveShopData();
        ApplyShopStats();
        return true;
    }

    public int GetShopUpgradeLevel(ShopUpgradeType type)
    {
        EnsureShopUpgradeArray();
        return shopUpgradeLevels[(int)type];
    }

    public int GetShopUpgradeCost(ShopUpgradeType type, int baseCost, int costIncrease)
    {
        return baseCost + GetShopUpgradeLevel(type) * costIncrease;
    }

    // 상점의 경험치 비용 감소 효과를 적용하되 최소 요구치는 1로 보정.
    public int GetRequiredExp(int levelIndex)
    {
        int baseExp = nextExp[Mathf.Min(levelIndex, nextExp.Length - 1)];
        float discount = ShopLevelUpCostDiscount;
        return Mathf.Max(1, Mathf.CeilToInt(baseExp * (1f - discount)));
    }

    public float ShopMoveSpeedRate
    {
        get { return GetShopUpgradeLevel(ShopUpgradeType.MoveSpeed) * shopMoveSpeedBonus; }
    }

    public float ShopDamageRate
    {
        get { return GetShopUpgradeLevel(ShopUpgradeType.Damage) * shopDamageBonus; }
    }

    public float ShopAttackSpeedRate
    {
        get { return Mathf.Clamp(GetShopUpgradeLevel(ShopUpgradeType.AttackSpeed) * shopAttackSpeedBonus, 0f, 0.45f); }
    }

    public float ShopEnemySpawnRate
    {
        get { return Mathf.Clamp(1f - GetShopUpgradeLevel(ShopUpgradeType.EnemySpawnTime) * shopEnemySpawnTimeReduction, 0.5f, 1f); }
    }

    public float ShopBoxDropChanceBonus
    {
        get { return Mathf.Clamp(GetShopUpgradeLevel(ShopUpgradeType.RandomBoxChance) * shopRandomBoxChanceBonus, 0f, 0.15f); }
    }

    public float ShopPickupRangeBonus
    {
        get { return GetShopUpgradeLevel(ShopUpgradeType.PickupRange) * shopPickupRangeBonus; }
    }

    public float ShopLevelUpCostDiscount
    {
        get { return Mathf.Clamp(GetShopUpgradeLevel(ShopUpgradeType.LevelUpCost) * shopLevelUpCostDiscount, 0f, 0.3f); }
    }

    public float ShopMaxHealthRate
    {
        get { return GetShopUpgradeLevel(ShopUpgradeType.MaxHealth) * shopMaxHealthBonus; }
    }

    public float ShopEnemySpawnTimeReductionRate
    {
        get { return 1f - ShopEnemySpawnRate; }
    }

    // 저장된 상점 강화 수치를 현재 게임에 사용하는 보정값으로 변환.
    public void ApplyShopStats()
    {
        maxHealth = baseMaxHealth * (1f + ShopMaxHealthRate);
    }

    void LoadShopData()
    {
        EnsureShopUpgradeArray();
        shopCurrency = PlayerPrefs.GetInt("ShopCurrency", 0);
        for (int i = 0; i < shopUpgradeLevels.Length; i++)
        {
            shopUpgradeLevels[i] = PlayerPrefs.GetInt("ShopUpgrade_" + i, 0);
        }
        ApplyShopStats();
    }

    void SaveShopData()
    {
        EnsureShopUpgradeArray();
        PlayerPrefs.SetInt("ShopCurrency", shopCurrency);
        for (int i = 0; i < shopUpgradeLevels.Length; i++)
        {
            PlayerPrefs.SetInt("ShopUpgrade_" + i, shopUpgradeLevels[i]);
        }
        PlayerPrefs.Save();
    }

    void EnsureShopUpgradeArray()
    {
        int count = System.Enum.GetValues(typeof(ShopUpgradeType)).Length;
        if (shopUpgradeLevels == null || shopUpgradeLevels.Length != count)
        {
            int[] oldLevels = shopUpgradeLevels;
            shopUpgradeLevels = new int[count];
            if (oldLevels != null)
            {
                for (int i = 0; i < Mathf.Min(oldLevels.Length, shopUpgradeLevels.Length); i++)
                {
                    shopUpgradeLevels[i] = oldLevels[i];
                }
            }
        }
    }

    void Update()
    {
        if (!isLive)
            return;

        gameTime += Time.deltaTime;    // 타이머 변수에 deltaTime 계속 더하기
        if (isBossBattle)
        {
            bossTime -= Time.deltaTime;
            if (bossTime <= 0f)
            {
                bossTime = 0f;
                GameOver();
            }
            return;
        }

        if (gameTime > maxGameTime)
        {
            gameTime = maxGameTime;
            StartBossBattle();
        }
    }

    void StartBossBattle()
    {
        if (isBossBattle)
            return;

        StartCoroutine(BossBattleRoutine());
    }

    // 보스 소환 연출 : 잡몹 정리, 경고문구, 보스 소환, 카메라 줌과 UI 표시를 순서대로 연출.
    IEnumerator BossBattleRoutine()
    {
        isBossBattle = true;
        bossTime = bossBattleTime;

        if (enemyCleaner != null)
        {
            enemyCleaner.SetActive(true);
            yield return new WaitForSeconds(0.2f);
            enemyCleaner.SetActive(false);
        }

        float prevTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        bool wasHealthActive = uiHealth != null && uiHealth.activeSelf;
        if (uiHealth != null)
        {
            uiHealth.SetActive(false);
        }

        yield return StartCoroutine(BossWarningRoutine());

        Enemy boss = null;
        if (spawner != null)
        {
            boss = spawner.SpawnBossMonster();
        }

        if (uiBossHP != null)
        {
            uiBossHP.Show(boss);
        }

        yield return StartCoroutine(BossZoomRoutine(boss != null ? boss.transform : null));

        if (uiHealth != null)
        {
            uiHealth.SetActive(wasHealthActive);
        }
        Time.timeScale = prevTimeScale > 0f ? prevTimeScale : 1f;
    }

    IEnumerator BossZoomRoutine(Transform bossTarget)
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null || bossTarget == null)
            yield break;

        if (defaultCameraSize <= 0f)
        {
            defaultCameraSize = mainCamera.orthographicSize;
        }

        Transform cameraTransform = mainCamera.transform;
        Transform originalParent = cameraTransform.parent;
        Vector3 originalLocalPosition = cameraTransform.localPosition;
        Quaternion originalLocalRotation = cameraTransform.localRotation;
        Behaviour[] cameraBehaviours = mainCamera.GetComponents<Behaviour>();
        List<Behaviour> disabledCameraBehaviours = new List<Behaviour>();

        for (int i = 0; i < cameraBehaviours.Length; i++)
        {
            Behaviour behaviour = cameraBehaviours[i];
            if (behaviour == null || !behaviour.enabled || behaviour == mainCamera || behaviour is AudioListener)
                continue;

            disabledCameraBehaviours.Add(behaviour);
            behaviour.enabled = false;
        }

        cameraTransform.SetParent(null, true);

        float startSize = mainCamera.orthographicSize;
        float targetSize = bossZoomSize;
        if (targetSize >= startSize)
        {
            targetSize = startSize * 0.55f;
        }
        Vector3 startPos = cameraTransform.position;
        Vector3 bossPos = new Vector3(bossTarget.position.x, bossTarget.position.y, startPos.z);
        float zoomDuration = uiBossHP != null ? Mathf.Max(0.1f, uiBossHP.fillDuration) : bossZoomDuration;
        float elapsed = 0f;

        while (elapsed < zoomDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / zoomDuration);
            mainCamera.orthographicSize = Mathf.Lerp(startSize, targetSize, t);
            cameraTransform.position = Vector3.Lerp(startPos, bossPos, t);
            yield return null;
        }

        yield return new WaitForSecondsRealtime(bossZoomHoldTime);

        Vector3 playerPos = new Vector3(player.transform.position.x, player.transform.position.y, startPos.z);
        elapsed = 0f;

        while (elapsed < bossZoomReturnDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / bossZoomReturnDuration);
            mainCamera.orthographicSize = Mathf.Lerp(targetSize, defaultCameraSize, t);
            cameraTransform.position = Vector3.Lerp(bossPos, playerPos, t);
            yield return null;
        }

        mainCamera.orthographicSize = defaultCameraSize;
        cameraTransform.SetParent(originalParent, true);
        cameraTransform.localPosition = originalLocalPosition;
        cameraTransform.localRotation = originalLocalRotation;

        for (int i = 0; i < disabledCameraBehaviours.Count; i++)
        {
            if (disabledCameraBehaviours[i] != null)
            {
                disabledCameraBehaviours[i].enabled = true;
            }
        }
    }

    IEnumerator BossWarningRoutine()
    {
        EnsureBossWarningUI();

        if (bossWarningOverlay == null)
            yield break;

        if (bossWarningImage != null)
        {
            bossWarningImage.sprite = bossWarningSprite;
            bossWarningImage.SetNativeSize();
            RectTransform warningRect = bossWarningImage.GetComponent<RectTransform>();
            warningRect.sizeDelta = bossWarningImageSize;
            bossWarningImage.gameObject.SetActive(true);
        }

        float elapsed = 0f;
        bool visible = false;

        while (elapsed < bossWarningDuration)
        {
            visible = !visible;
            bossWarningOverlay.gameObject.SetActive(visible);
            if (bossWarningImage != null)
            {
                bossWarningImage.gameObject.SetActive(visible);
            }

            yield return new WaitForSecondsRealtime(bossWarningBlinkInterval);
            elapsed += bossWarningBlinkInterval;
        }

        HideBossWarningUI();
    }

    void EnsureBossWarningUI()
    {
        if (bossWarningOverlay == null)
        {
            GameObject canvas = GameObject.Find("Canvas");
            if (canvas == null)
                return;

            GameObject overlayObject = new GameObject("BossWarningOverlay");
            overlayObject.transform.SetParent(canvas.transform, false);
            bossWarningOverlay = overlayObject.AddComponent<Image>();
            bossWarningOverlay.color = new Color(1f, 0f, 0f, 0.25f);

            RectTransform overlayRect = overlayObject.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;
        }

        if (bossWarningImage != null)
        {
            HideBossWarningUI();
            return;
        }

        GameObject imageObject = new GameObject("BossWarningImage");
        imageObject.transform.SetParent(bossWarningOverlay.transform, false);
        bossWarningImage = imageObject.AddComponent<Image>();
        bossWarningImage.sprite = bossWarningSprite;
        bossWarningImage.preserveAspect = true;

        RectTransform imageRect = imageObject.GetComponent<RectTransform>();
        imageRect.anchorMin = new Vector2(0.5f, 0.5f);
        imageRect.anchorMax = new Vector2(0.5f, 0.5f);
        imageRect.pivot = new Vector2(0.5f, 0.5f);
        imageRect.anchoredPosition = Vector2.zero;
        imageRect.sizeDelta = bossWarningImageSize;

        HideBossWarningUI();
    }

    void HideBossWarningUI()
    {
        if (bossWarningOverlay != null)
        {
            bossWarningOverlay.gameObject.SetActive(false);
        }

        if (bossWarningImage != null)
        {
            bossWarningImage.gameObject.SetActive(false);
        }
    }

    void SpawnBossDeathExplosion(Vector3 position)
    {
        if (bossDeathExplosionFxPrefabId < 0 || pool == null)
            return;

        GameObject fx = pool.Get(bossDeathExplosionFxPrefabId);
        if (fx == null)
            return;

        fx.transform.position = position;
        BombExplosionFX explosion = fx.GetComponent<BombExplosionFX>();
        if (explosion != null)
        {
            explosion.PlayExplosion(bossDeathExplosionSize);
        }
    }

    void SpawnBossRewardCoins(Vector3 bossPosition)
    {
        if (bossRewardCoinPrefabId < 0 || pool == null)
            return;

        int coinCount = Mathf.Max(0, bossShopCurrencyReward);
        for (int i = 0; i < coinCount; i++)
        {
            GameObject coin = pool.Get(bossRewardCoinPrefabId);
            if (coin == null)
                continue;

            coin.transform.position = bossPosition;
            ItemPickup pickup = coin.GetComponent<ItemPickup>();
            if (pickup != null)
            {
                if (bossRewardCoinSprite != null)
                {
                    pickup.coinSprite = bossRewardCoinSprite;
                }

                pickup.InitPickup(ItemPickup.PickupType.Coin, 0);
                Vector3 randomOffset = Quaternion.Euler(0, 0, Random.Range(0f, 360f)) * Vector3.up * Random.Range(bossRewardCoinRadius * 0.5f, bossRewardCoinRadius);
                pickup.StartBounce(bossPosition, bossPosition + randomOffset, bossRewardCoinBounceDuration);
            }
        }
    }

    IEnumerator BossDeathShakeRoutine()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
            yield break;

        Transform cameraTransform = mainCamera.transform;
        Vector3 origin = cameraTransform.position;
        float elapsed = 0f;

        while (elapsed < bossDeathShakeDuration)
        {
            Vector2 shake = Random.insideUnitCircle * bossDeathShakePower;
            cameraTransform.position = origin + new Vector3(shake.x, shake.y, 0f);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        cameraTransform.position = origin;
    }

    public void GetExp(int amount = 1)    // 경험치 증가함수 작성
    {
        if (!isLive)
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

    public void Stop()  // 시간을 정지, 작동시키는 함수 작성
    {
        isLive = false;
        Time.timeScale = 0;  // timeScale : 유니티의 시간속도(배율)
    }

    public void Resume()
    {
        isLive = true;
        Time.timeScale = 1;
    }

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

    public void PauseGame()
    {
        if (!isLive || Time.timeScale == 0f)
            return;

        isPaused = true;
        if (uiPause != null)
        {
            uiPause.Show();
        }
        Stop();
    }

    public void ResumePause()
    {
        if (!isPaused)
            return;

        isPaused = false;
        if (uiPause != null)
        {
            uiPause.Hide();
        }
        Resume();
    }
}
