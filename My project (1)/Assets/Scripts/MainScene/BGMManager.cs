using UnityEngine;
using UnityEngine.SceneManagement; // 씬 관리를 위해 필요

public class BGMManager : MonoBehaviour
{
    // BGMManager의 유일한 인스턴스를 저장
    private static BGMManager instance = null;
    private AudioSource audioSource;

    [Header("BGM Clips")]
    public AudioClip bgmClip_Set1; // 씬 1, 2, 3용 BGM
    public AudioClip bgmClip_Set2; // 씬 4, 5, 6용 BGM
    public AudioClip bgmClip_Set3;
    public AudioClip bgmClip_Set4;
    // 필요한 BGM 클립을 Inspector에서 연결합니다.

    void Awake()
    {
        // 1. 중복 인스턴스 제거 및 DontDestroyOnLoad 설정 (기존 로직 유지)
        if (instance != null && instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(this.gameObject);

        audioSource = GetComponent<AudioSource>();
    }

    void Start()
    {
        // 게임 시작 시 현재 씬에 맞는 BGM 설정
        CheckAndPlayBGM(SceneManager.GetActiveScene().name);
    }

    void OnEnable()
    {
        // 씬 로드 완료 시 BGM 확인 함수 연결
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        // 이벤트 중복 방지를 위해 제거
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // 씬 로드가 완료될 때마다 호출되는 함수
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        CheckAndPlayBGM(scene.name);
    }

    // 씬 이름에 따라 재생할 BGM을 결정하고 재생하는 핵심 함수
    private void CheckAndPlayBGM(string sceneName)
    {
        AudioClip targetClip = null;

        // 씬 이름에 따라 재생할 AudioClip 결정
        // 1. (Set3 그룹): 가장 구체적인 이름 ("1-0a", "1-1a") 먼저 검사
        if (sceneName.Contains("1-0a") || sceneName.Contains("1-1a"))
        {
            targetClip = bgmClip_Set3;
        }
        // 2. (Set2 그룹): 그 다음 구체적인 이름 ("1-0", "1-1", "1-2" 등) 검사
        else if (sceneName.Contains("1-0") || sceneName.Contains("1-1") || sceneName.Contains("1-1Cutscene")
                 || sceneName.Contains("1-2") || sceneName.Contains("1-2Cutscene") || sceneName.Contains("1-3") || sceneName.Contains("1-3Cutscene"))
        {
            targetClip = bgmClip_Set2;
        }
        // 3. (Set1 그룹): 가장 일반적인 이름과 다른 모든 이름 검사
        else if (sceneName.Contains("MainScreenScene") || sceneName.Contains("BeforeStartCutScene") || sceneName.Contains("GameScene") ||
                 sceneName.Contains("GameScene-1a") || sceneName.Contains("MemoryScene") || sceneName.Contains("FatherMemoryScene") ||
                 sceneName.Contains("FatherMemoryScene2") || sceneName.Contains("FatherMemoryScene3") || sceneName.Contains("MotherMemoryScene")
                 || sceneName.Contains("MotherMemoryScene2") || sceneName.Contains("MotherMemoryScene3") || sceneName.Contains("SonMemoryScene")
                 || sceneName.Contains("SonMemoryScene2") || sceneName.Contains("SonMemoryScene3") || sceneName.Contains("0-0-1a") || sceneName.Contains("0-0"))
        {
            targetClip = bgmClip_Set1;
        }
        // (필요하다면 else if로 다른 BGM 세트를 추가)

        // 씬에 해당하는 BGM이 존재하고, 현재 재생 중인 BGM과 다를 경우에만 교체 및 재생
        if (targetClip != null && audioSource.clip != targetClip)
        {
            audioSource.clip = targetClip;
            audioSource.Play();
        }
        else if (targetClip == null)
        {
            // BGM이 없는 씬 (예: 메뉴)이라면 재생 중지
            audioSource.Stop();
        }
        // audioSource.clip == targetClip 일 경우: 같은 곡이 재생 중이므로 아무것도 하지 않음 (끊김 방지)
    }
}