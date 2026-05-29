using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime.Tree;
using UnityEngine;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;  // 장면관리를 사용하기위해 SceneManagement 네임스페이스 추가

public class GameManager : MonoBehaviour
{
    public static GameManager instance;  // static : 정적으로 사용하겠다는 키워드, 바로 메모리에 얹어버림, 인스펙터에 나타나지않음, 정적변수는 즉시 클래스에서 호출가능
    [Header("# Game Control")]    // Header : 인스펙터의 속성들을 깔끔하게 구분시켜주는 타이틀
    public bool isLive;
    public float gameTime;
    public float maxGameTime = 2* 10;
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
    public Result uiResult;    // 게임 결과 UI 오브젝트를 저장할 변수 선언, 타입을 스크립트로 변경(영상13 28:30) 
    public GameObject enemyCleaner; // 게임 승리시 적을 정리하는 클리너 변수 선언


    void Awake()
    {
        instance = this;    // 인스턴스 변수를 자기자신으로 초기화
    }

    public void GameStart(int id)   // 게임 시작 함수에 Player의 ID 매개변수 추가
    {
        playerId = id;
        health = maxHealth; // 게임 시작시 현재체력을 최대체력으로 초기화

        player.gameObject.SetActive(true);  // 게임 시작시 Player 활성화 후 기본무기 지급
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

    IEnumerator GameVictoryRoutine()   // 게임오버의 딜레이를 위해 코루틴 작성
    {
        isLive = false;
        enemyCleaner.SetActive(true);   //게임 승리 코루틴 전반부에 적 클리너 활성화

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
        if (gameTime > maxGameTime)
        {
            gameTime = maxGameTime;
            GameVictory();  // 게임 시간이 최대시간을 넘기는 때에 게임승리 함수 호출
        }
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