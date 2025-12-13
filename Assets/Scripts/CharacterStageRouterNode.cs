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
        public StageRouteIndex stageRouteIndex;
        public List<StageConversation> routes;
        public string currentSceneName; // Match PlayerPrefsExtra location value (e.g., "Library")
        
        
        public EventReference ambientFMODEventName;
        public EventReference musicFMODEventName;
        public List<ConversationManager> fallbackConversations;
        public ConversationManager repeatableFallback;
        private bool hasAudioManager = false;
        private EventInstance musicFMODEvent;
        private EventInstance ambientFMODEvent;
#if UNITY_EDITOR
        private void OnValidate()
        {
            // Only do this in the editor to keep runtime clean
            if (stageRouteIndex == null) return;
            if (string.IsNullOrEmpty(currentSceneName)) return;

            // 1) Look up all index routes for this scene
            var sceneRoutes = stageRouteIndex.GetRoutesForScene(currentSceneName);
            if (sceneRoutes == null) return;

            // 2) Build a new list, preserving existing conversation refs
            var newRoutes = new List<StageConversation>();

            foreach (var meta in sceneRoutes)
            {
                if (meta == null) continue;

                // Try to find an existing row with same character + stage
                StageConversation existing = null;
                if (routes != null)
                {
                    existing = routes.Find(r =>
                        r.character == meta.character &&
                        r.stage == meta.stage);
                }

                var sc = existing ?? new StageConversation();
                sc.character   = meta.character;
                sc.stage       = meta.stage;
                sc.unlockWeek  = meta.unlockWeek;
                // sc.conversation stays whatever you previously assigned
                newRoutes.Add(sc);
            }

            routes = newRoutes;
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif

public override void Run_Node()
{
    if (FMODAudioManager.Instance != null)
    {
        if (!musicFMODEventName.IsNull)
            FMODAudioManager.Instance.PlayMusic(musicFMODEventName);

        if (!ambientFMODEventName.IsNull)
            FMODAudioManager.Instance.PlayAmbient(ambientFMODEventName);
    }

    int currentWeek = Mathf.RoundToInt(StatsManager.Get_Numbered_Stat("Week"));
    List<CharacterLocation> characterLocations =
        PlayerPrefsExtra.GetList<CharacterLocation>("characterLocations",
            new List<CharacterLocation>());

    CharacterLocation? selected = null;
    Debug.Log($"[ROUTER] {characterLocations.Count} Character Locations");

    // Find the first character placed on the map for this scene
    foreach (CharacterLocation loc in characterLocations)
    {
        Debug.Log($"[ROUTER] sceneName={currentSceneName} Character Location={loc} Current Week = {currentWeek}");

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

        foreach (StageConversation route in routes)
        {
            Debug.Log(
                $"[Router] cand: char={route.character} stage={route.stage} " +
                $"unlockWeek={route.unlockWeek} conv={(route.conversation ? route.conversation.name : "NULL")}");

            if (route.character == loc.character && route.stage == stage)
            {
                if (currentWeek < route.unlockWeek)
                {
                    Debug.Log(
                        $"[Router] Matched {loc.character} stage {stage}, " +
                        $"but locked until week {route.unlockWeek}. Current week: {currentWeek}. " +
                        "Falling through to fallbacks.");
                    break;
                }

                Debug.Log(
                    $"Routing {loc.character} at stage {stage} " +
                    $"(unlockWeek {route.unlockWeek}) to: {route.conversation.name}");

                // Drums
                if (loc.character == Character.CHARLI)
                    FMODAudioManager.Instance.SetDrums(0);
                else if (loc.character == Character.LEILANI)
                    FMODAudioManager.Instance.SetDrums(1);
                else if (loc.character == Character.DEEPAK)
                    FMODAudioManager.Instance.SetDrums(2);
                else
                    FMODAudioManager.Instance.SetDrums(3);

                // Clear pin
                var pins = PlayerPrefsExtra.GetList<CharacterLocation>(
                    "characterLocations", new List<CharacterLocation>());

                int removed = pins.RemoveAll(p =>
                    string.Equals(p.location, currentSceneName, System.StringComparison.Ordinal) &&
                    EqualityComparer<Character>.Default.Equals(p.character, loc.character));

                PlayerPrefsExtra.SetList("characterLocations", pins);
                if (removed > 0)
                    Debug.Log($"[LocationRouter] Cleared invite for {loc.character} @ {currentSceneName} after routing.");

                route.conversation.Start_Conversation();
                go_to_next_node = false;

                Finish_Node();
                return;
            }
        }
    }

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
