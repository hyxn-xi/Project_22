using UnityEngine;
using UnityEngine.SceneManagement;

public class MomSceneController : MonoBehaviour
{
    // ���� ��� '���ư���' ��ư Ŭ�� �� ���� ȭ�� ������ ��ȯ
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
