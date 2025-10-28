using UnityEngine;
using UnityEngine.SceneManagement;

public class FootstepManager : MonoBehaviour
{
    // ★★★ 씬 전환 시 오브젝트를 유지하기 위한 싱글톤 패턴 ★★★
    public static FootstepManager Instance;

    [System.Serializable]
    public struct FootstepGroup
    {
        public string[] sceneNames; // 이 발소리를 사용할 씬 이름 목록
        public AudioClip clip;      // 사용할 발소리 AudioClip
    }

    [Header("Footstep Settings")]
    public FootstepGroup[] footstepGroups; // Inspector에서 설정

    void Awake()
    {
        // 1. 싱글톤 패턴 적용 (BGMManager와 동일)
        if (Instance == null)
        {
            Instance = this;
            // 씬이 전환되어도 이 오브젝트를 파괴하지 않음
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            // 이미 인스턴스가 존재하면 새로 생성된 오브젝트를 파괴 (중복 방지)
            Destroy(gameObject);
            return;
        }

        // 2. 씬 로드 이벤트 연결
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    // (선택 사항: 애플리케이션 종료 시 이벤트 제거)
    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 씬이 로드될 때마다 PlayerController를 찾습니다.
        PlayerController player = FindAnyObjectByType<PlayerController>();
        if (player == null) return;

        AudioClip targetClip = GetFootstepClipForScene(scene.name);

        if (targetClip != null && player.footstepSource != null)
        {
            // PlayerController에 연결된 AudioSource의 AudioClip을 교체
            player.footstepSource.clip = targetClip;

            // 클립 교체 후 발소리 재생을 멈춰서 다음 LateUpdate에서 새 클립으로 다시 재생되도록 준비
            if (player.footstepSource.isPlaying)
                player.footstepSource.Stop();
        }
    }

    private AudioClip GetFootstepClipForScene(string sceneName)
    {
        foreach (var group in footstepGroups)
        {
            foreach (var name in group.sceneNames)
            {
                // 씬 이름이 목록에 포함되어 있는지 확인
                if (sceneName.Contains(name))
                {
                    return group.clip;
                }
            }
        }
        return null;
    }
}