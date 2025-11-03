using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LocationButton : MonoBehaviour
{
    [Header("UI")]
    public Button button;                     // assign the Button on this prefab
    public TextMeshProUGUI locationName;      // the location label
    public Transform friendsRoot;             // container for chips (Horizontal/Wrap layout)

    [Header("Prefabs")]
    public FriendChip friendChipPrefab;       // your FriendChip prefab (with icon + name)

    [Header("Display")]
    public int maxVisibleChips = 6;

    /// <summary>
    /// Configure this button for a location and populate friend chips.
    /// </summary>
    public void Bind(string locName, List<Character> friends, Action onClick)
    {
        if (locationName) locationName.text = locName;

        // Clear chips
        if (friendsRoot)
        {
            for (int i = friendsRoot.childCount - 1; i >= 0; i--)
                Destroy(friendsRoot.GetChild(i).gameObject);
        }

        // Add chips
        int count = friends?.Count ?? 0;
        int toShow = Mathf.Min(count, Mathf.Max(0, maxVisibleChips));

        for (int i = 0; i < toShow; i++)
        {
            var chip = Instantiate(friendChipPrefab, friendsRoot);
            // If you have a portrait lookup, pass it as the 2nd param. For now, null.
            chip.Bind(friends[i], portrait: null, displayNameOverride: null);
        }

        // Overflow as a compact FriendChip (“+N” text, no icon)
        if (count > toShow && friendChipPrefab != null)
        {
            var overflowChip = Instantiate(friendChipPrefab, friendsRoot);
            overflowChip.BindOverflow(count - toShow);
        }

        // Click
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            if (onClick != null) button.onClick.AddListener(() => onClick());
        }
    }
}