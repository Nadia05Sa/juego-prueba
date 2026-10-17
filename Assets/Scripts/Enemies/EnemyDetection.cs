using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Percepción del enemigo: visión (distancia, ángulo y obstáculos) y oído (eventos de NoiseSystem).
/// </summary>
public class EnemyDetection : MonoBehaviour
{
    [Header("Visión")]
    [SerializeField] private float viewDistance = 6f;
    [SerializeField] private float viewAngle = 100f;
    [Tooltip("A esta distancia detecta al jugador aunque esté fuera del ángulo de visión.")]
    [SerializeField] private float proximityRadius = 1.4f;

    [Header("Oído")]
    [Tooltip("Multiplica el radio de cada ruido. 0 = sordo.")]
    [SerializeField] private float hearingMultiplier = 1f;

    private Transform player;
    private PlayerHiding playerHiding;
    private Collider2D playerCollider;
    private readonly List<RaycastHit2D> hits = new List<RaycastHit2D>();
    private ContactFilter2D obstacleFilter;

    public float ViewDistance => viewDistance;
    public Transform Player => player;
    public Vector2 PlayerPosition => player != null ? (Vector2)player.position : (Vector2)transform.position;
    public bool PlayerHidden => playerHiding != null && playerHiding.IsHidden;

    public event Action<Vector2> OnHeardNoise;

    private void Awake()
    {
        obstacleFilter = ContactFilter2D.noFilter;
        obstacleFilter.useTriggers = false;
    }

    private void Start()
    {
        GameObject p = GameObject.FindWithTag("Player");
        if (p == null) return;
        player = p.transform;
        playerHiding = p.GetComponent<PlayerHiding>();
        playerCollider = p.GetComponent<Collider2D>();
    }

    private void OnEnable() => NoiseSystem.OnNoise += HandleNoise;

    private void OnDisable() => NoiseSystem.OnNoise -= HandleNoise;

    public float DistanceToPlayer() => player != null ? Vector2.Distance(transform.position, player.position) : float.MaxValue;

    public bool CanSeePlayer(Vector2 facing)
    {
        if (player == null || PlayerHidden) return false;

        Vector2 toPlayer = (Vector2)player.position - (Vector2)transform.position;
        float dist = toPlayer.magnitude;
        if (dist > viewDistance) return false;
        if (dist > proximityRadius && Vector2.Angle(facing, toPlayer) > viewAngle * 0.5f) return false;

        return HasLineOfSight(player.position);
    }

    public bool HasLineOfSight(Vector2 target)
    {
        hits.Clear();
        Physics2D.Linecast(transform.position, target, obstacleFilter, hits);
        foreach (RaycastHit2D h in hits)
        {
            if (h.collider == null) continue;
            if (h.collider.transform.IsChildOf(transform)) continue;
            if (playerCollider != null && h.collider == playerCollider) continue;
            if (h.collider.attachedRigidbody != null && h.collider.attachedRigidbody.bodyType == RigidbodyType2D.Dynamic) continue;
            return false;
        }
        return true;
    }

    private void HandleNoise(Vector2 position, float radius, GameObject source)
    {
        if (hearingMultiplier <= 0f) return;
        if (source != null && player != null && source.transform == player && PlayerHidden) return;

        if (Vector2.Distance(transform.position, position) <= radius * hearingMultiplier)
            OnHeardNoise?.Invoke(position);
    }
}
