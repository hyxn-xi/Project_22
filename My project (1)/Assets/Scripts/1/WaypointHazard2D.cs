using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class WaypointHazard2D : MonoBehaviour
{
    [Header("Path")]
    public List<Transform> waypoints = new List<Transform>();
    public int startIndex = 0;
    public bool pingPong = true;

    [Header("Motion")]
    [Tooltip("초당 이동 속도(m/s)")]
    public float speed = 2.0f;
    public float pauseAtPoints = 0.0f;

    [Header("Physics (optional)")]
    public Rigidbody2D rb;                  // 있으면 물리 루프 사용
    public bool setKinematicIfRB = true;

    public enum FlipAxis { AutoFromMovement, ForceX, ForceY, None }

    [Header("Auto Flip (visual sprite)")]
    public Transform visual;
    public FlipAxis flipAxis = FlipAxis.AutoFromMovement;
    public bool invertFlip = false;

    // 내부 상태
    Coroutine runner;
    int currentIndex;
    int dir = 1;
    readonly List<Vector3> cachedWorldPoints = new List<Vector3>();
    SpriteRenderer sr;

    void Reset()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
        if (tag == "Untagged") tag = "Obstacle";
    }

    void Awake()
    {
        if (!rb) rb = GetComponent<Rigidbody2D>();
        if (rb && setKinematicIfRB)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate; // 렌더 보간
        }

        if (!visual) visual = transform;
        sr = visual.GetComponent<SpriteRenderer>();
    }

    void OnEnable()
    {
        // 웨이포인트 준비
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
        SetPosition(cachedWorldPoints[currentIndex]);

        // ★ 물리용/비물리용 코루틴을 분리
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

    // ---------- 비물리(Transform) 경로 ----------
    IEnumerator MoveRoutineTransform()
    {
        if (speed <= 0.0001f) yield break;

        while (true)
        {
            int nextIndex = GetNextIndex();
            Vector3 target = cachedWorldPoints[nextIndex];

            while ((transform.position - target).sqrMagnitude > 0.00001f)
            {
                var cur = transform.position;
                ApplyFlipTowards(cur, target);

                Vector3 next = Vector3.MoveTowards(cur, target, speed * Time.deltaTime);
                transform.position = next;
                yield return null; // 렌더 프레임
            }

            currentIndex = nextIndex;
            UpdateDirAfterArrive();

            if (pauseAtPoints > 0f) yield return new WaitForSeconds(pauseAtPoints);
        }
    }

    // ---------- 물리(Rigidbody2D) 경로 ----------
    IEnumerator MoveRoutineRB()
    {
        if (speed <= 0.0001f) yield break;

        while (true)
        {
            int nextIndex = GetNextIndex();
            Vector3 target = cachedWorldPoints[nextIndex];

            while (((Vector2)rb.position - (Vector2)target).sqrMagnitude > 0.00001f)
            {
                var cur = (Vector2)rb.position;
                ApplyFlipTowards(cur, target);

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

    void SetPosition(Vector3 p)
    {
        if (rb) rb.MovePosition(p);
        else transform.position = p;
    }

    // === 이동 방향 기반 자동 Flip ===
    void ApplyFlipTowards(Vector3 currentWorldPos, Vector3 targetWorldPos)
    {
        if (!visual) return;

        Vector3 toTargetLocal = visual.InverseTransformDirection(targetWorldPos - currentWorldPos);

        bool useX;
        bool flipOn;

        switch (flipAxis)
        {
            case FlipAxis.ForceX:
                useX = true;
                flipOn = toTargetLocal.x < 0f;
                break;
            case FlipAxis.ForceY:
                useX = false;
                flipOn = toTargetLocal.y < 0f;
                break;
            case FlipAxis.None:
                return;
            default: // AutoFromMovement
                if (Mathf.Abs(toTargetLocal.x) >= Mathf.Abs(toTargetLocal.y))
                {
                    useX = true;
                    flipOn = toTargetLocal.x < 0f;
                }
                else
                {
                    useX = false;
                    flipOn = toTargetLocal.y < 0f;
                }
                break;
        }

        if (invertFlip) flipOn = !flipOn;

        if (sr)
        {
            if (useX) sr.flipX = flipOn;
            else sr.flipY = flipOn;
        }
        else
        {
            var s = visual.localScale;
            if (useX) s.x = Mathf.Abs(s.x) * (flipOn ? -1f : 1f);
            else s.y = Mathf.Abs(s.y) * (flipOn ? -1f : 1f);
            visual.localScale = s;
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        var pts = new List<Transform>();
        if (waypoints != null && waypoints.Count >= 2) pts.AddRange(waypoints);
        else foreach (Transform child in transform) pts.Add(child);
        if (pts.Count < 2) return;

        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.9f);
        for (int i = 0; i < pts.Count - 1; i++)
        {
            if (!pts[i] || !pts[i + 1]) continue;
            Gizmos.DrawLine(pts[i].position, pts[i + 1].position);
            Gizmos.DrawWireSphere(pts[i].position, 0.08f);
        }
        Gizmos.DrawWireSphere(pts[pts.Count - 1].position, 0.08f);
    }
#endif
}
