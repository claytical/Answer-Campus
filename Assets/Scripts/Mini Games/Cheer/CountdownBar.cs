using UnityEngine;
using UnityEngine.UI;

public class CountdownBar : MonoBehaviour
{
    [SerializeField] private Image bg;
    [SerializeField] private Image fill;

    [Header("Colors")]
    public Color bgColor = new Color(0,0,0,0.5f);
    public Color activeColor = Color.white;
    public Color warningColor = new Color(1f, .6f, .2f);
    public Color expiredColor = new Color(.7f, .2f, .2f);
    [Range(0f,1f)] public float warningThreshold = 0.33f;

    private float _duration;
    private double _endDSP;
    private bool _running;

    void Awake()
    {
        if (bg) bg.color = bgColor;
        HideVisualsImmediate();   // keep component active; just hide images
    }

    public void StartWindow(float durationSec)
    {
        _duration = Mathf.Max(0.01f, durationSec);
        _endDSP   = AudioSettings.dspTime + _duration;
        _running  = true;

        // Ensure we are active in case the parent turned us off previously
        if (!gameObject.activeSelf) gameObject.SetActive(true);

        if (bg)   bg.enabled   = true;
        if (fill)
        {
            fill.enabled      = true;
            fill.type         = Image.Type.Filled;
            fill.fillMethod   = Image.FillMethod.Horizontal;
            fill.fillOrigin   = (int)Image.OriginHorizontal.Left;
            fill.fillAmount   = 1f;
            fill.color        = activeColor;
        }
    }

    public void CompleteSuccess()
    {
        if (fill) fill.color = activeColor;
        StopAndHide(0.08f);
    }

    public void CompleteFail()
    {
        if (fill) fill.color = expiredColor;
        StopAndHide(0.18f);
    }

    public void Cancel()
    {
        // no coroutine, just immediately hide visuals
        _running = false;
        HideVisualsImmediate();
    }

    void Update()
    {
        if (!_running) return;

        double now     = AudioSettings.dspTime;
        float remaining = Mathf.Max(0f, (float)(_endDSP - now));
        float t         = Mathf.Clamp01(remaining / _duration); // 1 → 0

        if (fill)
        {
            fill.fillAmount = t;
            fill.color = (t <= warningThreshold && t > 0f)
                ? warningColor
                : (t > warningThreshold ? activeColor : expiredColor);
        }

        if (remaining <= 0f) _running = false;
    }

    private void StopAndHide(float delay)
    {
        _running = false;

        // If we’re not active/enabled, don’t try to schedule a coroutine; just hide now
        if (!isActiveAndEnabled || delay <= 0f)
        {
            HideVisualsImmediate();
            return;
        }

        // Small delayed hide for a nicer flash
        StopAllCoroutines();
        StartCoroutine(HideAfter(delay));
    }

    private System.Collections.IEnumerator HideAfter(float s)
    {
        yield return new WaitForSeconds(s);
        HideVisualsImmediate();
    }

    private void HideVisualsImmediate()
    {
        if (fill)
        {
            fill.fillAmount = 0f;
            fill.enabled    = false;
        }
        if (bg) bg.enabled = false;
        // IMPORTANT: do NOT SetActive(false) here
    }
}
