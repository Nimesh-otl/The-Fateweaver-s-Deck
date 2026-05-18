using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ScrollManager : MonoBehaviour
{
    public GameObject scrollPanel;
    public TextMeshProUGUI scrollText;
    public Button continueButton;
    public string scrollContent;
    public string nextSceneName;
    public PlayerData player;
    public RingManager ringManager;

    private Coroutine typeTextCoroutine;

    void Start()
    {
        if (scrollPanel) scrollPanel.SetActive(false);

        if (continueButton)
            continueButton.onClick.AddListener(OnContinuePressed);
    }

    public void ShowScroll()
    {
        if (scrollPanel) scrollPanel.SetActive(true);
        Time.timeScale = 0f;

        if (typeTextCoroutine != null)
            StopCoroutine(typeTextCoroutine);

        typeTextCoroutine = StartCoroutine(TypeText());
    }

    IEnumerator TypeText()
    {
        if (!scrollText)
            yield break;

        scrollText.text = string.Empty;
        string content = scrollContent ?? string.Empty;

        for (int i = 0; i < content.Length; i++)
        {
            scrollText.text += content[i];
            yield return new WaitForSecondsRealtime(0.03f);
        }

        typeTextCoroutine = null;
    }

    void OnContinuePressed()
    {
        if (typeTextCoroutine != null)
        {
            StopCoroutine(typeTextCoroutine);
            typeTextCoroutine = null;
        }

        if (ringManager != null && string.IsNullOrEmpty(nextSceneName))
        {
            RunData.AddScroll(scrollContent ?? string.Empty);

            if (player != null)
                RunData.SavePlayerHP(player.currentHealth, player.maxHealth);

            Time.timeScale = 1f;
            if (scrollPanel) scrollPanel.SetActive(false);
            ringManager.SpawnDoorCard();
            return;
        }

        RunData.AddScroll(scrollContent ?? string.Empty);

        if (player != null)
            RunData.SavePlayerHP(player.currentHealth, player.maxHealth);

        Time.timeScale = 1f;
        SceneManager.LoadScene(nextSceneName);
    }
}
