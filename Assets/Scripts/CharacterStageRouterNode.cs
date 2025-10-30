using UnityEngine;
using System.Collections.Generic;
using FMOD.Studio;
using FMODUnity;


namespace VNEngine
{
    public class CharacterStageRouterNode : Node
    {
        [System.Serializable]
        public class StageConversation
        {
            public Character character; // Exact match, e.g., "Breanna"
            public int stage;
            [Min(0)] public int unlockWeek = 0;
            public ConversationManager conversation;
        }

        public string currentSceneName; // Match PlayerPrefsExtra location value (e.g., "Library")
        public EventReference ambientFMODEventName;
        public EventReference musicFMODEventName;
        public List<StageConversation> routes;
        public List<ConversationManager> fallbackConversations;
        public ConversationManager repeatableFallback;
        private bool hasAudioManager = false;
        private EventInstance musicFMODEvent;
        private EventInstance ambientFMODEvent;
public override void Run_Node()
{
    if (FMODAudioManager.Instance != null)
    {

        if (!musicFMODEventName.IsNull) { FMODAudioManager.Instance.PlayMusic(musicFMODEventName); }

        if (!ambientFMODEventName.IsNull) { FMODAudioManager.Instance.PlayAmbient(ambientFMODEventName); }
    }

    List<CharacterLocation> characterLocations = PlayerPrefsExtra.GetList<CharacterLocation>("characterLocations", new List<CharacterLocation>());
    CharacterLocation? selected = null;

    // Find the first character placed on the map for this scene
    foreach (CharacterLocation loc in characterLocations)
    {
        if (loc.location == currentSceneName)
        {
            selected = loc;
            break;
        }
    }

    if (selected.HasValue)
    {
        var loc = selected.Value;
        string statKey = $"{loc.character} - {currentSceneName} - Stage";
        float stage = StatsManager.Get_Numbered_Stat(statKey);
        int currentWeek = Mathf.RoundToInt(StatsManager.Get_Numbered_Stat("Week"));
        foreach (StageConversation route in routes)
        {
            if (route.character == loc.character && route.stage == stage)
            {
                Debug.Log($"Routing {loc.character} at stage {stage} to conversation: {route.conversation.name}");

                if (currentWeek < route.unlockWeek)
                {
                    Debug.Log(
                        $"[Router] Matched {loc.character} stage {stage}, " +
                        $"but locked until week {route.unlockWeek}. Current week: {currentWeek}. " +
                        $"Falling through to fallbacks.");
                    break; // do NOT start this conversation yet; try fallbacks below
                }

                Debug.Log($"Routing {loc.character} at stage {stage} (unlockWeek {route.unlockWeek}) to: {route.conversation.name}");

                // Set the parameter dynamically
                if (loc.character == Character.CHARLI)
                    FMODAudioManager.Instance.SetDrums(0);
                else if (loc.character == Character.LEILANI)
                    FMODAudioManager.Instance.SetDrums(1);
                else if (loc.character == Character.DEEPAK)
                    FMODAudioManager.Instance.SetDrums(2);
                else
                    FMODAudioManager.Instance.SetDrums(3);

                route.conversation.Start_Conversation();
                Finish_Node();
                return;
            }
        }
    }

    // Fallbacks (unchanged)
    for (int i = 0; i < fallbackConversations.Count; i++)
    {
        var fallback = fallbackConversations[i];
        if (fallback == null) continue;

        string fallbackKey = $"Seen - {currentSceneName} - Fallback {i}";

        if (!StatsManager.Get_Boolean_Stat(fallbackKey))
        {
            Debug.Log($"Routing unseen fallback {i}: {fallback.name}");
            StatsManager.Set_Boolean_Stat(fallbackKey, true);
            fallback.Start_Conversation();
            go_to_next_node = false;
            Finish_Node();
            return;
        }
    }

    // Repeatable fallback
    if (repeatableFallback != null)
    {
        Debug.Log($"All fallbacks seen. Routing to repeatable fallback: {repeatableFallback.name}");
        repeatableFallback.Start_Conversation();
        go_to_next_node = false;
        Finish_Node();
        return;
    }
    else
    {
        Debug.Log("No matching character/stage found, and no repeatable fallback.");
    }

    go_to_next_node = false;
    Finish_Node();
    return;

}

        public override void Button_Pressed() { }
        public override void Finish_Node()
        {
            StopAllCoroutines();
            base.Finish_Node();
        }
    }

}
