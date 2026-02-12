using System;
using UnityEngine;

[CreateAssetMenu(menuName = "AnswerVerse/Cheer/Cheer Audio Config", fileName = "CheerAudioConfig")]
public class CheerAudioConfig : ScriptableObject
{
    [Header("Unity Audio (WebGL-safe)")]
    public AudioClip crowdLoop;
    public AudioClip countdownOneShot;

    [Tooltip("Cheer tracks. Order does not matter if selection is random.")]
    public CheerTrack[] cheers;

    [Serializable]
    public class CheerTrack
    {
        public string id;               // optional label (e.g., "Cheer5")
        public AudioClip clip;          // MP3
        public float[] cueTimesSeconds; // e.g., 0, 2.794, ...
    }
}