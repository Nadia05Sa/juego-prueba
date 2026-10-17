using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Panel para examinar una fotografía o lápida: frente/reverso, o inscripción sobre la imagen.
/// </summary>
public class PhotoInspectUI : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private RectTransform photoRoot;
    [SerializeField] private Image photoImage;
    [SerializeField] private Image frameImage;
    [SerializeField] private Text captionText;
    [SerializeField] private Text hintText;
    [SerializeField] private Text inscriptionOverlay;
    [SerializeField] private float flipDuration = 0.35f;

    private Sprite front;
    private Sprite back;
    private string frontCaption;
    private string backCaption;
    private bool showingBack;
    private bool flipping;
    private bool flipEnabled = true;
    private bool textOnImage;
    private int openedFrame = -1;
    private Coroutine flipRoutine;

    public bool IsOpen => panel != null && panel.activeSelf;
    public bool CanClose => IsOpen && !flipping && Time.frameCount != openedFrame;
    public bool CanFlip => IsOpen && flipEnabled && !flipping && Time.frameCount != openedFrame;

    public void Show(Sprite frontSprite, Sprite backSprite, string frontText, string backText,
        string titleHint = null, bool allowFlip = true, bool inscriptionOnImage = false)
    {
        if (panel == null || frontSprite == null) return;

        front = frontSprite;
        back = backSprite != null ? backSprite : frontSprite;
        frontCaption = frontText ?? string.Empty;
        backCaption = backText ?? string.Empty;
        showingBack = false;
        flipping = false;
        flipEnabled = allowFlip;
        textOnImage = inscriptionOnImage;

        if (photoImage != null)
        {
            photoImage.sprite = front;
            photoImage.preserveAspect = true;
            // Lápidas: el sprite ya trae color de piedra; fotos usan blanco.
            photoImage.color = inscriptionOnImage
                ? new Color(0.92f, 0.9f, 0.86f, 1f)
                : Color.white;
        }
        if (photoRoot != null) photoRoot.localScale = Vector3.one;

        ApplyCaption(frontCaption);

        if (hintText != null)
        {
            if (!string.IsNullOrEmpty(titleHint))
                hintText.text = titleHint;
            else if (!allowFlip)
                hintText.text = "Esc — Cerrar";
            else
                hintText.text = "E — Voltear    ·    Esc — Guardar";
            hintText.alignment = TextAnchor.MiddleCenter;
            hintText.color = new Color(0.9f, 0.84f, 0.72f, 1f);
        }

        panel.SetActive(true);
        openedFrame = Time.frameCount;
        AudioManager.Instance?.Play(AudioManager.Sfx.Clue, 0.45f);
    }

    public void Flip()
    {
        if (!CanFlip) return;
        if (flipRoutine != null) StopCoroutine(flipRoutine);
        flipRoutine = StartCoroutine(FlipRoutine());
    }

    public void Close()
    {
        if (!IsOpen) return;
        if (flipRoutine != null)
        {
            StopCoroutine(flipRoutine);
            flipRoutine = null;
        }
        flipping = false;
        if (inscriptionOverlay != null) inscriptionOverlay.gameObject.SetActive(false);
        panel.SetActive(false);
        if (photoRoot != null) photoRoot.localScale = Vector3.one;
    }

    private void ApplyCaption(string text)
    {
        if (textOnImage && inscriptionOverlay != null)
        {
            // Tinta oscura sobre piedra: alto contraste y tamaño legible.
            inscriptionOverlay.gameObject.SetActive(true);
            inscriptionOverlay.color = new Color(0.1f, 0.07f, 0.04f, 1f);
            inscriptionOverlay.fontSize = 26;
            inscriptionOverlay.alignment = TextAnchor.MiddleCenter;
            inscriptionOverlay.supportRichText = true;
            inscriptionOverlay.horizontalOverflow = HorizontalWrapMode.Wrap;
            inscriptionOverlay.verticalOverflow = VerticalWrapMode.Overflow;
            inscriptionOverlay.text = text;
            // Pie bajo la lápida: refuerzo legible sobre fondo oscuro.
            if (captionText != null)
            {
                captionText.color = new Color(0.92f, 0.86f, 0.74f, 1f);
                captionText.fontSize = 24;
                captionText.alignment = TextAnchor.MiddleCenter;
                captionText.supportRichText = true;
                captionText.text = string.Empty;
            }
        }
        else
        {
            if (inscriptionOverlay != null) inscriptionOverlay.gameObject.SetActive(false);
            if (captionText != null)
            {
                captionText.color = new Color(0.92f, 0.88f, 0.78f, 1f);
                captionText.fontSize = 28;
                captionText.text = text;
                captionText.alignment = TextAnchor.MiddleCenter;
                captionText.supportRichText = true;
            }
        }
    }

    private IEnumerator FlipRoutine()
    {
        flipping = true;
        AudioManager.Instance?.Play(AudioManager.Sfx.Click, 0.55f);

        float half = Mathf.Max(0.05f, flipDuration * 0.5f);
        Vector3 scale = photoRoot != null ? photoRoot.localScale : Vector3.one;

        for (float t = 0f; t < half; t += Time.unscaledDeltaTime)
        {
            float k = t / half;
            if (photoRoot != null) photoRoot.localScale = new Vector3(Mathf.Lerp(1f, 0.02f, k), scale.y, 1f);
            yield return null;
        }

        showingBack = !showingBack;
        if (photoImage != null) photoImage.sprite = showingBack ? back : front;
        ApplyCaption(showingBack ? backCaption : frontCaption);
        if (hintText != null && flipEnabled)
            hintText.text = showingBack
                ? "E — Voltear al frente    ·    Esc — Guardar"
                : "E — Voltear    ·    Esc — Guardar";

        for (float t = 0f; t < half; t += Time.unscaledDeltaTime)
        {
            float k = t / half;
            if (photoRoot != null) photoRoot.localScale = new Vector3(Mathf.Lerp(0.02f, 1f, k), scale.y, 1f);
            yield return null;
        }

        if (photoRoot != null) photoRoot.localScale = Vector3.one;
        flipping = false;
        flipRoutine = null;
    }
}
