using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Lectura del mapa Player. Clona el InputActionAsset en runtime para evitar
/// NullReferenceException del Input System cuando Enter Play Mode no recarga el dominio.
/// </summary>
public class GameInput : MonoBehaviour
{
    public static GameInput Instance { get; private set; }

    [SerializeField] private InputActionAsset actions;
    [SerializeField] private string mapName = "Player";

    private InputActionAsset runtimeActions;
    private InputActionMap map;
    private InputAction move;
    private InputAction interact;
    private InputAction sprint;
    private InputAction hide;
    private InputAction flashlight;
    private InputAction pause;
    private InputAction inventory;

    public Vector2 Move => move != null ? Vector2.ClampMagnitude(move.ReadValue<Vector2>(), 1f) : Vector2.zero;
    public bool SprintHeld => sprint != null && sprint.IsPressed();
    public bool InteractPressed => WasPressed(interact);
    public bool HidePressed => WasPressed(hide);
    public bool FlashlightPressed => WasPressed(flashlight);
    public bool PausePressed => WasPressed(pause);
    public bool InventoryPressed => WasPressed(inventory);

    private void Awake()
    {
        Instance = this;

        if (actions == null)
        {
            Debug.LogError("GameInput: falta asignar el InputActionAsset.", this);
            return;
        }

        // Copia propia: el asset del proyecto no debe compartirse entre Play Modes sin domain reload.
        runtimeActions = Instantiate(actions);
        runtimeActions.name = actions.name + " (Runtime)";
        runtimeActions.Disable();

        map = runtimeActions.FindActionMap(mapName, throwIfNotFound: false);
        if (map == null)
        {
            Debug.LogError($"GameInput: no existe el mapa '{mapName}'.", this);
            return;
        }

        move = map.FindAction("Move", throwIfNotFound: false);
        interact = map.FindAction("Interact", throwIfNotFound: false);
        sprint = map.FindAction("Sprint", throwIfNotFound: false);
        hide = map.FindAction("Hide", throwIfNotFound: false);
        flashlight = map.FindAction("Flashlight", throwIfNotFound: false);
        pause = map.FindAction("Pause", throwIfNotFound: false);
        inventory = map.FindAction("Inventory", throwIfNotFound: false);
    }

    private void OnEnable() => map?.Enable();

    private void OnDisable() => map?.Disable();

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;

        if (map != null)
        {
            map.Disable();
            map = null;
        }

        move = interact = sprint = hide = flashlight = pause = inventory = null;

        if (runtimeActions != null)
        {
            runtimeActions.Disable();
            Destroy(runtimeActions);
            runtimeActions = null;
        }
    }

    private static bool WasPressed(InputAction action)
    {
        if (action == null || !action.enabled) return false;
        try { return action.WasPressedThisFrame(); }
        catch (System.NullReferenceException) { return false; }
    }
}
