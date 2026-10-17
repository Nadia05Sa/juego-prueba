using UnityEngine;

/// <summary>
/// Objeto que solo se ve al iluminarlo con la linterna. Al revelarse por completo queda visible
/// y activa sus objetos asociados (por ejemplo, la zona de interacción de una nota).
/// </summary>
public class LightRevealable : MonoBehaviour
{
    [SerializeField] private SpriteRenderer[] renderers;
    [Tooltip("Se activan al revelarse (p. ej. el trigger de interacción).")]
    [SerializeField] private GameObject[] enableWhenRevealed;
    [SerializeField] private float revealTime = 0.6f;
    [TextArea(1, 3)]
    [SerializeField] private string revealMessage = "La luz de la linterna revela algo escondido.";

    private FlashlightController flashlight;
    private float progress;
    private float[] baseAlpha;

    public bool IsRevealed { get; private set; }

    private void Start()
    {
        flashlight = FindAnyObjectByType<FlashlightController>();
        baseAlpha = new float[renderers != null ? renderers.Length : 0];
        for (int i = 0; i < baseAlpha.Length; i++)
            baseAlpha[i] = renderers[i] != null ? renderers[i].color.a : 1f;

        SetTargetsActive(false);
        ApplyAlpha(0f);
    }

    private void Update()
    {
        if (IsRevealed) return;

        bool lit = flashlight != null && flashlight.IsIlluminating(transform.position);
        progress = Mathf.Clamp(progress + (lit ? Time.deltaTime : -Time.deltaTime * 0.5f), 0f, revealTime);
        ApplyAlpha(revealTime > 0f ? progress / revealTime : 1f);

        if (progress >= revealTime) Reveal();
    }

    public void Reveal()
    {
        if (IsRevealed) return;
        IsRevealed = true;
        ApplyAlpha(1f);
        SetTargetsActive(true);
        AudioManager.Instance?.Play(AudioManager.Sfx.Clue, 0.6f);
        UIManager.Instance?.ShowMessage(revealMessage);
    }

    private void SetTargetsActive(bool active)
    {
        if (enableWhenRevealed == null) return;
        foreach (GameObject go in enableWhenRevealed)
            if (go != null) go.SetActive(active);
    }

    private void ApplyAlpha(float k)
    {
        if (renderers == null) return;
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null) continue;
            Color c = renderers[i].color;
            c.a = baseAlpha[i] * k;
            renderers[i].color = c;
        }
    }
}
