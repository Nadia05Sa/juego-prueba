using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HUD y paneles del nivel: mensaje de interacción, objetivo, mensajes temporales,
/// panel de lectura, inventario, pausa, Game Over y fundidos.
/// </summary>
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("HUD")]
    [SerializeField] private Text promptText;
    [SerializeField] private Text objectiveText;
    [SerializeField] private Text messageText;

    [Header("Panel de lectura")]
    [SerializeField] private GameObject notePanel;
    [SerializeField] private Text noteTitle;
    [SerializeField] private Text noteBody;

    [Header("Inventario")]
    [SerializeField] private InventoryUI inventoryUI;

    [Header("Tutorial")]
    [SerializeField] private TutorialUI tutorialUI;

    [Header("Fotografía")]
    [SerializeField] private PhotoInspectUI photoInspectUI;

    [Header("Pantallas")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject levelCompletePanel;
    [SerializeField] private Text levelCompleteTitle;
    [SerializeField] private Text levelCompleteText;
    [SerializeField] private Image fadeImage;
    [SerializeField] private float fadeDuration = 1f;

    [Header("Referencias")]
    [SerializeField] private PlayerMovement playerMovement;

    private float messageTimer;
    private int noteOpenedFrame = -1;
    private int noteClosedFrame = -1;
    private Coroutine fadeRoutine;

    public bool IsReading => notePanel != null && notePanel.activeSelf;
    public bool IsInspectingPhoto => photoInspectUI != null && photoInspectUI.IsOpen;
    public bool IsInventoryOpen => inventoryUI != null && inventoryUI.IsOpen;
    public bool IsTutorialOpen => tutorialUI != null && tutorialUI.IsOpen;
    public bool IsPaused => pausePanel != null && pausePanel.activeSelf;
    public bool IsGameOver => gameOverPanel != null && gameOverPanel.activeSelf;
    public bool IsLevelComplete => levelCompletePanel != null && levelCompletePanel.activeSelf;
    public bool IsBlockingInput => IsReading || IsInspectingPhoto || IsInventoryOpen || IsTutorialOpen || IsPaused || IsGameOver || IsLevelComplete || Time.frameCount == noteClosedFrame;

    private void Awake()
    {
        Instance = this;
        SetPrompt(string.Empty);
        if (messageText != null) messageText.text = string.Empty;
        if (notePanel != null) notePanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (levelCompletePanel != null) levelCompletePanel.SetActive(false);
        if (inventoryUI == null) inventoryUI = GetComponentInChildren<InventoryUI>(true);
        if (photoInspectUI == null) photoInspectUI = GetComponentInChildren<PhotoInspectUI>(true);
        if (tutorialUI == null) tutorialUI = GetComponentInChildren<TutorialUI>(true);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        if (messageTimer > 0f)
        {
            messageTimer -= Time.unscaledDeltaTime;
            if (messageTimer <= 0f && messageText != null) messageText.text = string.Empty;
        }

        GameInput input = GameInput.Instance;
        if (input == null) return;

        if (IsTutorialOpen) return;

        if (IsReading)
        {
            if (Time.frameCount != noteOpenedFrame && (input.InteractPressed || input.PausePressed))
                CloseNote();
            return;
        }

        if (IsInspectingPhoto)
        {
            if (input.PausePressed && photoInspectUI.CanClose)
                ClosePhoto();
            else if (input.InteractPressed && photoInspectUI.CanFlip)
                photoInspectUI.Flip();
            return;
        }

        if (IsInventoryOpen)
        {
            // Solo teclado: el clic en notas lo maneja InventoryUI (no cerrar con E al hacer clic).
            if (input.InventoryPressed || input.PausePressed)
                CloseInventory();
            return;
        }

        if (input.InventoryPressed && !IsGameOver && !IsLevelComplete && !IsPaused)
            OpenInventory();

        if (input.PausePressed && !IsGameOver && !IsLevelComplete)
        {
            if (IsPaused) Resume();
            else Pause();
        }
    }

    // ---------- HUD ----------

    public void SetPrompt(string text)
    {
        if (promptText == null) return;
        promptText.text = text;
        promptText.enabled = !string.IsNullOrEmpty(text);
    }

    public void SetObjective(string text)
    {
        if (objectiveText != null) objectiveText.text = string.IsNullOrEmpty(text) ? string.Empty : "Objetivo: " + text;
    }

    public void ShowMessage(string text, float duration = 3.5f)
    {
        if (messageText == null || string.IsNullOrEmpty(text)) return;
        messageText.text = EcosRichText.Format(text);
        messageText.alignment = TextAnchor.MiddleCenter;
        messageText.supportRichText = true;
        messageTimer = duration;
    }

    public void ShowTutorial() => tutorialUI?.Show();

    // ---------- Panel de lectura ----------

    private string pendingAfterNote;

    public void ShowNote(string title, string body, string afterCloseMessage = null)
    {
        if (notePanel == null) return;
        CloseInventory();
        ClosePhoto();
        pendingAfterNote = afterCloseMessage;
        if (noteTitle != null)
        {
            noteTitle.text = EcosRichText.Format(title ?? string.Empty);
            noteTitle.alignment = TextAnchor.MiddleCenter;
            noteTitle.supportRichText = true;
        }
        if (noteBody != null)
        {
            noteBody.text = EcosRichText.Format(body ?? string.Empty);
            noteBody.alignment = TextAnchor.MiddleCenter;
            noteBody.supportRichText = true;
        }
        notePanel.SetActive(true);
        noteOpenedFrame = Time.frameCount;
        playerMovement?.AddLock(this);
    }

    public void CloseNote()
    {
        if (notePanel == null) return;
        notePanel.SetActive(false);
        noteClosedFrame = Time.frameCount;
        playerMovement?.RemoveLock(this);
        if (!string.IsNullOrEmpty(pendingAfterNote))
        {
            string follow = pendingAfterNote;
            pendingAfterNote = null;
            ShowMessage(follow, 5f);
        }
    }

    // ---------- Fotografía ----------

    public void ShowPhoto(Sprite front, Sprite back, string frontCaption, string backCaption,
        string hint = null, bool allowFlip = true, bool inscriptionOnImage = false)
    {
        if (photoInspectUI == null) return;
        CloseInventory();
        if (notePanel != null && notePanel.activeSelf) CloseNote();
        photoInspectUI.Show(front, back, EcosRichText.Format(frontCaption), EcosRichText.Format(backCaption),
            hint, allowFlip, inscriptionOnImage);
        playerMovement?.AddLock(this);
    }

    public void ClosePhoto()
    {
        if (photoInspectUI == null || !photoInspectUI.IsOpen) return;
        photoInspectUI.Close();
        noteClosedFrame = Time.frameCount;
        playerMovement?.RemoveLock(this);
    }

    // ---------- Inventario ----------

    public void OpenInventory()
    {
        if (inventoryUI == null) return;
        ClosePhoto();
        inventoryUI.Open();
        playerMovement?.AddLock(this);
    }

    public void CloseInventory()
    {
        if (inventoryUI == null || !inventoryUI.IsOpen) return;
        inventoryUI.Close();
        playerMovement?.RemoveLock(this);
    }

    // ---------- Pausa y Game Over ----------

    public void Pause()
    {
        if (pausePanel == null) return;
        CloseInventory();
        ClosePhoto();
        pausePanel.SetActive(true);
        Time.timeScale = 0f;
    }

    public void Resume()
    {
        if (pausePanel == null) return;
        pausePanel.SetActive(false);
        Time.timeScale = 1f;
    }

    public void ShowGameOver()
    {
        if (notePanel != null && notePanel.activeSelf) CloseNote();
        CloseInventory();
        ClosePhoto();
        if (gameOverPanel != null) gameOverPanel.SetActive(true);
    }

    public void HideGameOver()
    {
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
    }

    public void ShowLevelComplete(string title, string text)
    {
        if (notePanel != null && notePanel.activeSelf) CloseNote();
        CloseInventory();
        ClosePhoto();
        if (pausePanel != null) pausePanel.SetActive(false);
        SetPrompt(string.Empty);
        if (levelCompleteTitle != null) levelCompleteTitle.text = title;
        if (levelCompleteText != null) levelCompleteText.text = text;
        if (levelCompletePanel != null) levelCompletePanel.SetActive(true);
    }

    // ---------- Fundidos ----------

    public void FadeIn() => StartFade(1f, 0f);

    public void FadeOut() => StartFade(0f, 1f);

    private void StartFade(float from, float to)
    {
        if (fadeImage == null) return;
        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(Fade(from, to));
    }

    private IEnumerator Fade(float from, float to)
    {
        fadeImage.gameObject.SetActive(true);
        Color c = fadeImage.color;
        for (float t = 0f; t < fadeDuration; t += Time.unscaledDeltaTime)
        {
            c.a = Mathf.Lerp(from, to, t / fadeDuration);
            fadeImage.color = c;
            yield return null;
        }
        c.a = to;
        fadeImage.color = c;
        if (to <= 0f) fadeImage.gameObject.SetActive(false);
        fadeRoutine = null;
    }
}
