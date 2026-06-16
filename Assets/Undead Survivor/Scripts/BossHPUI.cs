using UnityEngine;
using UnityEngine.UI;

public class BossHPUI : MonoBehaviour
{
    public Slider hpSlider;
    public float fillDuration = 2.5f;
    public bool showPhaseMarkers = true;
    public float phase2MarkerRate = 0.7f;
    public float phase3MarkerRate = 0.4f;
    public float phaseMarkerWidth = 1f;
    public Color phaseMarkerColor = new Color(1f, 1f, 1f, 0.85f);

    Enemy boss;
    bool isIntroFill;
    Image phase2Marker;
    Image phase3Marker;

    void Awake()
    {
        if (hpSlider == null)
        {
            hpSlider = GetComponent<Slider>();
        }
    }

    public void Show(Enemy boss)
    {
        gameObject.SetActive(true);

        if (hpSlider == null)
        {
            hpSlider = GetComponent<Slider>();
        }

        if (boss == null)
        {
            Hide();
            return;
        }

        this.boss = boss;
        isIntroFill = true;
        EnsurePhaseMarkers();
        SetPhaseMarkersActive(showPhaseMarkers);

        if (hpSlider != null)
        {
            hpSlider.value = 0f;
        }
    }

    public void Hide()
    {
        boss = null;
        isIntroFill = false;

        if (hpSlider != null)
        {
            hpSlider.value = 0f;
        }

        SetPhaseMarkersActive(false);

        gameObject.SetActive(false);
    }

    void LateUpdate()
    {
        if (boss == null || hpSlider == null)
            return;

        if (isIntroFill)
        {
            float duration = Mathf.Max(0.1f, fillDuration);
            hpSlider.value = Mathf.MoveTowards(hpSlider.value, 1f, Time.unscaledDeltaTime / duration);
            if (hpSlider.value >= 1f)
            {
                isIntroFill = false;
            }
            return;
        }

        float curHealth = boss.healthPoint;
        float maxHealth = boss.maxhealthPoint;
        hpSlider.value = maxHealth > 0f ? curHealth / maxHealth : 0f;
    }

    void EnsurePhaseMarkers()
    {
        if (!showPhaseMarkers)
            return;

        if (phase2Marker == null)
        {
            phase2Marker = CreatePhaseMarker("Phase2Marker", phase2MarkerRate);
        }

        if (phase3Marker == null)
        {
            phase3Marker = CreatePhaseMarker("Phase3Marker", phase3MarkerRate);
        }

        UpdatePhaseMarker(phase2Marker, phase2MarkerRate);
        UpdatePhaseMarker(phase3Marker, phase3MarkerRate);
    }

    Image CreatePhaseMarker(string markerName, float rate)
    {
        GameObject markerObject = new GameObject(markerName);
        markerObject.transform.SetParent(transform, false);

        Image marker = markerObject.AddComponent<Image>();
        marker.color = phaseMarkerColor;

        RectTransform rect = markerObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(rate, 0f);
        rect.anchorMax = new Vector2(rate, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(phaseMarkerWidth, 0f);
        rect.anchoredPosition = Vector2.zero;

        markerObject.SetActive(false);
        return marker;
    }

    void UpdatePhaseMarker(Image marker, float rate)
    {
        if (marker == null)
            return;

        marker.color = phaseMarkerColor;

        RectTransform rect = marker.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(rate, 0f);
        rect.anchorMax = new Vector2(rate, 1f);
        rect.sizeDelta = new Vector2(phaseMarkerWidth, 0f);
        rect.anchoredPosition = Vector2.zero;
    }

    void SetPhaseMarkersActive(bool active)
    {
        if (phase2Marker != null)
        {
            phase2Marker.gameObject.SetActive(active);
        }

        if (phase3Marker != null)
        {
            phase3Marker.gameObject.SetActive(active);
        }
    }
}
