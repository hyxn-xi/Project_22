using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class PressFToContinue : MonoBehaviour
{
    [Header("Input")]
    public KeyCode interactKey = KeyCode.F;   // ���� Ű

    [Header("Scene")]
    [Tooltip("����θ� Build Settings �� '���� �ε���' ������ �̵�")]
    public string nextSceneName = "";

    [Header("Prompt UI (optional)")]
    [Tooltip("ȭ�鿡 'F Ű' �ȳ��� �ִٸ� ���⿡ ����(������ ����α�)")]
    public GameObject prompt;                 // ��: 'F Ű�� ���� ���'

    [Header("Fade Out (optional)")]
    [Tooltip("���� �������� Image. ������ ���ķ� ���̵�")]
    public Image fadeImage;                   // ������ ����α�
    [Tooltip("CanvasGroup���� ���̵��ϰ� �ʹٸ� ���⿡ ����(�� Image ���)")]
    public CanvasGroup fadeGroup;             // ������ ����α�
    public float fadeDuration = 0.6f;         // ���̵� �ð�
    public bool useUnscaledTime = true;       // �ƽſ� Ÿ�ӽ������� �ٲ� ���̵� �ǵ���

    [Header("Misc")]
    [Tooltip("�� ���� ���� ���Է� ������ ���� ���")]
    public float acceptDelay = 0.25f;         // �Է� ������ ���
    public AudioSource sfx;                   // Ȯ�� ���� �����(����)
    public AudioClip confirmClip;

    float _readyAt;
    bool _loading;

    void Awake()
    {
        _readyAt = Time.unscaledTime + acceptDelay;

        // ���ÿ�� �ʱ�ȭ
        if (fadeImage)
        {
            var c = fadeImage.color;
            c.a = 0f;
            fadeImage.color = c;
            fadeImage.raycastTarget = true;   // �Է� ����
        }
        if (fadeGroup)
        {
            fadeGroup.alpha = 0f;
            fadeGroup.blocksRaycasts = true;  // �Է� ����
            fadeGroup.interactable = true;
        }
        if (prompt) prompt.SetActive(true);
    }

    void Update()
    {
        if (_loading) return;

        // �Է� ��� Ÿ�ֱ̹��� ���
        float now = useUnscaledTime ? Time.unscaledTime : Time.time;
        if (now < _readyAt) return;

        if (Input.GetKeyDown(interactKey))
        {
            StartCoroutine(LoadNext());
        }
    }

    IEnumerator LoadNext()
    {
        _loading = true;
        if (prompt) prompt.SetActive(false);

        // SFX
        if (sfx && confirmClip)
        {
            sfx.PlayOneShot(confirmClip);
        }

        // Fade Out
        if (fadeDuration > 0f && (fadeImage || fadeGroup))
        {
            float t = 0f;
            while (t < fadeDuration)
            {
                t += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                float a = Mathf.Clamp01(t / fadeDuration);

                if (fadeImage)
                {
                    var c = fadeImage.color;
                    c.a = a;
                    fadeImage.color = c;
                }
                if (fadeGroup)
                {
                    fadeGroup.alpha = a;
                }
                yield return null;
            }

            // ����
            if (fadeImage)
            {
                var c = fadeImage.color; c.a = 1f; fadeImage.color = c;
            }
            if (fadeGroup) fadeGroup.alpha = 1f;
        }

        // �� ����
        if (!string.IsNullOrEmpty(nextSceneName))
        {
            SceneManager.LoadScene(nextSceneName, LoadSceneMode.Single);
        }
        else
        {
            // Build Settings���� ���� ���� ���� �ε����� ������ �װɷ�
            int idx = SceneManager.GetActiveScene().buildIndex;
            int count = SceneManager.sceneCountInBuildSettings;
            int next = Mathf.Clamp(idx + 1, 0, count - 1);
            SceneManager.LoadScene(next, LoadSceneMode.Single);
        }
    }
}
