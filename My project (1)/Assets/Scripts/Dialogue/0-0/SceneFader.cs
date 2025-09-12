using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneFader : MonoBehaviour
{
    public float fadeSpeed = 1f;           // 페이드 속도
    public bool canFade = true;            // 페이드 가능 여부
    public string sceneName;               // Inspector에서 입력할 씬 이름

    private Image fadeImage;

    void Awake()
    {
        Debug.Log("🟡 SceneFader.Awake() 호출됨");

        // 중복 제거
        if (FindObjectsOfType<SceneFader>().Length > 1)
        {
            Debug.LogWarning("⚠ 중복된 SceneFader 발견 — 제거됨");
            SceneManager.sceneLoaded -= OnSceneLoaded; // 이벤트 해제
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);     // 씬 넘어가도 유지
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        Debug.Log("🧹 SceneFader.OnDestroy() — 이벤트 해제");
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void Start()
    {
        Debug.Log("🟡 SceneFader.Start() 호출됨");
        fadeImage = GameObject.Find("FadeImage")?.GetComponent<Image>();
        if (fadeImage != null)
        {
            Debug.Log("▶ Start에서 페이드 인 시작");
            fadeImage.color = new Color(0, 0, 0, 1); // 시작 시 검은 화면
            StartCoroutine(Fade(1, 0));              // 페이드 인
        }
        else
        {
            Debug.LogWarning("⚠ Start에서 fadeImage를 찾지 못함");
        }
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log("🟢 씬 로드됨: " + scene.name);
        StartCoroutine(DelayedFadeIn(scene));
    }

    IEnumerator DelayedFadeIn(Scene scene)
    {
        yield return null; // 1프레임 기다림

        fadeImage = GameObject.Find("FadeImage")?.GetComponent<Image>();
        if (fadeImage != null)
        {
            Debug.Log("▶ DelayedFadeIn에서 페이드 인 시작: " + scene.name);
            fadeImage.color = new Color(0, 0, 0, 1);
            StartCoroutine(Fade(1, 0));
        }
        else
        {
            Debug.LogWarning("⚠ DelayedFadeIn에서 fadeImage를 찾지 못함");
        }
    }

    public void FadeToScene()
    {
        Debug.Log("🟣 FadeToScene() 호출됨 — sceneName: " + sceneName + ", canFade: " + canFade);
        if (canFade && !string.IsNullOrEmpty(sceneName))
        {
            StartCoroutine(FadeOutAndLoad(sceneName));
        }
        else
        {
            Debug.LogWarning("⚠ FadeToScene 실행 불가 — canFade: " + canFade + ", sceneName: " + sceneName);
        }
    }

    public void StartFadeOut(string targetScene)
    {
        Debug.Log("🟣 StartFadeOut() 호출됨 — targetScene: " + targetScene + ", canFade: " + canFade);
        if (canFade && !string.IsNullOrEmpty(targetScene))
        {
            StartCoroutine(FadeOutAndLoad(targetScene));
        }
        else
        {
            Debug.LogWarning("⚠ StartFadeOut 실행 불가 — 씬 이름이 없거나 canFade가 false");
        }
    }

    public void StartFadeOut()
    {
        Debug.Log("🟣 StartFadeOut() 호출됨 — sceneName: " + sceneName + ", canFade: " + canFade);
        if (canFade && !string.IsNullOrEmpty(sceneName))
        {
            StartCoroutine(FadeOutAndLoad(sceneName));
        }
        else
        {
            Debug.LogWarning("⚠ StartFadeOut 실행 불가 — sceneName이 비어있거나 canFade가 false");
        }
    }

    IEnumerator FadeOutAndLoad(string targetScene)
    {
        Debug.Log("▶ 페이드 아웃 시작 — targetScene: " + targetScene);
        Debug.Log("⏱ 현재 Time.timeScale: " + Time.timeScale);
        yield return StartCoroutine(Fade(0, 1));
        Debug.Log("✅ 페이드 완료, 씬 전환 시도: " + targetScene);

        try
        {
            SceneManager.LoadScene(targetScene);
            Debug.Log("🚀 SceneManager.LoadScene() 호출됨 — " + targetScene);
        }
        catch (System.Exception e)
        {
            Debug.LogError("❌ 씬 전환 실패: " + e.Message);
        }
    }

    IEnumerator Fade(float from, float to)
    {
        if (fadeImage == null)
        {
            Debug.LogWarning("⚠ fadeImage is NULL — 페이드 불가!");
            yield break;
        }

        Debug.Log("▶ 페이드 시작: " + from + " → " + to);

        float alpha = from;

        while (Mathf.Abs(alpha - to) > 0.01f)
        {
            alpha = Mathf.MoveTowards(alpha, to, Time.deltaTime * fadeSpeed);
            fadeImage.color = new Color(0, 0, 0, alpha);
            yield return null;
        }

        fadeImage.color = new Color(0, 0, 0, to); // 마지막 값 보정
        Debug.Log("✅ 페이드 완료: " + to);
    }
}
