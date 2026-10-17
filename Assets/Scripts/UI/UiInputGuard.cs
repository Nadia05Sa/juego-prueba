using UnityEngine;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Reinicia las acciones del InputSystemUIInputModule en cada Play.
/// Evita el NullReferenceException del Input System con Enter Play Mode sin domain reload.
/// </summary>
[DefaultExecutionOrder(-200)]
[RequireComponent(typeof(InputSystemUIInputModule))]
public class UiInputGuard : MonoBehaviour
{
    private InputSystemUIInputModule module;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureInScene()
    {
        AttachToAll();
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => AttachToAll();

    private static void AttachToAll()
    {
        foreach (InputSystemUIInputModule found in Object.FindObjectsByType<InputSystemUIInputModule>(FindObjectsInactive.Include))
        {
            if (found.GetComponent<UiInputGuard>() == null)
                found.gameObject.AddComponent<UiInputGuard>();
        }
    }

    private void Awake() => Refresh();

    private void OnDestroy()
    {
        if (module != null)
            module.UnassignActions();
    }

    private void Refresh()
    {
        module = GetComponent<InputSystemUIInputModule>();
        if (module == null) return;
        module.UnassignActions();
        module.AssignDefaultActions();
    }
}
