public enum CheerAudioBackendMode
{
    Auto,       // WebGL => UnityAudio, otherwise FMOD (if available)
    ForceUnity, // editor/testing
    ForceFMOD   // editor/testing
}