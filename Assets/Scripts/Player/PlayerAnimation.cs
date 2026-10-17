using UnityEngine;

[RequireComponent(typeof(PlayerMovement))]
public class PlayerAnimation : MonoBehaviour
{
    [SerializeField] private SpriteRenderer body;
    [Tooltip("Opcional: sprites por dirección. Si están vacíos se mantiene el sprite actual.")]
    [SerializeField] private Sprite spriteUp;
    [SerializeField] private Sprite spriteDown;
    [SerializeField] private Sprite spriteLeft;
    [SerializeField] private Sprite spriteRight;
    [Tooltip("Marcador provisional que indica hacia dónde mira el personaje.")]
    [SerializeField] private Transform facingIndicator;
    [SerializeField] private float indicatorDistance = 0.32f;
    [Tooltip("Opcional: Animator con parámetros MoveX, MoveY y Speed.")]
    [SerializeField] private Animator animator;
    [SerializeField] private float bobAmount = 0.07f;
    [SerializeField] private float bobSpeed = 11f;

    private static readonly int MoveX = Animator.StringToHash("MoveX");
    private static readonly int MoveY = Animator.StringToHash("MoveY");
    private static readonly int Speed = Animator.StringToHash("Speed");

    private PlayerMovement movement;
    private Vector3 bodyBasePosition;

    public Vector2 Facing { get; private set; } = Vector2.down;

    private void Awake()
    {
        movement = GetComponent<PlayerMovement>();
        if (body != null) bodyBasePosition = body.transform.localPosition;
    }

    private void Update()
    {
        Vector2 dir = movement.CurrentInput;
        if (dir.sqrMagnitude > 0.01f)
        {
            Facing = Mathf.Abs(dir.x) > Mathf.Abs(dir.y)
                ? new Vector2(Mathf.Sign(dir.x), 0f)
                : new Vector2(0f, Mathf.Sign(dir.y));
        }

        ApplyDirectionalSprite();

        if (facingIndicator != null)
            facingIndicator.localPosition = Facing * indicatorDistance;

        bool hasAnimator = animator != null && animator.runtimeAnimatorController != null;
        if (hasAnimator)
        {
            animator.SetFloat(MoveX, Facing.x);
            animator.SetFloat(MoveY, Facing.y);
            animator.SetFloat(Speed, dir.magnitude);
        }
        else if (body != null)
        {
            float bob = movement.IsMoving ? Mathf.Abs(Mathf.Sin(Time.time * bobSpeed)) * bobAmount : 0f;
            body.transform.localPosition = bodyBasePosition + Vector3.up * bob;
        }
    }

    private void ApplyDirectionalSprite()
    {
        if (body == null) return;

        Sprite s = null;
        if (Facing.y > 0f) s = spriteUp;
        else if (Facing.y < 0f) s = spriteDown;
        else if (Facing.x < 0f) s = spriteLeft;
        else if (Facing.x > 0f) s = spriteRight;

        if (s != null) body.sprite = s;
    }
}
