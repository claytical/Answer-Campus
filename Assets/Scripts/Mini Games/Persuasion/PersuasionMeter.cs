using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PersuasionMeter : MonoBehaviour
{
    [Header("References")]
    public Image dissuadeFill;
    public Image persuadeFill;
    public PersuasionQTE persuasionQTE;

    [Header("Config")]
    [Range(0f,1f)]
    public float currentValue = 0.5f;   // 0 = fully red, 1 = fully green, starts at middle
    public float failPenalty = 0.1f;    // How much to add to dissuade on failed QTE
    public float successGain = 0.1f;    // How much to add to persuaded on successful QTE
    public float passiveDecay = 0.01f;  // How fast dissuaded depletes per second


    private void OnEnable() {
        if (persuasionQTE != null) {
            persuasionQTE.onSuccess.AddListener(ApplySuccess);
            persuasionQTE.onFail.AddListener(ApplyFail);
        }
    }

    private void OnDisable() {
        if (persuasionQTE != null) {
            persuasionQTE.onSuccess.RemoveListener(ApplySuccess);
            persuasionQTE.onFail.RemoveListener(ApplyFail);
        }
    }

    void Update() {
        currentValue -= passiveDecay * Time.deltaTime;
        currentValue = Mathf.Clamp01(currentValue);

        UpdateMeter();
    }

    public void ApplySuccess() {
        currentValue += successGain;
        currentValue = Mathf.Clamp01(currentValue);
        Debug.Log($"[PersuasionMeter] Success! CurrentValue={currentValue}");
        UpdateMeter();

    }

    public void ApplyFail() {
        currentValue -= failPenalty;
        currentValue = Mathf.Clamp01(currentValue);
        Debug.Log($"[PersuasionMeter] Fail! CurrentValue={currentValue}");
        UpdateMeter();
    }

    private void UpdateMeter() {
        dissuadeFill.fillAmount = 1f - currentValue;
        persuadeFill.fillAmount = currentValue;
    }
}
