using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using FMODUnity;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using VNEngine;
using Random = UnityEngine.Random;

[Serializable]
public class FootballTeam
{
    public string schoolName;
    public string mascot;
    public bool isRival = false;

    public FootballTeam(string name, string mascot)
    {
        this.schoolName = name;
        this.mascot = mascot;
    }
}
[Serializable]
public class FootballGame
{
    public int week;
    public FootballTeam opponent;
    public bool isHome;
    public bool played;
    public bool won; // null = not played yet
    
}

public static class CheerQuarterScoring
{
    private const float pNoScore   = 0.62f; // most drives die
    private const float pFG        = 0.18f; // 3
    private const float pTD_XP     = 0.16f; // 7
    private const float pTD_MissXP = 0.02f; // 6
    private const float pTD_2pt    = 0.01f; // 8
    private const float pSafety    = 0.01f; // 2
    private const float tdShareWhenScoring = 0.58f;           // ~58% TD, 42% FG among scoring drives
    private const float xpMissRate = 0.04f;                   // 4% missed XP
    private const float twoPtRate  = 0.02f;                   // 2% 2-pt tries (on TDs), success assumed
    private const float safetyRate = 0.01f;                   // rare, independent small chance per drive
    private const bool  guaranteeAtLeastFieldGoalIfAnyDrives = true;
    // Soft caps keep blowouts down
    private const int softCapPointsPerTeam = 14;  // typical Q1 upper bound
    private const int hardCapPointsPerTeam = 21;  // never exceed

    // Optional: pace corrector to keep totals near a target when drive count varies
    private const float targetCombinedPointsPerQuarter = 10f; // both teams together

public static void ScoreQuarter(int totalCombos, int combosMade, out int homeScore, out int awayScore)
    {
        homeScore = 0; awayScore = 0;

        totalCombos = Mathf.Max(0, totalCombos);
        combosMade  = Mathf.Clamp(combosMade, 0, totalCombos);
        if (totalCombos == 0) return;

        int homeDrives = combosMade;
        int awayDrives = totalCombos - combosMade;

        // Derive per-drive scoring probability from target
        // Expected points *per scoring drive* (mix of TDs/FGs):
        //   E_pts_if_score ≈ FG*3 + TD*(7 - xpMiss*1 + twoPt*1)
        float ePtsIfScore = (1f - tdShareWhenScoring) * 3f
                          + tdShareWhenScoring * (7f - xpMissRate * 1f + twoPtRate * 1f); // ≈ 4.6–4.9

        float pScore = targetCombinedPointsPerQuarter / Mathf.Max(1f, totalCombos * ePtsIfScore);
        pScore = Mathf.Clamp01(pScore);           // 0..1, usually ~0.15–0.35
        float pFG = (1f - tdShareWhenScoring) * pScore;
        float pTD = tdShareWhenScoring * pScore;
        float pNo = 1f - (pFG + pTD);             // remainder is no-score

        // Score each team
        homeScore = ScoreTeam(homeDrives, pNo, pFG, pTD);
        awayScore = ScoreTeam(awayDrives, pNo, pFG, pTD);

        // Gentle sanity floor so active quarters don’t end 0–0
        if (guaranteeAtLeastFieldGoalIfAnyDrives)
        {
            if (homeDrives > 0 && homeScore == 0) homeScore = 3;
            if (awayDrives > 0 && awayScore == 0) awayScore = 3;
        }
    }

