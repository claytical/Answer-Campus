
using System;
using UnityEngine;
using FMODUnity;

public class CheerAudioFmodBackendComponent : MonoBehaviour
{
    [Header("FMOD")]
    public EventReference crowd;
    public EventReference countdownEvent;
    public EventReference gameEvent;

    public bool IsReady => true; // could validate EventReferences here

    public ICheerAudioBackend CreateBackend()
    {
        return new CheerAudioFmodBackend(this);
    }

    private class CheerAudioFmodBackend : ICheerAudioBackend
    {
        private readonly CheerAudioFmodBackendComponent _c;
        private FMOD.Studio.EventInstance _active;

        public int CheerCount => int.MaxValue; // selection handled by your authored FMOD sections

        public CheerAudioFmodBackend(CheerAudioFmodBackendComponent c) => _c = c;

        public void StartCrowd() => FMODAudioManager.Instance.PlayMusic(_c.crowd);
        public void StopCrowd(bool immediate = true) { /* implement if you have it */ }

        public void PlayCountdown(Action onFinished)
        {
            _c.StartCoroutine(FMODAudioManager.Instance.PlayOneShotAndWaitPrecise(
                _c.countdownEvent, "Countdown", 0 /* caller will pass param another way if needed */
            ));
            // If you still need the Countdown param, add it to interface or store it elsewhere.
            // For now, assume single countdown.
            _c.StartCoroutine(InvokeNextFrame(onFinished));
        }

        public void StartCheer(int cheerIndex, Action<string, int> onCue, Action onEnded)
        {
            _active = FMODAudioManager.Instance.StartEventWithTimeline(
                evt: _c.gameEvent,
                onMarker: onCue,
                onStopped: () => onEnded?.Invoke(),
                paramName: "Cheer",
                paramValue: cheerIndex,
                attachTo: (Camera.main ? Camera.main.transform : _c.transform),
                ignoreSeekSpeed: true
            );
        }

        public void StopCheer(bool immediate = true)
        {
            if (!_active.isValid()) return;
            _active.stop(immediate ? FMOD.Studio.STOP_MODE.IMMEDIATE : FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            _active.release();
        }

        private static System.Collections.IEnumerator InvokeNextFrame(Action a)
        {
            yield return null;
            a?.Invoke();
        }
    }
}

