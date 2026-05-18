using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TutorialManager : MonoBehaviour
{
    public GameObject tutorialOverlay;
    public TextMeshProUGUI instructionText;
    public Button nextButton;

    private bool clicked;

    void Awake()
    {
        if (nextButton)
            nextButton.onClick.AddListener(OnNextClicked);
    }

    IEnumerator Start()
    {
        if (SceneManager.GetActiveScene().name != "The_Fool")
            yield break;

        if (PlayerPrefs.GetInt("TutorialSeen", 0) == 1)
            yield break;

        yield return null;
        yield return null;
    }

    IEnumerator RunTutorial()
    {
        if (tutorialOverlay) tutorialOverlay.SetActive(true);
        Time.timeScale = 0f;

        string[] lines =
        {
            "Your HP bar is in the top left. Keep it above zero.",
            "Your hand cards are at the bottom. They give you abilities in combat.",
            "Use the Left and Right arrow buttons on the sides to rotate the ring.",
            "Bring an enemy card to the center and click it to start combat.",
            "During combat, use your hand cards from the bottom for special effects.",
            "That is all, Fool. The rest you will figure out yourself."
        };

        for (int i = 0; i < lines.Length; i++)
        {
            clicked = false;
            if (instructionText) instructionText.text = lines[i];
            while (!clicked)
            {
                yield return null;
            }
        }

        if (tutorialOverlay) tutorialOverlay.SetActive(false);
        Time.timeScale = 1f;
        PlayerPrefs.SetInt("TutorialSeen", 1);
        PlayerPrefs.Save();
    }

    public void StartTutorial()
    {
        StartCoroutine(RunTutorial());
    }

    void OnNextClicked()
    {
        clicked = true;
    }
}
