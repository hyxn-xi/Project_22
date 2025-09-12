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
    [SerializeField] Image blackOverlay;          // ���� 0���� ����
    [SerializeField] GameObject gameOverPanel;    // ���� �� ��Ȱ��

    [Header("HUD (hide on game over)")]
    [Tooltip("���ӿ��� �� ���� HUD��: �ɼ� ��ư, �������� ��ȣ, ��Ʈ ��")]
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
        // Singleton (����)
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (string.IsNullOrEmpty(retrySceneName))
            retrySceneName = SceneManager.GetActiveScene().name;

        // Overlay �ʱ�ȭ(������ ��İ� ���̵��� ����)
        if (blackOverlay)
        {
            blackOverlay.material = null;
            blackOverlay.raycastTarget = true;

            // ���� 0���� ����
            var c = blackOverlay.color; c.a = 0f; blackOverlay.color = c;

            // 1x1 ��� ��������Ʈ�� ����(�׶��̼�/���� ��������Ʈ ����)
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

            // ��ü ȭ�� ��Ʈ��ġ
            var rt = blackOverlay.rectTransform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

            // ���� �θ�� �г� ��������(�г��� ���� ������)
            if (gameOverPanel && gameOverPanel.transform.parent == blackOverlay.transform.parent)
            {
                int panelIdx = gameOverPanel.transform.GetSiblingIndex();
                blackOverlay.transform.SetSiblingIndex(Mathf.Max(0, panelIdx - 1));
            }
        }

        if (gameOverPanel) gameOverPanel.SetActive(false);
    }

    // �ܺο��� ȣ��: PlayerDeathWatcher / �ִϸ��̼� �̺�Ʈ ��
    public void TriggerGameOver()
    {
        if (!isActiveAndEnabled || _running) return;
        StartCoroutine(CoGameOver());
    }

    IEnumerator CoGameOver()
    {
        _running = true;

        // 0) HUD �����
        if (hideOnGameOver != null)
            foreach (var go in hideOnGameOver) if (go) go.SetActive(false);

        // 1) ���� ȭ�� ���̵� (unscaled)
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

        // 2) �г� ǥ�� & �Ͻ�����
        if (gameOverPanel) gameOverPanel.SetActive(true);
        if (pauseOnGameOver) Time.timeScale = 0f;
    }

    // ===== ��ư �ݹ� =====
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

    // ���� ����(���ϸ� ��𼭵� GameOverController.Request())
    public static void Request()
    {
        if (Instance) Instance.TriggerGameOver();
        else Debug.LogWarning("[GameOverController] Instance is null.");
    }
}
