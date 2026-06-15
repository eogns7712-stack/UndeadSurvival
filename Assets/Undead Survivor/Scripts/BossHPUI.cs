using UnityEngine;
using UnityEngine.UI;

public class BossHPUI : MonoBehaviour
{
    public Slider hpSlider;
    public float fillDuration = 2.5f;

    Enemy boss;
    bool isIntroFill;

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
}
