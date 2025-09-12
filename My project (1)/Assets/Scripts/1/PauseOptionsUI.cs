using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class PauseOptionsUI : MonoBehaviour
{
    [Header("Panels")]
    public GameObject optionsPanel;          // 옵션 메인 패널 (비활성 시작)
    public GameObject controlsPanel;         // 조작법 패널 (비활성 시작)
    public GameObject dimOverlay;            // 어두운 배경(있으면)

    [Header("Focus (선택)")]
    public GameObject optionsFirstSelected;  // 옵션 패널 열리면 포커스 줄 버튼
    public GameObject controlsFirstSelected; // 조작법 패널 열리면 포커스 줄 버튼

    [Header("Keys")]
    public KeyCode toggleKey = KeyCode.Escape; // ESC로 열고/닫기

    [Header("Scenes")]
    public string retrySceneName = "";          // 비우면 현재 씬 다시 로드
    public string mainMenuSceneName = "MainScreenScene";

    bool isOpen = false;
    float prevTimeScale = 1f;

    void Start()
    {
        if (optionsPanel) optionsPanel.SetActive(false);
        if (controlsPanel) controlsPanel.SetActive(false);
        if (dimOverlay) dimOverlay.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            if (!isOpen) OpenOptions();
            else
            {
                // 조작법 패널이 떠 있으면 옵션으로 돌아가기, 아니면 닫기
                if (controlsPanel && controlsPanel.activeSelf) ShowOptionsPanel();
                else CloseOptions();
            }
        }
    }

    // ====== 외부(옵션 버튼 OnClick)에 연결 ======
    public void OpenOptions()
    {
        if (isOpen) return;
        isOpen = true;

        prevTimeScale = Time.timeScale;
        Time.timeScale = 0f; // 일시정지

        if (dimOverlay) dimOverlay.SetActive(true);
        ShowOptionsPanel();
    }

    public void CloseOptions()
    {
        if (!isOpen) return;
        isOpen = false;

        if (optionsPanel) optionsPanel.SetActive(false);
        if (controlsPanel) controlsPanel.SetActive(false);
        if (dimOverlay) dimOverlay.SetActive(false);

        Time.timeScale = prevTimeScale;

        if (EventSystem.current) EventSystem.current.SetSelectedGameObject(null);
    }

    // ====== 버튼 핸들러 ======
    public void OnClickControls() => ShowControlsPanel();
    public void OnClickBackFromControls() => ShowOptionsPanel();

    public void OnClickRetry()
    {
        Time.timeScale = 1f;
        string scene = string.IsNullOrEmpty(retrySceneName)
            ? SceneManager.GetActiveScene().name
            : retrySceneName;
        TrySceneTransition(scene);
    }

    public void OnClickMainMenu()
    {
        Time.timeScale = 1f;
        if (!string.IsNullOrEmpty(mainMenuSceneName))
            TrySceneTransition(mainMenuSceneName);
    }

    // ====== 내부 전환 ======
    void ShowOptionsPanel()
    {
        if (controlsPanel) controlsPanel.SetActive(false);
        if (optionsPanel) optionsPanel.SetActive(true);

        if (EventSystem.current && optionsFirstSelected)
            EventSystem.current.SetSelectedGameObject(optionsFirstSelected);
    }

    void ShowControlsPanel()
    {
        if (optionsPanel) optionsPanel.SetActive(false);
        if (controlsPanel) controlsPanel.SetActive(true);

        if (EventSystem.current && controlsFirstSelected)
            EventSystem.current.SetSelectedGameObject(controlsFirstSelected);
    }

    void TrySceneTransition(string sceneName)
    {
        var stm = FindObjectOfType<SceneTransitionManager>();
        if (stm != null) stm.StartSceneTransition(sceneName);
        else SceneManager.LoadScene(sceneName);
    }

    void OnDisable()
    {
        // 혹시 비활성화될 때 정지 상태 방지
        if (isOpen) Time.timeScale = prevTimeScale;
    }
}
