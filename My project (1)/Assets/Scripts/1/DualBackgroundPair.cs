using UnityEngine;

/// <summary>
/// 배경 2장을 함께 관리(어두운/밝은).
/// - 둘 다 같은 Sorting Layer(예: "Background")를 쓰고
/// - baseOrder 기준으로 앞/뒤만 바꿔서 표시하거나(SetActive 대신 정렬로 교체)
/// - 필요 시 SetActive 토글 사용 가능.
/// </summary>
[DisallowMultipleComponent]
public class DualBackgroundPair : MonoBehaviour
{
    [Header("Refs")]
    public SpriteRenderer dark;     // 어두운 배경
    public SpriteRenderer bright;   // 밝은 배경

    [Header("Sorting")]
    public string sortingLayer = "Background";
    public int baseOrder = -100;        // 월드(장애물=2)보다 충분히 낮게
    public bool useSetActive = false;   // true면 SetActive로 ON/OFF, false면 Sorting Order로 교체

    void Reset()
    {
        // 자동으로 자식에서 찾아채우기 시도
        var srs = GetComponentsInChildren<SpriteRenderer>(true);
        if (srs.Length >= 2)
        {
            dark = srs[0];
            bright = srs[1];
        }
    }

    void Awake()
    {
        if (!dark || !bright)
        {
            Debug.LogWarning("[DualBackgroundPair] SpriteRenderer가 비었습니다.", this);
            return;
        }

        // 레이어 정규화
        if (!string.IsNullOrEmpty(sortingLayer))
        {
            dark.sortingLayerName = sortingLayer;
            bright.sortingLayerName = sortingLayer;
        }

        // 시작은 '어두운 배경이 앞' 상태로
        ShowDarkNow();
    }

    public void ShowDarkNow()
    {
        if (!dark || !bright) return;

        if (useSetActive)
        {
            dark.gameObject.SetActive(true);
            bright.gameObject.SetActive(false);
        }
        else
        {
            dark.sortingOrder = baseOrder;       // 앞
            bright.sortingOrder = baseOrder - 1;   // 뒤
        }
    }

    public void ShowBrightNow()
    {
        if (!dark || !bright) return;

        if (useSetActive)
        {
            dark.gameObject.SetActive(false);
            bright.gameObject.SetActive(true);
        }
        else
        {
            bright.sortingOrder = baseOrder;       // 앞
            dark.sortingOrder = baseOrder - 1;   // 뒤
        }
    }

    /// <summary>클리어 직후 호출해 밝은 배경으로 전환</summary>
    public void SwitchToBright()
    {
        ShowBrightNow();
    }

#if UNITY_EDITOR
    [ContextMenu("TEST/Show Dark")]
    void _TestDark() => ShowDarkNow();

    [ContextMenu("TEST/Show Bright")]
    void _TestBright() => ShowBrightNow();
#endif
}
