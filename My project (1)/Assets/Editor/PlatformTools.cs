#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class PlatformTools
{
    const float DefaultDistance = 4f;   // 왕복 거리 (원하는 값으로 바꿔도 됨)
    const float DefaultSpeed    = 2.5f; // 기본 속도
    const string PathSuffix     = "_Path";

    // ───────── 메뉴: 수평 이동 ─────────
    [MenuItem("Tools/Platforms/Make Horizontal Platforms")]
    public static void MakeHorizontalPlatforms()
    {
        MakePlatforms(new Vector3(+DefaultDistance, 0f, 0f));
    }

    // ───────── 메뉴: 수직 이동 (신규) ─────────
    [MenuItem("Tools/Platforms/Make Vertical Platforms")]
    public static void MakeVerticalPlatforms()
    {
        MakePlatforms(new Vector3(0f, +DefaultDistance, 0f));
    }

    // 선택 없으면 비활성 (두 메뉴 공통)
    [MenuItem("Tools/Platforms/Make Horizontal Platforms", true)]
    [MenuItem("Tools/Platforms/Make Vertical Platforms",   true)]
    public static bool ValidateMake() =>
        Selection.gameObjects != null && Selection.gameObjects.Length > 0;

    // 공통 구현
    static void MakePlatforms(Vector3 delta)
    {
        foreach (var go in Selection.gameObjects)
        {
            Undo.RegisterFullObjectHierarchyUndo(go, "Make Moving Platform");

            // Collider2D: 없으면 BoxCollider2D 추가, 비트리거
            var col = go.GetComponent<Collider2D>();
            if (!col) col = Undo.AddComponent<BoxCollider2D>(go);
            col.isTrigger = false;

            // Ground 레이어 권장
            int groundLayer = LayerMask.NameToLayer("Ground");
            if (groundLayer >= 0) go.layer = groundLayer;

            // Rigidbody2D: Kinematic
            var rb = go.GetComponent<Rigidbody2D>();
            if (!rb) rb = Undo.AddComponent<Rigidbody2D>(go);
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;

            // Path 컨테이너 찾기/만들기 (형제로 위치)
            Transform path = FindOrCreatePathContainer(go);

            // 기존 WP_ 자식 정리(이 컨테이너 안에서만)
            for (int i = path.childCount - 1; i >= 0; --i)
            {
                var t = path.GetChild(i);
                if (t.name.StartsWith("WP_"))
                    Undo.DestroyObjectImmediate(t.gameObject);
            }

            // MovingPlatform2D 추가/설정
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

            // 웨이포인트 2개 생성: A=현재, B=delta 만큼 이동
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

        // 형제 중에 기존 컨테이너가 있으면 재사용
        Transform existing = null;
        if (parent != null)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                var t = parent.GetChild(i);
                if (t && t.name == pathName) { existing = t; break; }
            }
        }

        // 없으면 새로 만들기
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
