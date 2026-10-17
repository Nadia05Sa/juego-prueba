using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float smoothTime = 0.15f;

    [Header("Límites del mapa (coordenadas del mundo)")]
    [SerializeField] private bool useBounds = true;
    [SerializeField] private Vector2 minBounds;
    [SerializeField] private Vector2 maxBounds = new Vector2(48f, 32f);

    private Camera cam;
    private Vector3 velocity;

    private void Awake()
    {
        cam = GetComponent<Camera>();
    }

    private void Start() => SnapToTarget();

    private void LateUpdate()
    {
        if (target == null) return;
        Vector3 desired = Clamp(target.position);
        transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, smoothTime);
    }

    public void SnapToTarget()
    {
        if (target == null) return;
        transform.position = Clamp(target.position);
        velocity = Vector3.zero;
    }

    private Vector3 Clamp(Vector3 pos)
    {
        pos.z = transform.position.z;
        if (!useBounds || cam == null || !cam.orthographic) return pos;

        float halfH = cam.orthographicSize;
        float halfW = halfH * cam.aspect;

        pos.x = (maxBounds.x - minBounds.x) <= halfW * 2f
            ? (minBounds.x + maxBounds.x) * 0.5f
            : Mathf.Clamp(pos.x, minBounds.x + halfW, maxBounds.x - halfW);
        pos.y = (maxBounds.y - minBounds.y) <= halfH * 2f
            ? (minBounds.y + maxBounds.y) * 0.5f
            : Mathf.Clamp(pos.y, minBounds.y + halfH, maxBounds.y - halfH);
        return pos;
    }
}
