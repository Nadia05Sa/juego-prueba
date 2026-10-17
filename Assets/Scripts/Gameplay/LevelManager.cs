using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum ObjectiveCondition
{
    Flag,
    HasItem,
    ClueCount
}

[Serializable]
public class ObjectiveStep
{
    [Tooltip("Texto del objetivo. {0} se reemplaza por el número de pistas encontradas.")]
    public string text;
    public ObjectiveCondition condition;
    public string id;
    public int count;
}

/// <summary>
/// Coordina el progreso del nivel: banderas, objetivos, Game Over, reintentos, salida
/// y transición al siguiente nivel. Las puertas, enemigos y acertijos controlan su propio comportamiento.
/// </summary>
public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    [SerializeField] private string levelTitle = "Nivel 1 — El lamento del aljibe";
    [Tooltip("Mensaje opcional que aparece después del título (por ejemplo, un control nuevo).")]
    [SerializeField] private string startHint;
    [SerializeField] private string nextScene = "Victoria";
    [SerializeField] private string menuScene = "Menu";
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private PlayerHiding playerHiding;
    [SerializeField] private List<ObjectiveStep> objectives = new List<ObjectiveStep>();

    [Header("Al completar el nivel")]
    [TextArea(1, 3)]
    [SerializeField] private string exitMessage = "Cruzas el portón. El llanto se apaga a tus espaldas...";
    [SerializeField] private string completeTitle = "Nivel completado";
    [TextArea(2, 5)]
    [SerializeField] private string completeText;
    [SerializeField] private float completeScreenTime = 4f;
    [Tooltip("Identificadores del inventario que pasan al siguiente nivel.")]
    [SerializeField] private string[] carryOverItems;

    private readonly HashSet<string> flags = new HashSet<string>();
    private string currentObjective;
    private bool completing;

    public event Action<string> OnFlagSet;
    public event Action OnRetry;
    public bool IsCompleting => completing;

    private void Awake()
    {
        Instance = this;
        Time.timeScale = 1f;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        if (InventoryManager.Instance != null)
        {
            foreach ((string id, string name) in GameProgress.CarriedItems())
                InventoryManager.Instance.Add(id, name);
            InventoryManager.Instance.OnChanged += RefreshObjective;
        }
        if (ClueManager.Instance != null) ClueManager.Instance.OnChanged += RefreshObjective;
        if (playerHealth != null) playerHealth.OnDeath += HandleDeath;

        GameProgress.SaveLevelReached(SceneManager.GetActiveScene().name);

        UIManager.Instance?.FadeIn();
        RefreshObjective();
        StartCoroutine(BootSequence());
    }

    private IEnumerator BootSequence()
    {
        yield return null;
        // En batch/pruebas automáticas se omite el tutorial.
        if (!Application.isBatchMode)
        {
            UIManager.Instance?.ShowTutorial();
            while (UIManager.Instance != null && UIManager.Instance.IsTutorialOpen)
                yield return null;
        }

        UIManager.Instance?.ShowMessage(levelTitle, 3.5f);
        if (!string.IsNullOrEmpty(startHint))
        {
            yield return new WaitForSeconds(3.8f);
            UIManager.Instance?.ShowMessage(startHint, 7f);
        }
    }

    public bool HasFlag(string flag) => !string.IsNullOrEmpty(flag) && flags.Contains(flag);

    public void SetFlag(string flag)
    {
        if (string.IsNullOrEmpty(flag) || !flags.Add(flag)) return;
        OnFlagSet?.Invoke(flag);
        RefreshObjective();
    }

    private bool IsMet(ObjectiveStep step)
    {
        switch (step.condition)
        {
            case ObjectiveCondition.Flag: return HasFlag(step.id);
            case ObjectiveCondition.HasItem: return InventoryManager.Instance != null && InventoryManager.Instance.Has(step.id);
            case ObjectiveCondition.ClueCount: return ClueManager.Instance != null && ClueManager.Instance.Count >= step.count;
            default: return false;
        }
    }

    public void RefreshObjective()
    {
        int clues = ClueManager.Instance != null ? ClueManager.Instance.Count : 0;
        string text = string.Empty;

        foreach (ObjectiveStep step in objectives)
        {
            if (IsMet(step)) continue;
            text = string.Format(step.text, clues);
            break;
        }

        if (text == currentObjective) return;
        bool firstTime = currentObjective == null;
        currentObjective = text;
        UIManager.Instance?.SetObjective(text);
        if (!firstTime && !string.IsNullOrEmpty(text) && !completing)
            UIManager.Instance?.ShowMessage("Nuevo objetivo: " + text);
    }

    private void HandleDeath()
    {
        if (completing) return;
        playerMovement?.AddLock(this);
        Time.timeScale = 0f;
        UIManager.Instance?.ShowGameOver();
    }

    public void RetryFromCheckpoint()
    {
        Time.timeScale = 1f;
        UIManager.Instance?.HideGameOver();
        playerHiding?.ForceExit();
        playerMovement?.ClearLocks();

        if (CheckpointManager.Instance != null && playerMovement != null)
            CheckpointManager.Instance.Respawn(playerMovement);

        playerHealth?.RestoreFull();

        Vector2 playerPos = playerMovement != null ? (Vector2)playerMovement.transform.position : Vector2.zero;
        foreach (EnemyBrain enemy in FindObjectsByType<EnemyBrain>())
            enemy.ResetForRetry(playerPos);

        Camera cam = Camera.main;
        if (cam != null && cam.TryGetComponent(out CameraFollow follow)) follow.SnapToTarget();

        OnRetry?.Invoke();
    }

    public void RestartLevel() => SceneLoader.ReloadCurrent();

    public void GoToMenu() => SceneLoader.Load(menuScene);

    public void CompleteLevel()
    {
        if (completing) return;
        completing = true;
        SetFlag("nivel_completado");

        foreach (EnemyBrain enemy in FindObjectsByType<EnemyBrain>())
            enemy.Deactivate();

        SaveProgress();
        StartCoroutine(CompleteRoutine());
    }

    private void SaveProgress()
    {
        if (carryOverItems != null && InventoryManager.Instance != null)
        {
            foreach (InventoryManager.Item item in InventoryManager.Instance.Items)
                if (Array.IndexOf(carryOverItems, item.id) >= 0)
                    GameProgress.CarryItem(item.id, item.displayName);
        }

        if (GameProgress.IsLevel(nextScene)) GameProgress.SaveLevelReached(nextScene);
        else GameProgress.NewGame();
    }

    private IEnumerator CompleteRoutine()
    {
        playerMovement?.AddLock(this);
        AudioManager.Instance?.Play(AudioManager.Sfx.Solve);
        UIManager.Instance?.ShowMessage(exitMessage, 3f);
        yield return new WaitForSecondsRealtime(1.2f);

        UIManager.Instance?.ShowLevelComplete(completeTitle, completeText);
        yield return new WaitForSecondsRealtime(completeScreenTime);

        UIManager.Instance?.FadeOut();
        yield return new WaitForSecondsRealtime(1.2f);
        SceneLoader.Load(nextScene);
    }
}
