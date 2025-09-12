using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class PressFToContinue : MonoBehaviour
{
    [Header("Input")]
    public KeyCode interactKey = KeyCode.F;   // 누를 키

    [Header("Scene")]
    [Tooltip("비워두면 Build Settings 상 '다음 인덱스' 씬으로 이동")]
    public string nextSceneName = "";

    [Header("Prompt UI (optional)")]
    [Tooltip("화면에 'F 키' 안내가 있다면 여기에 연결(없으면 비워두기)")]
    public GameObject prompt;                 // 예: 'F 키를 눌러 계속'

    [Header("Fade Out (optional)")]
    [Tooltip("검은 오버레이 Image. 있으면 알파로 페이드")]
    public Image fadeImage;                   // 없으면 비워두기
    [Tooltip("CanvasGroup으로 페이드하고 싶다면 여기에 연결(위 Image 대신)")]
    public CanvasGroup fadeGroup;             // 없으면 비워두기
    public float fadeDuration = 0.6f;         // 페이드 시간
    public bool useUnscaledTime = true;       // 컷신에 타임스케일이 바뀌어도 페이드 되도록

    [Header("Misc")]
    [Tooltip("씬 들어온 직후 오입력 방지를 위해 대기")]
    public float acceptDelay = 0.25f;         // 입력 허용까지 대기
    public AudioSource sfx;                   // 확인 사운드 재생용(선택)
    public AudioClip confirmClip;

    float _readyAt;
    bool _loading;

    void Awake()
    {
        _readyAt = Time.unscaledTime + acceptDelay;

        // 선택요소 초기화
        if (fadeImage)
        {
            var c = fadeImage.color;
            c.a = 0f;
            fadeImage.color = c;
            fadeImage.raycastTarget = true;   // 입력 막기
        }
        if (fadeGroup)
        {
            fadeGroup.alpha = 0f;
            fadeGroup.blocksRaycasts = true;  // 입력 막기
            fadeGroup.interactable = true;
        }
        if (prompt) prompt.SetActive(true);
    }

    void Update()
    {
        if (_loading) return;

        // 입력 허용 타이밍까지 대기
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

            // 보정
            if (fadeImage)
            {
                var c = fadeImage.color; c.a = 1f; fadeImage.color = c;
            }
            if (fadeGroup) fadeGroup.alpha = 1f;
        }

        // 씬 결정
        if (!string.IsNullOrEmpty(nextSceneName))
        {
            SceneManager.LoadScene(nextSceneName, LoadSceneMode.Single);
        }
        else
        {
            // Build Settings에서 현재 씬의 다음 인덱스가 있으면 그걸로
            int idx = SceneManager.GetActiveScene().buildIndex;
            int count = SceneManager.sceneCountInBuildSettings;
            int next = Mathf.Clamp(idx + 1, 0, count - 1);
            SceneManager.LoadScene(next, LoadSceneMode.Single);
        }
    }
}
