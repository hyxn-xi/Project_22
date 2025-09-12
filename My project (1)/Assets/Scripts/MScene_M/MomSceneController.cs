using UnityEngine;
using UnityEngine.SceneManagement;

public class MomSceneController : MonoBehaviour
{
    // 왼쪽 상단 '돌아가기' 버튼 클릭 시 메인 화면 씬으로 전환
    public void OnBackButtonClick()
    {
        SceneManager.LoadScene("MemoryScene");
    }

    public void OnRightButtonClick()
    {
        SceneManager.LoadScene("MotherMemoryScene2");
    }

    public void OnRightButtonClick2()
    {
        SceneManager.LoadScene("MotherMemoryScene3");
    }

    public void OnLeftButtonClick()
    {
        SceneManager.LoadScene("MotherMemoryScene");
    }

    public void OnLeftButtonClick2()
    {
        SceneManager.LoadScene("MotherMemoryScene2");
    }
}
