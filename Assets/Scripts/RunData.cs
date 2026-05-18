using System.Collections.Generic;
using UnityEngine;

public static class RunData
{
    public static int PlayerHP = 20;
    public static int PlayerMaxHP = 20;
    public static bool ChoseHermit
    {
        get => _choseHermit;
        set
        {
            Debug.Log("[RunData] ChoseHermit set to: " + value + "\n" + System.Environment.StackTrace);
            _choseHermit = value;
        }
    }
    private static bool _choseHermit = false;
    public static bool ComingFromIntro = false;
    public static List<string> ScrollsCollected = new List<string>();
    public static string NextSceneName = string.Empty;

    public static void Reset()
    {
        PlayerHP = 20;
        PlayerMaxHP = 20;
        ChoseHermit = false;
        ComingFromIntro = false;
        ScrollsCollected.Clear();
        NextSceneName = string.Empty;
    }

    public static void AddScroll(string scrollText)
    {
        ScrollsCollected.Add(scrollText);
    }

    public static void SavePlayerHP(int hp, int maxHp)
    {
        PlayerHP = hp;
        PlayerMaxHP = maxHp;
    }
}
