using System;
using System.Collections;
using UnityEngine;

public enum EnemyState
{
    Inactivo,
    Reposo,
    Patrulla,
    Sospecha,
    Persecucion,
    Investigar,
    Busqueda,
    Regreso,
    Aturdido
}

/// <summary>
/// Máquina de estados común a todas las entidades:
/// Patrulla → Sospecha → Persecución → Investigar/Búsqueda → Regreso → Patrulla.
/// Las diferencias entre entidades están en los parámetros de este componente,
/// EnemyDetection, EnemyPatrol y EnemyChase.
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(EnemyDetection), typeof(EnemyPatrol))]
[RequireComponent(typeof(EnemyChase))]
public class EnemyBrain : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private float patrolSpeed = 1.5f;
    [SerializeField] private float investigateSpeed = 2.2f;
    [SerializeField] private float arriveDistance = 0.2f;

    [Header("Comportamiento")]
    [SerializeField] private bool startActive = true;
    [Tooltip("Si empieza activo, comienza patrullando (true) o en reposo hasta oír algo (false).")]
    [SerializeField] private bool startPatrolling = true;
    [SerializeField] private float suspicionTime = 0.7f;
    [Tooltip("Al terminar la búsqueda o tras golpear, se desvanece y reaparece en su ruta (entidades fantasmales).")]
    [SerializeField] private bool vanishOnReturn;
    [SerializeField] private bool retreatAfterHit = true;
    [Tooltip("Si es mayor que 0, tras golpear queda aturdido estos segundos en lugar de retirarse.")]
    [SerializeField] private float stunAfterHit;
    [Tooltip("Opcional: despierta (o aparece) cuando el LevelManager activa esta bandera.")]
    [SerializeField] private string wakeOnFlag;
    [SerializeField] private float fadeDuration = 0.5f;

    [Header("Navegación")]
    [Tooltip("Opcional: red de puntos para rodear paredes. Sin ella va en línea recta.")]
    [SerializeField] private NavGraph navGraph;

    [Header("Visual y audio")]
    [SerializeField] private SpriteRenderer body;
    [Tooltip("Cono de visión provisional; apunta hacia +X en su rotación local.")]
    [SerializeField] private Transform visionCone;
    [SerializeField] private AudioSource voice;
    [SerializeField] private float idleVoiceVolume = 0.5f;
    [SerializeField] private float chaseVoiceVolume = 1f;

    private Rigidbody2D rb;
    private EnemyDetection detection;
    private EnemyPatrol patrol;
    private EnemyChase chase;
    private Collider2D[] colliders;
    private Collider2D playerCollider;

    private Vector2 desiredVelocity;
    private Vector2 lastKnownPosition;
    private Vector2 moveTarget;
    private float stateTimer;
    private float waitTimer;
    private float lostSightTimer;
    private float stuckTimer;
    private Vector2 stuckCheckPosition;
    private Coroutine vanishRoutine;
    private Coroutine appearRoutine;
    private float huntTimer;
    private float navTimer;
    private Vector2 navGoal;
    private Vector2 navStep;
    private Vector3 bodyBaseLocal;
    private bool bodyBaseCached;

    public EnemyState State { get; private set; } = EnemyState.Inactivo;
    public Vector2 Facing { get; private set; } = Vector2.down;
    public bool IsHarmless => State == EnemyState.Inactivo || State == EnemyState.Reposo
                              || State == EnemyState.Aturdido || vanishRoutine != null;

    public bool IsHunting => huntTimer > 0f;

    public event Action<EnemyState> OnStateChanged;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        detection = GetComponent<EnemyDetection>();
        patrol = GetComponent<EnemyPatrol>();
        chase = GetComponent<EnemyChase>();
        colliders = GetComponentsInChildren<Collider2D>(true);
    }

    private void Start()
    {
        GameObject player = GameObject.FindWithTag("Player");
        playerCollider = player != null ? player.GetComponent<Collider2D>() : null;
        SetCollidersEnabled(true);

        detection.OnHeardNoise += HandleNoise;
        if (!string.IsNullOrEmpty(wakeOnFlag) && LevelManager.Instance != null)
            LevelManager.Instance.OnFlagSet += HandleFlag;

        if (!startActive) Deactivate();
        else ChangeState(startPatrolling ? EnemyState.Patrulla : EnemyState.Reposo);
    }

    private void OnDestroy()
    {
        if (detection != null) detection.OnHeardNoise -= HandleNoise;
        if (LevelManager.Instance != null) LevelManager.Instance.OnFlagSet -= HandleFlag;
        AudioManager.Instance?.SetChasing(this, false);
    }

    private void Update()
    {
        desiredVelocity = Vector2.zero;
        if (vanishRoutine != null) return;

        stateTimer += Time.deltaTime;
        if (huntTimer > 0f) huntTimer -= Time.deltaTime;

        switch (State)
        {
            case EnemyState.Patrulla: UpdatePatrol(); break;
            case EnemyState.Sospecha: UpdateSuspicion(); break;
            case EnemyState.Persecucion: UpdateChase(); break;
            case EnemyState.Investigar: UpdateInvestigate(); break;
            case EnemyState.Busqueda: UpdateSearch(); break;
            case EnemyState.Regreso: UpdateReturn(); break;
            case EnemyState.Aturdido:
                if (stateTimer >= waitTimer) ChangeState(IsHunting ? EnemyState.Persecucion : EnemyState.Busqueda);
                break;
            case EnemyState.Reposo: if (CanSee()) ChangeState(EnemyState.Sospecha); break;
        }

        UpdateVisuals();
    }

    private void FixedUpdate()
    {
        rb.linearVelocity = desiredVelocity;
    }

    // ---------- Estados ----------

    private void UpdatePatrol()
    {
        if (CanSee()) { ChangeState(EnemyState.Sospecha); return; }
        if (!patrol.HasPoints) return;

        if (waitTimer > 0f)
        {
            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0f) patrol.Advance();
            return;
        }

        if (MoveTowards(patrol.CurrentTarget, patrolSpeed))
            waitTimer = patrol.WaitTime;
        else if (IsStuck())
            patrol.Advance();
    }

    private void UpdateSuspicion()
    {
        bool sees = CanSee();
        if (sees)
        {
            lastKnownPosition = detection.PlayerPosition;
            Face(lastKnownPosition - (Vector2)transform.position);
            if (detection.DistanceToPlayer() <= chase.CloseChaseDistance || stateTimer >= suspicionTime)
                ChangeState(EnemyState.Persecucion);
        }
        else if (stateTimer >= suspicionTime)
        {
            StartInvestigating(lastKnownPosition);
        }
    }

    private void UpdateChase()
    {
        bool tracking = IsHunting && !detection.PlayerHidden;
        if (CanSee() || tracking)
        {
            lastKnownPosition = detection.PlayerPosition;
            lostSightTimer = 0f;
        }
        else
        {
            lostSightTimer += Time.deltaTime;
            if (lostSightTimer >= chase.LoseSightTime || detection.PlayerHidden)
            {
                StartInvestigating(lastKnownPosition);
                return;
            }
        }

        MoveTowards(lastKnownPosition, chase.ChaseSpeed);
        if (IsStuck() && !tracking) StartInvestigating(lastKnownPosition);
    }

    private void UpdateInvestigate()
    {
        if (CanSee()) { ChangeState(EnemyState.Sospecha); return; }
        if (MoveTowards(moveTarget, investigateSpeed) || IsStuck())
            ChangeState(EnemyState.Busqueda);
    }

    private void UpdateSearch()
    {
        if (CanSee()) { ChangeState(EnemyState.Sospecha); return; }
        if (stateTimer >= chase.SearchDuration) { ChangeState(EnemyState.Regreso); return; }

        if (waitTimer > 0f)
        {
            waitTimer -= Time.deltaTime;
            return;
        }

        if (MoveTowards(moveTarget, patrolSpeed) || IsStuck())
        {
            waitTimer = 0.6f;
            moveTarget = chase.RandomSearchPoint(lastKnownPosition, detection);
        }
    }

    private void UpdateReturn()
    {
        if (CanSee()) { ChangeState(EnemyState.Sospecha); return; }

        if (vanishOnReturn)
        {
            int i = patrol.HasPoints ? patrol.FarthestIndex(detection.PlayerPosition) : 0;
            Vector2 target = patrol.HasPoints ? patrol.PointAt(i) : (Vector2)transform.position;
            vanishRoutine = StartCoroutine(VanishAndReappear(target, i, EnemyState.Patrulla));
            return;
        }

        if (!patrol.HasPoints) { ChangeState(EnemyState.Reposo); return; }
        if (MoveTowards(moveTarget, patrolSpeed) || IsStuck())
        {
            patrol.SetIndex(patrol.NearestIndex(transform.position));
            ChangeState(EnemyState.Patrulla);
        }
    }

    // ---------- API pública ----------

    public void Activate(Vector2 position, bool patrolling = true)
    {
        StopVanish();
        Teleport(position);
        SetVisible(true, 1f);
        SetCollidersEnabled(true);
        if (patrol.HasPoints) patrol.SetIndex(patrol.NearestIndex(position));
        ChangeState(patrolling ? EnemyState.Patrulla : EnemyState.Reposo);
    }

    public void Deactivate()
    {
        StopVanish();
        huntTimer = 0f;
        ChangeState(EnemyState.Inactivo);
        SetVisible(false, 0f);
        SetCollidersEnabled(false);
        rb.linearVelocity = Vector2.zero;
    }

    /// <summary>Aparece sin ser hostil ni reaccionar (aparición a distancia).</summary>
    public void Appear(Vector2 position, Vector2 lookDirection)
    {
        StopVanish();
        ChangeState(EnemyState.Inactivo);
        Teleport(position);
        SetCollidersEnabled(false);
        Face(lookDirection);
        UpdateVisuals();
        StopCoroutineSafe(appearRoutine);
        appearRoutine = StartCoroutine(FadeAppearRoutine());
    }

    /// <summary>Desaparece con fundido (aparición distante / fin de secuencia).</summary>
    public void FadeDeactivate()
    {
        StopVanish();
        StopCoroutineSafe(appearRoutine);
        appearRoutine = StartCoroutine(FadeDeactivateRoutine());
    }

    private IEnumerator FadeAppearRoutine()
    {
        SetVisible(true, 0f);
        for (float t = 0f; t < fadeDuration; t += Time.deltaTime)
        {
            SetVisible(true, t / fadeDuration);
            yield return null;
        }
        SetVisible(true, 1f);
        appearRoutine = null;
    }

    private IEnumerator FadeDeactivateRoutine()
    {
        huntTimer = 0f;
        ChangeState(EnemyState.Inactivo);
        SetCollidersEnabled(false);
        rb.linearVelocity = Vector2.zero;
        for (float t = 0f; t < fadeDuration; t += Time.deltaTime)
        {
            SetVisible(true, 1f - t / fadeDuration);
            yield return null;
        }
        SetVisible(false, 0f);
        appearRoutine = null;
    }

    private void StopCoroutineSafe(Coroutine c)
    {
        if (c != null) StopCoroutine(c);
    }

    public void Stun(float duration)
    {
        if (State == EnemyState.Inactivo) return;
        waitTimer = duration;
        ChangeState(EnemyState.Aturdido);
    }

    /// <summary>Sale del reposo, o aparece en el punto de patrulla más lejano al jugador si estaba inactivo.</summary>
    public void Wake()
    {
        if (State == EnemyState.Inactivo)
        {
            Vector2 start = patrol.HasPoints ? patrol.PointAt(patrol.FarthestIndex(detection.PlayerPosition)) : (Vector2)transform.position;
            Activate(start);
        }
        else if (State == EnemyState.Reposo && patrol.HasPoints)
        {
            patrol.SetIndex(patrol.NearestIndex(transform.position));
            ChangeState(EnemyState.Patrulla);
        }
    }

    /// <summary>
    /// Persecución forzada: aparece en la posición indicada y sigue al jugador durante el tiempo dado
    /// aunque no lo vea (salvo si se esconde).
    /// </summary>
    public void StartChase(Vector2 position, float relentlessTime)
    {
        Activate(position);
        huntTimer = relentlessTime;
        lastKnownPosition = detection.PlayerPosition;
        ChangeState(EnemyState.Persecucion);
    }

    public void OnHitPlayer()
    {
        lastKnownPosition = detection.PlayerPosition;
        if (stunAfterHit > 0f)
        {
            Stun(stunAfterHit);
            return;
        }
        if (!retreatAfterHit) return;
        ChangeState(EnemyState.Regreso);
    }

    public void ResetForRetry(Vector2 playerPosition)
    {
        huntTimer = 0f;
        if (State == EnemyState.Inactivo || State == EnemyState.Reposo) return;
        StopVanish();
        int i = patrol.HasPoints ? patrol.FarthestIndex(playerPosition) : 0;
        if (patrol.HasPoints)
        {
            patrol.SetIndex(i);
            Teleport(patrol.PointAt(i));
        }
        SetVisible(true, 1f);
        SetCollidersEnabled(true);
        ChangeState(EnemyState.Patrulla);
    }

    // ---------- Utilidades ----------

    private void HandleFlag(string flag)
    {
        if (flag == wakeOnFlag) Wake();
    }

    private void HandleNoise(Vector2 position)
    {
        if (State == EnemyState.Inactivo || State == EnemyState.Persecucion
            || State == EnemyState.Aturdido || vanishRoutine != null) return;
        if (State == EnemyState.Sospecha) return;
        StartInvestigating(position);
    }

    private void StartInvestigating(Vector2 position)
    {
        lastKnownPosition = position;
        moveTarget = position;
        ChangeState(EnemyState.Investigar);
    }

    private void ChangeState(EnemyState next)
    {
        EnemyState previous = State;
        State = next;
        stateTimer = 0f;
        lostSightTimer = 0f;
        stuckTimer = 0f;
        stuckCheckPosition = rb != null ? rb.position : (Vector2)transform.position;

        if (next != EnemyState.Aturdido) waitTimer = 0f;
        if (next == EnemyState.Busqueda) moveTarget = lastKnownPosition;
        if (next == EnemyState.Regreso && patrol.HasPoints)
            moveTarget = patrol.PointAt(patrol.NearestIndex(transform.position));

        AudioManager.Instance?.SetChasing(this, next == EnemyState.Persecucion);
        UpdateVoice();

        if (previous != next) OnStateChanged?.Invoke(next);
    }

    private bool CanSee() => detection.CanSeePlayer(Facing);

    /// <summary>Mueve hacia el objetivo; devuelve true al llegar.</summary>
    private bool MoveTowards(Vector2 target, float speed)
    {
        Vector2 delta = target - rb.position;
        if (delta.magnitude <= arriveDistance) return true;
        Vector2 step = navGraph != null ? NavStep(target) : target;
        Vector2 dir = (step - rb.position).normalized;
        desiredVelocity = dir * speed;
        Face(dir);
        return false;
    }

    private Vector2 NavStep(Vector2 target)
    {
        navTimer -= Time.deltaTime;
        bool sameGoal = (navGoal - target).sqrMagnitude < 0.25f;
        bool stepAhead = (navStep - rb.position).sqrMagnitude > 0.09f;
        if (navTimer > 0f && sameGoal && stepAhead) return navStep;

        navTimer = 0.25f;
        navGoal = target;
        navStep = navGraph.TryGetNextPoint(rb.position, target, out Vector2 next) ? next : target;
        return navStep;
    }

    private bool IsStuck()
    {
        stuckTimer += Time.deltaTime;
        if (stuckTimer < 1f) return false;

        bool stuck = (rb.position - stuckCheckPosition).sqrMagnitude < 0.04f;
        stuckTimer = 0f;
        stuckCheckPosition = rb.position;
        return stuck;
    }

    private void Face(Vector2 dir)
    {
        if (dir.sqrMagnitude > 0.0001f) Facing = dir.normalized;
    }

    private void UpdateVisuals()
    {
        if (visionCone != null)
        {
            float angle = Mathf.Atan2(Facing.y, Facing.x) * Mathf.Rad2Deg;
            visionCone.rotation = Quaternion.Euler(0f, 0f, angle);
        }
        if (body != null)
        {
            // Con Animator las direcciones vienen de los clips; sin él usamos flipX + bob.
            var anim = GetComponent<Animator>();
            bool hasAnim = anim != null && anim.runtimeAnimatorController != null;
            if (!hasAnim && Mathf.Abs(Facing.x) > 0.1f) body.flipX = Facing.x < 0f;
            if (!hasAnim)
            {
                if (!bodyBaseCached)
                {
                    bodyBaseLocal = body.transform.localPosition;
                    bodyBaseCached = true;
                }
                bool moving = desiredVelocity.sqrMagnitude > 0.01f && State != EnemyState.Inactivo && State != EnemyState.Reposo && State != EnemyState.Aturdido;
                float bob = moving ? Mathf.Abs(Mathf.Sin(Time.time * 10f)) * 0.04f : 0f;
                body.transform.localPosition = bodyBaseLocal + Vector3.up * bob;
            }
        }
    }

    private void UpdateVoice()
    {
        if (voice == null || voice.clip == null) return;
        bool audible = State != EnemyState.Inactivo;
        if (audible && !voice.isPlaying) voice.Play();
        if (!audible && voice.isPlaying) voice.Stop();
        voice.volume = State == EnemyState.Persecucion ? chaseVoiceVolume : idleVoiceVolume;
    }

    private IEnumerator VanishAndReappear(Vector2 target, int patrolIndex, EnemyState after)
    {
        rb.linearVelocity = Vector2.zero;
        AudioManager.Instance?.SetChasing(this, false);

        for (float t = 0f; t < fadeDuration; t += Time.deltaTime)
        {
            SetVisible(true, 1f - t / fadeDuration);
            yield return null;
        }
        SetVisible(true, 0f);

        yield return new WaitForSeconds(1f);
        Teleport(target);
        if (patrol.HasPoints) patrol.SetIndex(patrolIndex);

        for (float t = 0f; t < fadeDuration; t += Time.deltaTime)
        {
            SetVisible(true, t / fadeDuration);
            yield return null;
        }
        SetVisible(true, 1f);

        vanishRoutine = null;
        ChangeState(after);
    }

    private void StopVanish()
    {
        if (vanishRoutine != null) StopCoroutine(vanishRoutine);
        vanishRoutine = null;
        StopCoroutineSafe(appearRoutine);
        appearRoutine = null;
    }

    private void Teleport(Vector2 position)
    {
        rb.position = position;
        transform.position = position;
        rb.linearVelocity = Vector2.zero;
    }

    private void SetVisible(bool visible, float alpha)
    {
        foreach (SpriteRenderer r in GetComponentsInChildren<SpriteRenderer>(true))
        {
            r.enabled = visible;
            Color c = r.color;
            c.a = r.transform == visionCone ? Mathf.Min(alpha, 0.15f) : alpha;
            r.color = c;
        }
    }

    private void SetCollidersEnabled(bool on)
    {
        foreach (Collider2D c in colliders)
        {
            c.enabled = on;
            if (on && playerCollider != null && !c.isTrigger) Physics2D.IgnoreCollision(c, playerCollider, true);
        }
    }
}
