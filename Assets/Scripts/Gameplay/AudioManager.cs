using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public enum Sfx
    {
        Pickup,
        Clue,
        Error,
        Solve,
        Hurt,
        Door,
        Locked,
        PlatePress,
        Chains,
        Lever,
        Click,
        StoneDoor,
        Hooves,
        Footstep
    }

    public static AudioManager Instance { get; private set; }

    [Header("Fuentes")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource ambienceSource;
    [SerializeField] private AudioSource tensionSource;

    [Header("Efectos")]
    [SerializeField] private AudioClip pickup;
    [SerializeField] private AudioClip clue;
    [SerializeField] private AudioClip error;
    [SerializeField] private AudioClip solve;
    [SerializeField] private AudioClip hurt;
    [SerializeField] private AudioClip door;
    [SerializeField] private AudioClip locked;
    [SerializeField] private AudioClip platePress;
    [SerializeField] private AudioClip chains;
    [SerializeField] private AudioClip lever;
    [SerializeField] private AudioClip click;
    [SerializeField] private AudioClip stoneDoor;
    [SerializeField] private AudioClip hooves;
    [SerializeField] private AudioClip footstep;

    [Header("Mezcla")]
    [SerializeField] private float ambienceVolume = 0.18f;
    [SerializeField] private float tensionVolume = 0.36f;
    [SerializeField] private float fadeSpeed = 0.45f;

    private readonly HashSet<object> chasers = new HashSet<object>();
    private float duckFactor = 1f;
    private float duckTimer;
    private bool permanentDuck;

    private void Awake()
    {
        Instance = this;
        if (ambienceSource != null) ambienceSource.volume = ambienceVolume;
        if (tensionSource != null) tensionSource.volume = 0f;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        chasers.RemoveWhere(c => c == null || (c is Object o && o == null));

        if (duckTimer > 0f)
        {
            duckTimer -= Time.unscaledDeltaTime;
            if (duckTimer <= 0f && !permanentDuck) duckFactor = 1f;
        }

        float step = fadeSpeed * Time.unscaledDeltaTime;
        if (ambienceSource != null)
            ambienceSource.volume = Mathf.MoveTowards(ambienceSource.volume, ambienceVolume * duckFactor, step);
        if (tensionSource != null)
            tensionSource.volume = Mathf.MoveTowards(tensionSource.volume, chasers.Count > 0 ? tensionVolume : 0f, step);
    }

    public void Play(Sfx sfx, float volume = 1f)
    {
        AudioClip clip = sfx switch
        {
            Sfx.Pickup => pickup,
            Sfx.Clue => clue,
            Sfx.Error => error,
            Sfx.Solve => solve,
            Sfx.Hurt => hurt,
            Sfx.Door => door,
            Sfx.Locked => locked,
            Sfx.PlatePress => platePress,
            Sfx.Chains => chains,
            Sfx.Lever => lever,
            Sfx.Click => click,
            Sfx.StoneDoor => stoneDoor,
            Sfx.Hooves => hooves,
            Sfx.Footstep => footstep,
            _ => null
        };
        if (clip != null && sfxSource != null) sfxSource.PlayOneShot(clip, volume);
    }

    /// <summary>Baja el ambiente temporalmente (duration &gt; 0) o de forma permanente (duration &lt;= 0).</summary>
    public void DuckAmbience(float factor, float duration)
    {
        duckFactor = Mathf.Clamp01(factor);
        permanentDuck = duration <= 0f;
        duckTimer = duration;
    }

    public void SetChasing(object who, bool chasing)
    {
        if (chasing) chasers.Add(who);
        else chasers.Remove(who);
    }
}
