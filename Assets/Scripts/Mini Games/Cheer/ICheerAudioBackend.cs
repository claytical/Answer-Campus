using System;

public interface ICheerAudioBackend
{
    void StartCrowd();
    void StopCrowd(bool immediate = true);

    /// Plays countdown and invokes onFinished when done.
    /// Backend is responsible for “wait until finished”.
    void PlayCountdown(Action onFinished);

    /// Starts a cheer track, firing onCue(name, positionMs) at authored cue points.
    void StartCheer(int cheerIndex, Action<string, int> onCue, Action onEnded);

    void StopCheer(bool immediate = true);
    int  CheerCount { get; }
}