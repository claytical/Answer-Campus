using UnityEngine;
using UnityEngine.UI;

public class TimerBar : MonoBehaviour
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
        HideImmediate();
    }

    public void StartWindow(float durationSec)
    {
        _duration = Mathf.Max(0.01f, durationSec);
        _endDSP = AudioSettings.dspTime + _duration;
        _running = true;

        gameObject.SetActive(true);
        if (fill) { fill.type = Image.Type.Filled; fill.fillMethod = Image.FillMethod.Horizontal; fill.fillOrigin = (int)Image.OriginHorizontal.Left; fill.fillAmount = 1f; fill.color = activeColor; }
    }

    public void CompleteSuccess() { if (fill) fill.color = activeColor; StopAndHide(0.08f); }
    public void CompleteFail()    { if (fill) fill.color = expiredColor; StopAndHide(0.18f); }
    public void Cancel()          { StopAndHide(0f); }

    void Update()
    {
        if (!_running) return;

        double now = AudioSettings.dspTime;
        float remaining = Mathf.Max(0f, (float)(_endDSP - now));
        float t = Mathf.Clamp01(remaining / _duration); // 1 → 0

        if (fill)
        {
            fill.fillAmount = t;
            fill.color = (t <= warningThreshold && t > 0f) ? warningColor : (t > warningThreshold ? activeColor : expiredColor);
        }

        if (remaining <= 0f) _running = false;
    }

    private void StopAndHide(float delay)
    {
        _running = false;
        if (delay <= 0f) { HideImmediate(); }
        else { StopAllCoroutines(); StartCoroutine(HideAfter(delay)); }
    }
    private System.Collections.IEnumerator HideAfter(float s) { yield return new WaitForSeconds(s); HideImmediate(); }
    private void HideImmediate() { if (fill) fill.fillAmount = 0f; gameObject.SetActive(false); }
}
