using UnityEngine;
using System.Collections.Generic;
using FMODUnity;

namespace VNEngine
{
    public class ClassroomRouterNode : Node
    {
        public Character character = Character.LEILANI; // Fixed: this node only applies to Leilani
        public string sceneName = "Lecture Hall";
        public EventReference ambientFMODEventName;
        public EventReference musicFMODEventName;
        public EventReference characterMODEventName;

        [System.Serializable]
        public class StageConversation
        {
            public int stage;
            public ConversationManager conversation;
        }

        [Header("Standard Routing")]
        public List<StageConversation> stageRoutes;

        [Header("Exam Overrides")]
        public ConversationManager midtermConversation;
        public ConversationManager finalConversation;
        public int midtermWeek = 7;
        public int finalWeek = 15;

        [Header("Auto-Warp Missing Stages")] 
        public bool autoWarpToMidterm = true;
        public bool autoWarpToFinal = true;

        [Header("Fallback Conversations")]
        public List<ConversationManager> fallbackConversations;

        public override void Run_Node()
        {
            string statKey = $"{character} - {sceneName} - Stage";
            int currentStage = (int)StatsManager.Get_Numbered_Stat(statKey);
            int currentWeek = (int)StatsManager.Get_Numbered_Stat("Week");

            Debug.Log($"[ClassroomRouterNode] Stage: {currentStage}, Week: {currentWeek}");

            // Final exam override
            if (currentWeek >= finalWeek && finalConversation != null)
            {
                if (autoWarpToFinal && currentStage < 7)
                {
                    StatsManager.Set_Numbered_Stat(statKey, 7);
                }

                Debug.Log("Routing to final exam.");
                finalConversation.Start_Conversation();
                Finish_Node();
                return;
            }

            // Midterm override
            if (currentWeek >= midtermWeek && currentStage < 2 && midtermConversation != null)
            {
                if (autoWarpToMidterm)
                {
                    StatsManager.Set_Numbered_Stat(statKey, 2);
                }

                Debug.Log("Routing to midterm.");
                midtermConversation.Start_Conversation();
                Finish_Node();
                return;
            }

            // Standard stage-based routing
            foreach (StageConversation route in stageRoutes)
            {
                if (route.stage == currentStage)
                {
                    Debug.Log($"Routing to stage {currentStage} conversation.");
                    route.conversation.Start_Conversation();
                    if (!ambientFMODEventName.IsNull)
                    {
                        FMODAudioManager.Instance.PlayMusic(ambientFMODEventName);
                    }

                    if (!musicFMODEventName.IsNull)
                    {
                        FMODAudioManager.Instance.PlayMusic(musicFMODEventName);
                    }

                    if (!characterMODEventName.IsNull)
                    {
                        FMODAudioManager.Instance.PlayMusic(characterMODEventName);
                    }
                    Finish_Node();
                    return;
                }
            }

            // Fallback conversations (e.g., before classes begin)
            foreach (var fallback in fallbackConversations)
            {
                if (fallback != null)
                {
                    Debug.Log("No matching stage or exam. Using fallback conversation.");
                    fallback.Start_Conversation();
                    Finish_Node();
                    return;
                }
            }

            Debug.Log("No matching conversation found, and no fallback available.");
            Finish_Node();
        }

        public override void Button_Pressed() { }

        public override void Finish_Node()
        {
            StopAllCoroutines();
            base.Finish_Node();
        }
    }
}
