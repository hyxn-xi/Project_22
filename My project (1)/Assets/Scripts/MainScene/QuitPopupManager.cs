using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class QuitPopupManager : MonoBehaviour
{
    [Header("UI Panels")]
    [SerializeField] private GameObject quitPopupPanel;    // 확인 팝업 Panel
    [SerializeField] private GameObject backgroundOverlay; // 뒤 어두운 배경 Overlay

    private void Start()
    {
        // 시작 시에는 팝업과 오버레이 모두 비활성화
        if (quitPopupPanel != null) quitPopupPanel.SetActive(false);
        if (backgroundOverlay != null) backgroundOverlay.SetActive(false);
    }

    // 메인 화면의 '게임 종료' 버튼 OnClick() 에 연결
    public void OpenQuitPopup()
    {
        if (quitPopupPanel != null) quitPopupPanel.SetActive(true);
        if (backgroundOverlay != null) backgroundOverlay.SetActive(true);
    }

    // 팝업에서 '아니오' 버튼 OnClick() 에 연결
    public void CancelQuit()
    {
        if (quitPopupPanel != null) quitPopupPanel.SetActive(false);
        if (backgroundOverlay != null) backgroundOverlay.SetActive(false);
    }

    // 팝업에서 '예' 버튼 OnClick() 에 연결
    public void ConfirmQuit()
    {
        // 빌드된 게임일 때
        Application.Quit();

        // 에디터에서는 플레이 모드를 멈추기
        #if UNITY_EDITOR
        EditorApplication.isPlaying = false;
        #endif
    }
}