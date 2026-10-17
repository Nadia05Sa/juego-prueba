using System.Collections;
using UnityEngine;

/// <summary>
/// Persecución final: al activarse la bandera, la entidad aparece y sigue al jugador hasta la salida.
/// Si el jugador reintenta desde el punto de control, la persecución vuelve a empezar.
/// </summary>
public class FinalChaseSequence : MonoBehaviour
{
    [SerializeField] private string triggerFlag = "palanca_activada";
    [SerializeField] private EnemyBrain hunter;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private float delay = 2f;
    [SerializeField] private float retryDelay = 3f;
    [SerializeField] private float relentlessTime = 600f;
    [SerializeField] private AudioSource warningSound;
    [TextArea(1, 3)]
    [SerializeField] private string warningMessage = "Se oyen cascos golpeando la tierra, cada vez más cerca...";
    [TextArea(1, 3)]
    [SerializeField] private string chaseMessage = "¡Corre hacia el portón!";

    public bool Started { get; private set; }

    private void Start()
    {
        if (LevelManager.Instance == null) return;
        LevelManager.Instance.OnFlagSet += HandleFlag;
        LevelManager.Instance.OnRetry += HandleRetry;
    }

    private void OnDestroy()
    {
        if (LevelManager.Instance == null) return;
        LevelManager.Instance.OnFlagSet -= HandleFlag;
        LevelManager.Instance.OnRetry -= HandleRetry;
    }

    private void HandleFlag(string flag)
    {
        if (flag != triggerFlag || Started) return;
        Started = true;
        StartCoroutine(Begin(delay));
    }

    private void HandleRetry()
    {
        if (!Started || (LevelManager.Instance != null && LevelManager.Instance.IsCompleting)) return;
        StopAllCoroutines();
        StartCoroutine(Begin(retryDelay));
    }

    private IEnumerator Begin(float wait)
    {
        AudioManager.Instance?.DuckAmbience(0.3f, 0f);
        if (warningSound != null && warningSound.clip != null) warningSound.Play();
        UIManager.Instance?.ShowMessage(warningMessage, 4f);
        yield return new WaitForSeconds(wait);

        if (hunter == null || spawnPoint == null) yield break;
        hunter.StartChase(spawnPoint.position, relentlessTime);
        UIManager.Instance?.ShowMessage(chaseMessage, 4f);
    }
}
