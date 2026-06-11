using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    // 데미지와 관통변수 선언
    public float damage;
    public int per;

    Rigidbody2D rigid;  // Rigidbody2D 변수 생성 및 초기화

    // [추가] 최적화를 위한 최대 사거리 제한 변수들
    Vector3 startPosition;
    public float maxRange = 12f; // 총알이 화면 밖까지 불필요하게 날아가는 것을 방지하기 위한 최대 사거리 설정

    void Awake()
    {   // 변수 초기화
        rigid = GetComponent<Rigidbody2D>();
    }

    public void Init(float damage, int per, Vector3 dir, Vector3 startPos = default)
    {
        this.damage = damage;
        this.per = per;
        this.maxRange = 12f;
        this.startPosition = startPos == default ? transform.position : startPos;

        // [삽 잔상 해결] 풀에서 가져온 객체의 렌더러와 상태를 즉시 초기화
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.enabled = true;

        if (per >= 0)   // 관통(per)이 -1(무한)보다 큰 것에 대해서는 속도적용
        {
            rigid.velocity = dir * 15f;
        }
    }

    // [복구 완료] 탄환 및 수류탄이 일정 사거리를 날아가면 풀러로 되돌아가는 최적화 스케줄러 복구
    void Update()
    {
        if (!GameManager.instance.isLive)
            return;

        // 근접 공전 무기(삽, per == -100)의 경우는 최대 사거리 회수 검사에서 안전하게 제외합니다.
        if (per == -100)
            return;

        // 발사된 시작 지점으로부터 현재 이동한 물리적 거리를 연산합니다.
        float travelDistance = Vector3.Distance(startPosition, transform.position);
        
        if (travelDistance >= maxRange)
        {
            // 일반 탄환은 물리 속도를 0으로 밀고 화면상에서 안전하게 풀러로 비활성화 반입합니다.
            rigid.velocity = Vector2.zero;
            gameObject.SetActive(false);
        }
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Enemy") || per == -100) return;

        per--;

        if (per < 0)
        {
            rigid.velocity = Vector2.zero;
            gameObject.SetActive(false);
        }
    }
}