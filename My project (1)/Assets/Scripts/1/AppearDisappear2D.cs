using System.Collections;
using UnityEngine;

public class AppearDisappear2D : MonoBehaviour
{
    public enum Mode { AutoLoop, TriggerOnTouch, Manual }
    public Mode mode = Mode.AutoLoop;

    [Header("Loop Timings")]
    public float visibleTime = 1.5f;
    public float hiddenTime = 1.0f;
    public float fadeTime = 0.2f;
    public bool startVisible = true;

    [Header("Trigger Settings")]
    public string triggerTag = "Player";
    public bool toggleOnEnter = true;
    public bool toggleOnExit = false;

    [Header("Targets")]
    public SpriteRenderer[] renderers;
    public Collider2D[] colliders;
    public Behaviour[] extraBehaviours;

    float[] baseAlpha;
    bool visible;
    Coroutine loop;

    void Reset()
    {
        renderers = GetComponentsInChildren<SpriteRenderer>(true);
        colliders = GetComponentsInChildren<Collider2D>(true);
    }

    void Awake()
    {
        if (renderers == null || renderers.Length == 0)
            renderers = GetComponentsInChildren<SpriteRenderer>(true);
        if (colliders == null || colliders.Length == 0)
            colliders = GetComponentsInChildren<Collider2D>(true);

        baseAlpha = new float[renderers.Length];
        for (int i = 0; i < renderers.Length; i++) baseAlpha[i] = renderers[i].color.a;

        visible = startVisible;
        ApplyInstant(visible);
    }

    void OnEnable()
    {
        if (mode == Mode.AutoLoop)
            loop = StartCoroutine(CoLoop());
    }
    void OnDisable()
    {
        if (loop != null) StopCoroutine(loop);
    }

    IEnumerator CoLoop()
    {
        while (true)
        {
            yield return FadeTo(!visible, fadeTime);
            visible = !visible;
            yield return new WaitForSeconds(visible ? visibleTime : hiddenTime);
        }
    }

    public void SetVisible(bool v, bool instant = false)
    {
        if (loop != null) { StopCoroutine(loop); loop = null; }
        if (instant) ApplyInstant(v);
        else StartCoroutine(FadeTo(v, fadeTime));
        visible = v;
    }
    public void Toggle() => SetVisible(!visible);

    IEnumerator FadeTo(bool v, float t)
    {
        float start = v ? 0f : 1f;
        float end = v ? 1f : 0f;

        if (v) SetColliders(true); // 보일 때 미리 켜기

        float e = 0f;
        while (e < t)
        {
            e += Time.deltaTime;
            float k = t > 0f ? Mathf.Clamp01(e / t) : 1f;
            float a = Mathf.Lerp(start, end, k);
            for (int i = 0; i < renderers.Length; i++)
            {
                var c = renderers[i].color; c.a = baseAlpha[i] * a; renderers[i].color = c;
            }
            yield return null;
        }

        for (int i = 0; i < renderers.Length; i++)
        {
            var c = renderers[i].color; c.a = baseAlpha[i] * (v ? 1f : 0f); renderers[i].color = c;
            renderers[i].enabled = v || baseAlpha[i] * (v ? 1f : 0f) > 0f;
        }
        SetColliders(v);
        SetEnabled(extraBehaviours, v);
    }

    void ApplyInstant(bool v)
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            var c = renderers[i].color; c.a = baseAlpha[i] * (v ? 1f : 0f); renderers[i].color = c;
            renderers[i].enabled = v || baseAlpha[i] * (v ? 1f : 0f) > 0f;
        }
        SetColliders(v);
        SetEnabled(extraBehaviours, v);
    }

    void SetColliders(bool on)
    {
        for (int i = 0; i < colliders.Length; i++)
            if (colliders[i]) colliders[i].enabled = on;
    }
    void SetEnabled(Behaviour[] arr, bool on)
    {
        if (arr == null) return;
        for (int i = 0; i < arr.Length; i++) if (arr[i]) arr[i].enabled = on;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (mode != Mode.TriggerOnTouch) return;
        if (!string.IsNullOrEmpty(triggerTag) && !other.CompareTag(triggerTag)) return;
        if (toggleOnEnter) Toggle();
    }
    void OnTriggerExit2D(Collider2D other)
    {
        if (mode != Mode.TriggerOnTouch) return;
        if (!string.IsNullOrEmpty(triggerTag) && !other.CompareTag(triggerTag)) return;
        if (toggleOnExit) Toggle();
    }
}
