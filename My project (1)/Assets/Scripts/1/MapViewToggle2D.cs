using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class MapViewToggle2D : MonoBehaviour
{
    [Header("Camera / Bounds")]
    public Camera cam;                       // 비워두면 Camera.main
    public Collider2D levelBounds;           // 맵 전체 경계(박스/폴리곤/컴포지트 아무거나 OK)
    [Tooltip("여유 비율(1=딱 맞춤, 1.05=5% 여유)")]
    public float margin = 1.05f;

    [Header("Toggle")]
    public KeyCode toggleKey = KeyCode.M;    // 토글 단축키
    public float transitionDuration = 0.6f;  // 전환 시간
    public AnimationCurve ease = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Optional: 끄고/키고 싶은 컴포넌트들(시네머신, 팔로우, 플레이어컨 등)")]
    public Behaviour[] disableWhileMap;      // 맵 모드시 비활성화
    bool[] _prevEnable;                      // 원상복구용

    [Header("Optional: 시작 때 자동 맵 보기")]
    public bool showMapOnStart = false;      // 시작하자마자 맵뷰로
    public float startHold = 1.5f;           // 맵뷰 유지 시간(초)
    public bool returnAfterStartHold = true; // 유지 후 자동 복귀

    // --- state
    bool _isMap = false;
    bool _anim = false;
    Vector3 _returnPos;
    float _returnSize;

    void OnValidate()
    {
        if (!cam) cam = Camera.main;
    }

    void Awake()
    {
        if (!cam) cam = Camera.main;
        if (!cam)
        {
            Debug.LogError("[MapViewToggle2D] Camera를 찾지 못했습니다.");
            enabled = false;
            return;
        }
        if (!cam.orthographic)
            Debug.LogWarning("[MapViewToggle2D] 이 스크립트는 2D(Orthographic) 카메라에 최적화되어 있어요.");

        if (disableWhileMap != null && disableWhileMap.Length > 0)
            _prevEnable = new bool[disableWhileMap.Length];
    }

    IEnumerator Start()
    {
        if (showMapOnStart && levelBounds)
        {
            // 현재 상태를 '복귀 지점'으로 저장
            SnapshotReturnState();

            // 맵뷰로 진입
            yield return ToggleMapView(forceToMap: true);

            // 일정 시간 유지
            if (returnAfterStartHold)
            {
                yield return new WaitForSeconds(startHold);
                yield return ToggleMapView(forceToMap: false);
            }
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
            StartCoroutine(ToggleMapView());
    }

    // --- Public API: 외부에서 열고/닫기 원하면 이거 호출해도 됨 ---
    public void OpenMap() { if (!_isMap) StartCoroutine(ToggleMapView(true)); }
    public void CloseMap() { if (_isMap) StartCoroutine(ToggleMapView(false)); }

    IEnumerator ToggleMapView(bool? forceToMap = null)
    {
        if (_anim) yield break;
        if (!levelBounds)
        {
            Debug.LogWarning("[MapViewToggle2D] levelBounds 가 비어있어 맵뷰를 계산할 수 없어요.");
            yield break;
        }

        bool toMap = forceToMap ?? !_isMap;
        _anim = true;

        // 현재 상태를 복귀 지점으로 저장(맵으로 들어갈 때만)
        if (toMap && !_isMap) SnapshotReturnState();

        // 목표 값 계산
        Vector3 startPos = cam.transform.position;
        float startSize = cam.orthographicSize;

        Vector3 targetPos = startPos;
        float targetSize = startSize;

        if (toMap)
        {
            Bounds b = levelBounds.bounds;
            targetPos = new Vector3(b.center.x, b.center.y, startPos.z);
            targetSize = CalcOrthoToFit(b, margin);

            SetEnabled(disableWhileMap, false); // 팔로우/시네머신/플레이어컨 등 OFF
        }
        else
        {
            targetPos = _returnPos;
            targetSize = _returnSize;

            SetEnabled(disableWhileMap, true);  // 원복
        }

        // 스무스 전환
        float t = 0f;
        while (t < transitionDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / transitionDuration);
            float e = ease.Evaluate(k);

            cam.transform.position = Vector3.Lerp(startPos, targetPos, e);
            cam.orthographicSize = Mathf.Lerp(startSize, targetSize, e);

            yield return null;
        }

        cam.transform.position = targetPos;
        cam.orthographicSize = targetSize;

        _isMap = toMap;
        _anim = false;
    }

    void SnapshotReturnState()
    {
        _returnPos = cam.transform.position;
        _returnSize = cam.orthographicSize;
    }

    float CalcOrthoToFit(Bounds b, float m)
    {
        float halfH = b.extents.y * m;
        float halfW = (b.extents.x * m) / Mathf.Max(0.0001f, cam.aspect);
        return Mathf.Max(halfH, halfW);
    }

    void SetEnabled(Behaviour[] arr, bool on)
    {
        if (arr == null) return;
        for (int i = 0; i < arr.Length; i++)
        {
            var beh = arr[i];
            if (!beh) continue;

            if (_prevEnable != null && i < _prevEnable.Length)
            {
                if (!on) _prevEnable[i] = beh.enabled; // 저장
                beh.enabled = on ? _prevEnable[i] : false;
            }
            else
            {
                beh.enabled = on;
            }
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (!levelBounds) return;
        Gizmos.color = new Color(0, 1, 1, 0.35f);
        Gizmos.DrawWireCube(levelBounds.bounds.center, levelBounds.bounds.size);
    }
#endif
}
