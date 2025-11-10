using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class LocationRouter
{
    /// <summary>
    /// Route to a scene. Clears character "invites" (pins) for that location only.
    /// Does NOT touch message history (threads persist).
    /// </summary>
    public static void Go(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning("[LocationRouter] Go called with empty sceneName");
            return;
        }

        // Clear any invite pointing at this scene (all characters)
        var pins = PlayerPrefsExtra.GetList<CharacterLocation>("characterLocations", new List<CharacterLocation>());
        int removed = pins.RemoveAll(p => string.Equals(p.location, sceneName, System.StringComparison.Ordinal));
        PlayerPrefsExtra.SetList("characterLocations", pins);
        if (removed > 0) Debug.Log($"[LocationRouter] Cleared {removed} invite(s) for {sceneName}.");

        SceneManager.LoadScene(sceneName);
    }

    /// Optional overload: clear only a specific character’s invite at a location.
    public static void Go(string sceneName, Character character)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning("[LocationRouter] Go(char) called with empty sceneName");
            return;
        }

        var pins = PlayerPrefsExtra.GetList<CharacterLocation>("characterLocations", new List<CharacterLocation>());
        int removed = pins.RemoveAll(p =>
            string.Equals(p.location, sceneName, System.StringComparison.Ordinal) &&
            EqualityComparer<Character>.Default.Equals(p.character, character));

        PlayerPrefsExtra.SetList("characterLocations", pins);
        if (removed > 0) Debug.Log($"[LocationRouter] Cleared invite for {character} @ {sceneName}.");

        SceneManager.LoadScene(sceneName);
    }
}
