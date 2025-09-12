using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;

public class CutsceneManager : MonoBehaviour
{
    [SerializeField] VideoPlayer videoPlayer;
    [SerializeField] string nextSceneName;

    void Start()
    {
        Debug.Log("�ƽ� ������ SceneTransitionManager.Instance ����: " + (SceneTransitionManager.Instance == null ? "NULL" : "OK"));
        
        // �� ���� �� ���̵���
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.StartFadeIn();
        }
        else
        {
            Debug.LogWarning("[CutsceneManager] SceneTransitionManager �ν��Ͻ��� ã�� �� �����ϴ�.");
        }

        // ���� ���� �̺�Ʈ ����
        videoPlayer.loopPointReached += OnVideoEnd;
    }

    void OnVideoEnd(VideoPlayer vp)
    {
        // ���� ������ ���̵�ƿ� �� �� ��ȯ
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.StartSceneTransition(nextSceneName);
        }
        else
        {
            Debug.LogError("[CutsceneManager] SceneTransitionManager �ν��Ͻ��� ã�� �� �����ϴ�.");
            SceneManager.LoadScene(nextSceneName); // ���� ó��: ���̵� ���� ��ȯ
        }
    }
}
