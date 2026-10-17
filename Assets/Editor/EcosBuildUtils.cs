using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using Object = UnityEngine.Object;

/// <summary>Utilidades para construir escenas desde código.</summary>
public static class EcosBuildUtils
{
    private static Font font;

    public static Font UIFont => font != null ? font : font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

    // ---------------- Campos serializados ----------------

    /// <summary>Asigna un campo serializado (también privados con [SerializeField]).</summary>
    public static void Set(Object target, string field, object value)
    {
        var so = new SerializedObject(target);
        SerializedProperty p = so.FindProperty(field);
        if (p == null)
        {
            Debug.LogError($"No existe el campo '{field}' en {target.GetType().Name}.", target);
            return;
        }

        switch (value)
        {
            case null: p.objectReferenceValue = null; break;
            case Object obj: p.objectReferenceValue = obj; break;
            case string s: p.stringValue = s; break;
            case bool b: p.boolValue = b; break;
            case Enum e: p.intValue = Convert.ToInt32(e); break;
            case int i: p.intValue = i; break;
            case float f: p.floatValue = f; break;
            case Color c: p.colorValue = c; break;
            case Vector2 v: p.vector2Value = v; break;
            case string[] arr:
                p.arraySize = arr.Length;
                for (int k = 0; k < arr.Length; k++) p.GetArrayElementAtIndex(k).stringValue = arr[k];
                break;
            case Object[] objs:
                p.arraySize = objs.Length;
                for (int k = 0; k < objs.Length; k++) p.GetArrayElementAtIndex(k).objectReferenceValue = objs[k];
                break;
            default:
                Debug.LogError($"Tipo no soportado para '{field}': {value.GetType().Name}");
                break;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // ---------------- Objetos de escena ----------------

    public static GameObject GO(string name, Transform parent = null, Vector2? position = null)
    {
        var go = new GameObject(name);
        if (parent != null) go.transform.SetParent(parent, false);
        if (position.HasValue) go.transform.position = position.Value;
        return go;
    }

    public static SpriteRenderer Visual(Transform parent, Sprite sprite, Color color, Vector2 size, int order,
        string name = "Visual", Vector2 offset = default, float rotation = 0f)
    {
        GameObject go = GO(name, parent);
        go.transform.localPosition = offset;
        go.transform.localRotation = Quaternion.Euler(0f, 0f, rotation);
        go.transform.localScale = new Vector3(size.x, size.y, 1f);
        var r = go.AddComponent<SpriteRenderer>();
        r.sprite = sprite;
        r.color = color;
        r.sortingOrder = order;
        return r;
    }

    public static BoxCollider2D Box(GameObject go, Vector2 size, bool trigger, Vector2 offset = default)
    {
        var c = go.AddComponent<BoxCollider2D>();
        c.size = size;
        c.offset = offset;
        c.isTrigger = trigger;
        return c;
    }

    public static GameObject TriggerChild(Transform parent, Vector2 size, string name = "ZonaInteraccion")
    {
        GameObject go = GO(name, parent);
        go.transform.localPosition = Vector3.zero;
        Box(go, size, true);
        return go;
    }

    /// <summary>Obstáculo sólido con visual rectangular.</summary>
    public static GameObject Solid(Transform parent, string name, Vector2 position, Vector2 size, Color color, int order = -20)
    {
        GameObject go = GO(name, parent, position);
        Visual(go.transform, EcosAssetFactory.Square, color, size, order);
        Box(go, size, false);
        return go;
    }

    public static SpriteRenderer Decor(Transform parent, string name, Sprite sprite, Color color, Vector2 position,
        Vector2 size, int order = -60, float rotation = 0f)
    {
        GameObject go = GO(name, parent, position);
        go.transform.localRotation = Quaternion.Euler(0f, 0f, rotation);
        go.transform.localScale = new Vector3(size.x, size.y, 1f);
        var r = go.AddComponent<SpriteRenderer>();
        r.sprite = sprite;
        r.color = color;
        r.sortingOrder = order;
        return r;
    }

    // ---------------- Luces ----------------

    public static Light2D MakeLight(Transform parent, string name, Light2D.LightType type, Color color, float intensity,
        float radius = 3f, Vector2 offset = default)
    {
        GameObject go = GO(name, parent);
        go.transform.localPosition = offset;
        var light = go.AddComponent<Light2D>();
        light.lightType = type;
        light.color = color;
        light.intensity = intensity;
        if (type == Light2D.LightType.Point)
        {
            light.pointLightOuterRadius = radius;
            light.pointLightInnerRadius = radius * 0.15f;
        }

        var so = new SerializedObject(light);
        SerializedProperty layers = so.FindProperty("m_ApplyToSortingLayers");
        if (layers != null)
        {
            SortingLayer[] all = SortingLayer.layers;
            layers.arraySize = all.Length;
            for (int i = 0; i < all.Length; i++) layers.GetArrayElementAtIndex(i).intValue = all[i].id;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        return light;
    }

    /// <summary>Partículas ambientales discretas (polvo / pétalos / niebla).</summary>
    public static AmbientParticles MakeAmbientParticles(Transform parent, string name, Vector2 position,
        Vector2 area, Color color, float rate = 4f, Sprite sprite = null)
    {
        GameObject go = GO(name, parent, position);
        go.AddComponent<ParticleSystem>();
        var ambient = go.AddComponent<AmbientParticles>();
        ambient.Apply(color, area, rate, sprite != null ? sprite : EcosAssetFactory.Circle);
        return ambient;
    }

    // ---------------- Interfaz ----------------

    public static readonly Vector2 Center = new Vector2(0.5f, 0.5f);

    public static RectTransform MakeRect(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
        Vector2 position, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = position;
        rt.sizeDelta = size;
        return rt;
    }

    public static RectTransform Stretch(Transform parent, string name)
    {
        RectTransform rt = MakeRect(parent, name, Vector2.zero, Vector2.one, Center, Vector2.zero, Vector2.zero);
        return rt;
    }

    public static Image MakeImage(RectTransform rt, Color color, Sprite sprite = null, bool raycast = true)
    {
        var img = rt.gameObject.AddComponent<Image>();
        img.color = color;
        img.sprite = sprite;
        img.raycastTarget = raycast;
        return img;
    }

    public static Text MakeText(RectTransform rt, string content, int size, Color color, TextAnchor align, bool shadow = true)
    {
        var t = rt.gameObject.AddComponent<Text>();
        t.font = UIFont;
        t.text = content;
        t.fontSize = size;
        t.color = color;
        t.alignment = align;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.supportRichText = true;
        t.raycastTarget = false;
        if (shadow)
        {
            var s = rt.gameObject.AddComponent<Shadow>();
            s.effectColor = new Color(0f, 0f, 0f, 0.85f);
            s.effectDistance = new Vector2(2f, -2f);
        }
        return t;
    }

    public static Button MakeButton(Transform parent, string label, Vector2 position, MenuAction action,
        string sceneName = null, GameObject panel = null, float width = 560f)
    {
        RectTransform rt = MakeRect(parent, "Boton_" + label, Center, Center, Center, position, new Vector2(width, 68f));
        Image img = MakeImage(rt, Color.white);
        var button = rt.gameObject.AddComponent<Button>();
        button.targetGraphic = img;
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.22f, 0.1f, 0.08f, 0.95f);
        colors.highlightedColor = new Color(0.45f, 0.2f, 0.1f, 1f);
        colors.selectedColor = new Color(0.45f, 0.2f, 0.1f, 1f);
        colors.pressedColor = new Color(0.6f, 0.3f, 0.12f, 1f);
        button.colors = colors;

        RectTransform textRt = Stretch(rt, "Texto");
        MakeText(textRt, label, 30, new Color(0.96f, 0.9f, 0.8f), TextAnchor.MiddleCenter, false);

        var mb = rt.gameObject.AddComponent<MenuButton>();
        Set(mb, "action", action);
        if (sceneName != null) Set(mb, "sceneName", sceneName);
        if (panel != null) Set(mb, "panel", panel);
        return button;
    }

    public static Canvas MakeCanvas(string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        go.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    public static void MakeEventSystem()
    {
        var go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();
        go.AddComponent<InputSystemUIInputModule>();
        // UiInputGuard asigna/limpia acciones en Awake/OnDestroy (necesario sin domain reload).
        go.AddComponent<UiInputGuard>();
    }

    public static Camera MakeCamera(Vector3 position, Color background, float size = 6f)
    {
        var go = new GameObject("Main Camera");
        go.tag = "MainCamera";
        go.transform.position = position;
        var cam = go.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = size;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = background;
        cam.nearClipPlane = 0.3f;
        cam.farClipPlane = 100f;
        return cam;
    }

    public static AudioSource MakeAudio(GameObject go, AudioClip clip, float volume, bool loop, bool playOnAwake, bool spatial = false)
    {
        var src = go.AddComponent<AudioSource>();
        src.clip = clip;
        src.volume = volume;
        src.loop = loop;
        src.playOnAwake = playOnAwake;
        if (spatial)
        {
            src.spatialBlend = 1f;
            src.rolloffMode = AudioRolloffMode.Linear;
            src.minDistance = 2f;
            src.maxDistance = 14f;
            src.dopplerLevel = 0f;
        }
        return src;
    }
}
