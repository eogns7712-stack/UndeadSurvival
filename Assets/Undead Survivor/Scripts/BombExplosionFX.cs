using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BombExplosionFX : MonoBehaviour
{
    SpriteRenderer sr;
    float maxDuration = 0.22f;
    float elapsed;
    Vector3 maxScale;

    static Sprite circleSprite;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr == null)
        {
            sr = gameObject.AddComponent<SpriteRenderer>();
        }

        if (circleSprite == null)
        {
            circleSprite = CreateCircleSprite();
        }

        sr.sprite = circleSprite;
        sr.sortingOrder = 10;
    }

    public void PlayExplosion(float radius)
    {
        if (sr == null)
        {
            sr = GetComponent<SpriteRenderer>();
        }

        elapsed = 0f;
        transform.localScale = Vector3.one * 0.1f;
        maxScale = Vector3.one * radius * 1.8f;

        if (sr != null)
        {
            sr.enabled = true;
            sr.color = new Color(1f, 0.45f, 0.05f, 0.85f);
        }
    }

    static Sprite CreateCircleSprite()
    {
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

        elapsed += Time.deltaTime;
        float t = elapsed / maxDuration;

        transform.localScale = Vector3.Lerp(Vector3.one * 0.1f, maxScale, Mathf.Sin(t * Mathf.PI * 0.5f));

        if (sr != null)
        {
            float alpha = Mathf.Lerp(0.85f, 0f, t);
            sr.color = new Color(1f, Mathf.Lerp(0.45f, 0.15f, t), 0.05f, alpha);
        }

        if (elapsed >= maxDuration)
        {
            gameObject.SetActive(false);
        }
    }
}
