using UnityEngine;
using UnityEngine.UI;

// 보스 체력바의 등장 연출, 실시간 체력 표시 및 페이즈 마커를 관리하는 스크립트.

public class BossHPUI : MonoBehaviour
{
    const float MinFillDuration = 0.1f;

    [Header("HP Bar")]
    public Slider hpSlider; // 보스 체력바로 사용할 Slider.
    public float fillDuration = 2.5f; // 보스 등장 연출 때 체력바가 0에서 1까지 차는 시간.

    [Header("Phase Marker")]    //포스 체력바에 페이즈를 표기하는 변수.
    public bool showPhaseMarkers = true; // 보스 체력바에 페이즈 전환 지점 표시 여부.
    public float phase2MarkerRate = 0.7f; // 보스 HP 70% 지점 표시.
    public float phase3MarkerRate = 0.4f; // 보스 HP 40% 지점 표시.
    public float phaseMarkerWidth = 1f; // 페이즈 마커의 두께.
    public Color phaseMarkerColor = new Color(1f, 1f, 1f, 0.85f); // 페이즈 마커 색상.

    Enemy boss; // 현재 체력바와 연결된 보스.
    bool isIntroFill; // 보스 등장 연출로 체력바를 서서히 채우는 중인지 체크.
    Image phase2Marker; // 70% 페이즈 마커.
    Image phase3Marker; // 40% 페이즈 마커.

    void Awake()
    {
        EnsureSlider();
    }

    // 보스 등장 연출에 맞춰 BossHP UI를 켜고 빈 체력바를 서서히 채운다.
    public void Show(Enemy boss)
    {
        gameObject.SetActive(true);
        EnsureSlider();

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

    // 보스전이 끝나거나 게임이 초기화될 때 BossHP UI를 숨긴다.
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

    // 보스 등장 연출 중에는 fillDuration에 맞춰 채우고, 이후에는 실제 보스 체력 비율을 반영.
    void LateUpdate()
    {
        if (boss == null || hpSlider == null)
            return;

        if (isIntroFill)
        {
            float duration = Mathf.Max(MinFillDuration, fillDuration);
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

    // Slider 참조가 비어 있으면 같은 오브젝트에서 자동으로 찾아온다.
    void EnsureSlider()
    {
        if (hpSlider == null)
        {
            hpSlider = GetComponent<Slider>();
        }
    }

    // 70%와 40% 지점 마커를 생성하고, 인스펙터 값이 반영되도록 위치와 색상을 갱신.
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

    // 지정한 체력 비율 위치에 UI Image 마커를 만든다.
    Image CreatePhaseMarker(string markerName, float rate)
    {
        GameObject markerObject = new GameObject(markerName);
        markerObject.transform.SetParent(transform, false);

        Image marker = markerObject.AddComponent<Image>();
        UpdatePhaseMarker(marker, rate);

        markerObject.SetActive(false);
        return marker;
    }

    // 인스펙터에서 마커 색상, 위치, 두께를 바꿨을 때 생성된 마커에도 반영한다.
    void UpdatePhaseMarker(Image marker, float rate)
    {
        if (marker == null)
            return;

        marker.color = phaseMarkerColor;

        RectTransform rect = marker.GetComponent<RectTransform>();
        float markerRate = Mathf.Clamp01(rate);
        rect.anchorMin = new Vector2(markerRate, 0f);
        rect.anchorMax = new Vector2(markerRate, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(Mathf.Max(0f, phaseMarkerWidth), 0f);
        rect.anchoredPosition = Vector2.zero;
    }

    // 생성된 페이즈 마커들을 한 번에 켜거나 끈다.
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
