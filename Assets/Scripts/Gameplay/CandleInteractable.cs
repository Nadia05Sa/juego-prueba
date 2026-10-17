using UnityEngine;

public class CandleInteractable : Interactable
{
    [SerializeField] private SpriteRenderer flame;
    [Tooltip("Luz de la vela (por ejemplo Light2D).")]
    [SerializeField] private Behaviour flameLight;
    [SerializeField] private bool startLit = true;
    [SerializeField] private float flickerAmount = 0.15f;

    private bool lit;
    private Vector3 flameScale;

    private void Start()
    {
        if (flame != null) flameScale = flame.transform.localScale;
        SetLit(startLit);
    }

    private void Update()
    {
        if (!lit || flame == null) return;
        float k = 1f + (Mathf.PerlinNoise(Time.time * 6f, transform.position.x) - 0.5f) * flickerAmount * 2f;
        flame.transform.localScale = flameScale * k;
    }

    public override string GetPrompt() => lit ? "Apagar vela" : "Encender vela";

    public override void Interact(InteractionController interactor) => SetLit(!lit);

    private void SetLit(bool value)
    {
        lit = value;
        if (flame != null) flame.enabled = value;
        if (flameLight != null) flameLight.enabled = value;
    }
}
