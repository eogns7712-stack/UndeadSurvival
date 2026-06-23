using UnityEngine;

// 소이 수류탄(2단계 이상 초월된 수류탄)의 화염지대 피해, 지속시간 및 박동 효과를 처리하는 스크립트.

public class BombFireZone : MonoBehaviour
{
    // 모든 화염지대가 같은 원형 스프라이트를 공유하도록 static으로 보관.
    static Sprite fireZoneSprite;

    SpriteRenderer sr;

    // 이번 화염지대의 피해량, 범위, 지속시간과 경과 시간을 저장하는 변수.
    float damage;
    float radius;
    float duration;
    float timer;
    float tickTimer;

    // 피해 간격과 시각적 박동 효과의 속도/크기를 저장하는 변수.
    public float tickRate = 0.5f;
    public float pulseSpeed = 5f;
    public float pulseScale = 0.1f;

    // 풀에서 꺼낼 때 이전 사용 상태를 지우고 새로운 화염지대 값을 적용.
    public void SetupZone(float damage, float radius, float duration)
    {
        this.damage = damage;
        this.radius = radius;
        this.duration = duration;
        timer = 0f;
        tickTimer = 0f;

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

        sr.sprite = fireZoneSprite;
        sr.sortingOrder = 4;
        sr.enabled = true;

        // Sprite의 지름이 1이므로 반지름 값에 2를 곱해 실제 피해 범위와 크기를 맞춘다.
        transform.localScale = Vector3.one * radius * 2f;
    }

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

        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    void Update()
    {
        if (!GameManager.instance.isLive)
            return;

        // 지속시간이 끝나면 오브젝트 풀로 반환.
        timer += Time.deltaTime;
        if (timer >= duration)
        {
            ReturnToPool();
            return;
        }

        // Mathf.PingPong 값으로 크기와 색을 반복 변화시켜 천천히 박동하는 느낌을 만든다.
        float beat = Mathf.PingPong(timer * pulseSpeed, 1f);
        float scaleMultiplier = 1f + beat * pulseScale;
        transform.localScale = Vector3.one * radius * 2f * scaleMultiplier;

        if (sr != null)
        {
            sr.color = Color.Lerp(new Color(1f, 0.82f, 0f, 0.45f), new Color(1f, 0.35f, 0f, 0.3f), beat);
        }

        // tickRate마다 범위 안 적을 찾아 장판 데미지를 준다.
        tickTimer += Time.deltaTime;
        if (tickTimer < tickRate)
            return;

        tickTimer = 0f;
        Collider2D[] targets = Physics2D.OverlapCircleAll(transform.position, radius, LayerMask.GetMask("Enemy"));
        foreach (Collider2D target in targets)
        {
            Enemy enemy = target.GetComponent<Enemy>();
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
