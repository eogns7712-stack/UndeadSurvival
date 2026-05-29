using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class Hand : MonoBehaviour
{
    public bool isLeft;
    public SpriteRenderer spriter;
    
    SpriteRenderer player;
    // Player의 스프라이트렌더러 변수 선언 및 초기화
    
    Vector3 rightPos = new Vector3(0.35f, -0.15f, 0);
    Vector3 rightPosReverse = new Vector3(-0.15f, -0.15f, 0);
    Quaternion leftRot = Quaternion.Euler(0, 0, -35);  // 왼손의 각 형태를 Quaternion으로 저장
    Quaternion leftRotReverse = Quaternion.Euler(0, 0, -135);

    void Awake()
    {
        player = GetComponentsInParent<SpriteRenderer>()[1];    // [0]은 자기자신(hand), [1]이 Player
    }

    void LateUpdate()
    {
        bool isReverse = player.flipX;  // Player의 반전상태를 지역변수에 저장

        if (isLeft) // 근접무기의 경우
        {
            transform.localRotation = isReverse ? leftRotReverse : leftRot; // 왼손회전은 localRotation 사용
            spriter.flipY = isReverse;
            spriter.sortingOrder = isReverse ? 4 : 6;
        }
        else    // 원거리 무기
        {
            transform.localPosition = isReverse ? rightPosReverse : rightPos;
            spriter.flipX = isReverse;
            spriter.sortingOrder = isReverse ? 6 : 4;
        }
    }

}
