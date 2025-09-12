using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneController : MonoBehaviour
{
    // ���� ��� '���ư���' ��ư Ŭ�� �� ���� ȭ�� ������ ��ȯ
    public void OnBackButtonClick()
    {
        SceneManager.LoadScene("MainScreenScene");
    }

    // ���� ���� 1 (�ƺ��� ���) ��ư Ŭ�� �� �� ��ȯ
    public void OnFatherMemoryButtonClick()
    {
        SceneManager.LoadScene("FatherMemoryScene");
    }

    // ���� ���� 2 (������ ���) ��ư Ŭ�� �� �� ��ȯ
    public void OnMotherMemoryButtonClick()
    {
        SceneManager.LoadScene("MotherMemoryScene");
    }

    // ���� ���� 3 (�Ƶ��� ���) ��ư Ŭ�� �� �� ��ȯ
    public void OnSonMemoryButtonClick()
    {
        SceneManager.LoadScene("SonMemoryScene");
    }
}
