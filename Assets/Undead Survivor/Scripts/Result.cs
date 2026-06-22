using UnityEngine;
using UnityEngine.UI;

/// 승리 및 패배 결과 화면과 이번 판 처치 수를 표시하는 스크립트.

public class Result : MonoBehaviour
{
    public GameObject[] titles;
    public Text killText;
    public string killLabel = "KILL";

    public void Lose()  // 이미지 오브젝트를 활성화하는 패배 함수
    {
        titles[0].SetActive(true);
        titles[1].SetActive(false);
        RefreshKillText();
    }

    public void Win()  // 이미지 오브젝트를 활성화하는 승리 함수
    {
        titles[0].SetActive(false);
        titles[1].SetActive(true);
        RefreshKillText();
    }

    void RefreshKillText()
    {
        if (killText != null && GameManager.instance != null)
        {
            killText.text = killLabel + "  " + GameManager.instance.kill;
        }
    }
}
