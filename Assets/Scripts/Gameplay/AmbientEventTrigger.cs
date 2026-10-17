using System.Collections;
using UnityEngine;

/// <summary>
/// Evento ambiental de una sola vez: mensaje, sonido y una aparición breve opcional.
/// Sirve para construir tensión antes de que la entidad sea peligrosa.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class AmbientEventTrigger : MonoBehaviour
{
    [TextArea(1, 3)]
    [SerializeField] private string message;
    [SerializeField] private AudioSource audioSource;
    [Tooltip("Opcional: objeto que aparece unos instantes (silueta lejana).")]
    [SerializeField] private GameObject glimpse;
    [SerializeField] private float glimpseDuration = 1.5f;
    [Tooltip("Opcional: solo se dispara si la bandera está (o no está) activa.")]
    [SerializeField] private string requiredFlag;
    [SerializeField] private bool requireFlagAbsent;
    [SerializeField] private float ambienceDuck = 0.5f;

    private bool fired;

    private void Start()
    {
        if (glimpse != null) glimpse.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (fired || !other.CompareTag("Player")) return;

        if (!string.IsNullOrEmpty(requiredFlag) && LevelManager.Instance != null)
        {
            bool has = LevelManager.Instance.HasFlag(requiredFlag);
            if (has == requireFlagAbsent) return;
        }

        fired = true;
        StartCoroutine(Run());
    }

    private IEnumerator Run()
    {
        if (!string.IsNullOrEmpty(message)) UIManager.Instance?.ShowMessage(message, 4f);
        if (audioSource != null && audioSource.clip != null) audioSource.Play();
        AudioManager.Instance?.DuckAmbience(ambienceDuck, 4f);

        if (glimpse == null) yield break;
        glimpse.SetActive(true);
        yield return new WaitForSeconds(glimpseDuration);
        glimpse.SetActive(false);
    }
}
