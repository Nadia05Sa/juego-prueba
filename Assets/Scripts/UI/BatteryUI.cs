using UnityEngine;
using UnityEngine.UI;

/// <summary>Barra de batería de la linterna (HUD, abajo a la derecha).</summary>
public class BatteryUI : MonoBehaviour
{
    [SerializeField] private FlashlightController flashlight;
    [SerializeField] private RectTransform fill;
    [SerializeField] private Image fillImage;
    [SerializeField] private Text label;
    [SerializeField] private Color normalColor = new Color(1f, 0.85f, 0.4f);
    [SerializeField] private Color lowColor = new Color(0.9f, 0.2f, 0.15f);

    private void Update()
    {
        if (flashlight == null) return;

        float value = flashlight.Battery01;
        if (fill != null) fill.anchorMax = new Vector2(value, 1f);
        if (fillImage != null) fillImage.color = flashlight.IsLow ? lowColor : normalColor;
        if (label != null)
            label.text = flashlight.IsOn
                ? $"F — Linterna  {Mathf.CeilToInt(value * 100f)}%"
                : $"F — Linterna apagada  {Mathf.CeilToInt(value * 100f)}%";
    }
}
