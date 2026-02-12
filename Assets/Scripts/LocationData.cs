using UnityEngine;

[CreateAssetMenu(fileName = "LocationData", menuName = "AnswerVerse/Location Data")]
public class LocationData : ScriptableObject
{
    public string sceneName;       // must match the .unity file name you load
    public string displayName;     // optional UI label
    public Sprite mapIcon;         // optional for calendar/map preview
    public Sprite phoneIcon;
}