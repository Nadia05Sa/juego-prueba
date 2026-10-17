using UnityEngine;

public class BatteryPickup : Interactable
{
    [SerializeField] private float amount = 50f;
    [TextArea(1, 3)]
    [SerializeField] private string pickupMessage = "Recoges unas baterías. La linterna recupera energía.";

    private void Reset() => prompt = "Recoger baterías";

    public override void Interact(InteractionController interactor)
    {
        FlashlightController flashlight = interactor.GetComponent<FlashlightController>();
        if (flashlight == null)
        {
            UIManager.Instance?.ShowMessage("Unas baterías viejas. No tienes nada en qué usarlas.");
            return;
        }

        flashlight.Recharge(amount);
        AudioManager.Instance?.Play(AudioManager.Sfx.Pickup);
        UIManager.Instance?.ShowMessage(pickupMessage);
        gameObject.SetActive(false);
    }
}
