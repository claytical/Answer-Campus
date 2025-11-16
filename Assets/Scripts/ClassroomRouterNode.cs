using UnityEngine;
using System.Collections.Generic;
using FMODUnity;

namespace VNEngine
{
    public class ClassroomRouterNode : Node
    {
        public string sceneName = "Lecture Hall";
 
        [System.Serializable]
        public class StageConversation
        {
            public int stage;
            public ConversationManager conversation;
        }
        
        [Header("Exams")]
        public ConversationManager midtermConversation;
        public ConversationManager finalConversation;
        public int midtermWeek = 7;
        public int finalWeek = 15;

        [Header("Auto-Warp Missing Stages")] 
        public bool autoWarpToMidterm = false;
        public bool autoWarpToFinal = false;

        public override void Run_Node()
        {
            int currentWeek = (int)StatsManager.Get_Numbered_Stat("Week");
            bool routed = false;
            Debug.Log($"[ClassroomRouterNode] Week: {currentWeek}");

            // 1) Hard week overrides that DO NOT mutate stage
            // Final preempts everything on its specific week
            if (currentWeek == finalWeek && finalConversation != null)
            {
                Debug.Log("Routing to final exam (week-gated, exact match).");
                finalConversation.Start_Conversation();
                routed = true;
                return;
            }

            // Midterm preempts everything on its specific week
            if (currentWeek == midtermWeek && midtermConversation != null)
            {
                Debug.Log("Routing to midterm (week-gated, exact match).");
                midtermConversation.Start_Conversation();
                routed = true;
                return;
            }

            // Otherwise, fall through to whatever comes next in the graph
            go_to_next_node = !routed;
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
