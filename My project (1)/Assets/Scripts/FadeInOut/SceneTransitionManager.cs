using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance;

    [Header("Fade Settings")]
    public Image fadeOverlay;
    public float defaultFadeDuration = 1.5f;

    private float currentFadeDuration;

    void Awake()
    {
        Debug.Log("🟢 SceneTransitionManager.Awake() 호출됨");

        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded; // ✅ 씬 로드 이벤트 등록
            Debug.Log("✅ SceneTransitionManager 인스턴스 설정됨 — DontDestroyOnLoad 적용됨");
        }
        else if (Instance != this)
        {
            Debug.LogWarning("⚠ 중복된 SceneTransitionManager 발견 — 제거됨");
            Destroy(gameObject);
        }
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded; // ✅ 이벤트 해제
        }
    }

    /// <summary>
    /// 씬 로드 완료 시 자동 페이드인 실행
    /// </summary>
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"🟢 씬 로드 완료: {scene.name} — 자동 페이드인 시작");

        // 새 씬의 fadeOverlay 다시 연결
        fadeOverlay = GameObject.Find("FadeOverlay")?.GetComponent<Image>();
        if (fadeOverlay == null)
        {
            Debug.LogWarning("⚠ 새 씬에서 fadeOverlay를 찾지 못함 — 페이드 불가");
            return;
        }

        fadeOverlay.gameObject.SetActive(true); // ✅ 씬 로드 후 강제 활성화
        StartFadeIn(currentFadeDuration);
    }


    public void StartSceneTransition(string sceneName, float fadeDuration = -1f, bool useFade = true)
    {
        Debug.Log($"🟣 StartSceneTransition() 호출됨 — 씬 이름: {sceneName}, 페이드 사용: {useFade}, 페이드 시간: {(fadeDuration > 0 ? fadeDuration : defaultFadeDuration)}");

        currentFadeDuration = (fadeDuration > 0f) ? fadeDuration : defaultFadeDuration;

        if (!useFade)
        {
            Debug.Log($"▶ 페이드 없이 씬 전환: {sceneName}");
            SceneManager.LoadScene(sceneName);
            return;
        }

        if (fadeOverlay == null)
        {
            Debug.LogError("❌ fadeOverlay가 연결되지 않았습니다. 페이드 없이 씬 전환합니다.");
            SceneManager.LoadScene(sceneName);
            return;
        }

        fadeOverlay.gameObject.SetActive(true);
        StartCoroutine(FadeOutAndLoadScene(sceneName));
    }

    private IEnumerator FadeOutAndLoadScene(string sceneName)
    {
        Debug.Log($"▶ 페이드아웃 시작: {sceneName}");

        yield return null; // ✅ 한 프레임 기다려서 렌더링 보장

        Color c = fadeOverlay.color;
        c.a = 0f;
        fadeOverlay.color = c;

        float elapsed = 0f;
        while (elapsed < currentFadeDuration)
        {
            elapsed += Time.deltaTime;
            c.a = Mathf.Clamp01(elapsed / currentFadeDuration);
            fadeOverlay.color = c;
            yield return null;
        }

        Debug.Log($"🚀 씬 전환 실행: {sceneName}");
        SceneManager.LoadScene(sceneName);
    }

    public void StartFadeIn(float fadeDuration = -1f)
    {
        Debug.Log("🟢 StartFadeIn() 호출됨");

        currentFadeDuration = (fadeDuration > 0f) ? fadeDuration : defaultFadeDuration;

        if (fadeOverlay == null)
        {
            Debug.LogError("❌ fadeOverlay가 연결되지 않았습니다. 페이드인 불가");
            return;
        }

        fadeOverlay.gameObject.SetActive(true);
        StartCoroutine(FadeInCoroutine());
    }

    private IEnumerator FadeInCoroutine()
    {
        Debug.Log("▶ 페이드인 시작");

        Color c = fadeOverlay.color;
        c.a = 1f;
        fadeOverlay.color = c;

        float elapsed = 0f;
        while (elapsed < currentFadeDuration)
        {
            elapsed += Time.deltaTime;
            c.a = 1f - Mathf.Clamp01(elapsed / currentFadeDuration);
            fadeOverlay.color = c;
            yield return null;
        }

        fadeOverlay.gameObject.SetActive(false);
        Debug.Log("✅ 페이드인 완료");
    }
}