    private static int ScoreTeam(int drives, float pNo, float pFG, float pTD)
    {
        int pts = 0;
        for (int i = 0; i < drives; i++)
        {
            // Safety: tiny, independent chance even on "no-score" drives
            if (Random.value < safetyRate) { pts += 2; continue; }

            float r = Random.value;
            if ((r -= pNo) < 0f) continue;            // no score
            if ((r -= pFG) < 0f) { pts += 3; continue; } // field goal

            // Touchdown branch
            // Optionally mix XP miss vs 2-pt try
            float rTD = Random.value;
            if (rTD < twoPtRate)      pts += 8;
            else if (rTD < twoPtRate + xpMissRate) pts += 6;
            else                       pts += 7;
        }
        return pts;
    }
    private static int ScoreOneDrive(int currentTeamQuarterPoints, float paceFactor)
    {
        // Bias explosive plays down as points rise (end-of-quarter conservatism)
        float fatigue = Mathf.InverseLerp(0, softCapPointsPerTeam, currentTeamQuarterPoints);

        float noScore = pNoScore * Mathf.Lerp(1f, 1.3f, fatigue) * (1f / Mathf.Max(0.3f, paceFactor));
        float fg      = pFG      * Mathf.Lerp(1f, 0.95f, fatigue) * paceFactor;
        float tdXp    = pTD_XP   * Mathf.Lerp(1f, 0.8f,  fatigue) * paceFactor;
        float tdMiss  = pTD_MissXP * Mathf.Lerp(1f, 0.8f, fatigue) * paceFactor;
        float td2     = pTD_2pt  * Mathf.Lerp(1f, 0.6f,  fatigue) * paceFactor;
        float safety  = pSafety; // keep tiny & flat

        float sum = noScore + fg + tdXp + tdMiss + td2 + safety;
        noScore /= sum; fg /= sum; tdXp /= sum; tdMiss /= sum; td2 /= sum; safety /= sum;

        float r = Random.value;
        if ((r -= noScore) < 0f) return 0;
        if ((r -= fg)     < 0f) return 3;
        if ((r -= tdXp)   < 0f) return 7;
        if ((r -= tdMiss) < 0f) return 6;
        if ((r -= td2)    < 0f) return 8;
        return 2;
    }
}

public enum CheerDirection { Up, Down, Left, Right }

public enum CheerCombo
{
    Default,
    LeftDown,
    LeftRight,
    LeftUp,
    RightDown,
    RightUp,
    UpDown
}

[Serializable]
public class MatchSequence
{
    public CheerLeader cheerleader;  // Reference to the Cheer controller
}
[Serializable]
public struct CheerLeader
{
    public Cheer cheer;         // The cheerleader (pose logic)
    public Image glyphA;        // Left directional arrow
    public Image glyphB;        // Right directional arrow
    public CountdownBar countdownBar;

}

[Serializable]
public class CheerClip
{
    public int countdownParameter;
    public int gameClipParameter;

    public float[] beatTimes; // One timestamp (in seconds) per cheer
}

public class CheerGameManager : MonoBehaviour
{
    [Header("UI References")] public GameObject maqrueePanel;
    public GameObject playableGameRoot;
    //public TextMeshProUGUI comboDisplayText;
    public GameObject scoreboardUI;
    public AudioSource audioSource;
    public EventReference crowd;
    public EventReference countdownEvent;
    public EventReference gameEvent;
    public AudioClip introClip;
    public TextMeshProUGUI homeScoreText;
    public TextMeshProUGUI awayScoreText;
    public TextMeshProUGUI awayTeamNameText;
    public TextMeshProUGUI scoreboardAwayTeamText;
    public TextMeshProUGUI quarterText;
    [SerializeField] public List<MatchSequence> matchSequences;

    [Header("Crowd Feedback")]
    public float amplification = 0f;
    public float amplificationPerSuccess = 0.2f;
    public float amplificationDecayRate = 0.05f;

    [Header("Visual Feedback")]
    public Image[] leaderImages;
    public RectTransform spotlight;
    public Image glyphA, glyphB;
    public Vector2 glyphOffsetA = new Vector2(-40, 80);
    public Vector2 glyphOffsetB = new Vector2(40, 80);

    public Sprite upGlyph, downGlyph, leftGlyph, rightGlyph;
    public Sprite failUpGlyph, failDownGlyph, failLeftGlyph, failRightGlyph;

// Existing colors (keep as-is)
    public Color dimColor = new Color(1,1,1,0.4f);
    public Color highlightColor = Color.white;
    public Color successColor = Color.green;
    public Color failColor = Color.red;

// NEW: tint used during the input window
    public Color comboWindowColor = Color.blue;

