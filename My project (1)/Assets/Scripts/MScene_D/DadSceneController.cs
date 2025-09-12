using UnityEngine;
using UnityEngine.SceneManagement;

public class DadSceneController : MonoBehaviour
{
    // ���� ��� '���ư���' ��ư Ŭ�� �� ���� ȭ�� ������ ��ȯ
    public void OnBackButtonClick()
    {
        SceneManager.LoadScene("MemoryScene");
    }

    public void OnRightButtonClick()
    {
        SceneManager.LoadScene("FatherMemoryScene2");
    }

    public void OnRightButtonClick2()
    {
        SceneManager.LoadScene("FatherMemoryScene3");
    }

    public void OnLeftButtonClick()
    {
        SceneManager.LoadScene("FatherMemoryScene");
    }

    public void OnLeftButtonClick2()
    {
        SceneManager.LoadScene("FatherMemoryScene2");
    }
}
