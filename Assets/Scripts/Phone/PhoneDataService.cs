// PhoneDataService.cs
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class PhoneDataService
{
    // ==== MAP ====
    public static List<CharacterLocation> GetCharacterLocations()
    {
        return PlayerPrefsExtra.GetList<CharacterLocation>(
            "characterLocations", new List<CharacterLocation>()); // set elsewhere via your routing/placement
    }

    // ==== FRIENDS & MESSAGES ====
    public static Dictionary<Character, List<TextMessage>> GetMessageThreads()
    {
        var messages = PlayerPrefsExtra.GetList<TextMessage>("messages", new List<TextMessage>());
        return messages.GroupBy(m => m.from).ToDictionary(g => g.Key, g => g.ToList());
    }

    public static void ClearThread(Character character)
    {
        var dict = GetMessageThreads();
        if (dict.Remove(character))
        {
            var flattened = dict.Values.SelectMany(m => m).ToList();
            PlayerPrefsExtra.SetList("messages", flattened);
            PlayerPrefs.Save();
        }
    }

    // ==== AGENDA ====
    public struct AgendaBucket
    {
        public string label;  // "This Week" | "Next Week" | "Later"
        public List<EventInfo> events;
    }

    public static int GetCurrentWeek()
    {
        return Mathf.RoundToInt(VNEngine.StatsManager.Get_Numbered_Stat("Week"));
    }

    public static List<AgendaBucket> GetAgendaBuckets()
    {
        // derive buckets using the same preview the Calendar uses
        int week = GetCurrentWeek();
        List<EventInfo> thisWeek = GameEvents.GetWeekPreview(week);
        List<EventInfo> nextWeek = GameEvents.GetWeekPreview(week + 1);
        var later = new List<EventInfo>();
        for (int w = week + 2; w <= SemesterHelper.FinalsWeek; w++)
        {
            var list = GameEvents.GetWeekPreview(w);
            if (list != null && list.Count > 0) later.AddRange(list);
        }

        return new List<AgendaBucket> {
            new AgendaBucket { label = "This Week", events = thisWeek ?? new List<EventInfo>() },
            new AgendaBucket { label = "Next Week", events = nextWeek ?? new List<EventInfo>() },
            new AgendaBucket { label = "Later", events = later }
        };
    }

    // Example: pull friend attendance for agenda rows
    public static List<Character> GetAttendeesFor(EventInfo evt)
    {
        // Convention: If you store attendance per event somewhere, look it up here.
        // Fallback heuristic: anyone whose CharacterLocation.location == evt.location this week.
        var locs = GetCharacterLocations();
        return locs.Where(l => l.location == evt.location).Select(l => l.character).Distinct().ToList();
    }
}
