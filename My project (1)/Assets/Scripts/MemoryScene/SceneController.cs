using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneController : MonoBehaviour
{
    // 왼쪽 상단 '돌아가기' 버튼 클릭 시 메인 화면 씬으로 전환
    public void OnBackButtonClick()
    {
        SceneManager.LoadScene("MainScreenScene");
    }

    // 깨진 유리 1 (아빠의 기억) 버튼 클릭 시 씬 전환
    public void OnFatherMemoryButtonClick()
    {
        SceneManager.LoadScene("FatherMemoryScene");
    }

    // 깨진 유리 2 (엄마의 기억) 버튼 클릭 시 씬 전환
    public void OnMotherMemoryButtonClick()
    {
        SceneManager.LoadScene("MotherMemoryScene");
    }

    // 깨진 유리 3 (아들의 기억) 버튼 클릭 시 씬 전환
    public void OnSonMemoryButtonClick()
    {
        SceneManager.LoadScene("SonMemoryScene");
    }
}
