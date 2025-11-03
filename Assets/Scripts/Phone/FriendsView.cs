// FriendsView.cs
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FriendsView : MonoBehaviour
{
    [Header("UI")]
    public Transform listRoot;               // Container for friend buttons
    public GameObject threadButtonPrefab;    // Prefab with ViewTextMessage (name+icon+optional "message" text)
    public TextThreadPanel threadPanel;      // Right-side thread panel to show history & quick replies

    [Header("Data Mapping")]
    public ProfilePicture[] profiles;        // Character -> Sprite mapping (same struct used in TextMessageList)

    [Header("Contacts (optional)")]
    [Tooltip("If empty, we’ll infer contacts from message threads. If set, we’ll list exactly these.")]
    public Character[] contacts;

    // Call this whenever the phone opens Friends tab
    public void Render()
    {
        // Clear list
        foreach (Transform c in listRoot) Destroy(c.gameObject);

        // Build thread index from saved messages
        var allMsgs = TextThreads.GetAll();                                   // stored in PlayerPrefs ("messages")
        var threadsByCharacter = allMsgs
            .GroupBy(m => m.from)
            .ToDictionary(g => g.Key, g => g.OrderBy(m => m.unixTime).ToList()); // oldest->newest

        // Determine who to show: explicit contacts or inferred from threads
        var roster = (contacts != null && contacts.Length > 0)
            ? contacts.Distinct().ToList()
            : threadsByCharacter.Keys.Distinct().OrderBy(c => c.ToString()).ToList();

        // Current locations for “at X” label
        var charLocs = PlayerPrefsExtra.GetList<CharacterLocation>("characterLocations", new List<CharacterLocation>());
        // map Character -> last known location
        var locByChar = charLocs
            .GroupBy(cl => cl.character)
            .ToDictionary(g => g.Key, g => g.Last().location);

        foreach (var who in roster)
        {
            var go = Instantiate(threadButtonPrefab, listRoot);
            var vm = go.GetComponent<ViewTextMessage>(); // re-use your list item script
            if (vm == null)
            {
                Debug.LogError("threadButtonPrefab is missing ViewTextMessage.");
                continue;
            }

            // --- Name
            if (vm.from) vm.from.text = who.ToString();

            // --- Icon
            if (vm.profile)
            {
                var pic = profiles.FirstOrDefault(p => p.character.Equals(who)).picture;
                if (pic) vm.profile.sprite = pic;
            }

            // --- Location (if present)  -> put into vm.message for the row subtitle
            if (vm.message)
            {
                if (locByChar.TryGetValue(who, out var where) && !string.IsNullOrWhiteSpace(where))
                    vm.message.text = where;      // e.g., "Library" or "Stadium"
                else
                    vm.message.text = "";         // no location shown
            }

            // --- Click => open thread
            var btn = go.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() =>
                {
                    // Open the thread UI for this friend
                    threadPanel.Show(who);
                });
            }
        }
    }
}
