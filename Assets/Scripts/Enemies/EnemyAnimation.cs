using UnityEngine;

/// <summary>
/// Conecta EnemyBrain con un Animator (MoveX, MoveY, Speed, State, Chase).
/// Si no hay controller, no hace nada (el bob procedural de EnemyBrain sigue activo).
/// </summary>
[RequireComponent(typeof(EnemyBrain))]
public class EnemyAnimation : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private EnemyBrain brain;
    [SerializeField] private Rigidbody2D body;
    [SerializeField] private float chaseAnimSpeed = 1.35f;

    private static readonly int MoveX = Animator.StringToHash("MoveX");
    private static readonly int MoveY = Animator.StringToHash("MoveY");
    private static readonly int Speed = Animator.StringToHash("Speed");
    private static readonly int State = Animator.StringToHash("State");
    private static readonly int Chase = Animator.StringToHash("Chase");

    private void Awake()
    {
        if (brain == null) brain = GetComponent<EnemyBrain>();
        if (body == null) body = GetComponent<Rigidbody2D>();
        if (animator == null) animator = GetComponent<Animator>();
    }

    private void Update()
    {
        if (animator == null || animator.runtimeAnimatorController == null || brain == null) return;

        Vector2 facing = brain.Facing;
        float speed = body != null ? body.linearVelocity.magnitude : 0f;
        bool chasing = brain.State == EnemyState.Persecucion || brain.IsHunting;

        animator.SetFloat(MoveX, facing.x);
        animator.SetFloat(MoveY, facing.y);
        animator.SetFloat(Speed, speed);
        animator.SetInteger(State, (int)brain.State);
        animator.SetBool(Chase, chasing);
        animator.speed = chasing ? chaseAnimSpeed : 1f;
    }
}
