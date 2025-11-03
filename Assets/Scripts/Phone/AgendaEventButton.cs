using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AgendaEventButton : MonoBehaviour
{
    [Header("UI")]
    public Button button;              // root button on the prefab
    public TMP_Text titleText;         // main label (uses EventInfo.label)
    public TMP_Text timeText;          // secondary label (e.g., "Week 6")

    [Header("Attendees (optional)")]
    public Transform attendeesRoot;    // container for chips (can be null)
    public FriendChip attendeeChipPrefab; // optional
    public int maxAttendeeChips = 6;

    public void Bind(
        string title,
        string timeLabel,
        List<Character> attendees,
        Action onClick)
    {
        if (titleText) titleText.text = title ?? "";
        if (timeText)  timeText.text  = timeLabel ?? "";

        // Attendees (optional)
        if (attendeesRoot && attendeeChipPrefab)
        {
            for (int i = attendeesRoot.childCount - 1; i >= 0; i--)
                Destroy(attendeesRoot.GetChild(i).gameObject);

            var count = attendees?.Count ?? 0;
            var toShow = Mathf.Min(count, Mathf.Max(0, maxAttendeeChips));

            for (int i = 0; i < toShow; i++)
            {
                var chip = Instantiate(attendeeChipPrefab, attendeesRoot);
                // If you don’t want names here, pass "" to show icon-only later
                chip.Bind(attendees[i], portrait: null, displayNameOverride: "");
            }

            if (count > toShow)
            {
                var overflow = Instantiate(attendeeChipPrefab, attendeesRoot);
                overflow.BindOverflow(count - toShow);
            }
        }

        if (button)
        {
            button.onClick.RemoveAllListeners();
            if (onClick != null) button.onClick.AddListener(() => onClick());
        }
    }
}