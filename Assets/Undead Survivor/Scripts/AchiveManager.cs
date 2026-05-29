using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class AchiveManager : MonoBehaviour  // 업적관리
{
    public GameObject[] lockCharacter;  // 잠금 해금 버튼들을 담을 변수 추가
    public GameObject[] unlockCharacter;
    public GameObject uiNotice; // 업적 달성 알림 오브젝트를 저장할 변수 선언

    enum Achive { UnlockPotato, UnlockBean }    // 업적 데이터와 같은 열거형 enum으로 생성, 업적을 생성하는 구문
    Achive[] achives;
    WaitForSecondsRealtime wait;    // WaitForSecondsRealtime : 게임 시간이 멈춰도 실제 시간 기준으로 기다리는 코루틴 함수

    void Awake()
    {   // 변수 초기화
        achives = (Achive[])Enum.GetValues(typeof(Achive));    // Enum.GetValues : 주어진 열거형의 데이터를 모두 가져오는 함수
        // Enum.GetValues 앞에 타입을 명시적으로 지정해 Achive[]타입맞추기
        wait = new WaitForSecondsRealtime(5);

        if (!PlayerPrefs.HasKey("MyData"))  // HasKey 함수로 데이터 유무 체크 후 초기화 실행
        {
            Init();
        }
    }

    void Init()
    {   // 업적변수 초기화
        PlayerPrefs.SetInt("MyData", 1); // PlayerPrefs : Unity에서 간단한 저장기능을 제공하는 클래스
        // PlayerPrefs.SetInt("MyData", 1) : 'Mydata'라는 키에 int형 데이터 1을 저장

        // PlayerPrefs.SetInt("UnlockPotato", 0);
        // PlayerPrefs.SetInt("UnlockBean", 0);
        // 위와같은 형식으로 사용해도 되지만, 업적의 갯수가 많아지면 코드가 너무 길어짐, 대신 foreach문 사용 
        foreach (Achive achive in achives)
        {
            PlayerPrefs.SetInt(achive.ToString(), 0);   // key '업적명'의 키값이 0이면 업적 미달성, 1이면 업적달성
        }
    }

    void Start()
    {
        UnlockCharacter();
    }

    void UnlockCharacter()  // 캐릭터 버튼 해금을 위한 함수 작성
    {
        for (int index = 0; index < lockCharacter.Length; index++)  // 잠금 버튼 배열을 순회하면서 인덱스에 해당하는 업적 이름 가져오기
        {
            string achivesName = achives[index].ToString();
            // GetInt 함수로 저장된 업적 상태를 가져와서 버튼 활성화에 적용
            bool isUnlock = PlayerPrefs.GetInt(achivesName) == 1;
            lockCharacter[index].SetActive(!isUnlock);
            // 비활성화된 캐릭터버튼을 활성화하고, 활성화되있던 잠금버튼을 비활성화
            unlockCharacter[index].SetActive(isUnlock);
        }
    }

    void LateUpdate()
    {
        foreach (Achive achive in achives)  // 모든 업적 확인을 위한 반복문은 LateUpdate 구문에 작성
        {
            CheckAchive(achive);
        }
    }

    void CheckAchive(Achive achive)  // 업적 달성을 위한 함수, 업적달성조건을 작성하는 부분
    {
        bool isAchive = false;

        switch (achive)
        {
            case Achive.UnlockPotato:
                isAchive = GameManager.instance.kill >= 10;
                break;

            case Achive.UnlockBean:
                isAchive = GameManager.instance.gameTime == GameManager.instance.maxGameTime;
                break;
        }

        if (isAchive && PlayerPrefs.GetInt(achive.ToString()) == 0)  // 해당 업적이 처음 달성했다는 조건을 if문에 작성
        {
            PlayerPrefs.SetInt(achive.ToString(), 1);

            for (int index = 0; index < uiNotice.transform.childCount; index++) // 알림창의 자식 오브젝트를 순회하면서 순번이 맞으면 활성화 
            {
                bool isActive = index == (int)achive;
                uiNotice.transform.GetChild(index).gameObject.SetActive(isActive);
            }

            StartCoroutine(NoticeRoutine());
        }
    }

    IEnumerator NoticeRoutine() // 업적달성 알림창을 활성화 했다가 일정시간 이후 비활성화 하는 코루틴 생성
    {
        uiNotice.SetActive(true);   // 업적달성 알림창 UI 활성화, 5초대기, 비활성화
        AudioManager.instance.PlaySfx(AudioManager.Sfx.LevelUp); // 레벨업시 효과음 재생

        yield return wait;

        uiNotice.SetActive(false);
    }
}
