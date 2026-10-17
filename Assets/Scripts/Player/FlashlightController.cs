using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Linterna con batería. F la enciende o apaga; encendida consume batería y apunta hacia donde
/// se mueve el jugador. Otros scripts consultan IsIlluminating para reaccionar a la luz.
/// </summary>
[RequireComponent(typeof(PlayerMovement))]
public class FlashlightController : MonoBehaviour
{
    [SerializeField] private Light2D beam;

    [Header("Batería")]
    [SerializeField] private float maxBattery = 100f;
    [SerializeField] private float startBattery = 100f;
    [Tooltip("Unidades de batería por segundo con la linterna encendida.")]
    [SerializeField] private float drainPerSecond = 2.5f;
    [Range(0f, 1f)]
    [SerializeField] private float lowBatteryThreshold = 0.15f;

    [Header("Haz")]
    [SerializeField] private float range = 7f;
    [SerializeField] private float angle = 60f;
    [SerializeField] private float beamIntensity = 1.3f;
    [SerializeField] private float turnSpeed = 540f;

    private PlayerMovement movement;
    private PlayerHiding hiding;
    private Vector2 aim = Vector2.down;
    private Vector2 aimTarget = Vector2.down;
    private float battery;

    public bool IsOn { get; private set; }
    public float Battery => battery;
    public float Battery01 => maxBattery > 0f ? battery / maxBattery : 0f;
    public bool IsLow => Battery01 <= lowBatteryThreshold;
    public Vector2 Aim => aim;
    public float Range => range;

    public event Action<bool> OnToggled;

    private void Awake()
    {
        movement = GetComponent<PlayerMovement>();
        hiding = GetComponent<PlayerHiding>();
        battery = Mathf.Clamp(startBattery, 0f, maxBattery);
        ApplyBeam();
    }

    private void Update()
    {
        UIManager ui = UIManager.Instance;
        bool blocked = ui != null && ui.IsBlockingInput;
        if (!blocked && GameInput.Instance != null && GameInput.Instance.FlashlightPressed) Toggle();
        if (IsOn && hiding != null && hiding.IsHidden) SetOn(false);

        Vector2 input = movement.CurrentInput;
        if (input.sqrMagnitude > 0.01f) aimTarget = input.normalized;
        aim = Vector3.RotateTowards(aim, aimTarget, turnSpeed * Mathf.Deg2Rad * Time.deltaTime, 0f);
        ApplyRotation();

        if (!IsOn) return;

        battery = Mathf.Max(0f, battery - drainPerSecond * Time.deltaTime);
        if (battery <= 0f)
        {
            SetOn(false);
            UIManager.Instance?.ShowMessage("La linterna parpadea y se apaga. Necesitas baterías.");
            return;
        }

        if (beam != null)
        {
            bool flicker = IsLow && Mathf.PerlinNoise(Time.time * 9f, 0.37f) < 0.3f;
            beam.intensity = flicker ? beamIntensity * 0.25f : beamIntensity;
        }
    }

    public void Toggle()
    {
        if (!IsOn && battery <= 0f)
        {
            AudioManager.Instance?.Play(AudioManager.Sfx.Click);
            UIManager.Instance?.ShowMessage("La linterna no tiene batería.");
            return;
        }
        SetOn(!IsOn);
        AudioManager.Instance?.Play(AudioManager.Sfx.Click);
    }

    public void SetOn(bool on)
    {
        if (on && battery <= 0f) on = false;
        if (IsOn == on) return;
        IsOn = on;
        ApplyBeam();
        OnToggled?.Invoke(on);
    }

    /// <summary>Apunta la linterna en una dirección concreta (cinemáticas o pruebas).</summary>
    public void SetDirection(Vector2 direction)
    {
        if (direction.sqrMagnitude < 0.0001f) return;
        aim = aimTarget = direction.normalized;
        ApplyRotation();
    }

    public void Recharge(float amount)
    {
        battery = Mathf.Clamp(battery + amount, 0f, maxBattery);
    }

    public bool IsIlluminating(Vector2 point)
    {
        if (!IsOn) return false;
        Vector2 origin = transform.position;
        Vector2 toPoint = point - origin;
        if (toPoint.magnitude > range) return false;
        if (toPoint.sqrMagnitude > 0.04f && Vector2.Angle(aim, toPoint) > angle * 0.5f) return false;
        return LineOfSight.Clear(origin, point, transform);
    }

    private void ApplyBeam()
    {
        if (beam == null) return;
        beam.enabled = IsOn;
        beam.intensity = beamIntensity;
        beam.pointLightOuterRadius = range;
        beam.pointLightOuterAngle = angle;
        beam.pointLightInnerAngle = angle * 0.6f;
    }

    private void ApplyRotation()
    {
        if (beam == null) return;
        float z = Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg - 90f;
        beam.transform.rotation = Quaternion.Euler(0f, 0f, z);
    }
}
