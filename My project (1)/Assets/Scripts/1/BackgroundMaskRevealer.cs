using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class BackgroundMaskRevealer : MonoBehaviour
{
    [Header("Refs")]
    public DualBackgroundPair bgPair;              // BGs(DualBackgroundPair) 참조
    public Collider2D levelBounds;                 // 스테이지 경계(없으면 대략값으로 계산)
    public Sprite circleSprite;                    // 비워두면 자동 생성

    [Header("Timing / Ease")]
    public float revealDuration = 0.9f;
    public AnimationCurve ease = null;

    [Header("Auto Circle")]
    public bool autoCreateCircleIfMissing = true;
    public int autoCircleSize = 512;
    public Color autoCircleColor = Color.white;

    [Header("Debug")]
    public bool verboseLog = true;
    public bool keepMaskAfterReveal = true;        // 끝나도 마스크를 파괴하지 않음(하이어라키에서 확인용)

    SpriteMask _mask;

    public IEnumerator Reveal(Vector3 centerWorld)
    {
        if (!bgPair || !bgPair.bright || !bgPair.dark)
        {
            Debug.LogError("[BMR] bgPair/bgs null (DualBackgroundPair 연결 필요)");
            yield break;
        }
        if (!circleSprite && autoCreateCircleIfMissing)
            circleSprite = CreateCircle(autoCircleSize, autoCircleColor);
        if (!circleSprite)
        {
            Debug.LogError("[BMR] circleSprite null (Auto Create 끄면 직접 지정해야 함)");
            yield break;
        }

        var bright = bgPair.bright;
        var dark = bgPair.dark;

        // 같은 Sorting Layer 강제 & bright를 앞으로
        string layerName = bright.sortingLayerName;
        dark.sortingLayerName = layerName;

        int bo = bright.sortingOrder;
        int do_ = dark.sortingOrder;
        if (bo <= do_) { bo = do_ + 1; do_ = bo - 1; bright.sortingOrder = bo; dark.sortingOrder = do_; }

        int layerId = SortingLayer.NameToID(layerName);
        int frontOrder = bo + 1;   // 여유
        int backOrder = do_ - 1;

        // 마스크 인터랙션
        bright.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
        dark.maskInteraction = SpriteMaskInteraction.None;
        bright.gameObject.SetActive(true);
        dark.gameObject.SetActive(true);

        if (verboseLog)
            Debug.Log($"[BMR] Reveal start. Layer='{layerName}'  bright={bright.sortingOrder}, dark={dark.sortingOrder}  -> maskRange [{backOrder}..{frontOrder}]");

        // 스프라이트 마스크(★자식으로 반드시 생성)
        EnsureMaskObject();
        _mask.sprite = circleSprite;
        _mask.backSortingLayerID = layerId;
        _mask.frontSortingLayerID = layerId;
        _mask.backSortingOrder = backOrder;
        _mask.frontSortingOrder = frontOrder;

        _mask.transform.position = new Vector3(centerWorld.x, centerWorld.y, 0f);
        _mask.transform.localScale = Vector3.one * 0.001f;

        float targetScale = ComputeTargetScale(centerWorld, levelBounds, circleSprite);
        var curve = ease ?? AnimationCurve.EaseInOut(0, 0, 1, 1);
        float t = 0f;
        while (t < revealDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / revealDuration);
            float s = Mathf.Lerp(0.001f, targetScale, curve.Evaluate(k));
            _mask.transform.localScale = new Vector3(s, s, 1f);
            yield return null;
        }
        _mask.transform.localScale = Vector3.one * targetScale;

        // 마무리
        bright.maskInteraction = SpriteMaskInteraction.None;
        bgPair.ShowBrightNow();

        if (!keepMaskAfterReveal && _mask) { Destroy(_mask.gameObject); _mask = null; }

        if (verboseLog) Debug.Log("[BMR] Reveal done.");
    }

    void EnsureMaskObject()
    {
        if (_mask != null) return;
        var go = new GameObject("BG_RadialMask");
        go.transform.SetParent(transform, worldPositionStays: false);
        _mask = go.AddComponent<SpriteMask>();
        if (verboseLog) Debug.Log("[BMR] SpriteMask created as child: BG_RadialMask");
    }

    float ComputeTargetScale(Vector3 center, Collider2D boundsCol, Sprite circle)
    {
        Bounds b = boundsCol ? boundsCol.bounds : new Bounds(center, new Vector3(30, 20, 1));
        Vector2[] cs = { new(b.min.x, b.min.y), new(b.min.x, b.max.y), new(b.max.x, b.min.y), new(b.max.x, b.max.y) };
        float maxDist = 0f; Vector2 c = new(center.x, center.y);
        foreach (var v in cs) maxDist = Mathf.Max(maxDist, Vector2.Distance(c, v));
        float rWorld = circle.bounds.extents.x; if (rWorld <= 0f) rWorld = 0.5f;
        return (maxDist / rWorld) * 1.08f;
    }

    Sprite CreateCircle(int s, Color col)
    {
        var tex = new Texture2D(s, s, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
        var tr = new Color(0, 0, 0, 0); float cx = (s - 1) * 0.5f, r = cx - 1f;
        for (int y = 0; y < s; y++) for (int x = 0; x < s; x++)
            {
                float dx = x - cx, dy = y - cx; tex.SetPixel(x, y, (dx * dx + dy * dy <= r * r) ? col : tr);
            }
        tex.Apply();
        var sp = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f);
        sp.name = "AutoCircleSprite_" + s;
        if (verboseLog) Debug.Log("[BMR] Circle sprite auto-created.");
        return sp;
    }

    // 인스펙터에서 바로 확인용
    [ContextMenu("DEBUG/Spawn Mask Here")]
    void DebugSpawn()
    {
        if (!circleSprite && autoCreateCircleIfMissing) circleSprite = CreateCircle(autoCircleSize, autoCircleColor);
        EnsureMaskObject();

        var bright = bgPair ? bgPair.bright : null;
        var dark = bgPair ? bgPair.dark : null;
        string layerName = (bright ? bright.sortingLayerName : "bg");
        int layerId = SortingLayer.NameToID(layerName);
        int bo = bright ? bright.sortingOrder : -99;
        int do_ = dark ? dark.sortingOrder : -100;

        _mask.sprite = circleSprite;
        _mask.backSortingLayerID = layerId; _mask.frontSortingLayerID = layerId;
        _mask.backSortingOrder = Mathf.Min(bo, do_) - 1;
        _mask.frontSortingOrder = Mathf.Max(bo, do_) + 1;
        _mask.transform.position = Vector3.zero;
        _mask.transform.localScale = Vector3.one * 0.5f;

        if (bright) bright.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
        if (dark) dark.maskInteraction = SpriteMaskInteraction.None;

        Debug.Log("[BMR] Debug mask spawned under this object.");
    }
}
