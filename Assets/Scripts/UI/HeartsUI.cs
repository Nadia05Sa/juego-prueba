using UnityEngine;
using UnityEngine.UI;

public class HeartsUI : MonoBehaviour
{
    [SerializeField] private PlayerHealth health;
    [SerializeField] private Image[] hearts;
    [SerializeField] private Color fullColor = new Color(0.85f, 0.1f, 0.15f);
    [SerializeField] private Color emptyColor = new Color(0.2f, 0.2f, 0.2f, 0.6f);

    [Header("Destello de daño")]
    [SerializeField] private Image damageOverlay;
    [SerializeField] private float overlayAlpha = 0.35f;
    [SerializeField] private float overlayFadeSpeed = 1.5f;

    private void Start()
    {
        if (health == null) return;
        health.OnLivesChanged += Refresh;
        health.OnDamaged += Flash;
        Refresh(health.Lives);
        SetOverlayAlpha(0f);
    }

    private void OnDestroy()
    {
        if (health == null) return;
        health.OnLivesChanged -= Refresh;
        health.OnDamaged -= Flash;
    }

    private float pulseTimer;

    private void Update()
    {
        if (damageOverlay != null && damageOverlay.color.a > 0f)
            SetOverlayAlpha(Mathf.MoveTowards(damageOverlay.color.a, 0f, overlayFadeSpeed * Time.unscaledDeltaTime));

        if (pulseTimer > 0f)
        {
            pulseTimer -= Time.unscaledDeltaTime;
            float k = 1f + Mathf.Sin(Time.unscaledTime * 18f) * 0.12f * Mathf.Clamp01(pulseTimer);
            for (int i = 0; i < hearts.Length; i++)
                if (hearts[i] != null) hearts[i].rectTransform.localScale = Vector3.one * k;
        }
        else
        {
            for (int i = 0; i < hearts.Length; i++)
                if (hearts[i] != null) hearts[i].rectTransform.localScale = Vector3.one;
        }
    }

    private void Refresh(int lives)
    {
        for (int i = 0; i < hearts.Length; i++)
            if (hearts[i] != null) hearts[i].color = i < lives ? fullColor : emptyColor;
    }

    private void Flash()
    {
        SetOverlayAlpha(overlayAlpha);
        pulseTimer = 0.45f;
    }

    private void SetOverlayAlpha(float a)
    {
        if (damageOverlay == null) return;
        Color c = damageOverlay.color;
        c.a = a;
        damageOverlay.color = c;
    }
}
