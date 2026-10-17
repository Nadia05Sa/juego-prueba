using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Checkpoint : MonoBehaviour
{
    [SerializeField] private SpriteRenderer marker;
    [SerializeField] private Color inactiveColor = new Color(0.4f, 0.4f, 0.5f, 0.35f);
    [SerializeField] private Color activeColor = new Color(1f, 0.75f, 0.3f, 0.6f);
    [SerializeField] private bool showMessage = true;

    private void Start()
    {
        if (marker != null) marker.color = inactiveColor;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player") || CheckpointManager.Instance == null) return;
        if (!CheckpointManager.Instance.SetCheckpoint(transform.position)) return;

        foreach (Checkpoint c in FindObjectsByType<Checkpoint>())
            if (c.marker != null) c.marker.color = c == this ? activeColor : inactiveColor;

        if (showMessage) UIManager.Instance?.ShowMessage("Punto de control", 2f);
    }
}
