#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// 선택한 오브젝트에 WaypointHazard2D를 붙이고
/// 수평/수직 왕복용 웨이포인트(WP_A/WP_B)를 자동 생성하는 에디터 유틸.
/// - 선택된 오브젝트에만 적용됨
/// - 콜라이더/리짓바디가 없으면 자동 추가 (Collider2D.isTrigger = true, Rigidbody2D = Kinematic)
/// - 태그 "Obstacle" 설정 시도(없으면 무시)
/// </summary>
public static class WaypointHazardTools
{
    // ------------------------------------------------------------------
    // 메뉴 항목
    // ------------------------------------------------------------------

    // 수평 왕복 (단축키: Cmd/Ctrl + Shift + H)
    [MenuItem("Tools/Obstacles/Make Horizontal Movers %#h")]
    public static void MakeHorizontal() => MakeMovers(new Vector3(2f, 0f, 0f));

    // 수직 왕복 (단축키: Cmd/Ctrl + Shift + V)
    [MenuItem("Tools/Obstacles/Make Vertical Movers %#v")]
    public static void MakeVertical() => MakeMovers(new Vector3(0f, 2f, 0f));

    // 선택 없으면 비활성화
    [MenuItem("Tools/Obstacles/Make Horizontal Movers %#h", true)]
    [MenuItem("Tools/Obstacles/Make Vertical Movers %#v", true)]
    public static bool ValidateMake() => Selection.gameObjects != null && Selection.gameObjects.Length > 0;

    // 붙였던 이동 컴포넌트 제거(웨이포인트 자식도 같이 정리)
    [MenuItem("Tools/Obstacles/Remove Movers")]
    public static void RemoveMovers()
    {
        foreach (var go in Selection.gameObjects)
        {
            var mover = go.GetComponent<WaypointHazard2D>();
            if (mover) Undo.DestroyObjectImmediate(mover);

            // 자동 생성했던 웨이포인트 정리(WP_ 로 시작하는 자식)
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
    // 내부 로직
    // ------------------------------------------------------------------

    static void MakeMovers(Vector3 delta)
    {
        var selected = Selection.gameObjects;
        if (selected == null || selected.Length == 0) return;

        foreach (var go in selected)
        {
            Undo.RegisterFullObjectHierarchyUndo(go, "Make Waypoint Hazard");

            // Collider2D (없으면 BoxCollider2D 추가)
            var col = go.GetComponent<Collider2D>();
            if (!col) col = Undo.AddComponent<BoxCollider2D>(go);
            col.isTrigger = true;

            // 태그 설정 (프로젝트에 "Obstacle"이 없으면 무시)
            try { go.tag = "Obstacle"; } catch { /* ignore */ }

            // Rigidbody2D (Kinematic & FreezeRotation)
            var rb = go.GetComponent<Rigidbody2D>();
            if (!rb) rb = Undo.AddComponent<Rigidbody2D>(go);
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;

            // 기존 WP_ 자식이 있으면 지우고 새로 만든다
            var children = go.GetComponentsInChildren<Transform>(true);
            foreach (var t in children)
            {
                if (t == go.transform) continue;
                if (t.name.StartsWith("WP_"))
                    Undo.DestroyObjectImmediate(t.gameObject);
            }

            // WaypointHazard2D 추가/설정
            var mover = go.GetComponent<WaypointHazard2D>();
            if (!mover) mover = Undo.AddComponent<WaypointHazard2D>(go);

            mover.waypoints.Clear();
            mover.startIndex = 0;
            mover.pingPong = true;          // 왕복
            mover.speed = 2f;               // 기본 속도(원하면 바꿔도 됨)
            mover.pauseAtPoints = 0.0f;     // 끝에서 멈춤 시간(원하면 0.2f 추천)
            mover.rb = rb;
            mover.setKinematicIfRB = true;

            // 웨이포인트 두 개 생성: A=현재 위치, B=delta 만큼 떨어진 곳
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
