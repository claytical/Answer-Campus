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

    void Awake()
    {

    }
    public void Render()
    {
        // Clear existing
        Clear(panelRoot);
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
        _agendaBuckets = new List<PhoneDataService.AgendaBucket>();

        if (thisWeek.Count > 0)
        {
            _agendaBuckets.Add(new PhoneDataService.AgendaBucket() {events = thisWeek, label = "This Week"});
        }

        if (nextWeek.Count > 0)
        {
            _agendaBuckets.Add(new PhoneDataService.AgendaBucket() {events = nextWeek, label = "Next Week"});
        }

        if (later.Count > 0)
        {
            _agendaBuckets.Add(new PhoneDataService.AgendaBucket() {events = later, label = "Later"});
        }
        BuildSections(panelRoot);
    }

    void BuildSections(Transform root)
    {
        var charLocs = PlayerPrefsExtra.GetList<CharacterLocation>(
            "characterLocations", new List<CharacterLocation>());

        if (!root || _agendaBuckets == null) return;
        foreach (var bucket in _agendaBuckets)
        {
            GameObject go = Instantiate(groupLabelPrefab, root);
            foreach (var evt in bucket.events)
            {
                string title = evt.label;                         // from EventInfo
                Debug.Log($"Inserting Row: {title}");
                var row = Instantiate(agendaRowPrefab, root);
                //            string time  = $"Week {evt.week}";                // we only have week granularity
                var attendees = GetAttendeesFor(evt, charLocs);   // simple presence-by-location
                string key = BuildEventKey(evt);                  // stable-ish id for click routing
                var eventButton = row.GetComponent<AgendaEventButton>();
                eventButton.Bind(title, evt.type, attendees, () => onEventSelected?.Invoke(key));

            }
            var tmproText = go.GetComponent<TextMeshProUGUI>();
            if (tmproText != null)
            {
                tmproText.text = bucket.label;
            }
            
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
