using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class PauseOptionsUI : MonoBehaviour
{
    [Header("Panels")]
    public GameObject optionsPanel;          // �ɼ� ���� �г� (��Ȱ�� ����)
    public GameObject controlsPanel;         // ���۹� �г� (��Ȱ�� ����)
    public GameObject dimOverlay;            // ��ο� ���(������)

    [Header("Focus (����)")]
    public GameObject optionsFirstSelected;  // �ɼ� �г� ������ ��Ŀ�� �� ��ư
    public GameObject controlsFirstSelected; // ���۹� �г� ������ ��Ŀ�� �� ��ư

    [Header("Keys")]
    public KeyCode toggleKey = KeyCode.Escape; // ESC�� ����/�ݱ�

    [Header("Scenes")]
    public string retrySceneName = "";          // ���� ���� �� �ٽ� �ε�
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
                // ���۹� �г��� �� ������ �ɼ����� ���ư���, �ƴϸ� �ݱ�
                if (controlsPanel && controlsPanel.activeSelf) ShowOptionsPanel();
                else CloseOptions();
            }
        }
    }

    // ====== �ܺ�(�ɼ� ��ư OnClick)�� ���� ======
    public void OpenOptions()
    {
        if (isOpen) return;
        isOpen = true;

        prevTimeScale = Time.timeScale;
        Time.timeScale = 0f; // �Ͻ�����

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

    // ====== ��ư �ڵ鷯 ======
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

    // ====== ���� ��ȯ ======
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
        // Ȥ�� ��Ȱ��ȭ�� �� ���� ���� ����
        if (isOpen) Time.timeScale = prevTimeScale;
    }
}
