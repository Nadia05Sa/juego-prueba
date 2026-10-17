using System;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private int maxLives = 3;
    [SerializeField] private float invulnerabilityTime = 1.5f;
    [SerializeField] private float flashInterval = 0.1f;
    [SerializeField] private SpriteRenderer[] flashRenderers;

    private float invulnerableTimer;

    public int Lives { get; private set; }
    public int MaxLives => maxLives;
    public bool IsInvulnerable => invulnerableTimer > 0f;
    public bool IsDead { get; private set; }

    public event Action<int> OnLivesChanged;
    public event Action OnDamaged;
    public event Action OnDeath;

    private void Awake()
    {
        Lives = maxLives;
    }

    private void Update()
    {
        if (invulnerableTimer <= 0f) return;

        invulnerableTimer -= Time.deltaTime;
        bool visible = invulnerableTimer <= 0f || Mathf.FloorToInt(invulnerableTimer / flashInterval) % 2 == 0;
        SetRenderersVisible(visible);
    }

    /// <summary>Devuelve true si el daño se aplicó (no estaba invulnerable ni muerto).</summary>
    public bool TakeDamage(int amount = 1)
    {
        if (IsDead || IsInvulnerable || amount <= 0) return false;

        Lives = Mathf.Max(0, Lives - amount);
        invulnerableTimer = invulnerabilityTime;
        AudioManager.Instance?.Play(AudioManager.Sfx.Hurt);
        OnDamaged?.Invoke();
        OnLivesChanged?.Invoke(Lives);

        if (Lives == 0)
        {
            IsDead = true;
            SetRenderersVisible(true);
            OnDeath?.Invoke();
        }
        return true;
    }

    public void RestoreFull()
    {
        Lives = maxLives;
        IsDead = false;
        invulnerableTimer = invulnerabilityTime;
        OnLivesChanged?.Invoke(Lives);
    }

    private void SetRenderersVisible(bool visible)
    {
        if (flashRenderers == null) return;
        foreach (SpriteRenderer r in flashRenderers)
            if (r != null) r.enabled = visible;
    }
}
