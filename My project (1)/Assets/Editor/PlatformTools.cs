#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class PlatformTools
{
    const float DefaultDistance = 4f;   // �պ� �Ÿ� (���ϴ� ������ �ٲ㵵 ��)
    const float DefaultSpeed    = 2.5f; // �⺻ �ӵ�
    const string PathSuffix     = "_Path";

    // ������������������ �޴�: ���� �̵� ������������������
    [MenuItem("Tools/Platforms/Make Horizontal Platforms")]
    public static void MakeHorizontalPlatforms()
    {
        MakePlatforms(new Vector3(+DefaultDistance, 0f, 0f));
    }

    // ������������������ �޴�: ���� �̵� (�ű�) ������������������
    [MenuItem("Tools/Platforms/Make Vertical Platforms")]
    public static void MakeVerticalPlatforms()
    {
        MakePlatforms(new Vector3(0f, +DefaultDistance, 0f));
    }

    // ���� ������ ��Ȱ�� (�� �޴� ����)
    [MenuItem("Tools/Platforms/Make Horizontal Platforms", true)]
    [MenuItem("Tools/Platforms/Make Vertical Platforms",   true)]
    public static bool ValidateMake() =>
        Selection.gameObjects != null && Selection.gameObjects.Length > 0;

    // ���� ����
    static void MakePlatforms(Vector3 delta)
    {
        foreach (var go in Selection.gameObjects)
        {
            Undo.RegisterFullObjectHierarchyUndo(go, "Make Moving Platform");

            // Collider2D: ������ BoxCollider2D �߰�, ��Ʈ����
            var col = go.GetComponent<Collider2D>();
            if (!col) col = Undo.AddComponent<BoxCollider2D>(go);
            col.isTrigger = false;

            // Ground ���̾� ����
            int groundLayer = LayerMask.NameToLayer("Ground");
            if (groundLayer >= 0) go.layer = groundLayer;

            // Rigidbody2D: Kinematic
            var rb = go.GetComponent<Rigidbody2D>();
            if (!rb) rb = Undo.AddComponent<Rigidbody2D>(go);
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;

            // Path �����̳� ã��/����� (������ ��ġ)
            Transform path = FindOrCreatePathContainer(go);

            // ���� WP_ �ڽ� ����(�� �����̳� �ȿ�����)
            for (int i = path.childCount - 1; i >= 0; --i)
            {
                var t = path.GetChild(i);
                if (t.name.StartsWith("WP_"))
                    Undo.DestroyObjectImmediate(t.gameObject);
            }

            // MovingPlatform2D �߰�/����
            var mp = go.GetComponent<MovingPlatform2D>();
            if (!mp) mp = Undo.AddComponent<MovingPlatform2D>(go);

            mp.waypoints.Clear();
            mp.startIndex = 0;
            mp.pingPong = true;
            mp.speed = DefaultSpeed;
            mp.pauseAtPoints = 0f;
            mp.rb = rb;
            mp.playerTag = "Player";
            mp.parentPassenger = true;

            // ��������Ʈ 2�� ����: A=����, B=delta ��ŭ �̵�
            var a = new GameObject("WP_A").transform;
            var b = new GameObject("WP_B").transform;
            Undo.RegisterCreatedObjectUndo(a.gameObject, "Create WP_A");
            Undo.RegisterCreatedObjectUndo(b.gameObject, "Create WP_B");

            a.SetParent(path, worldPositionStays: false);
            b.SetParent(path, worldPositionStays: false);
            a.position = go.transform.position;
            b.position = go.transform.position + delta;

            mp.waypoints.Add(a);
            mp.waypoints.Add(b);

            EditorUtility.SetDirty(mp);
            Selection.activeGameObject = go;
        }
    }

    static Transform FindOrCreatePathContainer(GameObject go)
    {
        string pathName = go.name + PathSuffix;
        Transform parent = go.transform.parent;

        // ���� �߿� ���� �����̳ʰ� ������ ����
        Transform existing = null;
        if (parent != null)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                var t = parent.GetChild(i);
                if (t && t.name == pathName) { existing = t; break; }
            }
        }

        // ������ ���� �����
        if (existing == null)
        {
            var pathGO = new GameObject(pathName);
            Undo.RegisterCreatedObjectUndo(pathGO, "Create Path Container");
            existing = pathGO.transform;
            existing.SetParent(parent, worldPositionStays: false);
            existing.position = go.transform.position;
        }

        return existing;
    }
}
#endif
