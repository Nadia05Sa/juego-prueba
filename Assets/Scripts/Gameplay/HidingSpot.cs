using UnityEngine;

/// <summary>
/// Zona donde el jugador puede esconderse con Q. Requiere un Collider2D con Is Trigger
/// y no debe tener colisión sólida, porque el jugador se coloca en su interior.
/// </summary>
public class HidingSpot : MonoBehaviour
{
    [SerializeField] private string spotName = "escondite";
    [SerializeField] private Transform hidePoint;

    public string SpotName => spotName;
    public Vector2 HidePosition => hidePoint != null ? (Vector2)hidePoint.position : (Vector2)transform.position;
}
