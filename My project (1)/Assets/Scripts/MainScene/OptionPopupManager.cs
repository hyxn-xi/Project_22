using UnityEngine;

public class OptionPopupManager : MonoBehaviour
{
    [Header("팝업 Panels")]
    [SerializeField] private GameObject optionPopupPanel;     // 옵션창
    [SerializeField] private GameObject backgroundOverlay;    // 배경 어둡게
    [SerializeField] private GameObject controlsPopupPanel;   // 조작법 팝업

    private void Start()
    {
        // 시작 시 모든 팝업과 오버레이는 비활성화
        if (optionPopupPanel != null) optionPopupPanel.SetActive(false);
        if (backgroundOverlay != null) backgroundOverlay.SetActive(false);
        if (controlsPopupPanel != null) controlsPopupPanel.SetActive(false);
    }

    // 메인 → 옵션창 열기
    public void OpenOptionPanel()
    {
        optionPopupPanel.SetActive(true);
        backgroundOverlay.SetActive(true);
    }

    // 옵션창 안 닫기
    public void CloseOptionPanel()
    {
        optionPopupPanel.SetActive(false);
        backgroundOverlay.SetActive(false);
    }

    // 옵션창 안 → 조작법창 열기
    public void OpenControlsPanel()
    {
        optionPopupPanel.SetActive(false);
        controlsPopupPanel.SetActive(true);
    }

    // 조작법창 안 닫기
    public void CloseControlsPanel()
    {
        controlsPopupPanel.SetActive(false);
        optionPopupPanel.SetActive(true);
    }
}
