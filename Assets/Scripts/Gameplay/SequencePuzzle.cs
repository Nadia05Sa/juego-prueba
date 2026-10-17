using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Acertijo de secuencia: las placas deben activarse en un orden concreto.
/// La secuencia se comprueba completa al final, para que no se pueda resolver probando placa por placa.
/// </summary>
public class SequencePuzzle : MonoBehaviour
{
    [SerializeField] private List<string> correctOrder = new List<string>();
    [SerializeField] private List<PuzzlePlate> plates = new List<PuzzlePlate>();

    [Header("Requisitos")]
    [SerializeField] private int requiredClues;
    [TextArea(2, 4)]
    [SerializeField] private string notReadyMessage = "No sabes qué significan estos símbolos. Busca más pistas.";

    [Header("Resultado")]
    [SerializeField] private string flagOnSolved;
    [TextArea(2, 4)]
    [SerializeField] private string solvedMessage;
    [TextArea(2, 4)]
    [SerializeField] private string errorMessage = "El mecanismo se traba y las placas vuelven a su lugar. Prueba otro orden.";
    [SerializeField] private float noiseRadiusOnSolve = 8f;
    [Tooltip("Ruido de cada placa al presionarla (0 = silenciosa).")]
    [SerializeField] private float noiseRadiusOnPress;
    [SerializeField] private float noiseRadiusOnError;
    [SerializeField] private float errorResetDelay = 1f;

    public UnityEvent onSolved;
    public event Action Solved;

    private readonly List<string> entered = new List<string>();
    private bool resetting;

    public bool IsSolved { get; private set; }

    public bool CanPress(PuzzlePlate plate) => !IsSolved && !resetting && plate.State == PlateState.Inactiva;

    public void Press(PuzzlePlate plate)
    {
        if (!CanPress(plate)) return;

        if (requiredClues > 0 && (ClueManager.Instance == null || ClueManager.Instance.Count < requiredClues))
        {
            UIManager.Instance?.ShowMessage(notReadyMessage);
            return;
        }

        plate.SetState(PlateState.Activada);
        entered.Add(plate.SymbolId);
        AudioManager.Instance?.Play(AudioManager.Sfx.PlatePress);
        NoiseSystem.Emit(plate.transform.position, noiseRadiusOnPress, plate.gameObject);

        if (entered.Count < correctOrder.Count) return;

        bool correct = true;
        for (int i = 0; i < correctOrder.Count; i++)
            if (entered[i] != correctOrder[i]) correct = false;

        if (correct) Solve();
        else StartCoroutine(FailRoutine());
    }

    private IEnumerator FailRoutine()
    {
        resetting = true;
        AudioManager.Instance?.Play(AudioManager.Sfx.Error);
        NoiseSystem.Emit(transform.position, noiseRadiusOnError, gameObject);
        UIManager.Instance?.ShowMessage(errorMessage);
        foreach (PuzzlePlate p in plates) p.SetState(PlateState.Error);

        yield return new WaitForSeconds(errorResetDelay);
        ResetPuzzle();
    }

    public void ResetPuzzle()
    {
        if (IsSolved) return;
        entered.Clear();
        foreach (PuzzlePlate p in plates) p.SetState(PlateState.Inactiva);
        resetting = false;
    }

    private void Solve()
    {
        IsSolved = true;
        AudioManager.Instance?.Play(AudioManager.Sfx.Solve);
        NoiseSystem.Emit(transform.position, noiseRadiusOnSolve, gameObject);
        if (!string.IsNullOrEmpty(solvedMessage)) UIManager.Instance?.ShowMessage(solvedMessage, 5f);

        OnSolved();
        LevelManager.Instance?.SetFlag(flagOnSolved);
        onSolved?.Invoke();
        Solved?.Invoke();
    }

    protected virtual void OnSolved() { }
}
