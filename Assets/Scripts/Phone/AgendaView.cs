using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using VNEngine; // for StatsManager & SemesterHelper


public class AgendaView : MonoBehaviour
{
    
    public Transform panelRoot;
    
    [Header("Prefabs")]
    public GameObject groupLabelPrefab;
    public GameObject agendaRowPrefab;   // button prefab with AgendaEventButton

    [Header("Event")]
    public StringEvent onEventSelected;         // emits a stable key you can route on
    [Serializable] public class StringEvent : UnityEvent<string> {}
    private List<PhoneDataService.AgendaBucket> _agendaBuckets;

public void Render()
{
    Clear(panelRoot);

    int currentWeek = Mathf.RoundToInt(StatsManager.Get_Numbered_Stat("Week"));

    var past      = new List<EventInfo>();
    var thisWeek  = new List<EventInfo>();
    var later     = new List<EventInfo>();

    // Gather all events
    for (int w = 1; w <= SemesterHelper.FinalsWeek; w++)
    {
        var weekEvents = GameEvents.GetWeekPreview(w);
        if (weekEvents == null || weekEvents.Count == 0) continue;

        foreach (var evt in weekEvents)
        {
            bool isCompleted = evt.type == EventType.Custom
                ? GameEvents.IsCustomEventCompleted(evt.id)
                : false;

            if (isCompleted)
            {
                // TRUE past: crossed out
                past.Add(evt);
            }
            else
            {
                // NOT completed:
                if (evt.type == EventType.Custom)
                {
                    // If the event is still available regardless of week,
                    // it must stay in "This Week".
                    thisWeek.Add(evt);
                }
                else
                {
                    // Academic events still follow week logic
                    if (evt.week < currentWeek)
                        past.Add(evt);
                    else if (evt.week == currentWeek)
                        thisWeek.Add(evt);
                    else
                        later.Add(evt);
                }
            }
        }
    }

    // Render past (no header)
    foreach (var evt in past)
        CreateRow(evt, isPast: true, null, panelRoot);

    // Render “This Week”
    if (thisWeek.Count > 0)
    {
        InsertHeader("This Week");
        foreach (var evt in thisWeek)
            CreateRow(evt, isPast: false, null, panelRoot);
    }

    // Render “Later”
    if (later.Count > 0)
    {
        InsertHeader("Later");
        foreach (var evt in later)
            CreateRow(evt, isPast: false, null, panelRoot);
    }
}
private void InsertHeader(string label)
{
    var go = Instantiate(groupLabelPrefab, panelRoot);
    var t = go.GetComponent<TMPro.TextMeshProUGUI>();
    if (t) t.text = label;
}

    void BuildSections(Transform root, List<CharacterLocation> charLocs)
    {
        if (!root || _agendaBuckets == null) return;

        foreach (var bucket in _agendaBuckets)
        {
            // Group header ("This Week", "Later")
            GameObject go = Instantiate(groupLabelPrefab, root);
            var tmproText = go.GetComponent<TextMeshProUGUI>();
            if (tmproText != null)
                tmproText.text = bucket.label;

            // Events in this bucket (all future-oriented → not past)
            foreach (var evt in bucket.events)
            {
                CreateRow(evt, isPast: false, charLocs, root);
            }
        }
    }
    void CreateRow(EventInfo evt, bool isPast,
        List<CharacterLocation> charLocs,
        Transform root)
    {
        string title = evt.label;                         // from EventInfo
        Debug.Log($"Inserting Row: {title}");
        var row = Instantiate(agendaRowPrefab, root);

        string key    = BuildEventKey(evt);               // stable-ish id for click routing

        var eventButton = row.GetComponent<AgendaEventButton>();
        eventButton.Bind(title, evt.type, isPast,
            () => onEventSelected?.Invoke(key));
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
