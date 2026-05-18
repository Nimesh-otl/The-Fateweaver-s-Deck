using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class IntroManager : MonoBehaviour
{
    public GameObject lineTemplate;
    public Transform linesContainer;
    public float fadeInDuration = 1.0f;
    public float delayBetweenLines = 1.2f;
    public float holdAfterLastLine = 2.5f;
    public float fadeOutDuration = 1.5f;

    private readonly string[] lines =
    {
        "The threads are broken.",
        "For as long as anyone can remember, the Weaver",
        "held everything together. Every life, every choice,",
        "every small moment that felt like coincidence was",
        "a thread in a tapestry older than time itself.",
        "Then one morning the loom stood empty.",
        "The threads have been unraveling ever since.",
        "You are the Fool. You know what that means.",
        "You are the beginning of something, or the end of it.",
        "The question is which one.",
        "So then. Will you find out what happened?",
        "Or will you live up to your name."
    };

    private readonly List<TextMeshProUGUI> spawnedLines = new List<TextMeshProUGUI>();

    void Start()
    {
        StartCoroutine(PlayIntro());
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            StopAllCoroutines();
            RunData.ComingFromIntro = true;
            SceneManager.LoadScene("The_Fool");
        }
    }

    IEnumerator PlayIntro()
    {
        for (int i = 0; i < lines.Length; i++)
        {
            TextMeshProUGUI line = SpawnLine(lines[i]);
            spawnedLines.Add(line);
            yield return StartCoroutine(FadeLine(line, 0f, 1f, fadeInDuration));
            yield return new WaitForSeconds(delayBetweenLines);
        }

        yield return new WaitForSeconds(holdAfterLastLine);

        float elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeOutDuration);
            float alpha = Mathf.Lerp(1f, 0f, t);
            for (int i = 0; i < spawnedLines.Count; i++)
            {
                if (spawnedLines[i] != null)
                {
                    Color color = spawnedLines[i].color;
                    color.a = alpha;
                    spawnedLines[i].color = color;
                }
            }
            yield return null;
        }

        RunData.ComingFromIntro = true;
        SceneManager.LoadScene("The_Fool");
    }

    TextMeshProUGUI SpawnLine(string text)
    {
        GameObject instance = Instantiate(lineTemplate, linesContainer);
        instance.SetActive(true);
        TextMeshProUGUI tmp = instance.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        Color color = tmp.color;
        color.a = 0f;
        tmp.color = color;
        return tmp;
    }

    IEnumerator FadeLine(TextMeshProUGUI line, float fromAlpha, float toAlpha, float duration)
    {
        float elapsed = 0f;
        Color color = line.color;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            color.a = Mathf.Lerp(fromAlpha, toAlpha, t);
            line.color = color;
            yield return null;
        }
    }
}
