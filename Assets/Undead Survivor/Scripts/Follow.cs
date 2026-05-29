using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Follow : MonoBehaviour
{
    RectTransform rect;
    // RectTransform를 사용하기 위한 변수 선언 및 초기화
    void Awake()
    {
        rect = GetComponent<RectTransform>();
    }

    void FixedUpdate()
    {
        rect.position = Camera.main.WorldToScreenPoint(GameManager.instance.player.transform.position);
        // 월드좌표와 스크린좌표는 서로 다르기 때문에 GameManager.instance를 바로사용 불가능
        // Camera.main : 현재 사용하고있는 카메라에 바로 접근, WorldToScreenPoint : 월드상의 오브젝트 위치를 스크린 좌표로 변환
    }
}
