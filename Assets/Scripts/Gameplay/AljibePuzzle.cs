using System.Collections;
using UnityEngine;

public class AljibePuzzle : SequencePuzzle
{
    [Header("Aljibe")]
    [SerializeField] private SpriteRenderer aljibeRenderer;
    [SerializeField] private Color solvedColor = new Color(0.2f, 0.55f, 0.65f);
    [Tooltip("Luz (por ejemplo Light2D) que se enciende al resolver el acertijo.")]
    [SerializeField] private Behaviour aljibeLight;
    [SerializeField] private float riseDuration = 1.4f;
    [SerializeField] private float riseScale = 1.18f;

    protected override void OnSolved()
    {
        if (aljibeLight != null) aljibeLight.enabled = true;
        if (aljibeRenderer != null) StartCoroutine(RiseWater());
    }

    private IEnumerator RiseWater()
    {
        Color start = aljibeRenderer.color;
        Vector3 startScale = aljibeRenderer.transform.localScale;
        Vector3 endScale = startScale * riseScale;
        for (float t = 0f; t < riseDuration; t += Time.deltaTime)
        {
            float k = Mathf.SmoothStep(0f, 1f, t / riseDuration);
            aljibeRenderer.color = Color.Lerp(start, solvedColor, k);
            aljibeRenderer.transform.localScale = Vector3.Lerp(startScale, endScale, k);
            yield return null;
        }
        aljibeRenderer.color = solvedColor;
        aljibeRenderer.transform.localScale = endScale;
    }
}
