using UnityEngine;
using UnityEngine.UI;   // UI 컴포넌트를 사용할 때는 UnityEngine.UI 네임 스페이스 사용

/// 경험치, 레벨, 처치 수, 타이머와 player체력 등 플레이 중 HUD 값을 갱신하는 스크립트.

public class HUD : MonoBehaviour
{
    public enum InfoType { Exp, Level, Kill, Time, Health} // 다루기 될 데이터를 열거형 enum으로 선언
    public InfoType type; // 선언한 열거형을 타입으로 변수 추가
    public Text expText;    // 경험치 슬라이더 옆에 현재 경험치 / 필요 경험치를 표시할 텍스트.

    Text myText;    // Level, Kill, Time 표시용 Text 컴포넌트.
    Slider mySlider;    // Exp, Health 표시용 Slider 컴포넌트.
    Color defaultTextColor; // 보스전이 아닐 때 Timer 색상 복구용 기본 색상.
    // 변수 선언 및 초기화
    void Awake()
    {
        myText = GetComponent<Text>();  // 현재 HUD 오브젝트에 Text가 붙어 있으면 가져오기.
        mySlider = GetComponent<Slider>();  // 현재 HUD 오브젝트에 Slider가 붙어 있으면 가져오기.
        if (myText != null) // Text가 있는 HUD라면 기본 글자색 저장.
        {
            defaultTextColor = myText.color;
        }
    }

    void LateUpdate()   // LateUpdate사용 : UI는 보통 모든 게임 로직이 끝난 뒤 업데이트
    {
        switch (type)
        {
            case InfoType.Exp : // 슬라이더에 적용할 값 : 현재경험치 / 최대경험치
                float curExp = GameManager.instance.exp;  // 현재 경험치.
                float maxExp = GameManager.instance.GetRequiredExp(GameManager.instance.level);    // Mathf.Min 함수를 사용해 최고 경험치를 그대로 사용하도록 변경 (영상12 40:40)
                mySlider.value = curExp / maxExp;  // 경험치 슬라이더 비율 갱신.
                if (expText != null)   // 경험치 텍스트가 연결되어 있으면 현재/필요 경험치 표시.
                {
                    expText.text = string.Format("{0:F0} / {1:F0}", curExp, maxExp);
                }
                break;

            case InfoType.Level :
                myText.text = string.Format("Lv.{0:F0}",GameManager.instance.level);    
                // string.Format("Type","적용할 데이터") : 각 숫자 인자값을 지정된 형태의 문자열로 만들어주는 함수
                // 인자값의 문자열이 들어갈 자리를 {순번} 형태로 작성, F0, F1, F2.... 소수점의 자리를 지정
                break;

            case InfoType.Kill :
                myText.text = string.Format("{0:F0}",GameManager.instance.kill);    // 이번 판 처치 수 표시.
                break;

            case InfoType.Time :
                float remainTime = GameManager.instance.isBossBattle ? GameManager.instance.bossTime : GameManager.instance.maxGameTime - GameManager.instance.gameTime;    // 남은 시간 구하기
                remainTime = Mathf.Max(0f, remainTime);    // 게임 종료 시 시간이 음수로 표시되지 않도록 보정.
                int min = Mathf.FloorToInt(remainTime / 60);    // 60으로 나누어 분을 구하되 Mathf.FloorToInt()를 사용해 소수점 버리기
                int sec = Mathf.FloorToInt(remainTime % 60); // % 60 : 60으로 나눈 나머지
                myText.text = string.Format("{0:D2}:{1:D2}",min, sec);  // 이미 min과 sec을 구할때 소수점을 버려서 F0은 필요없음
                myText.color = GameManager.instance.isBossBattle ? Color.red : defaultTextColor;   // 보스전 중에는 타이머를 빨간색으로 표시.
                // D0, D1, D2.... : 자리수를 지정, 00:00 형태로 시간을 표시하기 때문에 2자리는 유지해야함
                break;

            case InfoType.Health :
                float curhealth = GameManager.instance.health; // 현재 플레이어 체력.
                float maxHealth = GameManager.instance.maxHealth;  // 상점 보너스까지 적용된 최대 체력.
                mySlider.value = curhealth / maxHealth;    // 체력 슬라이더 비율 갱신.
                break;
        }
    }
}
