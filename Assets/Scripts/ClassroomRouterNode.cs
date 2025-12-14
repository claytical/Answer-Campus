using UnityEngine;

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
        
        public override void Run_Node()
        {
            int currentWeek = (int)StatsManager.Get_Numbered_Stat("Week");
            int midWeek     = SemesterHelper.MidtermsWeek;
            int finWeek     = SemesterHelper.FinalsWeek;

            Debug.Log($"[ClassroomRouterNode] Week: {currentWeek} (Mid:{midWeek} Final:{finWeek})");

            // Finals preempts everything on its week, but only if not completed.
            if (currentWeek == finWeek && finalConversation != null)
            {
                bool finalDone = GameEvents.IsCustomEventCompleted(GameEvents.FinalsEventId);
                if (!finalDone)
                {
                    Debug.Log("[ClassroomRouterNode] Routing to FINAL exam (due, not completed).");
                    finalConversation.Start_Conversation();
                    go_to_next_node = false;
                    return;
                }

                Debug.Log("[ClassroomRouterNode] Final already completed; falling through.");
            }

            // Midterm preempts everything on its week, but only if not completed.
            if (currentWeek == midWeek && midtermConversation != null)
            {
                bool midDone = GameEvents.IsCustomEventCompleted(GameEvents.MidtermsEventId);
                if (!midDone)
                {
                    Debug.Log("[ClassroomRouterNode] Routing to MIDTERM exam (due, not completed).");
                    midtermConversation.Start_Conversation();
                    go_to_next_node = false;
                    return;
                }

                Debug.Log("[ClassroomRouterNode] Midterm already completed; falling through.");
            }

            // Otherwise, fall through to whatever comes next in the graph (e.g., CharacterStageRouterNode).
            go_to_next_node = true;
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
