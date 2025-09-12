using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

[DisallowMultipleComponent]
public class CutsceneOrchestratorSimple : MonoBehaviour
{
    [Header("Video")]
    public VideoPlayer videoPlayer;
    public bool playOnStart = true;
    public bool pauseVideoOnDialogue = true;

    [Header("Dialogue Start")]
    [Tooltip("�ƽ� ���� �� �� �ð��� ������ ��縦 �����մϴ�(��)")]
    public float delayBeforeDialogue = 5f;

    [Header("Dialogue (���� ��ũ��Ʈ �״�� ���)")]
    public TypingDialougeSimple dialogue;   // �� �װ� �̹� ���� ������Ʈ

    [Header("Scene Transition")]
    public string nextSceneName = "";       // ����θ� Build Settings�� ���� ������

    [Header("Optional UI")]
    [Tooltip("��� ���� ������ ���� ������Ʈ(��� �г� ��Ʈ ��)")]
    public GameObject[] hideUntilDialogue;

    bool started;
    bool seenAnyDialogueUI;    // �� ���̶� ��ǳ���� ��������
    bool closingDetected;      // ��ǳ���� �ٽ� ��� ���� ����(=����)

    void Awake()
    {
        // �ƽ� ���� ���� ��� ��ũ��Ʈ�� ��� ��Ȱ��ȭ �� 5�� �ڿ� �Ѽ� ����
        if (dialogue)
        {
            dialogue.enabled = false;
            if (dialogue.girlUI) dialogue.girlUI.SetActive(false);
            if (dialogue.dadUI) dialogue.dadUI.SetActive(false);
        }

        if (hideUntilDialogue != null)
            foreach (var go in hideUntilDialogue)
                if (go) go.SetActive(false);
    }

    void Start()
    {
        if (started) return;
        started = true;

        if (videoPlayer && playOnStart && !videoPlayer.isPlaying)
            videoPlayer.Play();

        StartCoroutine(CoRun());
    }

    IEnumerator CoRun()
    {
        // 1) ���� �ð� ���
        float t = 0f;
        while (t < delayBeforeDialogue) { t += Time.deltaTime; yield return null; }

        // 2) �ʿ��ϸ� ���� �Ͻ�����
        if (pauseVideoOnDialogue && videoPlayer) videoPlayer.Pause();

        // 3) ���ܵ� UI �ѱ�
        if (hideUntilDialogue != null)
            foreach (var go in hideUntilDialogue)
                if (go) go.SetActive(true);

        // 4) ��� ����(���� ��ũ��Ʈ �״��)
        if (dialogue)
        {
            dialogue.enabled = true;  // Start()�� ���鼭 �ڵ� ����
            // Ȥ�� �ڵ� ������ ���� �����̸� ���� ȣ�� ����:
            // dialogue.StartDialogue();
        }

        // 5) ��簡 �������¡� ���� ����: �� ���̶� UI�� �����ٰ� �� �� ������ �������� �Ǵ�
        while (!closingDetected)
        {
            if (dialogue)
            {
                bool girlOn = dialogue.girlUI && dialogue.girlUI.activeInHierarchy;
                bool dadOn = dialogue.dadUI && dialogue.dadUI.activeInHierarchy;

                if (girlOn || dadOn) seenAnyDialogueUI = true;
                if (seenAnyDialogueUI && !girlOn && !dadOn) closingDetected = true;
            }
            yield return null;
        }

        // 6) �� ��ȯ
        LoadNextScene();
    }

    void LoadNextScene()
    {
        if (!string.IsNullOrEmpty(nextSceneName))
        {
            if (SceneTransitionManager.Instance != null)
                SceneTransitionManager.Instance.StartSceneTransition(nextSceneName);
            else
                SceneManager.LoadScene(nextSceneName);
            return;
        }

        // ����� ������ Build Settings�� ���� �ε�����
        int cur = SceneManager.GetActiveScene().buildIndex;
        int count = SceneManager.sceneCountInBuildSettings;
        int next = Mathf.Clamp(cur + 1, 0, count - 1);
        string byName = System.IO.Path.GetFileNameWithoutExtension(SceneUtility.GetScenePathByBuildIndex(next));

        if (SceneTransitionManager.Instance != null)
            SceneTransitionManager.Instance.StartSceneTransition(byName);
        else
            SceneManager.LoadScene(next);
    }
}
