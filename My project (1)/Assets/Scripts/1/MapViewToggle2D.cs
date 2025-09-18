using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class MapViewToggle2D : MonoBehaviour
{
    [Header("Camera / Bounds")]
    public Camera cam;                       // ����θ� Camera.main
    public Collider2D levelBounds;           // �� ��ü ���(�ڽ�/������/������Ʈ �ƹ��ų� OK)
    [Tooltip("���� ����(1=�� ����, 1.05=5% ����)")]
    public float margin = 1.05f;

    [Header("Toggle")]
    public KeyCode toggleKey = KeyCode.M;    // ��� ����Ű
    public float transitionDuration = 0.6f;  // ��ȯ �ð�
    public AnimationCurve ease = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Optional: ����/Ű�� ���� ������Ʈ��(�ó׸ӽ�, �ȷο�, �÷��̾��� ��)")]
    public Behaviour[] disableWhileMap;      // �� ���� ��Ȱ��ȭ
    bool[] _prevEnable;                      // ���󺹱���

    [Header("Optional: ���� �� �ڵ� �� ����")]
    public bool showMapOnStart = false;      // �������ڸ��� �ʺ��
    public float startHold = 1.5f;           // �ʺ� ���� �ð�(��)
    public bool returnAfterStartHold = true; // ���� �� �ڵ� ����

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
            Debug.LogError("[MapViewToggle2D] Camera�� ã�� ���߽��ϴ�.");
            enabled = false;
            return;
        }
        if (!cam.orthographic)
            Debug.LogWarning("[MapViewToggle2D] �� ��ũ��Ʈ�� 2D(Orthographic) ī�޶� ����ȭ�Ǿ� �־��.");

        if (disableWhileMap != null && disableWhileMap.Length > 0)
            _prevEnable = new bool[disableWhileMap.Length];
    }

    IEnumerator Start()
    {
        if (showMapOnStart && levelBounds)
        {
            // ���� ���¸� '���� ����'���� ����
            SnapshotReturnState();

            // �ʺ�� ����
            yield return ToggleMapView(forceToMap: true);

            // ���� �ð� ����
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

    // --- Public API: �ܺο��� ����/�ݱ� ���ϸ� �̰� ȣ���ص� �� ---
    public void OpenMap() { if (!_isMap) StartCoroutine(ToggleMapView(true)); }
    public void CloseMap() { if (_isMap) StartCoroutine(ToggleMapView(false)); }

    IEnumerator ToggleMapView(bool? forceToMap = null)
    {
        if (_anim) yield break;
        if (!levelBounds)
        {
            Debug.LogWarning("[MapViewToggle2D] levelBounds �� ����־� �ʺ並 ����� �� �����.");
            yield break;
        }

        bool toMap = forceToMap ?? !_isMap;
        _anim = true;

        // ���� ���¸� ���� �������� ����(������ �� ����)
        if (toMap && !_isMap) SnapshotReturnState();

        // ��ǥ �� ���
        Vector3 startPos = cam.transform.position;
        float startSize = cam.orthographicSize;

        Vector3 targetPos = startPos;
        float targetSize = startSize;

        if (toMap)
        {
            Bounds b = levelBounds.bounds;
            targetPos = new Vector3(b.center.x, b.center.y, startPos.z);
            targetSize = CalcOrthoToFit(b, margin);

            SetEnabled(disableWhileMap, false); // �ȷο�/�ó׸ӽ�/�÷��̾��� �� OFF
        }
        else
        {
            targetPos = _returnPos;
            targetSize = _returnSize;

            SetEnabled(disableWhileMap, true);  // ����
        }

        // ������ ��ȯ
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
                if (!on) _prevEnable[i] = beh.enabled; // ����
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
