using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;   // UI 컴포넌트를 사용할 때는 UnityEngine.UI 네임 스페이스 사용

public class HUD : MonoBehaviour
{
    public enum InfoType { Exp, Level, Kill, Time, Health} // 다루기 될 데이터를 열거형 enum으로 선언
    public InfoType type; // 선언한 열거형을 타입으로 변수 추가

    Text myText;
    Slider mySlider;
    // 변수 선언 및 초기화
    void Awake()
    {
        myText = GetComponent<Text>();
        mySlider = GetComponent<Slider>();
    }

    void LateUpdate()   // LateUpdate사용 : UI는 보통 모든 게임 로직이 끝난 뒤 업데이트
    {
        switch (type)
        {
            case InfoType.Exp : // 슬라이더에 적용할 값 : 현재경험치 / 최대경험치
                float curExp = GameManager.instance.exp;
                float maxExp = GameManager.instance.nextExp[Mathf.Min(GameManager.instance.level, GameManager.instance.nextExp.Length - 1)];    // Mathf.Min 함수를 사용해 최고 경험치를 그대로 사용하도록 변경 (영상12 40:40)
                mySlider.value = curExp / maxExp;
                break;

            case InfoType.Level :
                myText.text = string.Format("Lv.{0:F0}",GameManager.instance.level);    
                // string.Format("Type","적용할 데이터") : 각 숫자 인자값을 지정된 형태의 문자열로 만들어주는 함수
                // 인자값의 문자열이 들어갈 자리를 {순번} 형태로 작성, F0, F1, F2.... 소수점의 자리를 지정
                break;

            case InfoType.Kill :
                myText.text = string.Format("{0:F0}",GameManager.instance.kill);    
                break;

            case InfoType.Time :
                float remainTime = GameManager.instance.maxGameTime - GameManager.instance.gameTime;    // 남은 시간 구하기
                int min = Mathf.FloorToInt(remainTime / 60);    // 60으로 나누어 분을 구하되 Mathf.FloorToInt()를 사용해 소수점 버리기
                int sec = Mathf.FloorToInt(remainTime % 60); // % 60 : 60으로 나눈 나머지
                myText.text = string.Format("{0:D2}:{1:D2}",min, sec);  // 이미 min과 sec을 구할때 소수점을 버려서 F0은 필요없음
                // D0, D1, D2.... : 자리수를 지정, 00:00 형태로 시간을 표시하기 때문에 2자리는 유지해야함
                break;

            case InfoType.Health :
                float curhealth = GameManager.instance.health;
                float maxHealth = GameManager.instance.maxHealth;
                mySlider.value = curhealth / maxHealth;
                break;
        }
    }
}
