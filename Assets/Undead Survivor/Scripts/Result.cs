using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Result : MonoBehaviour
{
    public GameObject[] titles;

    public void Lose()  // 이미지 오브젝트를 활성화하는 패배 함수
    {
        titles[0].SetActive(true);
        titles[1].SetActive(false);
    }

    public void Win()  // 이미지 오브젝트를 활성화하는 승리 함수
    {
        titles[0].SetActive(false);
        titles[1].SetActive(true);
    }
}
