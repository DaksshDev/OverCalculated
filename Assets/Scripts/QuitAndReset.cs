using UnityEngine;

public class QuitAndReset : MonoBehaviour
{
    public void Quit()
    {
        Application.Quit();
    }

    public void reset()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
    }
}
