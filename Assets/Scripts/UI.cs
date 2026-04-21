using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UI : MonoBehaviour
{
    public Slider slider;
    public Button start;
    public Button quit;

    void Start()
    {
        start.onClick.AddListener(OnStartClick);
        quit.onClick.AddListener(OnQuitClick);
    }

    void OnStartClick()
    {
        if (slider.value != 8 && slider.value !=9 )
        {
            DataHolder.spaceshipPos = (int)slider.value;
            //Debug.Log(DataHolder.spaceshipPos);
            SceneManager.LoadScene("Game");
        }
    }

    void OnQuitClick()
    {
        Application.Quit();
    }

    void OnDestroy()
    {
        start.onClick.RemoveListener(OnStartClick);
        quit.onClick.RemoveListener(OnQuitClick);
    }
}
public static class DataHolder
{
    public static int spaceshipPos;
}