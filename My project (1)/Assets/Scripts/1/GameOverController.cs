using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class GameOverController : MonoBehaviour
{
    public static GameOverController Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] Image blackOverlay;          // 알파 0에서 시작
    [SerializeField] GameObject gameOverPanel;    // 시작 시 비활성

    [Header("HUD (hide on game over)")]
    [Tooltip("게임오버 시 숨길 HUD들: 옵션 버튼, 스테이지 번호, 하트 등")]
    [SerializeField] List<GameObject> hideOnGameOver = new List<GameObject>();

    [Header("Scenes")]
    [SerializeField] string retrySceneName = "1-1";
    [SerializeField] string mainMenuSceneName = "MainMenu";

    [Header("Behavior")]
    [SerializeField] float fadeDuration = 0.8f;   // unscaled
    [SerializeField] bool pauseOnGameOver = true; // Time.timeScale = 0

    bool _running;

    void Awake()
    {
        // Singleton (선택)
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (string.IsNullOrEmpty(retrySceneName))
            retrySceneName = SceneManager.GetActiveScene().name;

        // Overlay 초기화(무조건 까맣게 보이도록 보정)
        if (blackOverlay)
        {
            blackOverlay.material = null;
            blackOverlay.raycastTarget = true;

            // 알파 0으로 시작
            var c = blackOverlay.color; c.a = 0f; blackOverlay.color = c;

            // 1x1 흰색 스프라이트를 보장(그라데이션/투명 스프라이트 방지)
            if (blackOverlay.sprite == null)
            {
                var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                tex.SetPixel(0, 0, Color.white);
                tex.Apply();
                var spr = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
                blackOverlay.sprite = spr;
                blackOverlay.type = Image.Type.Simple;
                blackOverlay.preserveAspect = false;
            }

            // 전체 화면 스트레치
            var rt = blackOverlay.rectTransform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

            // 같은 부모면 패널 뒤쪽으로(패널이 위에 나오게)
            if (gameOverPanel && gameOverPanel.transform.parent == blackOverlay.transform.parent)
            {
                int panelIdx = gameOverPanel.transform.GetSiblingIndex();
                blackOverlay.transform.SetSiblingIndex(Mathf.Max(0, panelIdx - 1));
            }
        }

        if (gameOverPanel) gameOverPanel.SetActive(false);
    }

    // 외부에서 호출: PlayerDeathWatcher / 애니메이션 이벤트 등
    public void TriggerGameOver()
    {
        if (!isActiveAndEnabled || _running) return;
        StartCoroutine(CoGameOver());
    }

    IEnumerator CoGameOver()
    {
        _running = true;

        // 0) HUD 숨기기
        if (hideOnGameOver != null)
            foreach (var go in hideOnGameOver) if (go) go.SetActive(false);

        // 1) 검은 화면 페이드 (unscaled)
        if (blackOverlay)
        {
            float t = 0f;
            var c = blackOverlay.color;
            while (t < fadeDuration)
            {
                t += Time.unscaledDeltaTime;
                c.a = Mathf.Clamp01(t / fadeDuration);
                blackOverlay.color = c;
                yield return null;
            }
            c.a = 1f; blackOverlay.color = c;
        }

        // 2) 패널 표시 & 일시정지
        if (gameOverPanel) gameOverPanel.SetActive(true);
        if (pauseOnGameOver) Time.timeScale = 0f;
    }

    // ===== 버튼 콜백 =====
    public void OnClickRetry()
    {
        Time.timeScale = 1f;
        if (!string.IsNullOrEmpty(retrySceneName))
            SceneManager.LoadScene(retrySceneName, LoadSceneMode.Single);
    }

    public void OnClickMainMenu()
    {
        Time.timeScale = 1f;
        if (!string.IsNullOrEmpty(mainMenuSceneName))
            SceneManager.LoadScene(mainMenuSceneName, LoadSceneMode.Single);
    }

    // 정적 헬퍼(원하면 어디서든 GameOverController.Request())
    public static void Request()
    {
        if (Instance) Instance.TriggerGameOver();
        else Debug.LogWarning("[GameOverController] Instance is null.");
    }
}
