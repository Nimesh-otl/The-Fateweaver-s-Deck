using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// Controls start menu, pause menu, and options menu flow.
public class GameMenuManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject startMenuPanel;
    public GameObject pauseMenuPanel;
    public GameObject optionsPanel;

    public Slider volumeSlider;
    public Toggle musicToggle;

    [Header("Start Menu Buttons")]
    public Button startButton;
    public Button continueButton;
    public Button startOptionsButton;
    public Button startQuitButton;

    [Header("Pause Menu Buttons")]
    public Button pausePlayButton;
    public Button pauseOptionsButton;
    public Button pauseQuitButton;

    [Header("Options Buttons")]
    public Button optionsBackButton;

    private bool gameStarted;
    private bool isPaused;
    private bool optionsOpenedFromPause;

    [Header("Cursor")]
    public Texture2D customCursor;
    public Vector2 cursorHotspot = Vector2.zero;

    void Start()
    {
        if (customCursor)
        {
            Cursor.SetCursor(customCursor, cursorHotspot, CursorMode.Auto);
            Debug.Log("Cursor set");
        }
        else
            Debug.Log("Cursor is null - not assigned");
        if (startButton) startButton.onClick.AddListener(OnStartPressed);
        if (continueButton) continueButton.onClick.AddListener(OnContinueGamePressed);
        if (startOptionsButton) startOptionsButton.onClick.AddListener(OpenOptionsFromStartMenu);
        if (startQuitButton) startQuitButton.onClick.AddListener(QuitGame);

        if (continueButton)
            continueButton.gameObject.SetActive(SaveSystem.LoadExists());

        if (pausePlayButton) pausePlayButton.onClick.AddListener(ResumeGame);
        if (pauseOptionsButton) pauseOptionsButton.onClick.AddListener(OpenOptionsFromPauseMenu);
        if (pauseQuitButton) pauseQuitButton.onClick.AddListener(QuitToMainMenu);

        if (optionsBackButton) optionsBackButton.onClick.AddListener(CloseOptions);

        if (volumeSlider != null)
        {
            volumeSlider.value = 0.75f;
            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        }

        if (musicToggle != null)
        {
            musicToggle.isOn = true;
            musicToggle.onValueChanged.AddListener(OnMusicToggled);
        }

        if (SceneManager.GetActiveScene().name == "The_Fool" && !RunData.ComingFromIntro)
        {
            ShowStartMenu();
        }
        else
        {
            RunData.ComingFromIntro = false;
            gameStarted = true;
            isPaused = false;
            optionsOpenedFromPause = false;

            if (startMenuPanel) startMenuPanel.SetActive(false);
            if (pauseMenuPanel) pauseMenuPanel.SetActive(false);
            if (optionsPanel) optionsPanel.SetActive(false);

            Time.timeScale = 1f;
            TutorialManager tutorialManager = FindObjectOfType<TutorialManager>();
            if (tutorialManager != null)
                tutorialManager.StartTutorial();
        }
    }

    void Update()
    {
        TutorialManager tutorialManager = FindObjectOfType<TutorialManager>();
        if (tutorialManager != null && tutorialManager.tutorialOverlay != null && tutorialManager.tutorialOverlay.activeSelf)
            return;
        if (!gameStarted) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (optionsPanel && optionsPanel.activeSelf)
            {
                CloseOptions();
                return;
            }

            if (isPaused)
                ResumeGame();
            else
                PauseGame();
        }
    }

    void ShowStartMenu()
    {
        gameStarted = false;
        isPaused = true;
        optionsOpenedFromPause = false;

        if (startMenuPanel) startMenuPanel.SetActive(true);
        if (pauseMenuPanel) pauseMenuPanel.SetActive(false);
        if (optionsPanel) optionsPanel.SetActive(false);

        Time.timeScale = 0f;
    }

    void OnStartPressed()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Intro");
    }

    void OnContinueGamePressed()
    {
        Debug.Log("Save exists: " + SaveSystem.LoadExists());
        string sceneName = SaveSystem.LoadGame();
        Debug.Log("Loaded scene name: " + sceneName);
        if (!string.IsNullOrEmpty(sceneName))
            SceneManager.LoadScene(sceneName);
    }

    void PauseGame()
    {
        isPaused = true;
        optionsOpenedFromPause = false;

        if (pauseMenuPanel) pauseMenuPanel.SetActive(true);
        if (optionsPanel) optionsPanel.SetActive(false);

        Time.timeScale = 0f;
    }

    public void ResumeGame()
    {
        isPaused = false;

        if (pauseMenuPanel) pauseMenuPanel.SetActive(false);
        if (optionsPanel) optionsPanel.SetActive(false);

        Time.timeScale = 1f;
    }

    void OpenOptionsFromStartMenu()
    {
        optionsOpenedFromPause = false;
        if (startMenuPanel) startMenuPanel.SetActive(false);
        if (pauseMenuPanel) pauseMenuPanel.SetActive(false);
        if (optionsPanel) optionsPanel.SetActive(true);
    }

    void OpenOptionsFromPauseMenu()
    {
        optionsOpenedFromPause = true;
        if (pauseMenuPanel) pauseMenuPanel.SetActive(false);
        if (startMenuPanel) startMenuPanel.SetActive(false);
        if (optionsPanel) optionsPanel.SetActive(true);
    }

    void OnVolumeChanged(float value)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetMasterVolume(value);
    }

    void OnMusicToggled(bool isOn)
    {
        if (AudioManager.Instance != null)
        {
            if (isOn)
                AudioManager.Instance.musicSource.UnPause();
            else
                AudioManager.Instance.musicSource.Pause();
        }
    }

    public void CloseOptions()
    {
        if (optionsPanel) optionsPanel.SetActive(false);

        if (!gameStarted)
        {
            if (startMenuPanel) startMenuPanel.SetActive(true);
            return;
        }

        if (optionsOpenedFromPause)
        {
            if (pauseMenuPanel) pauseMenuPanel.SetActive(true);
            return;
        }

        if (startMenuPanel) startMenuPanel.SetActive(true);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void QuitToMainMenu()
    {
        Time.timeScale = 1f;
        RunData.Reset();
        SceneManager.LoadScene("The_Fool");
    }
}
