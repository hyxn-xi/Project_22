using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class ScreenFlash : MonoBehaviour
{
    public static ScreenFlash Instance { get; private set; }

    [Header("Overlay (풀스크린 Image)")]
    [Tooltip("비워두면 런타임에 자동 Canvas + Image를 생성합니다.")]
    public Image overlay;

    [Header("Defaults")]
    public Color defaultColor = Color.red;
    [Tooltip("간단 Flash() 호출 시 사용할 총 길이(대략 in 25% / out 75%)")]
    public float defaultDuration = 0.15f;
    [Range(0f, 1f)] public float defaultMaxAlpha = 0.35f;

    [Header("Sprite Options")]
    [Tooltip("Inspector의 Overlay.Image에 이미 스프라이트가 들어있다면 그걸 유지합니다.")]
    public bool useOverlaySpriteIfAssigned = true;
    [Tooltip("Overlay에 스프라이트가 비어있을 때 쓸 기본 스프라이트(없으면 1x1 흰색).")]
    public Sprite fallbackSprite;
    public bool preserveAspect = true;
    public bool setNativeSize = false;

    Coroutine _co;
    static Sprite _white1x1;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (overlay == null)
            overlay = CreateRuntimeOverlay();

        PrepareOverlay(overlay);
        overlay.gameObject.SetActive(false);
    }

    Image CreateRuntimeOverlay()
    {
        var canvasGO = new GameObject("ScreenFXCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        DontDestroyOnLoad(canvasGO);
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9990;

        var imgGO = new GameObject("ScreenFlashOverlay", typeof(RectTransform), typeof(Image));
        imgGO.transform.SetParent(canvasGO.transform, false);

        var rt = imgGO.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

        return imgGO.GetComponent<Image>();
    }

    void PrepareOverlay(Image img)
    {
        if (_white1x1 == null)
        {
            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            _white1x1 = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
        }

        if (!(useOverlaySpriteIfAssigned && img.sprite != null))
            img.sprite = fallbackSprite != null ? fallbackSprite : _white1x1;

        img.type = Image.Type.Simple;
        img.preserveAspect = preserveAspect;
        if (setNativeSize) img.SetNativeSize();

        img.material = null;
        img.raycastTarget = false;

        var c = img.color; c.a = 0f; img.color = c;
    }

    // ========= Public API =========

    // 색/시간 모두 지정 (스프라이트는 현재 overlay.sprite 사용)
    public void Flash(Color color, float fadeIn, float hold, float fadeOut, float maxAlpha = -1f)
    {
        if (maxAlpha < 0f) maxAlpha = defaultMaxAlpha;
        if (_co != null) StopCoroutine(_co);
        _co = StartCoroutine(CoFlash(color, fadeIn, hold, fadeOut, maxAlpha, null, false));
    }

    // 간단 오버로드들
    public void Flash(Color color, float fadeIn, float fadeOut) => Flash(color, fadeIn, 0f, fadeOut, defaultMaxAlpha);
    public void Flash(float maxAlpha, float fadeIn, float hold, float fadeOut) => Flash(defaultColor, fadeIn, hold, fadeOut, maxAlpha);
    public void Flash(float maxAlpha, float fadeIn, float fadeOut) => Flash(defaultColor, fadeIn, 0f, fadeOut, maxAlpha);
    public void Flash() => Flash(defaultColor, defaultDuration * 0.25f, 0f, defaultDuration * 0.75f, defaultMaxAlpha);

    // ★ 스프라이트 지정해서 깜빡이기 (끝나면 원래 스프라이트로 복구)
    public void FlashSprite(Sprite sprite, Color tint, float fadeIn, float hold, float fadeOut, float maxAlpha = -1f, bool preserve = true, bool nativeSize = false)
    {
        if (overlay == null || sprite == null) { Flash(tint, fadeIn, hold, fadeOut, maxAlpha); return; }
        if (maxAlpha < 0f) maxAlpha = defaultMaxAlpha;
        if (_co != null) StopCoroutine(_co);
        _co = StartCoroutine(CoFlash(tint, fadeIn, hold, fadeOut, maxAlpha, sprite, true, preserve, nativeSize));
    }

    IEnumerator CoFlash(Color color, float tIn, float tHold, float tOut, float maxA, Sprite tmpSprite, bool revertSprite,
                        bool preserve = true, bool nativeSize = false)
    {
        if (overlay == null) yield break;

        Sprite prev = overlay.sprite;
        if (tmpSprite != null)
        {
            overlay.sprite = tmpSprite;
            overlay.preserveAspect = preserve;
            if (nativeSize) overlay.SetNativeSize();
        }

        if (!overlay.gameObject.activeSelf) overlay.gameObject.SetActive(true);

        color.a = 0f;
        overlay.color = color;

        float t = 0f;
        while (tIn > 0f && t < tIn)
        {
            t += Time.unscaledDeltaTime;
            SetAlpha(color, Mathf.Lerp(0f, maxA, t / tIn));
            yield return null;
        }
        SetAlpha(color, maxA);

        t = 0f;
        while (tHold > 0f && t < tHold) { t += Time.unscaledDeltaTime; yield return null; }

        t = 0f;
        while (tOut > 0f && t < tOut)
        {
            t += Time.unscaledDeltaTime;
            SetAlpha(color, Mathf.Lerp(maxA, 0f, t / tOut));
            yield return null;
        }

        SetAlpha(color, 0f);
        overlay.gameObject.SetActive(false);

        if (revertSprite) overlay.sprite = prev;

        _co = null;
    }

    void SetAlpha(Color baseColor, float a)
    {
        var c = baseColor; c.a = a;
        overlay.color = c;
    }
}