    private Dictionary<int, (CheerDirection a, CheerDirection b)> activeDirections = new();
    private readonly ConcurrentQueue<double> _beatQueue = new();
    private double _cheerAnchorDSP;
    private bool _roundActive;
    [SerializeField] private float inputWindowSeconds = 1.0f;   // base window (tweak for difficulty)
    [SerializeField] private float fadeAfterSuccess = 0.25f;    // quick flash time
    [SerializeField] private float fadeAfterFail    = 0.35f;

    private int _markerLeaderIdx = 0;
    private bool _markerBusy = false;
    private Coroutine _markerCo;
    private System.Random _rng = new System.Random();
    [Header("Game Variables")]
    public CheerClip[] cheers;
    private int homeScore = 0;
    private int awayScore = 0;
    private int currentQuarter = 1;
    private CheerClip selectedCheerClip;
    private int _lastCheerIdx = -1;
    private int _beatsProcessedThisRound = 0;
    [Header("Round Settings")]
    public float comboDisplayTime = 1f;
    private int combosMade = 0;
    public int gameNumber = 0;
    public int weekNumber = 0;
    string awayTeam; 
    private readonly Dictionary<int, Coroutine> countdowns = new();
    private bool gotTwo;
    private double _fmodBaseDSP = double.NaN; // computed from first beat
    private double _lastBeatDSP;
    private int _debugBeatCount;
    private int _debugMarkerCount;
    private int _lastCueStep;
    private double _anchorDSP;
    private readonly ConcurrentQueue<double> _cueQueue = new();
    private double _baseDSP; // aligns FMOD timeline ms to Unity DSP seconds
    private FMOD.Studio.EventInstance _activeCheer;
    
    int home, away;
    private static readonly Dictionary<CheerCombo, CheerDirection[]> comboMap = new()
    {
        { CheerCombo.LeftDown,  new[]{ CheerDirection.Left,  CheerDirection.Down  } },
        { CheerCombo.LeftRight, new[]{ CheerDirection.Left,  CheerDirection.Right } },
        { CheerCombo.LeftUp,    new[]{ CheerDirection.Left,  CheerDirection.Up    } },
        { CheerCombo.RightDown, new[]{ CheerDirection.Right, CheerDirection.Down  } },
        { CheerCombo.RightUp,   new[]{ CheerDirection.Right, CheerDirection.Up    } },
        { CheerCombo.UpDown,    new[]{ CheerDirection.Up,    CheerDirection.Down  } },
    };
    private Coroutine _paramTracerCo;
    private Coroutine _watchCueRoutine;
    private Coroutine _runRoundRoutine;
    private int opportunitiesThisRound = 0; // total “drives” shown (each step is one)
    private int markersThisRound = 0;

