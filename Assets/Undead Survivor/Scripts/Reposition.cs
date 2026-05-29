using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Reposition : MonoBehaviour
{

    Collider2D coll;    // Collider 2D 변수생성, Collider 2D는 기본도형의 모든 콜라이더2D를 포함
        void Awake()
        {
            coll = GetComponent<Collider2D>();  // 변수 초기화
        }
    void OnTriggerExit2D(Collider2D other)
    {
        
        if (!other.CompareTag("Area"))   //OnTrigger2D 의 매개변수 상대방 콜라이더의 태그를 조건으로
            return;

        Vector3 playerPos = GameManager.instance.player.transform.position;  //거리를 구하기 위해 플레이어 위치와 타일맵 위치를 저장
        Vector3 myPos = transform.position;


        switch (transform.tag)  // switch ~ case : 값의 상태에 따라 로직을 나누어주는 키워드
        {   // Player가 직접이동이 아닌 오브젝트에 밀려서 이동할때 타일이 움직이지 않는현상 해결
            case "Ground":
                    float diffX = playerPos.x - myPos.x;
                    float diffY = playerPos.y - myPos.y;    // Player와 Tilemap의 거리 계산

                    float dirX = diffX < 0 ? -1 : 1;    // 3항 연산자 : (조건) ? (true일때 값) : (false일때 값)
                    float dirY = diffY < 0 ? -1 : 1;    // 방향결정
                    diffX = Mathf.Abs(diffX);   // Mathf.Abs : 절댓값으로 변경, 뱡향제거 → 순수 거리만 비교
                    diffY = Mathf.Abs(diffY);   // 두 오브젝트의 위치 차이를 활용한 로직으로 변경

                if (diffX > diffY)   //플레이어가 X축으로만 멀어진다면
                {
                    transform.Translate(Vector3.right * dirX * 40);   // Translate : 지정된 값 만큼 현재 위치에서 이동, Tilemap이동, dirX의 방향으로 40칸 떨어진곳으로
                    //플레이어 이동 방향 기준으로 40칸 앞으로 이동, 왜 40인가? 타일맵 하나의 크기가 20칸, 양쪽 끝에서 반대편으로 보내려면 보통 20 x 2 = 40, 타일맵 크기 20 + 반대편 위치까지 거리 20 = 총 40
                } 
                else if (diffX < diffY)
                {
                    transform.Translate(Vector3.up * dirY * 40);    //Tilemap이동, dirY의 방향으로 40칸 떨어진곳으로
                }
                break;
    
            case "Enemy":
                if (coll.enabled)   // Enemy가 죽어있으면 다른오브젝트와 충돌하지않도록
                {
                    Vector3 dist = playerPos - myPos;   // 거리차이 계산
                    Vector3 ran = new Vector3(Random.Range(-3, 3), Random.Range(-3, 3), 0); // 랜덤벡터를 더해 퍼져있는 몬스터 재배치
                    transform.Translate(ran + dist * 2);    // transform.Translate(dist) 만 작성한다면 적들이 Player의 위치에 바로 재배치 될 수 있음,
                    // 방지하기 위한 dise * 2 + random거리 추가
                }
                break;
        }
    }
}
