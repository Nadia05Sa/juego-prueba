using UnityEngine;

public class CheckpointManager : MonoBehaviour
{
    public static CheckpointManager Instance { get; private set; }

    [SerializeField] private Transform initialSpawn;

    private Vector2 current;

    public Vector2 CurrentPosition => current;

    private void Awake()
    {
        Instance = this;
        if (initialSpawn != null) current = initialSpawn.position;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>Devuelve true si es un punto de control nuevo.</summary>
    public bool SetCheckpoint(Vector2 position)
    {
        if ((position - current).sqrMagnitude < 0.01f) return false;
        current = position;
        return true;
    }

    public void Respawn(PlayerMovement player) => player.Teleport(current);
}