    void Start()
    {
        weekNumber = (int)StatsManager.Get_Numbered_Stat("Week");
        FootballGame game = FootballScheduler.GetThisWeeksGame(weekNumber);
        FMODAudioManager.Instance.PlayMusic(crowd);
        
        if (game != null && game.isHome && !game.played)
        {
            // Trigger cheer mini-game
            // You can access: game.opponent
            awayTeamNameText.text = game.opponent.schoolName;
            scoreboardAwayTeamText.text = game.opponent.mascot;
            FMODAudioManager.Instance.PlayMusic(crowd);
        }
        else
        {
            awayTeamNameText.text = "Everton Eagles";
            scoreboardAwayTeamText.text = "OTHER TEAM'S MASCOT";

        Debug.Log($"No game this week, going home...faking it");
        }
        
        StartCoroutine(GameFlowRoutine());
    }
// Add/replace inside CheerGameManager

private void CleanupCheerForQuarterEnd()
{
    // stop anything we started
    if (_watchCueRoutine != null) { StopCoroutine(_watchCueRoutine); _watchCueRoutine = null; }
    if (_runRoundRoutine != null) { StopCoroutine(_runRoundRoutine); _runRoundRoutine = null; }
    if (_markerCo != null)       { StopCoroutine(_markerCo);        _markerCo = null; }
    _markerBusy = false;                 // <<< important: allow next marker
    _markerLeaderIdx = 0;                // <<< important: start next quarter on first leader

    // stop FMOD event if it’s still going
    if (_activeCheer.isValid())
    {
        _activeCheer.getPlaybackState(out var st);
        if (st != FMOD.Studio.PLAYBACK_STATE.STOPPED)
            _activeCheer.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
        _activeCheer.release();
    }
    _roundActive = false;

    // clear timing/cue state
    _baseDSP = double.NaN;
    _lastCueStep = 0;
    while (_cueQueue.TryDequeue(out _)) {}

    // reset round counters
    _beatsProcessedThisRound = 0;
    combosMade = 0;
    opportunitiesThisRound = 0;
    markersThisRound = 0;

    // hide & reset UI
    ResetAllLeadersVisuals();
    for (int i = 0; i < matchSequences.Count; i++)
    {
        ResetGlyphTintAndHide(i);
        matchSequences[i].cheerleader.countdownBar?.Cancel();
    }
    spotlight?.gameObject.SetActive(false);

    // clear input buffers
    CheerInputBridge.Instance?.Clear();
}

private void ResetAllLeadersVisuals()
{
    // Clear leader UI state so nothing “sticks” between quarters
    if (matchSequences == null) return;

    for (int i = 0; i < matchSequences.Count; i++)
    {
        var cl = matchSequences[i].cheerleader;

        if (cl.glyphA)
        {
            cl.glyphA.sprite = null;
            cl.glyphA.color = highlightColor;
            cl.glyphA.gameObject.SetActive(false);
        }
        if (cl.glyphB)
        {
            cl.glyphB.sprite = null;
            cl.glyphB.color = highlightColor;
            cl.glyphB.gameObject.SetActive(false);
        }

        if (cl.cheer) cl.cheer.SetCombo(CheerCombo.Default);
        activeDirections.Remove(i);
    }

    UnhighlightAll();
}

    IEnumerator GameFlowRoutine()
    {
        yield return StartCoroutine(FadeFromBlack());
        maqrueePanel.SetActive(false);
        if (playableGameRoot != null) playableGameRoot.SetActive(false);

        for (int q = 1; q <= 4; q++)
        {
            currentQuarter = q;
            UpdateScoreboardUI();

            scoreboardUI.SetActive(true);
            playableGameRoot.SetActive(false);
            if (introClip != null)
            {
                //audioSource.PlayOneShot(introClip);
                yield return new WaitForSeconds(introClip.length);
            }
            CleanupCheerForQuarterEnd();
            scoreboardUI.SetActive(false);
            playableGameRoot.SetActive(true);
            if (playableGameRoot != null) playableGameRoot.SetActive(true);
            int cheerIndex = PickCheerIndex();
            int authoredMaxCheer = 1; // you currently have CHEER0 and CHEER1
            if (cheerIndex > authoredMaxCheer)
            {
                Debug.LogWarning($"[CHEER] Picked Cheer={cheerIndex} but only 0..{authoredMaxCheer} are authored. Clamping.");
                cheerIndex = Mathf.Clamp(cheerIndex, 0, authoredMaxCheer);
            }
            selectedCheerClip = cheers[cheerIndex];
            Debug.Log($"[CHEER] Starting Band Cheer with Cheer={cheerIndex}");
            Debug.Log($"Playing Event: {countdownEvent.ToString()}");
            yield return StartCoroutine(FMODAudioManager.Instance.PlayOneShotAndWaitPrecise(
                countdownEvent,
                "Countdown",
                selectedCheerClip.countdownParameter
            ));
            
            // Wait one frame to ensure playback is initialized
            yield return null;
            _baseDSP = double.NaN;
            _lastCueStep = 0;
            while (_cueQueue.TryDequeue(out _)) {}

// IMPORTANT: mark active before watcher starts
            _roundActive = true;
            _activeCheer = FMODAudioManager.Instance.StartEventWithTimeline(
                evt: gameEvent,                    // the single Band Cheer event
                onMarker: OnMarker,                   
                onStopped: () => { _roundActive = false; },
                paramName: "Cheer",
                paramValue: cheerIndex,            // <<< choose which section
                attachTo: (Camera.main ? Camera.main.transform : this.transform),
                ignoreSeekSpeed: true
            );
            _watchCueRoutine = StartCoroutine(WatchCueStep(_activeCheer));

            // Wait until the cheer event ends (onStopped sets _roundActive = false)
            yield return new WaitWhile(() => _roundActive);

            // Score using the marker-driven counters
            FinalizeQuarterScore(opportunitiesThisRound, combosMade);

            UpdateScoreboardUI();

            CleanupCheerForQuarterEnd();
            combosMade = 0;
            yield return new WaitForSeconds(1f);
        }

        EndGame();
    }

