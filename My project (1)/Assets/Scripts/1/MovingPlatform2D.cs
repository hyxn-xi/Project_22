using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 웨이포인트 사이를 왕복/순환하는 2D 이동 플랫폼.
/// FixedUpdate 내에서 Transform을 직접 조작하여 Kinematic Rigidbody의 떨림을 방지합니다.
/// </summary>
[RequireComponent(typeof(Collider2D))]
[DisallowMultipleComponent]
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
    public Rigidbody2D rb;              // 충돌 감지용
    public bool forceKinematic = true;

    [Header("Passengers")]
    public string playerTag = "Player"; // 플레이어 태그
    public bool parentPassenger = true;

    // --- 내부 상태
    readonly List<Vector3> cachedWorldPoints = new List<Vector3>();
    int currentIndex;
    int dir = 1; // +1 정방향, -1 역방향
    float pauseTimer = 0f; // 정지 시간 타이머

    const float EPS = 0.00004f; // 도착 판정

    void Reset()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = false;

        try { gameObject.layer = LayerMask.NameToLayer("Ground"); } catch { }

        var r = GetComponent<Rigidbody2D>();
        if (!r) r = gameObject.AddComponent<Rigidbody2D>();
        r.bodyType = RigidbodyType2D.Kinematic;
        r.gravityScale = 0f;
        r.constraints = RigidbodyConstraints2D.FreezeRotation;
        r.interpolation = RigidbodyInterpolation2D.None;
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
            rb.interpolation = RigidbodyInterpolation2D.None;
            rb.useFullKinematicContacts = true;
            rb.simulated = true;
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

        // ★★★ StartCoroutine(MoveRoutineTransform()); 함수 호출은 FixedUpdate로 이동했으므로 삭제합니다. ★★★
    }

    void CacheWorldPoints()
    {
        cachedWorldPoints.Clear();
        foreach (var t in waypoints)
            if (t) cachedWorldPoints.Add(t.position);
    }

    // FixedUpdate 원칙을 지키며 Transform을 조작하는 안정적인 이동 방식
    void FixedUpdate()
    {
        if (speed <= 0.0001f || cachedWorldPoints.Count < 2) return;

        // 1. 정지 타이머 체크
        if (pauseTimer > 0f)
        {
            pauseTimer -= Time.fixedDeltaTime;
            return;
        }

        // 2. 이동할 목표 위치 설정
        int nextIndex = GetNextIndex();
        Vector3 target = cachedWorldPoints[nextIndex];
        Vector3 current = transform.position;

        // 3. 거리 및 이동 스텝 계산 (Time.fixedDeltaTime 사용)
        float dist = Vector3.Distance(current, target);
        float step = Mathf.Min(speed * Time.fixedDeltaTime, dist);

        // 4. Transform 위치 직접 조작 (덜덜거림 방지)
        transform.position = Vector3.MoveTowards(current, target, step);

        // 5. 도착 판정 및 다음 웨이포인트 설정
        if (dist <= EPS)
        {
            transform.position = target; // 최종 스냅
            currentIndex = nextIndex;

            // 방향 업데이트 및 정지 시간 설정
            UpdateDirAfterArrive();
            if (pauseAtPoints > 0f)
            {
                pauseTimer = pauseAtPoints;
            }
        }
    }

    int GetNextIndex()
    {
        if (pingPong)
        {
            int next = Mathf.Clamp(currentIndex + dir, 0, cachedWorldPoints.Count - 1);
            return next;
        }
        else
        {
            return (currentIndex + 1) % cachedWorldPoints.Count;
        }
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
        transform.position = p;
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