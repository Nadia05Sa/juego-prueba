using UnityEngine;

public enum PlateState
{
    Inactiva,
    Activada,
    Error
}

public class PuzzlePlate : Interactable
{
    [SerializeField] private string symbolId = "gota";
    [SerializeField] private string symbolName = "la gota";
    [SerializeField] private SequencePuzzle puzzle;
    [SerializeField] private SpriteRenderer plateRenderer;
    [SerializeField] private Color inactiveColor = new Color(0.48f, 0.46f, 0.42f);
    [SerializeField] private Color activeColor = new Color(1f, 0.88f, 0.35f);
    [SerializeField] private Color errorColor = new Color(1f, 0.2f, 0.18f);

    public string SymbolId => symbolId;
    public PlateState State { get; private set; }

    private void Start() => SetState(PlateState.Inactiva);

    public override string GetPrompt() => "Presionar la placa de " + symbolName;

    public override bool CanInteract => base.CanInteract && puzzle != null && puzzle.CanPress(this);

    public override void Interact(InteractionController interactor) => puzzle.Press(this);

    public void SetState(PlateState state)
    {
        State = state;
        if (plateRenderer == null) return;
        plateRenderer.color = state switch
        {
            PlateState.Activada => activeColor,
            PlateState.Error => errorColor,
            _ => inactiveColor
        };
        plateRenderer.transform.localScale = Vector3.one;
    }

    private void Update()
    {
        if (State != PlateState.Activada || plateRenderer == null) return;
        float pulse = 1f + Mathf.Sin(Time.time * 7f) * 0.1f;
        plateRenderer.transform.localScale = Vector3.one * pulse;
    }
}