    private void OnMarker(string name, int positionMs)
    {

        if (!_roundActive) return;
        if (_markerBusy)   return; // ignore overlapping markers if a sequence is in flight
        markersThisRound++;
        // rotate leader each marker
        int leaderIdx = _markerLeaderIdx % Mathf.Max(1, matchSequences.Count);
        _markerLeaderIdx++;

        _markerBusy = true;
        if (_markerCo != null) StopCoroutine(_markerCo);
        _markerCo = StartCoroutine(RunMarkerTwoStepSequence(leaderIdx));
    }
    private IEnumerator RunMarkerTwoStepSequence(int leaderIdx)
{
    // Safety
    if (matchSequences == null || matchSequences.Count == 0)
    {
        _markerBusy = false;
        yield break;
    }

    // Visual focus on leader
    HighlightLeader(leaderIdx);
    MoveSpotlight(leaderIdx); // both exist already
    var leader = matchSequences[leaderIdx].cheerleader;
    var leaderCheer = matchSequences[leaderIdx].cheerleader.cheer;
    leader.countdownBar?.StartWindow(inputWindowSeconds);
    // STEP 1: Direction
    var dirA = RandomDir();
    var dirB = RandomDirNot(dirA);
    ResetGlyphTintAndHide(leaderIdx);
    SetGlyphVisible(leaderIdx, true, false);
    SetGlyphSpriteAndColor(leaderIdx, true, dirA, highlightColor);
   
    if (leaderCheer) leaderCheer.SetCombo(AnticipationForDir(dirA));
    yield return null; 
    // Open window #1
    CheerInputBridge.Instance.Clear(); // reset queue for a clean take
    opportunitiesThisRound++;
    double deadline1 = AudioSettings.dspTime + inputWindowSeconds;
    bool gotA   = false;
    bool rightA = false;
    while (AudioSettings.dspTime < deadline1 && !gotA)
    {
        if (CheerInputBridge.Instance.TryGetNextDirection(out var d, out var ts))
        {
            gotA   = true;
            rightA = (d == dirA);
        }
        yield return null;
    }

    // Feedback #1
    if (rightA)
    {
        SetGlyphSpriteAndColor(leaderIdx, true, dirA, successColor);
        if (leaderCheer) leaderCheer.SetCombo(PoseForDir(dirA));
        amplification += amplificationPerSuccess; // your crowd amp
        combosMade++;                             // your scoring
        leader.countdownBar?.CompleteSuccess();
        yield return new WaitForSecondsRealtime(fadeAfterSuccess);
    }
    else
    {
        // set fail-colored version of the same dir
        SetGlyphSpriteAndColor(leaderIdx, true, dirA, failColor);
        leader.countdownBar?.CompleteFail();
        yield return new WaitForSecondsRealtime(fadeAfterFail);
    }

    // STEP 2: Second direction (must differ)
    SetGlyphVisible(leaderIdx, true, true); // show both now
    SetGlyphSpriteAndColor(leaderIdx, false,  dirB, highlightColor);

    if (leaderCheer) leaderCheer.SetCombo(AnticipationForDir(dirB)); 
    CheerInputBridge.Instance.Clear();
    opportunitiesThisRound++;
    double deadline2 = AudioSettings.dspTime + inputWindowSeconds;
    bool gotB   = false;
    bool rightB = false;
    while (AudioSettings.dspTime < deadline2 && !gotB)
    {
        if (CheerInputBridge.Instance.TryGetNextDirection(out var d, out var ts))
        {
            gotB   = true;
            rightB = (d == dirB);
        }
        yield return null;
    }

    // Feedback #2
    if (rightB)
    {
        SetGlyphSpriteAndColor(leaderIdx, false, dirB, successColor);
        if (leaderCheer) leaderCheer.SetCombo(PoseForDir(dirB));
        amplification += amplificationPerSuccess;
        combosMade++;
        leader.countdownBar?.CompleteSuccess();
        yield return new WaitForSecondsRealtime(fadeAfterSuccess);
    }
    else
    {
        SetGlyphSpriteAndColor(leaderIdx, false, dirB, failColor);
        leader.countdownBar?.CompleteFail();

        if (leaderCheer) leaderCheer.SetCombo(CheerCombo.Default);
        yield return new WaitForSecondsRealtime(fadeAfterFail);
    }

    // Fade & clear
    SetGlyphVisible(leaderIdx, false, false);
    UnhighlightAll();
    if (leaderCheer) leaderCheer.SetCombo(CheerCombo.Default);
    // Keep the Cheer pose in sync with what the leader “did”
    // (optional; you already have SetCombo for two-direction combos)
    matchSequences[leaderIdx].cheerleader.cheer.SetCombo(CheerCombo.Default);

    _markerBusy = false;
    Debug.Log($"[CHEER] {combosMade} combos made");
}
    private CheerCombo PoseForDir(CheerDirection dir)
    {
        switch (dir)
        {
            case CheerDirection.Up:    return CheerCombo.UpDown;
            case CheerDirection.Down:  return CheerCombo.Default;
            case CheerDirection.Left:  return CheerCombo.LeftDown;
            case CheerDirection.Right: return CheerCombo.RightUp;
            default:                   return CheerCombo.Default;
        }
    }
    private CheerCombo AnticipationForDir(CheerDirection dir)
    {
        // Optional: if you’ve authored special pre-move frames like UpPrep/DownPrep, map them here.
        // Otherwise just return the final pose or Default to keep it simple.
        return PoseForDir(dir);
    }

