using System;
using System.Collections.Generic;
using UnityEngine;
using VNEngine;

[Serializable]
public class CustomEvent
{
    public string id;          // stable key
    public string name;        // e.g., "Art Show Opening"
    public int week;           // absolute week number
    public Sprite icon;
    public string location;    // scene name
    public bool unlocked;      // set true when a convo unlocks it
}

[Serializable]
class CustomEventList { public List<CustomEvent> items = new(); }

public enum EventType { Midterms, Finals, FootballHomeGame, Custom }
[Serializable]
public struct EventInfo
{
    public EventType type;
    public string label;     // display label
    public int week;
    public string location;  // may be null
    public Sprite icon;
}

public static class GameEvents
{
    const string CustomEventsKey = "CustomEvents";

    public static List<CustomEvent> LoadCustomEvents()
    {
        if (!StatsManager.String_Stat_Exists(CustomEventsKey)) return new();
        string json = StatsManager.Get_String_Stat(CustomEventsKey);
        if (string.IsNullOrEmpty(json)) return new();
        var list = JsonUtility.FromJson<CustomEventList>(json);
        return list?.items ?? new();
    }

    public static void SaveCustomEvents(List<CustomEvent> items)
    {
        var wrap = new CustomEventList { items = items ?? new() };
        string json = JsonUtility.ToJson(wrap);
        StatsManager.Set_String_Stat(CustomEventsKey, json);
    }

    public static void RegisterOrUpdateCustomEvent(CustomEvent ev)
    {
        var items = LoadCustomEvents();
        int idx = items.FindIndex(x => x.id == ev.id);
        if (idx >= 0) items[idx] = ev; else items.Add(ev);
        SaveCustomEvents(items);
    }

    public static List<EventInfo> GetWeekPreview(int week)
    {
        var outList = new List<EventInfo>();

        // Academic anchors
        if (week == SemesterHelper.MidtermsWeek)
            outList.Add(new EventInfo { type = EventType.Midterms, label = "Midterms", week = week });

        if (week == SemesterHelper.FinalsWeek)
            outList.Add(new EventInfo { type = EventType.Finals, label = "Finals", week = week });

        // Football (home) this week?
        var game = FootballScheduler.GetThisWeeksGame(week);
        if (game != null && game.isHome && !game.played)
        {
            outList.Add(new EventInfo {
                type = EventType.FootballHomeGame,
                label = $"{game.opponent.schoolName} {game.opponent.mascot}",
                week = week,
                location = "FootballStadium"
            });
        }

        // Custom unlocked events
        foreach (var ev in LoadCustomEvents())
        {
            if (ev.unlocked && ev.week == week)
            {
                outList.Add(new EventInfo {
                    type = EventType.Custom,
                    label = ev.name,
                    week = ev.week,
                    location = ev.location,
                    icon = ev.icon
                });
            }
        }

        return outList;
    }
}
