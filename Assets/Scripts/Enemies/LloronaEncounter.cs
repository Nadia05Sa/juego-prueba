using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public enum LloronaPhase
{
    Dormida,
    Aparicion,
    Busqueda,
    Escape
}

/// <summary>
/// Secuencia del encuentro final del nivel 1: aparición distante, búsqueda y escape.
/// Comienza cuando se resuelve el acertijo del aljibe.
/// </summary>
public class LloronaEncounter : MonoBehaviour
{
    [SerializeField] private SequencePuzzle triggerPuzzle;
    [SerializeField] private EnemyBrain llorona;

    [Header("Fase 1: aparición distante")]
    [SerializeField] private Transform apparitionPoint;
    [SerializeField] private float apparitionTime = 3f;
    [SerializeField] private AudioSource lamentSource;
    [TextArea(1, 3)]
    [SerializeField] private string apparitionMessage = "<b>¡Ay, mis hijos!</b> Un lamento helado sale del aljibe.";

    [Header("Fase 2: búsqueda")]
    [SerializeField] private Transform huntStartPoint;
    [SerializeField] private float delayBeforeHunt = 2f;
    [TextArea(1, 3)]
    [SerializeField] private string huntMessage = "Algo recorre el patio. Si te ve, corre (Shift) o escóndete (Q).";

    [Header("Ambiente")]
    [SerializeField] private Light2D globalLight;
    [SerializeField] private float dimmedIntensity = 0.16f;
    [SerializeField] private Color dimmedColor = new Color(0.35f, 0.42f, 0.7f);
    [TextArea(1, 2)]
    [SerializeField] private string gateHint = "A lo lejos, el portón principal cruje… ya puedes salir. Corre.";

    public LloronaPhase Phase { get; private set; } = LloronaPhase.Dormida;

    private void Start()
    {
        if (triggerPuzzle != null) triggerPuzzle.Solved += Begin;
        if (llorona != null) llorona.Deactivate();
    }

    private void OnDestroy()
    {
        if (triggerPuzzle != null) triggerPuzzle.Solved -= Begin;
    }

    public void Begin()
    {
        if (Phase != LloronaPhase.Dormida) return;
        StartCoroutine(Sequence());
    }

    private IEnumerator Sequence()
    {
        Phase = LloronaPhase.Aparicion;
        AudioManager.Instance?.DuckAmbience(0.3f, 0f);
        if (lamentSource != null && lamentSource.clip != null) lamentSource.Play();
        UIManager.Instance?.ShowMessage(apparitionMessage, 4f);
        yield return DimLights(1.5f);

        if (llorona != null && apparitionPoint != null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            Vector2 look = player != null ? (Vector2)(player.transform.position - apparitionPoint.position) : Vector2.down;
            llorona.Appear(apparitionPoint.position, look);
        }

        yield return new WaitForSeconds(apparitionTime);
        if (llorona != null) llorona.FadeDeactivate();
        yield return new WaitForSeconds(0.6f);

        yield return new WaitForSeconds(delayBeforeHunt);

        Phase = LloronaPhase.Busqueda;
        if (llorona != null)
        {
            Vector2 start = huntStartPoint != null ? (Vector2)huntStartPoint.position : (Vector2)llorona.transform.position;
            llorona.Activate(start);
        }
        UIManager.Instance?.ShowMessage(huntMessage, 5f);

        yield return new WaitForSeconds(4f);
        Phase = LloronaPhase.Escape;
        if (!string.IsNullOrEmpty(gateHint))
            UIManager.Instance?.ShowMessage(gateHint, 4.5f);
    }

    private IEnumerator DimLights(float duration)
    {
        if (globalLight == null) yield break;

        float startIntensity = globalLight.intensity;
        Color startColor = globalLight.color;
        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            float k = t / duration;
            globalLight.intensity = Mathf.Lerp(startIntensity, dimmedIntensity, k);
            globalLight.color = Color.Lerp(startColor, dimmedColor, k);
            yield return null;
        }
        globalLight.intensity = dimmedIntensity;
        globalLight.color = dimmedColor;
    }
}
