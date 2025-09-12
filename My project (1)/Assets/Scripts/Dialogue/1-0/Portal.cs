using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(Collider2D))]
public class Portal : MonoBehaviour
{
    [Header("Scene")]
    [Tooltip("비워두면 Build Settings의 다음 인덱스 씬으로 이동")]
    public string nextSceneName = "";

    [Header("Input")]
    public KeyCode interactKey = KeyCode.F;   // 눌러서 진입할 키
    public bool requireKeyPress = true;       // false면 들어가기만 해도 즉시 이동

    [Header("UI (optional)")]
    public GameObject prompt;                 // "F 키를 눌러 이동" 같은 안내
    public Image fadeImage;                   // 검은 화면 페이드(선택)
    public float fadeDuration = 0.3f;

    bool _playerInRange;
    bool _loading;

    void Awake()
    {
        // 포탈 콜라이더는 트리거여야 함
        var col = GetComponent<Collider2D>();
        if (!col.isTrigger)
        {
            col.isTrigger = true;
            Debug.LogWarning($"[Portal] {name}의 Collider2D를 isTrigger=true로 수정했어요.");
        }

        // 페이드 이미지 초기화
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

    // Old/New Input System 둘 다 어느 정도 커버
    bool GetPress()
    {
        // Old Input Manager
        if (Input.GetKeyDown(interactKey)) return true;

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        // New Input System만 활성화된 경우: F 키 눌림 간단 감지
        // (프로젝트에 Input Actions가 없어도 키보드 디바이스는 직접 읽을 수 있음)
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb != null && kb.fKey.wasPressedThisFrame) return true;
#endif
        return false;
    }

    IEnumerator LoadNext()
    {
        _loading = true;
        if (prompt) prompt.SetActive(false);

        // 씬 이름 결정
        string sceneToLoad = nextSceneName;
        if (string.IsNullOrEmpty(sceneToLoad))
        {
            int idx = SceneManager.GetActiveScene().buildIndex;
            int count = SceneManager.sceneCountInBuildSettings;
            sceneToLoad = (idx + 1 < count) ? Path.GetFileNameWithoutExtension(SceneUtility.GetScenePathByBuildIndex(idx + 1)) : "";
        }

        if (string.IsNullOrEmpty(sceneToLoad))
        {
            Debug.LogError("[Portal] 다음 씬 이름을 찾지 못했어요. nextSceneName을 지정하거나 Build Settings에 다음 씬을 추가하세요.");
            _loading = false;
            yield break;
        }

        // Build Settings에 존재 확인(이름 기준)
        if (!ExistsInBuildSettingsByName(sceneToLoad))
        {
            Debug.LogError($"[Portal] '{sceneToLoad}' 씬이 Build Settings에 없습니다. File > Build Settings… 에서 추가하세요.");
            _loading = false;
            yield break;
        }

        // 페이드 아웃
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

        // 로드
        Debug.Log($"[Portal] Loading '{sceneToLoad}' …");
        yield return SceneManager.LoadSceneAsync(sceneToLoad, LoadSceneMode.Single);
    }

    // 트리거 감지: 자식 콜라이더/루트 태그 모두 커버
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
        return c.GetComponentInParent<PlayerController>() != null; // 태그가 자식/부모 어긋난 경우 대비
    }

    // Build Settings에 "이름"으로 등록돼 있는지 확인
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
