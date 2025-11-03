// Phone.cs  (refactor – keep animator + public hooks)
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class Phone : MonoBehaviour
{
    public TextMeshProUGUI title;
    [Header("Panels")]
    public GameObject mapPanel;
    public GameObject friendsPanel;
    public GameObject agendaPanel;
    
    [Header("Sub-Views")]
    public MapView mapView;
    public FriendsView friendsView;
    public AgendaView agendaView;

    private Animator anim;

    void Start()
    {
        anim = GetComponent<Animator>();
        RefreshNotificationBadge();
        ShowMap(); // default
        
    }

    // ---- Public tab actions (UI buttons) ----
    public void ShowMap()
    {
        title.text = "Map";
        TogglePanels(map:true);
        mapView.Render(PhoneDataService.GetCharacterLocations());
    }

    public void ShowFriends()
    {
        title.text = "Friends";
        TogglePanels(friends:true);
        friendsView.Render();
        ClearNotifications(); // opening inbox clears
    }

    public void ShowAgenda()
    {
        title.text = "Agenda";
        TogglePanels(agenda:true);
        agendaView.Render();
    }

    // ---- Notifications ----
    public void RefreshNotificationBadge()
    {
        var threads = PhoneDataService.GetMessageThreads();
        bool hasNew = threads.Count > 0;
        if (anim != null)
            anim.SetTrigger(hasNew ? "notification" : "default"); // your existing triggers
    }

    public void ClearNotifications()
    {
        if (anim != null) anim.SetTrigger("default");
    }

    private void TogglePanels(bool map=false, bool friends=false, bool agenda=false)
    {
        if (mapPanel) mapPanel.SetActive(map);
        if (friendsPanel) friendsPanel.SetActive(friends);
        if (agendaPanel) agendaPanel.SetActive(agenda);
    }
}