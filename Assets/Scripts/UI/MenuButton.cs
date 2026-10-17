using UnityEngine;
using UnityEngine.UI;

public enum MenuAction
{
    CargarEscena,
    Continuar,
    ReintentarCheckpoint,
    ReiniciarNivel,
    MenuPrincipal,
    AlternarPanel,
    Salir,
    NuevaPartida,
    ContinuarPartida
}

[RequireComponent(typeof(Button))]
public class MenuButton : MonoBehaviour
{
    [SerializeField] private MenuAction action;
    [Tooltip("Para CargarEscena y MenuPrincipal.")]
    [SerializeField] private string sceneName = "Menu";
    [Tooltip("Para AlternarPanel.")]
    [SerializeField] private GameObject panel;

    private void Awake() => GetComponent<Button>().onClick.AddListener(Run);

    private void Start()
    {
        if (action == MenuAction.ContinuarPartida && !GameProgress.HasSave) gameObject.SetActive(false);
    }

    private void Run()
    {
        switch (action)
        {
            case MenuAction.CargarEscena:
            case MenuAction.MenuPrincipal:
                SceneLoader.Load(sceneName);
                break;
            case MenuAction.Continuar:
                UIManager.Instance?.Resume();
                break;
            case MenuAction.ReintentarCheckpoint:
                LevelManager.Instance?.RetryFromCheckpoint();
                break;
            case MenuAction.ReiniciarNivel:
                SceneLoader.ReloadCurrent();
                break;
            case MenuAction.AlternarPanel:
                if (panel != null) panel.SetActive(!panel.activeSelf);
                break;
            case MenuAction.Salir:
                SceneLoader.Quit();
                break;
            case MenuAction.NuevaPartida:
                GameProgress.NewGame();
                SceneLoader.Load(GameProgress.FirstLevel);
                break;
            case MenuAction.ContinuarPartida:
                SceneLoader.Load(GameProgress.SavedScene);
                break;
        }
    }
}
