using System;                // for Array.Find/Exists
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VNEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

// A single-pass "truth" about what the phone/fullscreen map should show right now.
public static class MapAvailability
{
    public enum LockReason { None, InviteOnly, TimeGated }

    public sealed class Status
    {
        public string locationName;                // label shown on phone map
        public string displayName;
        public bool interactable;                  // can navigate?
        public LockReason lockReason;             // if not interactable, why
        public bool hasFriendChip;                 // any character physically there now
        public List<Character> friends = new();    // for FriendChips
        public bool hasAvailableConversation;      // your "AvailableBadge" concept
        public Sprite mapIcon;
        public Sprite phoneIcon;
    }

    public sealed class Context
    {
        public int currentWeek;
        public bool requireFriendToTrack;

        public IEnumerable<Location> lockableLocationsScene = Array.Empty<Location>();
        public IEnumerable<string>   lockableLocationNames  = Array.Empty<string>(); // ← ADD

        public IEnumerable<CharacterLocation> characterPins = Array.Empty<CharacterLocation>();
        public IEnumerable<Location> allSceneLocations = Array.Empty<Location>();
        public IEnumerable<StageRouteIndex> npcRouteIndices = Array.Empty<StageRouteIndex>();

        public Func<int, object> getThisWeeksGame;
        public Func<object, bool> isHomeGame;
        public Func<object, bool> gamePlayed;
    }
    // Main entry. Builds a per-location status table using the same logic your Map.Start() applies.
public static Dictionary<string, Status> Build(Context ctx)
{
    var result = new Dictionary<string, Status>(StringComparer.Ordinal);

    // 0) Football weekly gating
    bool footballInteractable = false;
    if (ctx.getThisWeeksGame != null && ctx.isHomeGame != null && ctx.gamePlayed != null)
    {
        var g = ctx.getThisWeeksGame(ctx.currentWeek);
        footballInteractable = (g != null && ctx.isHomeGame(g) && !ctx.gamePlayed(g));
    }

    // --- NEW: Build a map from scene key -> displayName/icon from LocationData assets ---
    var displayNameByKey = new Dictionary<string, string>(StringComparer.Ordinal);
    var iconByKey        = new Dictionary<string, Sprite>(StringComparer.Ordinal);

    var allLocationAssets = Resources.LoadAll<LocationData>("");  // authority
    foreach (var ld in allLocationAssets)
    {
        if (ld == null) continue;
        var key = !string.IsNullOrWhiteSpace(ld.sceneName) ? ld.sceneName : ld.name;
        if (string.IsNullOrWhiteSpace(key)) continue;

        if (!displayNameByKey.ContainsKey(key))
            displayNameByKey[key] = string.IsNullOrWhiteSpace(ld.displayName) ? key : ld.displayName;

        if (ld.mapIcon && !iconByKey.ContainsKey(key))
            iconByKey[key] = ld.mapIcon;
    }

    // 1) Authoritative universe of keys (sceneName preferred)
    var allNames = new HashSet<string>(displayNameByKey.Keys, StringComparer.Ordinal); // seed with LocationData

    // (b) From StageRouteIndex (routes reference LocationData)
    foreach (var idx in ctx.npcRouteIndices ?? Array.Empty<StageRouteIndex>())
    {
        if (idx?.Routes == null) continue;
        foreach (var r in idx.Routes)
        {
            if (r?.location == null) continue;
            var key = !string.IsNullOrWhiteSpace(r.location.sceneName) ? r.location.sceneName : r.location.name;
            if (string.IsNullOrWhiteSpace(key)) continue;
            allNames.Add(key);

            // If the route points to an LD that has a displayName/icon we haven’t captured yet, capture it.
            if (!displayNameByKey.ContainsKey(key))
                displayNameByKey[key] = string.IsNullOrWhiteSpace(r.location.displayName) ? key : r.location.displayName;
            if (r.location.mapIcon && !iconByKey.ContainsKey(key))
                iconByKey[key] = r.location.mapIcon;
        }
    }

    // (c) From live scene Locations (big map)
    foreach (var loc in ctx.allSceneLocations ?? Array.Empty<Location>())
    {
        string key = null;
        if (!string.IsNullOrWhiteSpace(loc.scene)) key = loc.scene;
        else if (loc.data && !string.IsNullOrWhiteSpace(loc.data.sceneName)) key = loc.data.sceneName;
        else if (loc.data && !string.IsNullOrWhiteSpace(loc.data.name)) key = loc.data.name;
        if (string.IsNullOrWhiteSpace(key)) continue;

        allNames.Add(key);

        // Prefer LocationData.displayName if available; otherwise use key
        if (!displayNameByKey.ContainsKey(key))
        {
            var friendly = (loc.data && !string.IsNullOrWhiteSpace(loc.data.displayName))
                ? loc.data.displayName
                : key;
            displayNameByKey[key] = friendly;
        }
        if (loc.data && loc.data.mapIcon && !iconByKey.ContainsKey(key))
            iconByKey[key] = loc.data.mapIcon;
    }

    // (d) From pins as last resort (routing key only; no display info here)
    foreach (var cl in (ctx.characterPins ?? Array.Empty<CharacterLocation>()))
        if (!string.IsNullOrWhiteSpace(cl.location))
            allNames.Add(cl.location);

    // Persist the universe once (optional, as you already had)
    try
    {
        PlayerPrefsExtra.SetList("registryLocationNames", allNames.ToList());
        PlayerPrefs.Save();
    }
    catch (Exception e) { Debug.LogWarning($"[MapAvailability] persist registryLocationNames failed: {e.Message}"); }

    // 2) Lookups
    var pinsByLoc = (ctx.characterPins ?? Array.Empty<CharacterLocation>())
        .Where(p => !string.IsNullOrWhiteSpace(p.location))
        .GroupBy(p => p.location)
        .ToDictionary(g => g.Key, g => g.Select(p => p.character).Distinct().ToList(), StringComparer.Ordinal);

    // Lockables (big map writes, phone reads)
    var lockableNames = new HashSet<string>(StringComparer.Ordinal);
    foreach (var l in ctx.lockableLocationsScene ?? Array.Empty<Location>())
    {
        string n = null;
        if (!string.IsNullOrWhiteSpace(l.scene)) n = l.scene;
        else if (l.data && !string.IsNullOrWhiteSpace(l.data.sceneName)) n = l.data.sceneName;
        else if (l.data) n = l.data.name;
        if (!string.IsNullOrWhiteSpace(n)) lockableNames.Add(n);

        // Also capture friendly name/icon from this scene object if we don’t have them yet
        if (l.data)
        {
            if (!displayNameByKey.ContainsKey(n))
                displayNameByKey[n] = string.IsNullOrWhiteSpace(l.data.displayName) ? n : l.data.displayName;
            if (l.data.mapIcon && !iconByKey.ContainsKey(n))
                iconByKey[n] = l.data.mapIcon;
        }
    }
    if (lockableNames.Count == 0)
    {
        try
        {
            var persisted = PlayerPrefsExtra.GetList<string>("lockableLocationNames", new List<string>());
            foreach (var n in persisted) if (!string.IsNullOrWhiteSpace(n)) lockableNames.Add(n);
        }
        catch (Exception e) { Debug.LogWarning($"[MapAvailability] load lockables failed: {e.Message}"); }
    }
    else
    {
        try
        {
            PlayerPrefsExtra.SetList("lockableLocationNames", lockableNames.ToList());
            PlayerPrefs.Save();
        }
        catch (Exception e) { Debug.LogWarning($"[MapAvailability] persist lockables failed: {e.Message}"); }
    }

    // 3) Route window helper
    bool IsInRouteWindow(string locName)
    {
        if (ctx.npcRouteIndices == null || !ctx.npcRouteIndices.Any())
            return true; // phone fallback: lockable+invite handles gating
        foreach (var idx in ctx.npcRouteIndices)
        {
            if (idx?.Routes == null) continue;
            foreach (var r in idx.Routes)
            {
                if (r?.location == null) continue;
                var key = !string.IsNullOrWhiteSpace(r.location.sceneName) ? r.location.sceneName : r.location.name;
                if (!string.Equals(key, locName, StringComparison.Ordinal)) continue;
                if (r.unlockWeek <= 0 || ctx.currentWeek >= r.unlockWeek) return true;
            }
        }
        return false;
    }

    // 4) Build Status objects (now with displayName/icon)
    foreach (var name in allNames.OrderBy(n => n))
    {
        var st = new Status
        {
            locationName = name,
            displayName  = displayNameByKey.TryGetValue(name, out var dn) ? dn : name, 
            mapIcon         = iconByKey.TryGetValue(name, out var ic) ? ic : null
        };

        if (!lockableNames.Contains(name))
        {
            st.interactable = true;
            st.lockReason   = LockReason.None;
        }
        else
        {
            bool hasInvite = pinsByLoc.TryGetValue(name, out var inviteFriends) && inviteFriends.Count > 0;
            bool inWindow  = IsInRouteWindow(name);
            st.interactable = hasInvite && inWindow;
            st.lockReason   = st.interactable ? LockReason.None : LockReason.InviteOnly;
        }

        // Football & Shuttle gating
        if (name.Equals("Football Game", StringComparison.OrdinalIgnoreCase) ||
            name.Equals("Shuttle", StringComparison.OrdinalIgnoreCase))
        {
            st.interactable = footballInteractable;
            st.lockReason   = st.interactable ? LockReason.None : LockReason.TimeGated;
        }

        if (pinsByLoc.TryGetValue(name, out var friends))
        {
            st.hasFriendChip = friends.Count > 0;
            st.friends = friends;
        }

        result[name] = st;
    }

    Debug.Log($"[BUILD] allNames ({allNames.Count}): {string.Join(", ", allNames)}");
    return result;
}

}

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
    int currentWeek = (int)StatsManager.Get_Numbered_Stat("Week");

    characterLocations = PlayerPrefsExtra.GetList<CharacterLocation>("characterLocations", new List<CharacterLocation>());
    var allLocations = GetComponentsInChildren<Location>(true);

    var ctx = new MapAvailability.Context {
        currentWeek = currentWeek,
        requireFriendToTrack = requireFriendToTrack,
        lockableLocationsScene = lockableLocations,          // your array from the Inspector
        characterPins = characterLocations,
        allSceneLocations = allLocations,
        npcRouteIndices = npcRouteIndices,
        getThisWeeksGame = FootballScheduler.GetThisWeeksGame,
        isHomeGame = g => ((FootballGame)g).isHome,
        gamePlayed = g => ((FootballGame)g).played
    };

    var snapshot = MapAvailability.Build(ctx);

    // Now just apply it to the scene UI like you already do:
    foreach (var loc in allLocations)
    {
        var name = !string.IsNullOrWhiteSpace(loc.scene) ? loc.scene
                  : (loc.data != null ? loc.data.name : loc.name);

        if (!snapshot.TryGetValue(name, out var st)) continue;

        // Button interactable
        var btn = loc.GetComponent<Button>();
        if (btn) btn.interactable = st.interactable;

        // Portraits for presence (independent of availability)
        if (loc.characterWaiting)
        {
            var who = st.friends.FirstOrDefault(); // or keep your per-profile loop
            var profile = Array.Find(characters.profiles, p => p.character == who);
            if (profile.pictureLarge != null)
            {
                loc.characterWaiting.sprite = profile.pictureLarge;
                loc.characterWaiting.gameObject.SetActive(st.hasFriendChip);
            }
            else loc.characterWaiting.gameObject.SetActive(false);
        }

        // "Available" badge
        var badge = loc.transform.Find("AvailableBadge");
        if (badge) badge.gameObject.SetActive(st.hasAvailableConversation);
    }
}

}
