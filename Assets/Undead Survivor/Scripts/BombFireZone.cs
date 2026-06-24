using UnityEngine;

// 소이 수류탄(2단계 이상 초월된 수류탄)의 화염지대 피해, 지속시간 및 박동 효과를 처리하는 스크립트.

public class BombFireZone : MonoBehaviour
{
    // 모든 화염지대가 같은 원형 스프라이트를 공유하도록 static으로 보관.
    static Sprite fireZoneSprite;

    SpriteRenderer sr; // 화염지대 원형 스프라이트를 표시할 SpriteRenderer.

    // 이번 화염지대의 피해량, 범위, 지속시간과 경과 시간을 저장하는 변수.
    float damage;
    float radius;
    float duration;
    float timer;
    float tickTimer;

    // 피해 간격과 시각적 박동 효과의 속도/크기를 저장하는 변수.
    public float tickRate = 0.5f;  // 몇 초마다 장판 피해를 줄지 결정하는 간격.
    public float pulseSpeed = 5f;  // 화염지대가 밝아졌다 어두워지는 박동 속도.
    public float pulseScale = 0.1f;    // 박동할 때 커졌다 작아지는 크기 비율.

    // 풀에서 꺼낼 때 이전 사용 상태를 지우고 새로운 화염지대 값을 적용.
    public void SetupZone(float damage, float radius, float duration)
    {
        this.damage = damage;  // 이번 화염지대가 tick마다 줄 데미지.
        this.radius = radius;  // 적을 탐지하고 피해를 줄 반경.
        this.duration = duration;  // 화염지대가 유지될 시간.
        timer = 0f;    // 지속시간 타이머 초기화.
        tickTimer = 0f;    // 피해 간격 타이머 초기화.

        // 프리팹에 SpriteRenderer가 없더라도 런타임에 붙여 최소 동작을 보장.
        if (sr == null)
        {
            sr = GetComponent<SpriteRenderer>();
            if (sr == null)
            {
                sr = gameObject.AddComponent<SpriteRenderer>();
            }
        }

        // 화염지대 스프라이트는 한 번만 생성하고 이후 풀링 오브젝트들이 재사용.
        if (fireZoneSprite == null)
        {
            fireZoneSprite = CreateFireZoneSprite();
        }

        sr.sprite = fireZoneSprite;    // 생성 또는 재사용한 원형 화염 스프라이트 적용.
        sr.sortingOrder = 4;   // 바닥 장판처럼 보이도록 낮은 정렬값 사용.
        sr.enabled = true; // 풀에서 재사용될 때 꺼져 있을 수 있는 렌더러 활성화.

        // Sprite의 지름이 1이므로 반지름 값에 2를 곱해 실제 피해 범위와 크기를 맞춘다.
        transform.localScale = Vector3.one * radius * 2f;
    }

    // 별도 이미지 파일 없이 소이 수류탄 화염지대용 원형 스프라이트를 생성하는 함수.
    Sprite CreateFireZoneSprite()
    {
        // 별도 이미지 파일 없이 반투명 원형 텍스처를 코드로 생성한다.
        int size = 128;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float center = size / 2f;
        float r = size / 2f - 4f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                if (dist <= r)
                {
                    // 가장자리를 부드럽게 흐리게 해서 화염 장판이 딱딱한 원으로 보이지 않게 만든다.
                    float alpha = Mathf.Clamp01((r - dist) / 12.0f) * 0.5f;
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
                else
                {
                    texture.SetPixel(x, y, Color.clear);
                }
            }
        }

        texture.Apply();   // SetPixel로 만든 픽셀 정보를 실제 텍스처에 적용.
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);    // 텍스처를 Sprite로 변환.
    }

    void Update()
    {
        if (!GameManager.instance.isLive)   // 게임이 진행 중이 아니면 장판 시간과 데미지 처리 중단.
            return;

        // 지속시간이 끝나면 오브젝트 풀로 반환.
        timer += Time.deltaTime;
        if (timer >= duration)
        {
            ReturnToPool();
            return;
        }

        // Mathf.PingPong 값으로 크기와 색을 반복 변화시켜 천천히 박동하는 느낌을 만든다.
        float beat = Mathf.PingPong(timer * pulseSpeed, 1f);   // 0~1 사이 값을 반복해서 박동 연출에 사용.
        float scaleMultiplier = 1f + beat * pulseScale;    // 기본 크기에서 pulseScale만큼만 살짝 확대.
        transform.localScale = Vector3.one * radius * 2f * scaleMultiplier;

        if (sr != null)
        {
            sr.color = Color.Lerp(new Color(1f, 0.82f, 0f, 0.45f), new Color(1f, 0.35f, 0f, 0.3f), beat);   // 노랑/주황색 사이를 오가며 박동.
        }

        // tickRate마다 범위 안 적을 찾아 장판 데미지를 준다.
        tickTimer += Time.deltaTime;
        if (tickTimer < tickRate)
            return;

        tickTimer = 0f; // 이번 tick을 처리했으므로 피해 타이머 초기화.
        Collider2D[] targets = Physics2D.OverlapCircleAll(transform.position, radius, LayerMask.GetMask("Enemy"));    // 화염 반경 안의 적 검색.
        foreach (Collider2D target in targets)
        {
            Enemy enemy = target.GetComponent<Enemy>();    // Enemy 컴포넌트가 있는 대상에게만 피해 적용.
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
            }
        }
    }

    void ReturnToPool()
    {
        // 풀에 돌아가기 전 렌더러를 꺼서 재사용 대기 중 화면에 남지 않게 한다.
        if (sr != null)
        {
            sr.enabled = false;
        }

        gameObject.SetActive(false);
    }
}
