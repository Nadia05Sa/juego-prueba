using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class LevelExit : MonoBehaviour
{
    [SerializeField] private string requiredFlag = "aljibe_resuelto";
    [SerializeField] private string notReadyMessage = "Todavía no puedes irte.";

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player") || LevelManager.Instance == null) return;

        if (LevelManager.Instance.HasFlag(requiredFlag)) LevelManager.Instance.CompleteLevel();
        else UIManager.Instance?.ShowMessage(notReadyMessage);
    }
}
