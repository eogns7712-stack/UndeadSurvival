using UnityEngine;

// 풀링된 수류탄 폭발 VFX의 크기와 투명도를 시간에 따라 변화시키는 스크립트.

public class BombExplosionFX : MonoBehaviour
{
    SpriteRenderer sr; // 폭발 원형 이펙트를 화면에 표시할 SpriteRenderer.

    // 폭발 이펙트의 재생 시간, 시작 크기, 폭발 반경 대비 이펙트 크기 배율.
    public float maxDuration = 0.22f;  // 폭발 이펙트가 유지되는 시간. 짧을수록 빠르게 사라짐.
    public float initialScale = 0.1f;  // 폭발이 시작될 때의 초기 크기.
    public float radiusScaleMultiplier = 1.8f;  // 수류탄 폭발 반경 대비 화면에 보이는 이펙트 크기 배율.
    public int sortingOrder = 10;  // 폭발 이펙트가 다른 스프라이트보다 앞에 보이도록 하는 정렬값.

    // 현재 재생 시간과 이번 폭발에서 도달할 최종 크기.
    float elapsed;
    Vector3 maxScale;

    // 모든 폭발 이펙트가 같은 원형 스프라이트를 공유하도록 static으로 보관.
    static Sprite circleSprite;

    void Awake()
    {
        EnsureRenderer();  // SpriteRenderer가 없더라도 폭발 이펙트가 출력되도록 보장.

        // 원형 스프라이트는 한 번만 생성하고 이후 풀링 오브젝트들이 재사용.
        if (circleSprite == null)
        {
            circleSprite = CreateCircleSprite();
        }

        sr.sprite = circleSprite;  // 생성한 원형 스프라이트를 렌더러에 적용.
        sr.sortingOrder = sortingOrder;    // 인스펙터에서 지정한 정렬 순서 적용.
    }

    // 수류탄이나 보스 사망 연출에서 폭발 이펙트를 재생할 때 호출하는 함수.
    public void PlayExplosion(float radius)
    {
        EnsureRenderer();   // 풀에서 재사용될 때 렌더러 참조가 비어있을 가능성 보완.

        // 새 폭발이 시작될 때 시간과 크기를 초기값으로 리셋.
        elapsed = 0f;   // 이전 재생 시간 초기화.
        transform.localScale = Vector3.one * initialScale; // 폭발 시작 크기 적용.
        maxScale = Vector3.one * radius * radiusScaleMultiplier;    // 이번 폭발에서 도달할 최대 크기 계산.

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
        if (sr == null) // 캐싱된 렌더러가 없으면 현재 오브젝트에서 먼저 검색.
        {
            sr = GetComponent<SpriteRenderer>();
        }

        if (sr == null) // 그래도 없다면 런타임에 추가해서 오류를 막음.
        {
            sr = gameObject.AddComponent<SpriteRenderer>();
        }
    }

    // 별도 이미지 파일 없이 폭발 VFX에 사용할 부드러운 원형 스프라이트를 생성하는 함수.
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

        texture.Apply();   // SetPixel로 찍은 픽셀 정보를 실제 텍스처에 반영.
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);    // 텍스처를 Unity Sprite로 변환.
    }

    void Update()
    {
        if (!GameManager.instance.isLive)   // 게임이 멈추거나 끝났으면 이펙트 진행 중단.
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