    private void SetGlyphVisible(int leaderIdx, bool showA, bool showB)
    {
        if (matchSequences == null || leaderIdx < 0 || leaderIdx >= matchSequences.Count) return;
        var leader = matchSequences[leaderIdx].cheerleader;
        if (leader.glyphA) leader.glyphA.gameObject.SetActive(showA);
        if (leader.glyphB) leader.glyphB.gameObject.SetActive(showB);
    }
private void ResetGlyphTintAndHide(int leaderIdx)
{
    if (matchSequences == null || leaderIdx < 0 || leaderIdx >= matchSequences.Count) return;
    var leader = matchSequences[leaderIdx].cheerleader;

    // stop any countdown tint that might still be affecting colors
    StopCountdownTintSafe(leaderIdx);

    if (leader.glyphA)
    {
        leader.glyphA.color = highlightColor; // your default “ready” tint
        leader.glyphA.gameObject.SetActive(false);
        leader.glyphA.sprite = null;          // optional: clear sprite to be explicit
    }
    if (leader.glyphB)
    {
        leader.glyphB.color = highlightColor;
        leader.glyphB.gameObject.SetActive(false);
        leader.glyphB.sprite = null;
    }

    // make sure no stale directions bleed into feedback code
    activeDirections.Remove(leaderIdx);
}

    private void SetGlyphSpriteAndColor(int leaderIdx, bool isA, CheerDirection dir, Color color)
    {
        if (matchSequences == null || leaderIdx < 0 || leaderIdx >= matchSequences.Count) return;
        var leader = matchSequences[leaderIdx].cheerleader;

        var img = isA ? leader.glyphA : leader.glyphB;
        if (!img) return;

        // Reuse your sprite getters & colors
        img.sprite = GetGlyphSprite(dir);                 // exists in your file
        img.color  = color;                               // success/fail/highlight
    }

