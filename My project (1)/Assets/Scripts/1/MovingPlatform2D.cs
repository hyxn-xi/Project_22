using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 웨이포인트를 이동하는 2D 플랫폼.
/// - Collider2D는 "Is Trigger=false"로 설정되어 플레이어가 밟을 수 있음.
/// - Rigidbody2D를 사용하여 Kinematic + 부드러운 이동(MovePosition) 처리
/// - 웨이포인트의 월드 좌표를 캐싱
/// </summary>
[RequireComponent(typeof(Collider2D))]
[DisallowMultipleComponent]
public class MovingPlatform2D : MonoBehaviour
{
    [Header("Path")]
    public List<Transform> waypoints = new List<Transform>(); // 2개 이상
    public int startIndex = 0;
    public bool pingPong = true;        // 왕복으로 되돌아옴. false면 순환

    [Header("Motion")]
    [Tooltip("초당 이동 속도(m/s)")]
    public float speed = 2f;
    [Tooltip("각 웨이포인트에 도착 후 멈추는 시간(초)")]
    public float pauseAtPoints = 0f;

    [Header("Physics")]
    public Rigidbody2D rb;              // 부드러운 이동을 위해 MovePosition 사용
    public bool forceKinematic = true;

    [Header("Passengers")]
    public string playerTag = "Player"; // 플레이어 태그 (정보성으로 유지)
    public bool parentPassenger = true; 

    // --- 내부 상태
    readonly List<Vector3> cachedWorldPoints = new List<Vector3>();
    int currentIndex;
    int dir = 1; // +1 정방향, -1 역방향
    Coroutine runner;

    const float EPS = 0.00004f; // 근접 체크 오차

    void Reset()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = false;

        // 기본 Ground 레이어 설정
        try { gameObject.layer = LayerMask.NameToLayer("Ground"); } catch { }

        var r = GetComponent<Rigidbody2D>();
        if (!r) r = gameObject.AddComponent<Rigidbody2D>();
        r.bodyType = RigidbodyType2D.Kinematic;
        r.gravityScale = 0f;
        r.constraints = RigidbodyConstraints2D.FreezeRotation;
        r.interpolation = RigidbodyInterpolation2D.Interpolate; // 렌더링 부드럽게
        rb = r;
    }

    void Awake()
    {
        if (!rb) rb = GetComponent<Rigidbody2D>();
        if (rb && forceKinematic)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.useFullKinematicContacts = true; // 접촉 안정화
        }
    }

    void OnEnable()
    {
        // 웨이포인트 자식들을 자동으로 채움(폴백)
        if (waypoints == null || waypoints.Count < 2)
        {
            waypoints = new List<Transform>();
            foreach (Transform child in transform) waypoints.Add(child);
        }
        if (waypoints.Count < 2) return;

        CacheWorldPoints();

        currentIndex = Mathf.Clamp(startIndex, 0, cachedWorldPoints.Count - 1);
        dir = (pingPong && currentIndex == cachedWorldPoints.Count - 1) ? -1 : 1;

        // 초기 위치 설정
        SnapPosition(cachedWorldPoints[currentIndex]);

        // ★★★ Rigidbody 방식 (MoveRoutineRB())으로 복구하여 물리 안정성을 높입니다.
        runner = StartCoroutine(MoveRoutineRB()); 
    }

    void OnDisable()
    {
        if (runner != null) StopCoroutine(runner);
    }

    void CacheWorldPoints()
    {
        cachedWorldPoints.Clear();
        foreach (var t in waypoints)
            if (t) cachedWorldPoints.Add(t.position);
    }

    // ---------- Transform 방식 (Update 프레임에서 움직임) ----------
    IEnumerator MoveRoutineTransform()
    {
        if (speed <= 0.0001f) yield break;

        while (true)
        {
            int nextIndex = GetNextIndex();
            Vector3 target = cachedWorldPoints[nextIndex];

            while ((transform.position - target).sqrMagnitude > EPS)
            {
                Vector3 cur = transform.position;
                float dist = Vector3.Distance(cur, target);
                float step = Mathf.Min(speed * Time.deltaTime, dist);
                Vector3 next = Vector3.MoveTowards(cur, target, step);
                transform.position = next; 
                yield return null; // Update 프레임 대기
            }

            // 정확한 위치 설정
            transform.position = target;

            currentIndex = nextIndex;
            UpdateDirAfterArrive();

            if (pauseAtPoints > 0f) yield return new WaitForSeconds(pauseAtPoints);
        }
    }

    // ---------- Rigidbody2D 방식 (FixedUpdate 프레임에서 움직임 - 안정적인 물리 이동) ----------
    IEnumerator MoveRoutineRB()
    {
        if (speed <= 0.0001f) yield break;

        while (true)
        {
            int nextIndex = GetNextIndex();
            Vector3 target = cachedWorldPoints[nextIndex];

            while (((Vector2)rb.position - (Vector2)target).sqrMagnitude > EPS)
            {
                Vector2 cur = rb.position;
                Vector2 to = (Vector2)target - cur;
                float dist = to.magnitude;
                if (dist <= EPS) break;

                float step = Mathf.Min(speed * Time.fixedDeltaTime, dist);
                Vector2 next = cur + to / dist * step;

                rb.MovePosition(next);
                yield return new WaitForFixedUpdate(); // 다음 물리 프레임 대기 
            }

            // 정확한 위치 설정
            rb.position = target;

            currentIndex = nextIndex;
            UpdateDirAfterArrive();

            if (pauseAtPoints > 0f) yield return new WaitForSeconds(pauseAtPoints);
        }
    }

    int GetNextIndex()
    {
        if (pingPong)
            return Mathf.Clamp(currentIndex + dir, 0, cachedWorldPoints.Count - 1);
        else
            return (currentIndex + 1) % cachedWorldPoints.Count;
    }

    void UpdateDirAfterArrive()
    {
        if (pingPong)
        {
            if (currentIndex == cachedWorldPoints.Count - 1) dir = -1;
            else if (currentIndex == 0) dir = 1;
        }
    }

    void SnapPosition(Vector3 p)
    {
        if (rb) rb.position = p; 
        else transform.position = p; 
    }

    // ---- 승객 부모-자식 관계 처리 로직은 PlayerController.cs에서 담당하므로 제거합니다. ----
    /*
    void OnCollisionEnter2D(Collision2D c) { ... }
    void OnCollisionExit2D(Collision2D c) { ... }
    */

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        var pts = new List<Transform>();
        if (waypoints != null && waypoints.Count >= 2) pts.AddRange(waypoints);
        else foreach (Transform child in transform) pts.Add(child);

        if (pts.Count < 2) return;

        Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.95f);
        for (int i = 0; i < pts.Count - 1; i++)
        {
            if (!pts[i] || !pts[i + 1]) continue;
            Gizmos.DrawLine(pts[i].position, pts[i + 1].position);
            Gizmos.DrawWireCube(pts[i].position, Vector3.one * 0.08f);
        }
        Gizmos.DrawWireCube(pts[pts.Count - 1].position, Vector3.one * 0.08f);
    }
#endif
}