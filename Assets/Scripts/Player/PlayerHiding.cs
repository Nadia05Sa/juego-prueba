using System;
using UnityEngine;

[RequireComponent(typeof(PlayerMovement))]
public class PlayerHiding : MonoBehaviour
{
    [SerializeField] private SpriteRenderer[] renderers;
    [SerializeField] private float hiddenAlpha = 0.3f;
    [SerializeField] private float toggleCooldown = 0.4f;

    private PlayerMovement movement;
    private float cooldown;
    private Vector2 returnPosition;

    public bool IsHidden { get; private set; }
    public HidingSpot NearbySpot { get; private set; }
    public bool CanHide => !IsHidden && NearbySpot != null && cooldown <= 0f && !movement.IsLocked;

    public event Action<bool> OnHiddenChanged;

    private void Awake()
    {
        movement = GetComponent<PlayerMovement>();
    }

    private void Update()
    {
        if (cooldown > 0f) cooldown -= Time.deltaTime;
        if (GameInput.Instance == null || !GameInput.Instance.HidePressed) return;
        if (UIManager.Instance != null && UIManager.Instance.IsBlockingInput) return;

        if (IsHidden)
        {
            if (cooldown <= 0f) Exit();
        }
        else if (CanHide)
        {
            Enter(NearbySpot);
        }
    }

    private void Enter(HidingSpot spot)
    {
        returnPosition = transform.position;
        movement.AddLock(this);
        movement.Teleport(spot.HidePosition);
        SetAlpha(hiddenAlpha);
        IsHidden = true;
        cooldown = toggleCooldown;
        OnHiddenChanged?.Invoke(true);
        UIManager.Instance?.ShowMessage("Te escondes en " + spot.SpotName + ". Contén la respiración...");
    }

    private void Exit()
    {
        movement.Teleport(returnPosition);
        movement.RemoveLock(this);
        SetAlpha(1f);
        IsHidden = false;
        cooldown = toggleCooldown;
        OnHiddenChanged?.Invoke(false);
    }

    /// <summary>Sale del escondite sin volver a la posición anterior (p. ej. al reaparecer).</summary>
    public void ForceExit()
    {
        if (!IsHidden) return;
        movement.RemoveLock(this);
        SetAlpha(1f);
        IsHidden = false;
        OnHiddenChanged?.Invoke(false);
    }

    private void SetAlpha(float a)
    {
        if (renderers == null) return;
        foreach (SpriteRenderer r in renderers)
        {
            if (r == null) continue;
            Color c = r.color;
            c.a = a;
            r.color = c;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        HidingSpot spot = other.GetComponent<HidingSpot>();
        if (spot != null) NearbySpot = spot;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        HidingSpot spot = other.GetComponent<HidingSpot>();
        if (spot != null && spot == NearbySpot && !IsHidden) NearbySpot = null;
    }
}
