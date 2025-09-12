using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class RadialReveal : MonoBehaviour
{
    public RawImage rawImage;
    [Range(0, 1)] public float edgeSoftness = 0.08f;
    public bool debugLog = false;

    Material _mat;
    bool _inConfigure;

    // 후보 프로퍼티 이름들(셰이더마다 달라질 수 있음)
    static readonly string[] P_Center = { "_Center", "Center", "_CenterUV", "_CenterVP" };
    static readonly string[] P_Aspect = { "_Aspect", "Aspect", "_AspectRatio" };
    static readonly string[] P_MaxR = { "_MaxRadius", "MaxRadius", "_RadiusMax" };
    static readonly string[] P_Progress = { "_Progress", "Progress" };
    static readonly string[] P_Soft = { "_Softness", "Softness", "_EdgeSoftness" };
    static readonly string[] P_MainTex = { "_MainTex", "_BaseMap" };

    // --- Unity lifecycle ---
    void Awake()
    {
        if (!rawImage) rawImage = GetComponent<RawImage>();

        if (rawImage && rawImage.material)
        {
            _mat = new Material(rawImage.material);   // 공유 머티 오염 방지
            rawImage.material = _mat;
        }
        else
        {
            var sh = Shader.Find("UI/RadialReveal");
            if (sh != null)
            {
                _mat = new Material(sh);
                if (rawImage) rawImage.material = _mat;
            }
        }

        SetEdge(edgeSoftness);
        SetProgress(0f);

        if (debugLog && _mat)
        {
            Debug.Log($"[RadialReveal] Using shader: {_mat.shader.name}");
            LogWhichPropsExist();
        }
    }

    // --- Public API ---
    public void SetTexture(Texture tex)
    {
        if (!rawImage) return;
        rawImage.texture = tex;
        SetTextureMulti(P_MainTex, tex);
    }

    public void SetProgress(float v)
    {
        float vv = Mathf.Clamp01(v);
        SetFloatMulti(P_Progress, vv);
    }

    public void SetCenter01(Vector2 uv01)
    {
        SetVectorMulti(P_Center, new Vector4(uv01.x, uv01.y, 0, 0));
        if (debugLog) Debug.Log($"[RadialReveal] SetCenter01 uv={uv01}");
    }

    public void SetEdge(float s)
    {
        SetFloatMulti(P_Soft, s);
    }

    public void SetAspect(float aspect)
    {
        SetFloatMulti(P_Aspect, aspect);
    }

    public void SetMaxRadius(float r)
    {
        SetFloatMulti(P_MaxR, r);
    }

    /// RawImage의 UV(0~1)가 이미 있으면 직접 세팅
    public void ConfigureForImageUV(Vector2 uv01)
    {
        if (!IsReady()) return;

        RectTransform rt = rawImage.rectTransform;
        Rect r = rt.rect;
        float aspect = r.width / Mathf.Max(1e-6f, r.height);
        SetAspect(aspect);

        float maxR = ComputeMaxRadius(uv01, aspect);
        SetCenter01(uv01);
        SetMaxRadius(maxR);
    }

    /// 플레이어의 월드 좌표 → RawImage UV(0~1)로 변환 후 세팅
    public void ConfigureForPlayer(Camera cam, Vector3 worldCenter)
    {
        if (!IsReady() || !cam) return;
        if (_inConfigure) return;

        _inConfigure = true;
        try
        {
            // 1) 월드 → 스크린 픽셀
            Vector3 sp3 = cam.WorldToScreenPoint(worldCenter);
            if (sp3.z < 0f)
            {
                ConfigureForImageUV(new Vector2(0.5f, 0.5f)); // 뒤쪽이면 임시 대체
                return;
            }

            // 2) 스크린 → RawImage 로컬
            RectTransform rt = rawImage.rectTransform;
            var canvas = rt.GetComponentInParent<Canvas>();
            Camera uiCam = null;
            if (canvas && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                uiCam = canvas.worldCamera; // Screen Space - Camera / World Space

            Vector2 local;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rt, new Vector2(sp3.x, sp3.y), uiCam, out local);

            // 3) 로컬(중심 원점) → UV 0~1
            Rect r = rt.rect;
            float u = Mathf.InverseLerp(r.xMin, r.xMax, local.x);
            float v = Mathf.InverseLerp(r.yMin, r.yMax, local.y);
            Vector2 uv01 = new Vector2(Mathf.Clamp01(u), Mathf.Clamp01(v));

            // 4) 세팅
            ConfigureForImageUV(uv01);

            if (debugLog)
                Debug.Log($"[RadialReveal] world={worldCenter} screen={sp3} uv={uv01} cam={cam.name}");
        }
        finally { _inConfigure = false; }
    }

    // 기존 시그니처와의 호환 (내부는 RawImage 기준 계산)
    public void ConfigureForCamera(Camera cam, Vector2 centerUV) => ConfigureForImageUV(centerUV);
    public void ConfigureForCamera(Camera cam, Vector3 worldCenter) => ConfigureForPlayer(cam, worldCenter);

    // --- Helpers ---
    bool IsReady() => _mat && rawImage;

    float ComputeMaxRadius(Vector2 uv01, float aspect)
    {
        Vector2 scale = aspect >= 1f ? new Vector2(aspect, 1f)
                                     : new Vector2(1f, 1f / aspect);
        Vector2[] corners = { new(0, 0), new(1, 0), new(0, 1), new(1, 1) };

        float maxR = 0f;
        for (int i = 0; i < 4; i++)
        {
            Vector2 d = corners[i] - uv01;
            d = new Vector2(d.x * scale.x, d.y * scale.y);
            maxR = Mathf.Max(maxR, d.magnitude);
        }
        return maxR * 1.02f; // 여유
    }

    // 멀티-프로퍼티 세터 (존재하는 첫 번째 프로퍼티에만 기록)
    void SetFloatMulti(string[] names, float v)
    {
        if (!_mat) return;
        for (int i = 0; i < names.Length; i++)
        {
            if (_mat.HasProperty(names[i]))
            {
                _mat.SetFloat(names[i], v);
                if (debugLog) Debug.Log($"[RadialReveal] SetFloat {names[i]}={v}");
                return;
            }
        }
    }

    void SetVectorMulti(string[] names, Vector4 v)
    {
        if (!_mat) return;
        for (int i = 0; i < names.Length; i++)
        {
            if (_mat.HasProperty(names[i]))
            {
                _mat.SetVector(names[i], v);
                if (debugLog) Debug.Log($"[RadialReveal] SetVector {names[i]}={v}");
                return;
            }
        }
    }

    void SetTextureMulti(string[] names, Texture t)
    {
        if (!_mat) return;
        for (int i = 0; i < names.Length; i++)
        {
            if (_mat.HasProperty(names[i]))
            {
                _mat.SetTexture(names[i], t);
                if (debugLog) Debug.Log($"[RadialReveal] SetTexture {names[i]}={(t ? t.name : "null")}");
                return;
            }
        }
    }

    void LogWhichPropsExist()
    {
        string Has(string[] a)
        {
            for (int i = 0; i < a.Length; i++)
                if (_mat.HasProperty(a[i])) return a[i];
            return "(none)";
        }
        Debug.Log($"[RadialReveal] Center:{Has(P_Center)} Aspect:{Has(P_Aspect)} MaxR:{Has(P_MaxR)} Progress:{Has(P_Progress)} Soft:{Has(P_Soft)} MainTex:{Has(P_MainTex)}");
    }
}
