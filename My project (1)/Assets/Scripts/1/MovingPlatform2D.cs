using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 웨이포인트 사이를 왕복하는 2D 이동 플랫폼.
/// - Collider2D 는 "비트리거=false" 여야 플레이어가 밟을 수 있음.
/// - Rigidbody2D가 있으면 Kinematic + 물리 프레임 기반 이동(권장)
/// - 웨이포인트는 월드 좌표로 캐시
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class MovingPlatform2D : MonoBehaviour
{
    [Header("Path")]
    public List<Transform> waypoints = new List<Transform>(); // 2개 이상
    public int startIndex = 0;
    public bool pingPong = true;        // 끝에서 되돌아오기(왕복). false면 루프

    [Header("Motion")]
    [Tooltip("초당 이동 속도(m/s)")]
    public float speed = 2f;
    [Tooltip("각 포인트에서 잠깐 정지 시간(초)")]
    public float pauseAtPoints = 0f;

    [Header("Physics")]
    public Rigidbody2D rb;              // 있으면 물리 프레임으로 MovePosition
    public bool forceKinematic = true;

    [Header("Passengers")]
    public string playerTag = "Player"; // 플레이어 태그
    public bool parentPassenger = true; // 위에 올라오면 부모로 붙이기 (※ PlayerController가 '델타로 태우기'를 쓰면 끄세요)

    // --- 내부 상태
    readonly List<Vector3> cachedWorldPoints = new List<Vector3>();
    int currentIndex;
    int dir = 1; // +1 정방향, -1 역방향
    Coroutine runner;

    const float EPS = 0.00001f;

    void Reset()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = false;

        // 흔히 Ground 레이어 사용
        try { gameObject.layer = LayerMask.NameToLayer("Ground"); } catch { }

        var r = GetComponent<Rigidbody2D>();
        if (!r) r = gameObject.AddComponent<Rigidbody2D>();
        r.bodyType = RigidbodyType2D.Kinematic;
        r.gravityScale = 0f;
        r.constraints = RigidbodyConstraints2D.FreezeRotation;
    }

    void Awake()
    {
        if (!rb) rb = GetComponent<Rigidbody2D>();
        if (rb && forceKinematic)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate; // 렌더 보간
        }
    }

    void OnEnable()
    {
        // 비어 있으면 자식들을 자동으로 채움(편의)
        if (waypoints == null || waypoints.Count < 2)
        {
            waypoints = new List<Transform>();
            foreach (Transform child in transform) waypoints.Add(child);
        }
        if (waypoints.Count < 2) return;

        CacheWorldPoints();

        currentIndex = Mathf.Clamp(startIndex, 0, cachedWorldPoints.Count - 1);
        dir = (pingPong && currentIndex == cachedWorldPoints.Count - 1) ? -1 : 1;

        // 시작 위치 스냅
        SnapPosition(cachedWorldPoints[currentIndex]);

        // ★ Rigidbody 유무에 따라 다른 루틴
        runner = StartCoroutine(rb ? MoveRoutineRB() : MoveRoutineTransform());
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

    // ---------- Transform 기반(비물리) ----------
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
                Vector3 next = Vector3.MoveTowards(cur, target, speed * Time.deltaTime);
                transform.position = next;
                yield return null; // 렌더 프레임
            }

            currentIndex = nextIndex;
            UpdateDirAfterArrive();

            if (pauseAtPoints > 0f) yield return new WaitForSeconds(pauseAtPoints);
        }
    }

    // ---------- Rigidbody2D 기반(물리 프레임) ----------
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
                Vector2 next = Vector2.MoveTowards(cur, (Vector2)target, speed * Time.fixedDeltaTime);
                rb.MovePosition(next);
                yield return new WaitForFixedUpdate(); // ★ 물리 프레임
            }

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
        if (rb) rb.position = p; // 시작 스냅은 MovePosition 대신 직접 배치
        else transform.position = p;
    }

    // ---- 승객 태우기(플레이어만) ----
    void OnCollisionEnter2D(Collision2D c)
    {
        if (!parentPassenger) return;
        if (!c.collider || !c.collider.CompareTag(playerTag)) return;

        // 위에서 밟는 중이면 붙여주기
        var other = c.collider;
        if (other.bounds.min.y >= GetComponent<Collider2D>().bounds.max.y - 0.02f)
        {
            other.transform.SetParent(transform, true);
        }
    }

    void OnCollisionExit2D(Collision2D c)
    {
        if (!parentPassenger) return;
        if (!c.collider || !c.collider.CompareTag(playerTag)) return;

        if (c.collider.transform.parent == transform)
            c.collider.transform.SetParent(null, true);
    }

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
