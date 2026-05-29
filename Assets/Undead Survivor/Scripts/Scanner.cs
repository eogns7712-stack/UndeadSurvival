using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Scanner : MonoBehaviour
{
    // 코드 구조
    // 1. Player주변 일정 범위(scanRange)안에 있는 Enemy 탐지
    // 2. 그 중 가장 가까운 Enemy 찾기
    // 3. nearestTarget 변수에 가장 가까운 Enemy의 위치를 저장

    // 범위, 레이어, 스캔 결과 배열, 가장 가까운 목표를 담을 변수 선언
    public float scanRange; // 스캔의 범위를 지정하는 변수, Unity 내부에서 조정가능
    public LayerMask targetLayer;
    public RaycastHit2D[] targets;
    public Transform nearestTarget;

    void FixedUpdate()  // 물리 계산 프레임마다 실행
    {   // Player 주변 원형 범위 안에있는 모든 Enemy를 탐지해 배열 targets[]에 저장
        targets = Physics2D.CircleCastAll(transform.position, scanRange, Vector2.zero, 0, targetLayer);   // 원형의 캐스트를 쏘고 모든결과를 반환하는 함수
        // Physics2D.CircleCastAll(캐스팅 시작위치, 원의 반지름, 캐스팅 방향, 캐스팅 길이, 대상 레이어)
        nearestTarget = GetNearest();   // 변수 nearestTarget에 GetNearest()함수의 결과값 result 저장
    }

    Transform GetNearest()  //가장 가까운 목표를 찾는 함수 추가
    // Transform : Unity에서 가장 기본적임 컴포넌트로, Unity의 모든 GameObject가 반드시 갖고있음, 위치/회전/크기를 담당
    {
        Transform result = null;    // result변수 초기화
        float diff = 100;   // 현재까지 찾은 최소거리, 처음에는 매우 큰 값으로 시작

        foreach (RaycastHit2D target in targets)    // foreach문으로 캐스팅 결과 오브젝트를 하나씩 접근, 탐지된 배열 targets[]에서 target(Enemy)들을 하나씩 검사
        {
            Vector3 myPos = transform.position; // Player의 위치저장 변수
            Vector3 targetPos = target.transform.position;  // target(Enemy)의 위치 저장 변수
            float curDiff = Vector3.Distance(myPos, targetPos);    // Vector3.Distance(A,B) : 벡터 A와 B의 거리를 계산해주는 함수
            // curDiff 변수에 Enemy의 위치와 Player의 위치 거리를 저장

            if (curDiff < diff) // 반복문을 돌며 가져온 거리가 저장된 거리보다 작으면 교체
            {   // 현재 Enemy가 더 가까우면
                diff = curDiff; // 최소 거리 갱신
                result = target.transform;  // result 변수에 Enemy의 위치 저장(목표 교체)
            }
        }

        return result;  // 결과값 result 반환
    }
}
