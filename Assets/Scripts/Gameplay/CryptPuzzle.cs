using UnityEngine;

/// <summary>
/// Acertijo de los cuatro símbolos de la cripta. La puerta de piedra escucha la bandera de resuelto;
/// aquí solo se encienden los símbolos grabados en la pared.
/// </summary>
public class CryptPuzzle : SequencePuzzle
{
    [Header("Cripta")]
    [Tooltip("Luces o resplandores que se encienden al resolver el acertijo.")]
    [SerializeField] private Behaviour[] glowOnSolved;

    protected override void OnSolved()
    {
        if (glowOnSolved == null) return;
        foreach (Behaviour b in glowOnSolved)
            if (b != null) b.enabled = true;
    }
}
