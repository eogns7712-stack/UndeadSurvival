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

    // [추가] 폭발형 무기(폭탄) 관련 변수들
    public bool isBomb = false;
    public float explosionRadius = 2.5f;

    void Awake()
    {   // 변수 초기화
        rigid = GetComponent<Rigidbody2D>();
    }

    public void Init(float damage, int per, Vector3 dir)  // 변수 초기화 함수 선언
    {
        this.damage = damage;   // this : 해당 클래스의 변수로 접근
        this.per = per;
        this.isBomb = false;
        this.maxRange = 12f;
        this.startPosition = transform.position;

        if (per >= 0)   // 관통(per)이 -1(무한)보다 큰 것에 대해서는 속도적용, 원거리무기의 per값이 0이거나 0보다 크면 속도적용하게 변경 (영상16 6:30)
        {
            rigid.velocity = dir * 15f;   // .velocity : 속도
        }
    }

    // [추가 오버로드] Weapon.cs의 Fire() 및 FireBomb() 호출 규격에 정확히 맞춘 4개 인수 버전의 Init 메서드 추가
    public void Init(float damage, int per, Vector3 dir, Vector3 spawnPos)
    {
        this.damage = damage;   // this : 해당 클래스의 변수로 접근
        this.per = per;
        this.isBomb = false;
        this.maxRange = 12f;
        this.startPosition = spawnPos;

        if (per >= 0)   // 관통(per)이 -1(무한)보다 큰 것에 대해서는 속도적용
        {
            rigid.velocity = dir * 15f;   // .velocity : 속도
        }
    }

    void Update()
    {
        if (!GameManager.instance.isLive)
            return;

        // [수정] 근접 공전 무기(무한 관통, per == -100)는 최대 사거리 최적화 검사에서 제외합니다.
        if (per == -100)
            return;

        // [최적화 적용] 총알의 최대거리 설정 - 탄생 지점으로부터의 거리를 실시간 체크하여 풀 회수
        float travelDistance = Vector3.Distance(startPosition, transform.position);
        if (travelDistance >= maxRange)
        {
            if (isBomb)
            {
                Explode(); // 폭탄은 최대거리에 도달 시 강제 폭발
            }
            else
            {
                gameObject.SetActive(false); // 일반 탄환은 조용히 비활성화
            }
        }
    }

    void OnTriggerEnter2D(Collider2D collision) // 총알 관통 로직
    {   //충돌한 오브젝트의 태그가 'Enemy'가 아니거나(!,or) 관통값(per)이 -100이라면
        if (!collision.CompareTag("Enemy") || per == -100) // || : or
            return;     // 즉시 반환

        if (isBomb)
        {
            Explode(); // 폭발 조건 발동
            return;
        }

        //충돌한 오브젝트의 태그가 'Enemy'거나 관통값(per)이 -1이 아니라면
        per --; // 관통값(per) 감소
        
        if ( per < 0)     // 관통값(per)이 -1이라면 비활성화, 관통 이후 로직을 감싸는 if조건을 안전하게 변경(영상16 6:30)
        {
            rigid.velocity = Vector2.zero;  // 비활성화 이전에 물리속도 초기화, 재활용 하기 위함
            gameObject.SetActive(false);    // Desrtoy는 게임 최적화 문제로 사용x, 재활용 하기위해 비활성화 방식 선택
        }
    }

    // [추가] 폭탄 무기의 스플래시 폭발 범위 대미지 연산
    void Explode()
    {
        // 1. 폭발 반경 내부의 모든 Enemy 레이어 스캔
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(transform.position, explosionRadius, LayerMask.GetMask("Enemy"));
        
        foreach (Collider2D enemyCollider in hitEnemies)
        {
            Enemy enemy = enemyCollider.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
            }
        }

        // 2. 효과음 재생
        AudioManager.instance.PlaySfx(AudioManager.Sfx.Dead); // 폭발 사운드 대용 활용

        // 3. 탄환 객체 리셋 및 풀 반입
        rigid.velocity = Vector2.zero;
        isBomb = false;
        gameObject.SetActive(false);
    }

    void OnTriggerExit2D(Collider2D collision)
    {
        if (!collision.CompareTag("Area") || per == -100)
            return;
        
        gameObject.SetActive(false);    // Player of Area 밖으로 총알이 나가면 오브젝트 비활성화
    }
}

// [추가] 폭탄 무기 폭발 시 시각적 만족감을 채워 줄 경량 파동 이펙트 컴포넌트
public class TemporaryExplosionFX : MonoBehaviour
{
    private SpriteRenderer sr;
    private float maxDuration = 0.22f; // 폭발이 커지고 사라지는 시간
    private float elapsed = 0f;
    private Vector3 maxScale;

    public void PlayExplosion(Sprite bombSprite, float radius)
    {
        sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sprite = bombSprite;
        sr.color = new Color(1f, 0.45f, 0.05f, 0.85f); // 뜨거운 화염의 오렌지-레드 광원 컬러
        sr.sortingOrder = 5; // 적들보다 상위에 렌더링되도록 레이어 가산
        
        transform.localScale = Vector3.one * 0.1f;
        // 폭발 반경에 비례하여 부풀어 오를 최종 스케일 지정
        maxScale = Vector3.one * radius * 1.8f; 
    }

    void Update()
    {
        elapsed += Time.deltaTime;
        float t = elapsed / maxDuration;

        // 1. 크기가 급격하게 부풀어 올랐다가 멈춤
        transform.localScale = Vector3.Lerp(Vector3.one * 0.1f, maxScale, Mathf.Sin(t * Mathf.PI * 0.5f));

        // 2. 색상이 붉은색으로 변하면서 서서히 투명해짐
        if (sr != null)
        {
            float alpha = Mathf.Lerp(0.85f, 0f, t);
            sr.color = new Color(1f, Mathf.Lerp(0.45f, 0.15f, t), 0.05f, alpha);
        }

        // 3. 연출 시간이 끝나면 오브젝트 자동 파괴 및 메모리 정리
        if (elapsed >= maxDuration)
        {
            Destroy(gameObject);
        }
    }
}