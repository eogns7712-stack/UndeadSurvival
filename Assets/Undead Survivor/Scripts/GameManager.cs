using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime.Tree;
using UnityEngine;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;  // 장면관리를 사용하기위해 SceneManagement 네임스페이스 추가
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;  // static : 정적으로 사용하겠다는 키워드, 바로 메모리에 얹어버림, 인스펙터에 나타나지않음, 정적변수는 즉시 클래스에서 호출가능
    [Header("# Game Control")]    // Header : 인스펙터의 속성들을 깔끔하게 구분시켜주는 타이틀
    public bool isLive;
    public bool isBossBattle;
    public float gameTime;
    public float maxGameTime = 2* 10;
    public float bossBattleTime = 180f;
    public float bossTime;
    [Header("# Player Info")]
    public int playerId;    // 캐릭터 ID를 저장할 변수 선언
    public float health;
    public float maxHealth = 100;
    public int level;   // 게임매니저에 레벨, 킬수, 경험치 변수 선언
    public int kill;
    public int exp;
    public int[] nextExp = { 15, 30, 60, 100, 150, 210, 280, 360, 450, 600 };   // 각 레벨의 필요 경험치를 보관할 배열 변수 선언
    [Header("# Game Object")]
    public Player player;
    public PoolManager pool;
    public LevelUp uiLevelUp;
    public BossHPUI uiBossHP;
    public Result uiResult;    // 게임 결과 UI 오브젝트를 저장할 변수 선언, 타입을 스크립트로 변경(영상13 28:30) 
    public GameObject uiHealth;
    public GameObject enemyCleaner; // 게임 승리시 적을 정리하는 클리너 변수 선언
    public Spawner spawner;
    public float bossZoomSize = 4.5f;
    public float bossZoomDuration = 1.2f;
    public float bossZoomHoldTime = 0.6f;
    public float bossZoomReturnDuration = 0.8f;
    public Image bossWarningOverlay;
    public Text bossWarningText;
    public string bossWarningMessage = "WARNING";
    public int bossWarningFontSize = 28;
    public float bossWarningDuration = 1.2f;
    public float bossWarningBlinkInterval = 0.15f;
    public int bossDeathExplosionFxPrefabId = -1;
    public int bossDeathExplosionCount = 8;
    public float bossDeathExplosionRadius = 2.5f;
    public float bossDeathExplosionSize = 2.5f;
    public float bossDeathSlowScale = 0.25f;
    public float bossDeathSlowDuration = 0.8f;
    public float bossDeathShakePower = 0.25f;
    public float bossDeathShakeDuration = 0.8f;

    float defaultCameraSize;
    bool isBossDeathRoutine;


    void Awake()
    {
        instance = this;    // 인스턴스 변수를 자기자신으로 초기화
        if (spawner == null)
        {
            spawner = FindObjectOfType<Spawner>();
        }
        if (uiHealth == null)
        {
            uiHealth = GameObject.Find("Canvas/HUD/Health");
        }
        EnsureBossWarningUI();
    }

    public void GameStart(int id)   // 게임 시작 함수에 Player의 ID 매개변수 추가
    {
        playerId = id;
        health = maxHealth; // 게임 시작시 현재체력을 최대체력으로 초기화
        gameTime = 0f;
        bossTime = bossBattleTime;
        isBossBattle = false;

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
        isLive = false;
        isBossBattle = false;
        if (uiBossHP != null)
        {
            uiBossHP.Hide();
        }

        yield return new WaitForSeconds(0.5f);  // 0.5초 기다리기

        uiResult.gameObject.SetActive(true);   // 게임결과 UI 활성화
        uiResult.Lose();    // 이미지 오브젝트를 활성화하는 패배 함수 호출
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

        StartCoroutine(BossDeathRoutine(bossPosition));
    }

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
        isLive = false;
        isBossBattle = false;
        if (uiBossHP != null)
        {
            uiBossHP.Hide();
        }
        if (enemyCleaner != null)
        {
            enemyCleaner.SetActive(true);   //게임 승리 코루틴 전반부에 적 클리너 활성화
        }

        yield return new WaitForSeconds(0.5f);  // 0.5초 기다리기

        uiResult.gameObject.SetActive(true);   // 게임결과 UI 활성화
        uiResult.Win();    // 이미지 오브젝트를 활성화하는 승리 함수 호출
        Stop();

        
        AudioManager.instance.PlayBgm(false);   // 게임 종료시 Bgm종료
        AudioManager.instance.PlaySfx(AudioManager.Sfx.Win); // 게임 승리시 효과음 재생
    }

    public void GameRetry() // 게임 재시작 함수 작성
    {
        SceneManager.LoadScene(0);    // LoadScene이름 혹은 인덱스로 장면을 새롭게 부르는 함수
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

        if (bossWarningText != null)
        {
            bossWarningText.text = bossWarningMessage;
            bossWarningText.gameObject.SetActive(true);
        }

        float elapsed = 0f;
        bool visible = false;

        while (elapsed < bossWarningDuration)
        {
            visible = !visible;
            bossWarningOverlay.gameObject.SetActive(visible);
            if (bossWarningText != null)
            {
                bossWarningText.gameObject.SetActive(visible);
            }

            yield return new WaitForSecondsRealtime(bossWarningBlinkInterval);
            elapsed += bossWarningBlinkInterval;
        }

        HideBossWarningUI();
    }

    void EnsureBossWarningUI()
    {
        if (bossWarningOverlay != null)
            return;

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

        GameObject textObject = new GameObject("BossWarningText");
        textObject.transform.SetParent(overlayObject.transform, false);
        bossWarningText = textObject.AddComponent<Text>();
        bossWarningText.text = bossWarningMessage;
        bossWarningText.alignment = TextAnchor.MiddleCenter;
        bossWarningText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        bossWarningText.fontSize = bossWarningFontSize;
        bossWarningText.color = Color.red;

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        HideBossWarningUI();
    }

    void HideBossWarningUI()
    {
        if (bossWarningOverlay != null)
        {
            bossWarningOverlay.gameObject.SetActive(false);
        }

        if (bossWarningText != null)
        {
            bossWarningText.gameObject.SetActive(false);
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

    public void GetExp()    // 경험치 증가함수 작성
    {
        if (!isLive)
            return;

        exp ++;

        if (exp == nextExp[Mathf.Min(level, nextExp.Length - 1)])  // exp가 현재 레벨의 최대경험치와 같아지면
        // Mathf.Min 함수를 사용해 최고 경험치를 그대로 사용하도록 변경 (영상12 40:40)
        {
            level ++;   // level + 1
            exp = 0;    // exp는 0으로 초기화
            uiLevelUp.Show();   // LevelUp UI 출력
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
}
