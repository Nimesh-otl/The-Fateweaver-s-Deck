using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StoryRevealManager : MonoBehaviour
{
    public TextMeshProUGUI storyText;
    public TextMeshProUGUI pathText;
    public Button playAgainButton;

    [TextArea(10, 20)]
    public string hermitEndingText;

    [TextArea(10, 20)]
    public string devilEndingText;

    void Start()
    {
        Debug.Log("[Tower] ChoseHermit = " + RunData.ChoseHermit);
        // rest of your code
        if (pathText)
            pathText.text = RunData.ChoseHermit
                ? "You walked the path of the Hermit"
                : "You walked the path of the Devil";

        if (storyText)
            storyText.text = RunData.ChoseHermit ? hermitEndingText : devilEndingText;

        if (playAgainButton)
            playAgainButton.onClick.AddListener(OnPlayAgainPressed);
    }

    void OnPlayAgainPressed()
    {
        RunData.Reset();
        SceneManager.LoadScene("The_Fool");
    }
}