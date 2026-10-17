using UnityEngine;

/// <summary>Balanceo suave para objetos del mundo (notas, llaves, baterías).</summary>
public class SpriteFloat : MonoBehaviour
{
    [SerializeField] private float height = 0.07f;
    [SerializeField] private float speed = 2.2f;
    [SerializeField] private float spinDegrees = 0f;
    [SerializeField] private Transform target;

    private Vector3 baseLocal;
    private float phase;

    private void Awake()
    {
        if (target == null) target = transform;
        baseLocal = target.localPosition;
        phase = Random.Range(0f, Mathf.PI * 2f);
    }

    private void Update()
    {
        if (target == null) return;
        float y = Mathf.Sin(Time.time * speed + phase) * height;
        target.localPosition = baseLocal + Vector3.up * y;
        if (spinDegrees != 0f)
            target.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * speed * 0.5f + phase) * spinDegrees);
    }
}
