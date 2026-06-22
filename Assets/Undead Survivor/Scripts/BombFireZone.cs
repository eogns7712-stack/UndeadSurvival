using UnityEngine;

/// 소이 수류탄(2단계 이상 초월된 수류탄)의 화염지대 피해, 지속시간 및 박동 효과를 처리하는 스크립트

public class BombFireZone : MonoBehaviour
{
    static Sprite fireZoneSprite;

    SpriteRenderer sr;
    float damage;
    float radius;
    float duration;
    float timer;
    float tickTimer;
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

        if (sr == null)
        {
            sr = GetComponent<SpriteRenderer>();
            if (sr == null)
            {
                sr = gameObject.AddComponent<SpriteRenderer>();
            }
        }

        if (fireZoneSprite == null)
        {
            fireZoneSprite = CreateFireZoneSprite();
        }

        sr.sprite = fireZoneSprite;
        sr.sortingOrder = 4;
        sr.enabled = true;

        transform.localScale = Vector3.one * radius * 2f;
    }

    Sprite CreateFireZoneSprite()
    {
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

        timer += Time.deltaTime;
        if (timer >= duration)
        {
            ReturnToPool();
            return;
        }

        float beat = Mathf.PingPong(timer * pulseSpeed, 1f);
        float scaleMultiplier = 1f + beat * pulseScale;
        transform.localScale = Vector3.one * radius * 2f * scaleMultiplier;

        if (sr != null)
        {
            sr.color = Color.Lerp(new Color(1f, 0.82f, 0f, 0.45f), new Color(1f, 0.35f, 0f, 0.3f), beat);
        }

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
        if (sr != null)
        {
            sr.enabled = false;
        }

        gameObject.SetActive(false);
    }
}
