using System;                // for Array.Find/Exists
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VNEngine;

public class Map : MonoBehaviour
{
    public Location[] lockableLocations;
    private List<CharacterLocation> characterLocations;
    public Characters characters;
    public Location footballGame;

    [Header("Conversation Availability")]
    public List<StageRouteIndex> npcRouteIndices = new(); // ← list, not single
    public bool requireFriendToTrack = false;              // Sentinel gate

    void Start()
    {
        // 0) Football toggle for this week
        int currentWeek = (int)StatsManager.Get_Numbered_Stat("Week");
        var thisWeeksGame = FootballScheduler.GetThisWeeksGame(currentWeek);
        footballGame.GetComponent<Button>().interactable =
            (thisWeeksGame != null && thisWeeksGame.isHome && !thisWeeksGame.played);

        // 1) Load presence and cache all scene Locations (include inactive)
        characterLocations = PlayerPrefsExtra.GetList<CharacterLocation>("characterLocations", new List<CharacterLocation>());
        var allLocations = GetComponentsInChildren<Location>(true);

        // 2) Lock all lockable locations; hide portraits & badges
        foreach (var location in lockableLocations)
        {
            if (location == null) continue;
            var btn = location.GetComponent<Button>();
            if (btn) btn.interactable = false;

            if (location.characterWaiting) location.characterWaiting.gameObject.SetActive(false);

            var badge = location.transform.Find("AvailableBadge");
            if (badge) badge.gameObject.SetActive(false);
        }

        // 3) Place portraits for characters who are present, and unlock those locations
        foreach (var cl in characterLocations)
        {
            var profile = Array.Find(characters.profiles, p => p.character == cl.character);
            if (profile.picture == null) continue;

            // Match either by scene (preferred) or by GO name
            foreach (var loc in allLocations)
            {
                if (loc == null) continue;
                if (loc.scene == cl.location || loc.name == cl.location)
                {
                    // Show portrait for presence (independent of availability)
                    if (loc.characterWaiting)
                    {
                        loc.characterWaiting.sprite = profile.picture;
                        loc.characterWaiting.gameObject.SetActive(true);
                    }

                    // Unlock if this Location is in the lockable set
                    if (Array.Exists(lockableLocations, l => l == loc))
                    {
                        var btn = loc.GetComponent<Button>();
                        if (btn) btn.interactable = true;
                        Debug.Log($"Unlocked Location {loc.name} with character {profile.character}.");
                    }

                    break; // matched a scene Location
                }
            }
        }

        // 4) Compute availability ONCE across all NPCs, then light badges on the matching scene Locations
        var available = CharacterProgressHelper.GetAvailableNow(npcRouteIndices, characterLocations, requireFriendToTrack);
        foreach (var a in available)
        {
            // a.location is LocationData — get the actual scene Location instance
            var locInstance = LocationRegistry.Get(a.location);
            if (locInstance == null) continue;

            var badge = locInstance.transform.Find("AvailableBadge");
            if (badge) badge.gameObject.SetActive(true);

            Debug.Log($"[Map] Conversation AVAILABLE: {a.character} at {locInstance.scene} (Stage {a.stage}, unlock {a.unlockWeek})");
        }
    }
}
