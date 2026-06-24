using UnityEngine;
using UnityEngine.UI;

/// 승리 및 패배 결과 화면과 이번 판 처치 수를 표시하는 스크립트.

public class Result : MonoBehaviour
{
    public GameObject[] titles; // 0번은 패배 타이틀, 1번은 승리 타이틀로 사용.
    public Text killText;   // 결과창에 이번 판 처치 수를 표시할 Text.
    public string killLabel = "KILL";   // 처치 수 앞에 붙일 라벨 문구.

    public void Lose()  // 이미지 오브젝트를 활성화하는 패배 함수
    {
        titles[0].SetActive(true); // 패배 타이틀 활성화.
        titles[1].SetActive(false);    // 승리 타이틀 비활성화.
        RefreshKillText(); // 결과창에 이번 판 처치 수 갱신.
    }

    public void Win()  // 이미지 오브젝트를 활성화하는 승리 함수
    {
        titles[0].SetActive(false);    // 패배 타이틀 비활성화.
        titles[1].SetActive(true); // 승리 타이틀 활성화.
        RefreshKillText(); // 결과창에 이번 판 처치 수 갱신.
    }

    // GameManager의 kill 값을 읽어 결과창 처치 수 텍스트를 갱신하는 함수.
    void RefreshKillText()
    {
        if (killText != null && GameManager.instance != null)  // Text와 GameManager가 모두 있을 때만 표시.
        {
            killText.text = killLabel + "  " + GameManager.instance.kill;
        }
    }
}
