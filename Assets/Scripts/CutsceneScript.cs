using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FMODUnity;
using FMOD.Studio;

public class CutsceneScript : MonoBehaviour
{
    public GameObject cutScene;
    public TextMeshProUGUI display;
    public string[] sentences;
    public float timeBetweenSentences;

    public GameObject exterior;
    public GameObject dialogueManager;
    public GameObject story;

    public bool skip = false;

    private int sentenceIndex = 0;
    private bool cutsceneEnded = false;

    private Coroutine cutsceneRoutine;

    private EventInstance fmodEvent;

    void Start()
    {
        if (sentences.Length > 0)
        {
            display.text = sentences[0];
        }
        cutsceneRoutine = StartCoroutine(RunCutscene());
    }

    void Update()
    {
        if (skip && !cutsceneEnded)
        {
            StopCoroutine(cutsceneRoutine);
            StartCoroutine(SkipToEnd());
        }
    }

    IEnumerator RunCutscene()
    {
        for (sentenceIndex = 1; sentenceIndex < sentences.Length; sentenceIndex++)
        {
            yield return new WaitForSeconds(timeBetweenSentences);
            display.text = sentences[sentenceIndex];
        }

        yield return new WaitForSeconds(timeBetweenSentences);
        StartCoroutine(EndCutscene());
    }

    IEnumerator SkipToEnd()
    {
        cutsceneEnded = true;

        display.gameObject.SetActive(false);
        exterior.SetActive(true);

        yield return new WaitUntil(() =>
        {
            var img = exterior.GetComponent<Image>();
            return img == null || img.color == Color.black;
        });

        //StartCoroutine(FadeOutFMOD());
        EndSequence();
    }

    IEnumerator EndCutscene()
    {
        display.gameObject.SetActive(false);
        exterior.SetActive(true);

        yield return new WaitUntil(() =>
        {
            var img = exterior.GetComponent<Image>();
            return img == null || img.color == Color.black;
        });

        EndSequence();
    }

    void EndSequence()
    {
        cutScene.SetActive(false);
        dialogueManager.SetActive(true);
        story.SetActive(true);
        cutsceneEnded = true;
    }

    IEnumerator FadeOutFMOD()
    {
        float duration = 1f;
        float currentTime = 0f;
        float startVolume;

        fmodEvent.getVolume(out startVolume);

        while (currentTime < duration)
        {
            currentTime += Time.deltaTime;
            float newVolume = Mathf.Lerp(startVolume, 0, currentTime / duration);
            fmodEvent.setVolume(newVolume);
            yield return null;
        }

        fmodEvent.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
        fmodEvent.release();
    }
}
