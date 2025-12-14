using UnityEngine;

namespace VNEngine
{
    public class NodeCompleteEvent : Node
    {
        public enum IdSourceMode
        {
            CustomId,   // explicit string, e.g. "Onboarding_StudyMiniGameOnce"
            StageRoute  // derived from (Character, Scene, Stage)
        }

        [Header("Event ID Source")]
        public IdSourceMode idSource = IdSourceMode.CustomId;

        [Tooltip("Used when IdSourceMode = CustomId")]
        public string customId;

        [Header("StageRoute Lookup")]
        public StageRouteIndex stageRouteIndex;
        public Character character;
        public string scene;

        [Tooltip("If true, use the current stage stat for this character/scene. " +
                 "If false, use explicitStage.")]
        public bool useCurrentStage = true;

        [Tooltip("Only used when useCurrentStage is false.")]
        public int explicitStage = -1;

        public override void Run_Node()
        {
            string id = ResolveEventId();

            if (string.IsNullOrEmpty(id))
            {
                Debug.LogWarning("[NodeCompleteEvent] No event id resolved; nothing to complete.");
                Finish_Node();
                return;
            }

            if (!GameEvents.HasCustomEvent(id))
            {
                Debug.LogWarning(
                    $"[NodeCompleteEvent] No custom event with id '{id}' found. " +
                    "Did you register it with GameEvents.RegisterOrUpdateCustomEvent?");
                Finish_Node();
                return;
            }

            GameEvents.MarkCustomEventCompleted(id);
            Debug.Log($"[NodeCompleteEvent] Completed event '{id}'");

            Finish_Node();
        }

        private string ResolveEventId()
        {
            switch (idSource)
            {
                case IdSourceMode.CustomId:
                    return customId;

                case IdSourceMode.StageRoute:
                    return ResolveStageRouteEventId();

                default:
                    return null;
            }
        }

        private string ResolveStageRouteEventId()
        {
            if (stageRouteIndex == null)
            {
                Debug.LogWarning("[NodeCompleteEvent] StageRouteIndex is null; cannot resolve StageRoute-based event id.");
                return null;
            }

            if (character == Character.NONE)
            {
                Debug.LogWarning("[NodeCompleteEvent] Character is NONE; cannot resolve StageRoute-based event id.");
                return null;
            }

            if (string.IsNullOrEmpty(scene))
            {
                Debug.LogWarning("[NodeCompleteEvent] Scene is empty; cannot resolve StageRoute-based event id.");
                return null;
            }

            int stage;
            if (useCurrentStage)
            {
                string stageKey = $"{character} - {scene} - Stage";
                stage = Mathf.RoundToInt(StatsManager.Get_Numbered_Stat(stageKey));
            }
            else
            {
                stage = explicitStage;
            }

            if (stage < 0)
            {
                Debug.LogWarning("[NodeCompleteEvent] Stage is negative; cannot resolve StageRoute-based event id.");
                return null;
            }

            // Optional: validate against StageRouteIndex to catch typos
            if (!stageRouteIndex.HasRoute(character, scene, stage))
            {
                Debug.LogWarning(
                    $"[NodeCompleteEvent] No StageRouteIndex entry for {character} @ '{scene}' stage {stage}. " +
                    "You may be trying to complete an event that was never registered.");
            }

            return GameEvents.BuildStageRouteEventId(character, scene, stage);
        }

        public override void Button_Pressed()
        {
            // Intentionally empty
        }

        public override void Finish_Node()
        {
            StopAllCoroutines();
            base.Finish_Node();
        }
    }
}
