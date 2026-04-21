using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ExitScreen : MonoBehaviour
{
    public GameObject popupPanel;
    public Button yesButton;
    public Button noButton;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ShowPopup();
        }
    }

    void ShowPopup()
    {
        popupPanel.SetActive(true);
        yesButton.onClick.AddListener(OnYesClick);
        noButton.onClick.AddListener(OnNoClick);
    }

    void OnYesClick()
    {
        SceneManager.LoadScene("Menu");
    }

    void OnNoClick()
    {
        popupPanel.SetActive(false);
    }

    private void OnDestroy()
    {
        yesButton.onClick.RemoveListener(OnYesClick);
        noButton.onClick.RemoveListener(OnNoClick);
    }
}
