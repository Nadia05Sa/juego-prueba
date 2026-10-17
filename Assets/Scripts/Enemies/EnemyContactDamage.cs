using UnityEngine;

/// <summary>
/// Va en un hijo del enemigo con un Collider2D en modo Is Trigger. Aplica daño al tocar al jugador;
/// la invulnerabilidad de PlayerHealth evita que pierda vidas cada fotograma.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class EnemyContactDamage : MonoBehaviour
{
    [SerializeField] private EnemyBrain brain;
    [SerializeField] private int damage = 1;

    private void OnTriggerStay2D(Collider2D other)
    {
        if (brain == null || brain.IsHarmless) return;
        if (!other.TryGetComponent(out PlayerHealth health)) return;
        if (other.TryGetComponent(out PlayerHiding hiding) && hiding.IsHidden) return;

        if (health.TakeDamage(damage)) brain.OnHitPlayer();
    }
}
