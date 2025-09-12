#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// ������ ������Ʈ�� WaypointHazard2D�� ���̰�
/// ����/���� �պ��� ��������Ʈ(WP_A/WP_B)�� �ڵ� �����ϴ� ������ ��ƿ.
/// - ���õ� ������Ʈ���� �����
/// - �ݶ��̴�/�����ٵ� ������ �ڵ� �߰� (Collider2D.isTrigger = true, Rigidbody2D = Kinematic)
/// - �±� "Obstacle" ���� �õ�(������ ����)
/// </summary>
public static class WaypointHazardTools
{
    // ------------------------------------------------------------------
    // �޴� �׸�
    // ------------------------------------------------------------------

    // ���� �պ� (����Ű: Cmd/Ctrl + Shift + H)
    [MenuItem("Tools/Obstacles/Make Horizontal Movers %#h")]
    public static void MakeHorizontal() => MakeMovers(new Vector3(2f, 0f, 0f));

    // ���� �պ� (����Ű: Cmd/Ctrl + Shift + V)
    [MenuItem("Tools/Obstacles/Make Vertical Movers %#v")]
    public static void MakeVertical() => MakeMovers(new Vector3(0f, 2f, 0f));

    // ���� ������ ��Ȱ��ȭ
    [MenuItem("Tools/Obstacles/Make Horizontal Movers %#h", true)]
    [MenuItem("Tools/Obstacles/Make Vertical Movers %#v", true)]
    public static bool ValidateMake() => Selection.gameObjects != null && Selection.gameObjects.Length > 0;

    // �ٿ��� �̵� ������Ʈ ����(��������Ʈ �ڽĵ� ���� ����)
    [MenuItem("Tools/Obstacles/Remove Movers")]
    public static void RemoveMovers()
    {
        foreach (var go in Selection.gameObjects)
        {
            var mover = go.GetComponent<WaypointHazard2D>();
            if (mover) Undo.DestroyObjectImmediate(mover);

            // �ڵ� �����ߴ� ��������Ʈ ����(WP_ �� �����ϴ� �ڽ�)
            var children = go.GetComponentsInChildren<Transform>(true);
            foreach (var t in children)
            {
                if (t == go.transform) continue;
                if (t.name.StartsWith("WP_"))
                    Undo.DestroyObjectImmediate(t.gameObject);
            }
        }
    }

    // ------------------------------------------------------------------
    // ���� ����
    // ------------------------------------------------------------------

    static void MakeMovers(Vector3 delta)
    {
        var selected = Selection.gameObjects;
        if (selected == null || selected.Length == 0) return;

        foreach (var go in selected)
        {
            Undo.RegisterFullObjectHierarchyUndo(go, "Make Waypoint Hazard");

            // Collider2D (������ BoxCollider2D �߰�)
            var col = go.GetComponent<Collider2D>();
            if (!col) col = Undo.AddComponent<BoxCollider2D>(go);
            col.isTrigger = true;

            // �±� ���� (������Ʈ�� "Obstacle"�� ������ ����)
            try { go.tag = "Obstacle"; } catch { /* ignore */ }

            // Rigidbody2D (Kinematic & FreezeRotation)
            var rb = go.GetComponent<Rigidbody2D>();
            if (!rb) rb = Undo.AddComponent<Rigidbody2D>(go);
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;

            // ���� WP_ �ڽ��� ������ ����� ���� �����
            var children = go.GetComponentsInChildren<Transform>(true);
            foreach (var t in children)
            {
                if (t == go.transform) continue;
                if (t.name.StartsWith("WP_"))
                    Undo.DestroyObjectImmediate(t.gameObject);
            }

            // WaypointHazard2D �߰�/����
            var mover = go.GetComponent<WaypointHazard2D>();
            if (!mover) mover = Undo.AddComponent<WaypointHazard2D>(go);

            mover.waypoints.Clear();
            mover.startIndex = 0;
            mover.pingPong = true;          // �պ�
            mover.speed = 2f;               // �⺻ �ӵ�(���ϸ� �ٲ㵵 ��)
            mover.pauseAtPoints = 0.0f;     // ������ ���� �ð�(���ϸ� 0.2f ��õ)
            mover.rb = rb;
            mover.setKinematicIfRB = true;

            // ��������Ʈ �� �� ����: A=���� ��ġ, B=delta ��ŭ ������ ��
            var a = new GameObject("WP_A").transform;
            var b = new GameObject("WP_B").transform;
            Undo.RegisterCreatedObjectUndo(a.gameObject, "Create WP_A");
            Undo.RegisterCreatedObjectUndo(b.gameObject, "Create WP_B");

            a.SetParent(go.transform, true);
            b.SetParent(go.transform, true);
            a.position = go.transform.position;
            b.position = go.transform.position + delta;

            mover.waypoints.Add(a);
            mover.waypoints.Add(b);

            EditorUtility.SetDirty(mover);
        }
    }
}
#endif
