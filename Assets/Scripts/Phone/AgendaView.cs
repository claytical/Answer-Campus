using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using VNEngine; // for StatsManager & SemesterHelper

public class AgendaView : MonoBehaviour
{
    [Header("Section Roots (under headings)")]
    public Transform thisWeekRoot;
    public Transform nextWeekRoot;
    public Transform laterRoot;

    [Header("Prefabs")]
    public AgendaEventButton agendaRowPrefab;   // button prefab with AgendaEventButton

    [Header("Event")]
    public StringEvent onEventSelected;         // emits a stable key you can route on
    [Serializable] public class StringEvent : UnityEvent<string> {}

    public void Render()
    {
        // Clear existing
        Clear(thisWeekRoot); Clear(nextWeekRoot); Clear(laterRoot);

        int currentWeek = Mathf.RoundToInt(StatsManager.Get_Numbered_Stat("Week"));

        // Build buckets from your API
        var thisWeek = GameEvents.GetWeekPreview(currentWeek) ?? new List<EventInfo>();
        var nextWeek = GameEvents.GetWeekPreview(currentWeek + 1) ?? new List<EventInfo>();

        var later = new List<EventInfo>();
        for (int w = currentWeek + 2; w <= SemesterHelper.FinalsWeek; w++)
        {
            var list = GameEvents.GetWeekPreview(w);
            if (list != null && list.Count > 0) later.AddRange(list);
        }

        // Render sections
        BuildSection(thisWeekRoot, thisWeek);
        BuildSection(nextWeekRoot, nextWeek);
        BuildSection(laterRoot, later);
    }

    void BuildSection(Transform root, List<EventInfo> events)
    {
        if (!root || events == null) return;

        // Current character locations (for attendees)
        var charLocs = PlayerPrefsExtra.GetList<CharacterLocation>(
            "characterLocations", new List<CharacterLocation>());

        foreach (var evt in events)
        {
            var row = Instantiate(agendaRowPrefab, root);

            string title = evt.label;                         // from EventInfo
            string time  = $"Week {evt.week}";                // we only have week granularity
            var attendees = GetAttendeesFor(evt, charLocs);   // simple presence-by-location

            string key = BuildEventKey(evt);                  // stable-ish id for click routing

            row.Bind(title, time, attendees, () => onEventSelected?.Invoke(key));
        }
    }

    List<Character> GetAttendeesFor(EventInfo evt, List<CharacterLocation> currentLocations)
    {
        if (string.IsNullOrWhiteSpace(evt.location)) return new List<Character>();

        return currentLocations
            .Where(cl => cl.location == evt.location)
            .Select(cl => cl.character)
            .Distinct()
            .OrderBy(c => c.ToString())
            .ToList();
    }

    string BuildEventKey(EventInfo evt)
    {
        // Use type + week; include label for Custom to differentiate same-week customs.
        if (evt.type == EventType.Custom)
            return $"Custom:{evt.week}:{evt.label}";
        return $"{evt.type}:{evt.week}";
    }

    void Clear(Transform t)
    {
        if (!t) return;
        for (int i = t.childCount - 1; i >= 0; i--) Destroy(t.GetChild(i).gameObject);
    }
}
