using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ��������Ʈ ���̸� �պ��ϴ� 2D �̵� �÷���.
/// - Collider2D �� "��Ʈ����=false" ���� �÷��̾ ���� �� ����.
/// - Rigidbody2D�� ������ Kinematic + ���� ������ ��� �̵�(����)
/// - ��������Ʈ�� ���� ��ǥ�� ĳ��
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class MovingPlatform2D : MonoBehaviour
{
    [Header("Path")]
    public List<Transform> waypoints = new List<Transform>(); // 2�� �̻�
    public int startIndex = 0;
    public bool pingPong = true;        // ������ �ǵ��ƿ���(�պ�). false�� ����

    [Header("Motion")]
    [Tooltip("�ʴ� �̵� �ӵ�(m/s)")]
    public float speed = 2f;
    [Tooltip("�� ����Ʈ���� ��� ���� �ð�(��)")]
    public float pauseAtPoints = 0f;

    [Header("Physics")]
    public Rigidbody2D rb;              // ������ ���� ���������� MovePosition
    public bool forceKinematic = true;

    [Header("Passengers")]
    public string playerTag = "Player"; // �÷��̾� �±�
    public bool parentPassenger = true; // ���� �ö���� �θ�� ���̱� (�� PlayerController�� '��Ÿ�� �¿��'�� ���� ������)

    // --- ���� ����
    readonly List<Vector3> cachedWorldPoints = new List<Vector3>();
    int currentIndex;
    int dir = 1; // +1 ������, -1 ������
    Coroutine runner;

    const float EPS = 0.00001f;

    void Reset()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = false;

        // ���� Ground ���̾� ���
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
            rb.interpolation = RigidbodyInterpolation2D.Interpolate; // ���� ����
        }
    }

    void OnEnable()
    {
        // ��� ������ �ڽĵ��� �ڵ����� ä��(����)
        if (waypoints == null || waypoints.Count < 2)
        {
            waypoints = new List<Transform>();
            foreach (Transform child in transform) waypoints.Add(child);
        }
        if (waypoints.Count < 2) return;

        CacheWorldPoints();

        currentIndex = Mathf.Clamp(startIndex, 0, cachedWorldPoints.Count - 1);
        dir = (pingPong && currentIndex == cachedWorldPoints.Count - 1) ? -1 : 1;

        // ���� ��ġ ����
        SnapPosition(cachedWorldPoints[currentIndex]);

        // �� Rigidbody ������ ���� �ٸ� ��ƾ
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

    // ---------- Transform ���(�񹰸�) ----------
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
                yield return null; // ���� ������
            }

            currentIndex = nextIndex;
            UpdateDirAfterArrive();

            if (pauseAtPoints > 0f) yield return new WaitForSeconds(pauseAtPoints);
        }
    }

    // ---------- Rigidbody2D ���(���� ������) ----------
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
                yield return new WaitForFixedUpdate(); // �� ���� ������
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
        if (rb) rb.position = p; // ���� ������ MovePosition ��� ���� ��ġ
        else transform.position = p;
    }

    // ---- �°� �¿��(�÷��̾) ----
    void OnCollisionEnter2D(Collision2D c)
    {
        if (!parentPassenger) return;
        if (!c.collider || !c.collider.CompareTag(playerTag)) return;

        // ������ ��� ���̸� �ٿ��ֱ�
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