    private CheerDirection RandomDir()
    {
        int v = _rng.Next(0, 4);
        return (CheerDirection)v; // 0..3 -> Up/Down/Left/Right
    }

    private CheerDirection RandomDirNot(CheerDirection notThis)
    {
        CheerDirection d;
        do { d = RandomDir(); } while (d == notThis);
        return d;
    }

    private int _dbgTick; // add near your other fields
    private IEnumerator WatchCueStep(FMOD.Studio.EventInstance inst)
    {
        const double leadAheadSec = 0.12; // give the main coroutine time to show glyphs etc.
        int lastMs = -1;

        while (_roundActive)
        {
            if (!inst.isValid()) { Debug.LogWarning("[CUE] inst invalid; watcher exit"); yield break; }

            inst.getPlaybackState(out var st);
            if (st == FMOD.Studio.PLAYBACK_STATE.STOPPED) { Debug.Log("[CUE] playback stopped"); yield break; }

            inst.getTimelinePosition(out int ms);

            if (double.IsNaN(_baseDSP))
                _baseDSP = AudioSettings.dspTime - (ms / 1000.0);

            if (inst.getParameterByName("CueStep", out float value) == FMOD.RESULT.OK)
            {
                int cue = Mathf.RoundToInt(value);
                if (cue < _lastCueStep) _lastCueStep = -1;
                if (cue != _lastCueStep)
                {
                    double abs = _baseDSP + (ms / 1000.0) + leadAheadSec; // <-- shifted into the near future
                    _cueQueue.Enqueue(abs);
                    _lastCueStep = cue;
                }
            }

            lastMs = ms;
            yield return null;
        }
    }


private void FinalizeQuarterScore(int totalCombos, int combosMade)
{
    int home, away;
    CheerQuarterScoring.ScoreQuarter(totalCombos, combosMade, out home, out away);
    Debug.Log($"[Cheer] ROUND END → totalCombos={totalCombos}, combosMade={combosMade} → Home+{home}, Away+{away}");

    // Apply to game totals here:
    homeScore += home;
    awayScore += away;
}

    private Sprite GetFailGlyphSprite(CheerDirection dir)
    {
        return dir switch
        {
            CheerDirection.Up => failUpGlyph,
            CheerDirection.Down => failDownGlyph,
            CheerDirection.Left => failLeftGlyph,
            CheerDirection.Right => failRightGlyph,
            _ => null
        };
    }


    private void StartCountdownTintSafe(int idx, float start, float end)
    {
        if (countdowns.Remove(idx, out var prev) && prev != null) StopCoroutine(prev);
        if (Time.time >= end) return;
        var co = StartCoroutine(ComboCountdownVisual(idx, start, end));
        if (co != null) countdowns[idx] = co;
    }

    private void StopCountdownTintSafe(int idx)
    {
        if (countdowns.Remove(idx, out var co) && co != null) StopCoroutine(co);
    }
    
    private IEnumerator ComboCountdownVisual(int idx, float startTime, float deadlineTime)
    {
        if (matchSequences == null || idx < 0 || idx >= matchSequences.Count) yield break;

        var leader = matchSequences[idx];
        var a = leader.cheerleader.glyphA;
        var b = leader.cheerleader.glyphB;
        if (a == null || b == null) yield break;

        // Lerp color from blue → red across the window
        while (Time.time < deadlineTime)
        {
            float t = Mathf.InverseLerp(startTime, deadlineTime, Time.time);
            Color c = Color.Lerp(highlightColor, failColor, t);
            a.color = c;
            b.color = c;
            yield return null;
        }
    }

    IEnumerator FadeFromBlack()
    {
        yield return new WaitForSeconds(1f);
    }
    public static void RecordGameResult(int week, bool didWin)
    {
        string json = StatsManager.Get_String_Stat("FootballSchedule");
        var wrapper = JsonUtility.FromJson<FootballGameListWrapper>(json);

        var game = wrapper.games.Find(g => g.week == week);
        if (game != null)
        {
            game.played = true;
            game.won = didWin;

            string updatedJson = JsonUtility.ToJson(wrapper);
            StatsManager.Set_String_Stat("FootballSchedule", updatedJson);
        }
    }

