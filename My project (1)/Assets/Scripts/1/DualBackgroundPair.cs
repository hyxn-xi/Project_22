using UnityEngine;

/// <summary>
/// ��� 2���� �Բ� ����(��ο�/����).
/// - �� �� ���� Sorting Layer(��: "Background")�� ����
/// - baseOrder �������� ��/�ڸ� �ٲ㼭 ǥ���ϰų�(SetActive ��� ���ķ� ��ü)
/// - �ʿ� �� SetActive ��� ��� ����.
/// </summary>
[DisallowMultipleComponent]
public class DualBackgroundPair : MonoBehaviour
{
    [Header("Refs")]
    public SpriteRenderer dark;     // ��ο� ���
    public SpriteRenderer bright;   // ���� ���

    [Header("Sorting")]
    public string sortingLayer = "Background";
    public int baseOrder = -100;        // ����(��ֹ�=2)���� ����� ����
    public bool useSetActive = false;   // true�� SetActive�� ON/OFF, false�� Sorting Order�� ��ü

    void Reset()
    {
        // �ڵ����� �ڽĿ��� ã��ä��� �õ�
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
            Debug.LogWarning("[DualBackgroundPair] SpriteRenderer�� ������ϴ�.", this);
            return;
        }

        // ���̾� ����ȭ
        if (!string.IsNullOrEmpty(sortingLayer))
        {
            dark.sortingLayerName = sortingLayer;
            bright.sortingLayerName = sortingLayer;
        }

        // ������ '��ο� ����� ��' ���·�
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
            dark.sortingOrder = baseOrder;       // ��
            bright.sortingOrder = baseOrder - 1;   // ��
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
            bright.sortingOrder = baseOrder;       // ��
            dark.sortingOrder = baseOrder - 1;   // ��
        }
    }

    /// <summary>Ŭ���� ���� ȣ���� ���� ������� ��ȯ</summary>
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
