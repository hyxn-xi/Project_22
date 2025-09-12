using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(Collider2D))]
public class Portal : MonoBehaviour
{
    [Header("Scene")]
    [Tooltip("����θ� Build Settings�� ���� �ε��� ������ �̵�")]
    public string nextSceneName = "";

    [Header("Input")]
    public KeyCode interactKey = KeyCode.F;   // ������ ������ Ű
    public bool requireKeyPress = true;       // false�� ���⸸ �ص� ��� �̵�

    [Header("UI (optional)")]
    public GameObject prompt;                 // "F Ű�� ���� �̵�" ���� �ȳ�
    public Image fadeImage;                   // ���� ȭ�� ���̵�(����)
    public float fadeDuration = 0.3f;

    bool _playerInRange;
    bool _loading;

    void Awake()
    {
        // ��Ż �ݶ��̴��� Ʈ���ſ��� ��
        var col = GetComponent<Collider2D>();
        if (!col.isTrigger)
        {
            col.isTrigger = true;
            Debug.LogWarning($"[Portal] {name}�� Collider2D�� isTrigger=true�� �����߾��.");
        }

        // ���̵� �̹��� �ʱ�ȭ
        if (fadeImage)
        {
            var c = fadeImage.color;
            c.a = 0f;
            fadeImage.color = c;
            fadeImage.raycastTarget = true;
        }
    }

    void OnEnable()
    {
        if (prompt) prompt.SetActive(false);
    }

    void Update()
    {
        if (_loading) return;

        if (_playerInRange)
        {
            if (prompt && requireKeyPress) prompt.SetActive(true);

            bool pressed = requireKeyPress ? GetPress() : true;
            if (pressed)
            {
                StartCoroutine(LoadNext());
            }
        }
        else
        {
            if (prompt) prompt.SetActive(false);
        }
    }

    // Old/New Input System �� �� ��� ���� Ŀ��
    bool GetPress()
    {
        // Old Input Manager
        if (Input.GetKeyDown(interactKey)) return true;

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        // New Input System�� Ȱ��ȭ�� ���: F Ű ���� ���� ����
        // (������Ʈ�� Input Actions�� ��� Ű���� ����̽��� ���� ���� �� ����)
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb != null && kb.fKey.wasPressedThisFrame) return true;
#endif
        return false;
    }

    IEnumerator LoadNext()
    {
        _loading = true;
        if (prompt) prompt.SetActive(false);

        // �� �̸� ����
        string sceneToLoad = nextSceneName;
        if (string.IsNullOrEmpty(sceneToLoad))
        {
            int idx = SceneManager.GetActiveScene().buildIndex;
            int count = SceneManager.sceneCountInBuildSettings;
            sceneToLoad = (idx + 1 < count) ? Path.GetFileNameWithoutExtension(SceneUtility.GetScenePathByBuildIndex(idx + 1)) : "";
        }

        if (string.IsNullOrEmpty(sceneToLoad))
        {
            Debug.LogError("[Portal] ���� �� �̸��� ã�� ���߾��. nextSceneName�� �����ϰų� Build Settings�� ���� ���� �߰��ϼ���.");
            _loading = false;
            yield break;
        }

        // Build Settings�� ���� Ȯ��(�̸� ����)
        if (!ExistsInBuildSettingsByName(sceneToLoad))
        {
            Debug.LogError($"[Portal] '{sceneToLoad}' ���� Build Settings�� �����ϴ�. File > Build Settings�� ���� �߰��ϼ���.");
            _loading = false;
            yield break;
        }

        // ���̵� �ƿ�
        if (fadeImage && fadeDuration > 0f)
        {
            float t = 0f; var c = fadeImage.color;
            while (t < fadeDuration)
            {
                t += Time.unscaledDeltaTime;
                c.a = Mathf.Clamp01(t / fadeDuration);
                fadeImage.color = c;
                yield return null;
            }
            c.a = 1f; fadeImage.color = c;
        }

        // �ε�
        Debug.Log($"[Portal] Loading '{sceneToLoad}' ��");
        yield return SceneManager.LoadSceneAsync(sceneToLoad, LoadSceneMode.Single);
    }

    // Ʈ���� ����: �ڽ� �ݶ��̴�/��Ʈ �±� ��� Ŀ��
    void OnTriggerEnter2D(Collider2D other)
    {
        if (IsPlayer(other)) _playerInRange = true;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (IsPlayer(other)) _playerInRange = false;
    }

    bool IsPlayer(Collider2D c)
    {
        if (c.CompareTag("Player")) return true;
        if (c.attachedRigidbody && c.attachedRigidbody.CompareTag("Player")) return true;
        return c.GetComponentInParent<PlayerController>() != null; // �±װ� �ڽ�/�θ� ��߳� ��� ���
    }

    // Build Settings�� "�̸�"���� ��ϵ� �ִ��� Ȯ��
    bool ExistsInBuildSettingsByName(string sceneName)
    {
        int count = SceneManager.sceneCountInBuildSettings;
        for (int i = 0; i < count; i++)
        {
            var path = SceneUtility.GetScenePathByBuildIndex(i);
            if (Path.GetFileNameWithoutExtension(path) == sceneName) return true;
        }
        return false;
    }
}
