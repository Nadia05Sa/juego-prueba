using System.Collections;
using UnityEngine;

/// <summary>
/// Silueta inofensiva a lo lejos que se desvanece cuando el jugador se acerca
/// (fase de presencia antes de que la entidad sea peligrosa).
/// </summary>
public class ApparitionGlimpse : MonoBehaviour
{
    [SerializeField] private SpriteRenderer[] renderers;
    [SerializeField] private float vanishDistance = 5f;
    [SerializeField] private float fadeTime = 0.6f;
    [SerializeField] private AudioSource sound;
    [TextArea(1, 3)]
    [SerializeField] private string message;
    [Tooltip("Opcional: desaparece sin más cuando se activa esta bandera.")]
    [SerializeField] private string hideOnFlag;

    private Transform player;
    private bool gone;

    private void Start()
    {
        GameObject p = GameObject.FindWithTag("Player");
        if (p != null) player = p.transform;
    }

    private void Update()
    {
        if (gone) return;

        if (!string.IsNullOrEmpty(hideOnFlag) && LevelManager.Instance != null && LevelManager.Instance.HasFlag(hideOnFlag))
        {
            gone = true;
            gameObject.SetActive(false);
            return;
        }

        if (player != null && Vector2.Distance(player.position, transform.position) <= vanishDistance)
            StartCoroutine(Vanish());
    }

    private IEnumerator Vanish()
    {
        gone = true;
        if (sound != null && sound.clip != null) sound.Play();
        if (!string.IsNullOrEmpty(message)) UIManager.Instance?.ShowMessage(message);

        for (float t = 0f; t < fadeTime; t += Time.deltaTime)
        {
            SetAlpha(1f - t / fadeTime);
            yield return null;
        }
        SetAlpha(0f);
    }

    private void SetAlpha(float k)
    {
        if (renderers == null) return;
        foreach (SpriteRenderer r in renderers)
        {
            if (r == null) continue;
            Color c = r.color;
            c.a = Mathf.Min(c.a, k);
            r.color = c;
        }
    }
}
