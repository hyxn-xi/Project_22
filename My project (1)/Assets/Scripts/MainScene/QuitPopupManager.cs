using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class QuitPopupManager : MonoBehaviour
{
    [Header("UI Panels")]
    [SerializeField] private GameObject quitPopupPanel;    // Ȯ�� �˾� Panel
    [SerializeField] private GameObject backgroundOverlay; // �� ��ο� ��� Overlay

    private void Start()
    {
        // ���� �ÿ��� �˾��� �������� ��� ��Ȱ��ȭ
        if (quitPopupPanel != null) quitPopupPanel.SetActive(false);
        if (backgroundOverlay != null) backgroundOverlay.SetActive(false);
    }

    // ���� ȭ���� '���� ����' ��ư OnClick() �� ����
    public void OpenQuitPopup()
    {
        if (quitPopupPanel != null) quitPopupPanel.SetActive(true);
        if (backgroundOverlay != null) backgroundOverlay.SetActive(true);
    }

    // �˾����� '�ƴϿ�' ��ư OnClick() �� ����
    public void CancelQuit()
    {
        if (quitPopupPanel != null) quitPopupPanel.SetActive(false);
        if (backgroundOverlay != null) backgroundOverlay.SetActive(false);
    }

    // �˾����� '��' ��ư OnClick() �� ����
    public void ConfirmQuit()
    {
        // ����� ������ ��
        Application.Quit();

        // �����Ϳ����� �÷��� ��带 ���߱�
        #if UNITY_EDITOR
        EditorApplication.isPlaying = false;
        #endif
    }
}