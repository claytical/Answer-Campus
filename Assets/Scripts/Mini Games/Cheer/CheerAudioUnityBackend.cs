using System;
using System.Collections;
using UnityEngine;

public class CheerAudioUnityBackend : ICheerAudioBackend
{
    private readonly MonoBehaviour _host;
    private readonly CheerAudioConfig _cfg;

    private readonly AudioSource _crowd;
    private readonly AudioSource _countdown;
    private readonly AudioSource _cheer;

    private Coroutine _countdownCo;
    private Coroutine _cueCo;

    public int CheerCount => _cfg != null && _cfg.cheers != null ? _cfg.cheers.Length : 0;

    public CheerAudioUnityBackend(CheerAudioBackendSelector selector, CheerAudioConfig cfg)
    {
        _host = selector;
        _cfg = cfg;

        _crowd = selector.crowdSource;
        _countdown = selector.countdownSource;
        _cheer = selector.cheerSource;

        if (_cfg == null)
            Debug.LogError("[CheerAudioUnityBackend] Missing CheerAudioConfig.");
    }

    public void StartCrowd()
    {
        if (_cfg == null || _cfg.crowdLoop == null) return;
        _crowd.clip = _cfg.crowdLoop;
        if (!_crowd.isPlaying) _crowd.Play();
    }

    public void StopCrowd(bool immediate = true)
    {
        if (immediate) _crowd.Stop();
        else _host.StartCoroutine(FadeOutAndStop(_crowd, 0.35f));
    }

    public void PlayCountdown(Action onFinished)
    {
        if (_countdownCo != null) _host.StopCoroutine(_countdownCo);
        _countdownCo = _host.StartCoroutine(CountdownRoutine(onFinished));
    }

    private IEnumerator CountdownRoutine(Action onFinished)
    {
        if (_cfg == null || _cfg.countdownOneShot == null)
        {
            onFinished?.Invoke();
            yield break;
        }

        _countdown.clip = _cfg.countdownOneShot;
        _countdown.Play();

        // NEW: wait for playback to actually start
        yield return WaitForPlaybackStart(_countdown, 0.5f);

        // Wait until finish
        while (_countdown.isPlaying)
            yield return null;

        onFinished?.Invoke();
    }

    public void StartCheer(int cheerIndex, Action<string, int> onCue, Action onEnded)
    {
        if (_cfg == null || _cfg.cheers == null || _cfg.cheers.Length == 0) return;
        cheerIndex = Mathf.Clamp(cheerIndex, 0, _cfg.cheers.Length - 1);

        var track = _cfg.cheers[cheerIndex];
        if (track.clip == null) return;

        StopCheer(true);

        _cheer.clip = track.clip;
        _cheer.Play();

        // Schedule cues using authored timestamps
        if (_cueCo != null) _host.StopCoroutine(_cueCo);
        _cueCo = _host.StartCoroutine(CueRoutine(track, onCue, onEnded));
    }

    private IEnumerator CueRoutine(CheerAudioConfig.CheerTrack track, Action<string, int> onCue, Action onEnded)
    {
        // NEW: wait for playback to actually start
        yield return WaitForPlaybackStart(_cheer, 0.5f);

        // If it still isn't playing, treat as ended but log already happened.
        if (!_cheer.isPlaying)
        {
            onEnded?.Invoke();
            yield break;
        }

        double dspStart = AudioSettings.dspTime;
        float[] cues = track.cueTimesSeconds ?? Array.Empty<float>();
        int next = 0;

        while (_cheer != null && _cheer.isPlaying)
        {
            double elapsed = AudioSettings.dspTime - dspStart;

            while (next < cues.Length && elapsed >= cues[next])
            {
                int posMs = Mathf.RoundToInt(cues[next] * 1000f);
                onCue?.Invoke($"Cue{next}", posMs);
                next++;
            }

            yield return null;
        }

        onEnded?.Invoke();
    }
    public void PreloadAll()
    {
        if (_cfg == null) return;

        PreloadClip(_cfg.crowdLoop);
        PreloadClip(_cfg.countdownOneShot);

        if (_cfg.cheers != null)
        {
            foreach (var t in _cfg.cheers)
                PreloadClip(t.clip);
        }
    }

    private void PreloadClip(AudioClip clip)
    {
        if (clip == null) return;

        // If audio data is not loaded, request it.
        if (!clip.preloadAudioData && clip.loadState == AudioDataLoadState.Unloaded)
        {
            // If Preload Audio Data is off, Unity may never load until requested.
            clip.LoadAudioData();
        }
        else if (clip.loadState == AudioDataLoadState.Unloaded)
        {
            clip.LoadAudioData();
        }
    }

    private static IEnumerator WaitForPlaybackStart(AudioSource src, float timeoutSeconds = 2.0f)
    {
        if (!src || src.clip == null) yield break;

        // Ensure audio data is requested
        if (src.clip.loadState == AudioDataLoadState.Unloaded)
            src.clip.LoadAudioData();

        // Wait for load (or time out)
        float t = 0f;
        while (t < timeoutSeconds && src.clip.loadState != AudioDataLoadState.Loaded)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        // Now give Unity a frame to start playback
        yield return null;
    }

    public void StopCheer(bool immediate = true)
    {
        if (_cueCo != null) { _host.StopCoroutine(_cueCo); _cueCo = null; }
        if (immediate) _cheer.Stop();
        else _host.StartCoroutine(FadeOutAndStop(_cheer, 0.25f));
    }

    private static IEnumerator FadeOutAndStop(AudioSource src, float dur)
    {
        if (!src || !src.isPlaying) yield break;

        float start = src.volume;
        float t = 0f;
        while (t < dur && src)
        {
            t += Time.unscaledDeltaTime;
            src.volume = Mathf.Lerp(start, 0f, t / dur);
            yield return null;
        }
        if (src) { src.Stop(); src.volume = start; }
    }
}
