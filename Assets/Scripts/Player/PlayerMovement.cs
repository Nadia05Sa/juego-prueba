using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Velocidad")]
    [SerializeField] private float walkSpeed = 3.5f;
    [SerializeField] private float runSpeed = 5f;

    [Header("Ruido")]
    [Tooltip("Radio de ruido al caminar (usado por enemigos que escuchan).")]
    [SerializeField] private float walkNoiseRadius = 1.5f;
    [SerializeField] private float runNoiseRadius = 4.5f;
    [SerializeField] private float noiseInterval = 0.4f;

    [Header("Pasos")]
    [SerializeField] private float walkStepInterval = 0.38f;
    [SerializeField] private float runStepInterval = 0.26f;
    [SerializeField] private float walkStepVolume = 0.28f;
    [SerializeField] private float runStepVolume = 0.42f;

    private Rigidbody2D rb;
    private Vector2 input;
    private float noiseTimer;
    private float stepTimer;
    private readonly HashSet<object> locks = new HashSet<object>();

    public Vector2 CurrentInput => input;
    public bool IsMoving => input.sqrMagnitude > 0.01f;
    public bool IsRunning { get; private set; }
    public bool IsLocked => locks.Count > 0;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        input = IsLocked || GameInput.Instance == null ? Vector2.zero : GameInput.Instance.Move;
        IsRunning = IsMoving && GameInput.Instance != null && GameInput.Instance.SprintHeld;

        if (!IsMoving)
        {
            noiseTimer = 0f;
            stepTimer = 0f;
            return;
        }

        noiseTimer -= Time.deltaTime;
        if (noiseTimer <= 0f)
        {
            noiseTimer = noiseInterval;
            NoiseSystem.Emit(rb.position, IsRunning ? runNoiseRadius : walkNoiseRadius, gameObject);
        }

        stepTimer -= Time.deltaTime;
        if (stepTimer <= 0f)
        {
            stepTimer = IsRunning ? runStepInterval : walkStepInterval;
            AudioManager.Instance?.Play(AudioManager.Sfx.Footstep, IsRunning ? runStepVolume : walkStepVolume);
        }
    }

    private void FixedUpdate()
    {
        rb.linearVelocity = input * (IsRunning ? runSpeed : walkSpeed);
    }

    public void AddLock(object owner)
    {
        locks.Add(owner);
        input = Vector2.zero;
        rb.linearVelocity = Vector2.zero;
    }

    public void RemoveLock(object owner) => locks.Remove(owner);

    public void ClearLocks() => locks.Clear();

    public void Teleport(Vector2 position)
    {
        rb.position = position;
        transform.position = position;
        rb.linearVelocity = Vector2.zero;
    }
}
