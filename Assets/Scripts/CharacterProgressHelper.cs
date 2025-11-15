// CharacterProgressHelper.cs (new)
using System.Collections.Generic;
using UnityEngine;
using VNEngine;

public static class CharacterProgressHelper
{
    public struct AvailableConversation
    {
        public Character character;
        public LocationData location;
        public int stage;
        public int unlockWeek;
    }

    static int CurrentWeek() => Mathf.RoundToInt(StatsManager.Get_Numbered_Stat("Week"));

    static float GetStage(Character c, string scene)
        => StatsManager.Get_Numbered_Stat($"{c} - {scene} - Stage");
    public static List<AvailableConversation> GetAvailableNow(
        IEnumerable<StageRouteIndex> perNpcIndices,
        List<CharacterLocation> characterLocations,
        bool requireFriendToTrack = false)
    {
        var results = new List<AvailableConversation>();
        if (perNpcIndices == null) return results;

        // de-dupe in case multiple indices point to the same step definition
        var seen = new HashSet<string>();

        foreach (var idx in perNpcIndices)
        {
            if (idx == null) continue;

            var chunk = GetAvailableConversationsThisWeek(idx, characterLocations, requireFriendToTrack);
            foreach (var a in chunk)
            {
                var key = $"{a.character}|{a.location?.sceneName}|{a.stage}|{a.unlockWeek}";
                if (seen.Add(key)) results.Add(a);
            }
        }

        return results;
    }

    static bool IsFriend(Character c)
        => StatsManager.Get_Boolean_Stat($"{c}_is_friend"); // Sentinel App gate (optional)
    public static bool IsNight()
    {
        // Persisted as numbered 0/1 to keep StatsManager uniform
        if (!StatsManager.Numbered_Stat_Exists("IsNight")) return false;
        return StatsManager.Get_Numbered_Stat("IsNight") > 0.5f;
    }

    public static void SetNight(bool value) =>
        StatsManager.Set_Numbered_Stat("IsNight", value ? 1 : 0);

    public static List<AvailableConversation> GetAvailableConversationsThisWeek(
        StageRouteIndex index, List<CharacterLocation> characterLocations, bool requireFriendToTrack = false)
    {
        var results = new List<AvailableConversation>();
        if (index == null || index.Routes == null) return results;

        int week = CurrentWeek();

        // Build presence map from your saved list
        var where = new Dictionary<Character, string>();
        foreach (var cl in characterLocations) where[cl.character] = cl.location;

        foreach (var meta in index.Routes)
        {
            if (meta.location == null) continue;

            // Week gate
            if (week < meta.unlockWeek) continue;

            // Optional Sentinel rule: must be a friend to surface
            if (requireFriendToTrack && !IsFriend(meta.character)) continue;

            // Character must be at this Location now
            if (!where.TryGetValue(meta.character, out var sceneNow)) continue;
            if (sceneNow != meta.location.sceneName) continue;

            // Player must be at the required stage for (character, scene)
            var stageNow = Mathf.RoundToInt(GetStage(meta.character, meta.location.sceneName));
            if (stageNow != meta.stage) continue;

            results.Add(new AvailableConversation {
                character = meta.character,
                location = meta.location,
                stage = meta.stage,
                unlockWeek = meta.unlockWeek
            });
        }
        return results;
    }
}