    void EndGame(bool won = false)
    {
        Debug.Log("Game Over - Final Routine Placeholder");
        RecordGameResult(weekNumber, won);
        SceneManager.LoadScene("Post Game");
    }

    void Update()
    {
        amplification = Mathf.Max(0f, amplification - amplificationDecayRate * Time.deltaTime);
    }

    void UpdateScoreboardUI()
    {
        homeScoreText.text = homeScore.ToString();
        awayScoreText.text = awayScore.ToString();
        quarterText.text = $"{currentQuarter}";
    }

    CheerCombo GetComboFromDirs(CheerDirection a, CheerDirection b)
    {
        foreach (var kv in comboMap)
        {
            var dirs = kv.Value;
            if ((dirs[0] == a && dirs[1] == b) || (dirs[0] == b && dirs[1] == a))
                return kv.Key;
        }

        Debug.LogWarningFormat("Unknown combo from dirs: {0}, {1}", a, b);
        return CheerCombo.Default;
    }

    void HighlightLeader(int idx)
    {
        if (leaderImages == null) return;
        for (int j = 0; j < leaderImages.Length; j++)
            leaderImages[j].color = (j == idx) ? highlightColor : dimColor;
    }

    void UnhighlightAll()
    {
        if (leaderImages != null)
            foreach (var img in leaderImages) img.color = highlightColor;
        if (spotlight) spotlight.gameObject.SetActive(false);
    }

    void MoveSpotlight(int idx)
    {
        if (spotlight == null || leaderImages == null || idx < 0 || idx >= leaderImages.Length) return;
        spotlight.gameObject.SetActive(true);
        spotlight.position = leaderImages[idx].transform.position;
    }

    void ShowGlyphs(CheerCombo combo, int idx)
    {
        if (matchSequences == null || idx < 0 || idx >= matchSequences.Count) return;

        var leader = matchSequences[idx];
        if (leader.cheerleader.glyphA == null || leader.cheerleader.glyphB == null || leader.cheerleader.cheer == null) return;

        var dirs = comboMap[combo];

        // Assign sprites
        leader.cheerleader.glyphA.sprite = GetGlyphSprite(dirs[0]);
        leader.cheerleader.glyphB.sprite = GetGlyphSprite(dirs[1]);
        activeDirections[idx] = (dirs[0], dirs[1]);

        // Set default white color
        leader.cheerleader.glyphA.color = Color.white;
        leader.cheerleader.glyphB.color = Color.white;

        // Position near cheer pose
        Vector3 leaderPos = leader.cheerleader.cheer.transform.position;
        leader.cheerleader.glyphA.transform.position = leaderPos + (Vector3)glyphOffsetA;
        leader.cheerleader.glyphB.transform.position = leaderPos + (Vector3)glyphOffsetB;

        // Show them
        leader.cheerleader.glyphA.gameObject.SetActive(true);
        leader.cheerleader.glyphB.gameObject.SetActive(true);
    }
    void HideGlyphs(int idx)
    {
        if (matchSequences == null || idx < 0 || idx >= matchSequences.Count) return;

        var leader = matchSequences[idx];
        if (leader.cheerleader.glyphA != null) leader.cheerleader.glyphA.gameObject.SetActive(false);
        if (leader.cheerleader.glyphB != null) leader.cheerleader.glyphB.gameObject.SetActive(false);
    }

    Sprite GetGlyphSprite(CheerDirection dir)
    {
        return dir switch
        {
            CheerDirection.Up => upGlyph,
            CheerDirection.Down => downGlyph,
            CheerDirection.Left => leftGlyph,
            CheerDirection.Right => rightGlyph,
            _ => null
        };
    }

    int PickCheerIndex()
    {
        if (cheers == null || cheers.Length == 0) return -1;
        int idx;
        if (cheers.Length == 1) return 0;
        do { idx = Random.Range(0, cheers.Length); } while (idx == _lastCheerIdx);
        _lastCheerIdx = idx;
        return idx;
    }


}
