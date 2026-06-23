using UnityEngine;

// 풀링된 수류탄 폭발 VFX의 크기와 투명도를 시간에 따라 변화시키는 스크립트.

public class BombExplosionFX : MonoBehaviour
{
    SpriteRenderer sr;

    // 폭발 이펙트의 재생 시간, 시작 크기, 폭발 반경 대비 이펙트 크기 배율.
    public float maxDuration = 0.22f;
    public float initialScale = 0.1f;
    public float radiusScaleMultiplier = 1.8f;
    public int sortingOrder = 10;

    // 현재 재생 시간과 이번 폭발에서 도달할 최종 크기.
    float elapsed;
    Vector3 maxScale;

    // 모든 폭발 이펙트가 같은 원형 스프라이트를 공유하도록 static으로 보관.
    static Sprite circleSprite;

    void Awake()
    {
        EnsureRenderer();

        // 원형 스프라이트는 한 번만 생성하고 이후 풀링 오브젝트들이 재사용.
        if (circleSprite == null)
        {
            circleSprite = CreateCircleSprite();
        }

        sr.sprite = circleSprite;
        sr.sortingOrder = sortingOrder;
    }

    public void PlayExplosion(float radius)
    {
        EnsureRenderer();

        // 새 폭발이 시작될 때 시간과 크기를 초기값으로 리셋.
        elapsed = 0f;
        transform.localScale = Vector3.one * initialScale;
        maxScale = Vector3.one * radius * radiusScaleMultiplier;

        if (sr != null)
        {
            // 시작 색상은 밝은 주황색으로 두고, Update에서 점점 투명하게 만든다.
            sr.enabled = true;
            sr.color = new Color(1f, 0.45f, 0.05f, 0.85f);
        }
    }

    void EnsureRenderer()
    {
        // 프리팹에 SpriteRenderer가 없더라도 런타임에 붙여 최소 동작을 보장.
        if (sr == null)
        {
            sr = GetComponent<SpriteRenderer>();
        }

        if (sr == null)
        {
            sr = gameObject.AddComponent<SpriteRenderer>();
        }
    }

    static Sprite CreateCircleSprite()
    {
        // 별도 이미지 파일 없이 흰색 원형 텍스처를 코드로 생성한다.
        int size = 128;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float center = size / 2f;
        float radius = size / 2f - 4f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                if (dist <= radius)
                {
                    // 원의 가장자리는 알파를 낮춰 폭발 이펙트가 부드럽게 보이도록 처리.
                    float alpha = Mathf.Clamp01((radius - dist) / 4.0f);
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

        // 재생 시간을 0~1 비율로 바꿔 크기와 투명도 보강.
        elapsed += Time.deltaTime;
        float duration = Mathf.Max(0.0001f, maxDuration);
        float t = Mathf.Clamp01(elapsed / duration);

        // Sin 보간으로 초반에는 빠르게 커지고 끝으로 갈수록 부드럽게 멈추는 느낌을 준다.
        transform.localScale = Vector3.Lerp(Vector3.one * initialScale, maxScale, Mathf.Sin(t * Mathf.PI * 0.5f));

        if (sr != null)
        {
            // 시간이 지날수록 어두운 주황색에 가까워지고 투명해진다.
            float alpha = Mathf.Lerp(0.85f, 0f, t);
            sr.color = new Color(1f, Mathf.Lerp(0.45f, 0.15f, t), 0.05f, alpha);
        }

        if (elapsed >= duration)
        {
            // 재생이 끝난 오브젝트는 비활성화해서 풀로 되돌린다.
            gameObject.SetActive(false);
        }
    }
}
