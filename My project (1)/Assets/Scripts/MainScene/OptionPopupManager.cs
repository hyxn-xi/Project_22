using UnityEngine;

public class OptionPopupManager : MonoBehaviour
{
    [Header("�˾� Panels")]
    [SerializeField] private GameObject optionPopupPanel;     // �ɼ�â
    [SerializeField] private GameObject backgroundOverlay;    // ��� ��Ӱ�
    [SerializeField] private GameObject controlsPopupPanel;   // ���۹� �˾�

    private void Start()
    {
        // ���� �� ��� �˾��� �������̴� ��Ȱ��ȭ
        if (optionPopupPanel != null) optionPopupPanel.SetActive(false);
        if (backgroundOverlay != null) backgroundOverlay.SetActive(false);
        if (controlsPopupPanel != null) controlsPopupPanel.SetActive(false);
    }

    // ���� �� �ɼ�â ����
    public void OpenOptionPanel()
    {
        optionPopupPanel.SetActive(true);
        backgroundOverlay.SetActive(true);
    }

    // �ɼ�â �� �ݱ�
    public void CloseOptionPanel()
    {
        optionPopupPanel.SetActive(false);
        backgroundOverlay.SetActive(false);
    }

    // �ɼ�â �� �� ���۹�â ����
    public void OpenControlsPanel()
    {
        optionPopupPanel.SetActive(false);
        controlsPopupPanel.SetActive(true);
    }

    // ���۹�â �� �ݱ�
    public void CloseControlsPanel()
    {
        controlsPopupPanel.SetActive(false);
        optionPopupPanel.SetActive(true);
    }
}
