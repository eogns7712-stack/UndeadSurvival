using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    // 데미지와 관통변수 선언
    public float damage;
    public int per;
    public int prefabId; // [추가] 파편 생성을 위한 프리팹 ID 저장 변수
    public GameObject explosionVFXPrefab;

    Rigidbody2D rigid;  // Rigidbody2D 변수 생성 및 초기화

    // [추가] 최적화를 위한 최대 사거리 제한 변수들
    Vector3 startPosition;
    public float maxRange = 12f; // 총알이 화면 밖까지 불필요하게 날아가는 것을 방지하기 위한 최대 사거리 설정

    // [추가] 폭발형 무기(폭탄) 관련 변수들
    public bool isBomb = false;
    public float explosionRadius = 4.5f;
    public int bombStage = 0; // 0: 일반, 1: M1 파편, 2: M2 불바다
    public GameObject fireZonePrefab; // 인스펙터에서 설정

    void Awake()
    {   // 변수 초기화
        rigid = GetComponent<Rigidbody2D>();
    }

    public void Init(float damage, int per, Vector3 dir, Vector3 startPos = default, int bombStage = 0, int prefabId = 0)
    {
        this.damage = damage;
        this.per = per;
        this.prefabId = prefabId;
        this.bombStage = bombStage;
        this.isBomb = false;
        this.maxRange = 12f;
        this.startPosition = startPos == default ? transform.position : startPos;

        // [삽 잔상 해결] 풀에서 가져온 객체의 렌더러와 상태를 즉시 초기화
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.enabled = true;

        if (per >= 0)   // 관통(per)이 -1(무한)보다 큰 것에 대해서는 속도적용, 원거리무기의 per값이 0이거나 0보다 크면 속도적용하게 변경 (영상16 6:30)
        {
            rigid.velocity = dir * 15f; // .velocity : 속도
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

        // 1. 폭탄이면 적에 닿는 즉시 폭발
        if (isBomb)
        {
            // 적 태그를 확인해야 한다면 여기서 필터링
            if (collision.CompareTag("Enemy"))
            {
                Explode();
            }
            return;
        }

        if (!collision.CompareTag("Enemy") || per == -100) return;
        //충돌한 오브젝트의 태그가 'Enemy'거나 관통값(per)이 -1이 아니라면
        per --; // 관통값(per) 감소
        
        if ( per < 0)     // 관통값(per)이 -1이라면 비활성화, 관통 이후 로직을 감싸는 if조건을 안전하게 변경(영상16 6:30)
        {
            rigid.velocity = Vector2.zero;  // 비활성화 이전에 물리속도 초기화, 재활용 하기 위함
            gameObject.SetActive(false);    // Desrtoy는 게임 최적화 문제로 사용x, 재활용 하기위해 비활성화 방식 선택
        }
    }

    // [추가] 폭탄 무기의 스플래시 폭발 범위 대미지 연산 및 폭발 이펙트 호출
    void Explode()
    {
        // 1. 데미지 판정
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(transform.position, explosionRadius, LayerMask.GetMask("Enemy"));
        foreach (Collider2D enemyCollider in hitEnemies)
        {
            Enemy enemy = enemyCollider.GetComponent<Enemy>();
            if (enemy != null) enemy.TakeDamage(damage);
        }

        // 2. [비주얼] 동적 생성 대신 프리팹 인스턴스화 (훨씬 확실함)
        if (explosionVFXPrefab != null)
        {
            GameObject vfx = Instantiate(explosionVFXPrefab, transform.position, Quaternion.identity);
            Destroy(vfx, 1.0f); // 1초 후 자동 삭제
        }
        else
        {
            // 만약 프리팹이 없다면, 기존 코드 방식대로 하되 SpriteRenderer를 강제 추가
            GameObject fxObject = new GameObject("TemporaryExplosionVFX");
            fxObject.transform.position = transform.position;
            fxObject.AddComponent<SpriteRenderer>(); // 이게 없어서 안 보였을 가능성 높음
            TemporaryExplosionFX fxComp = fxObject.AddComponent<TemporaryExplosionFX>();
            fxComp.PlayExplosion(explosionRadius);
        }

        // 3. 파편 로직
        if (bombStage >= 1)
        {
            for (int i = 0; i < 8; i++)
            {
                Transform fragment = GameManager.instance.pool.Get(prefabId).transform;
                fragment.position = transform.position;
                Vector3 dir = Quaternion.Euler(0, 0, i * 45) * Vector3.up;
                fragment.GetComponent<Bullet>().Init(damage * 0.5f, 0, dir, transform.position, 0, prefabId);
            }
        }

        // 4. 불바다 로직 및 정리
        if (bombStage >= 2 && fireZonePrefab != null)
        {
            GameObject fireZone = Instantiate(fireZonePrefab, transform.position, Quaternion.identity);
            Destroy(fireZone, 3f);
        }

        AudioManager.instance.PlaySfx(AudioManager.Sfx.Dead);
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

    // [버그 해결] 수류탄이 커지는 현상을 막기 위해, 원형 텍스처를 절차적으로 생성하여 깔끔한 화염파로 표현
    public void PlayExplosion(float radius)
    {
        sr = gameObject.AddComponent<SpriteRenderer>();
        
        // 런타임에 부드러운 원형 화염 텍스처를 그립니다.
        sr.sprite = CreateSmoothCircleSprite();
        sr.color = new Color(1f, 0.4f, 0.05f, 0.85f); // 화사하고 뜨거운 오렌지 파동
        sr.sortingOrder = 5; // 피아 구분선보다 항상 위로 드로잉
        
        transform.localScale = Vector3.one * 0.1f;
        // 인게임 실제 스플래시 대미지 영역(radius)과 정확하게 눈으로 매칭되도록 가중치 1.05로 고정
        maxScale = Vector3.one * radius * 1.05f; 
    }

    // 픽셀 연산을 이용해 외곽선이 계단현상 없이 부드러운 전용 원형 스프라이트 생성
    private Sprite CreateSmoothCircleSprite()
    {
        int size = 64;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float center = size / 2f;
        float radius = size / 2f - 2f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                if (dist <= radius)
                {
                    // 가장자리(AA) 투명도 보간 연산
                    float alpha = Mathf.Clamp01((radius - dist) / 2.0f);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
                else
                {
                    texture.SetPixel(x, y, Color.clear);
                }
            }
        }
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
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
            // 안쪽은 백열색, 끝으로 갈수록 빨간색이 되며 사라지는 그라데이션 불꽃 구현
            sr.color = new Color(1f, Mathf.Lerp(0.45f, 0.1f, t), 0.02f, alpha);
        }

        // 3. 연출 시간이 끝나면 오브젝트 자동 파괴 및 메모리 정리
        if (elapsed >= maxDuration)
        {
            Destroy(gameObject);
        }
    }
}