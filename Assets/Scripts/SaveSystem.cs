using UnityEngine;
using UnityEngine.SceneManagement;

public static class SaveSystem
{
    public static void SaveGame()
    {
        PlayerPrefs.SetInt("PlayerHP", RunData.PlayerHP);
        PlayerPrefs.SetInt("PlayerMaxHP", RunData.PlayerMaxHP);
        PlayerPrefs.SetString("SavedScene", SceneManager.GetActiveScene().name);
        PlayerPrefs.SetInt("SaveExists", 1);
        PlayerPrefs.Save();
    }

    public static bool LoadExists()
    {
        return PlayerPrefs.GetInt("SaveExists") == 1;
    }

    public static string LoadGame()
    {
        RunData.PlayerHP = PlayerPrefs.GetInt("PlayerHP");
        RunData.PlayerMaxHP = PlayerPrefs.GetInt("PlayerMaxHP");
        return PlayerPrefs.GetString("SavedScene");
    }

    public static void DeleteSave()
    {
        PlayerPrefs.DeleteKey("PlayerHP");
        PlayerPrefs.DeleteKey("PlayerMaxHP");
        PlayerPrefs.DeleteKey("SavedScene");
        PlayerPrefs.DeleteKey("SaveExists");
        PlayerPrefs.Save();
    }
}
