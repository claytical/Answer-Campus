using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace VNEngine
{
    public class NodeEvent : Node
    {
        [Tooltip("Display for Agenda")]
        public EventInfo information;

        // Called when the node is run in a conversation
        private string customId;
        public override void Run_Node()
        {
            // Force this to be treated as a Custom event, regardless of what’s in the inspector
            information.type = EventType.Custom;

            var ev = new CustomEvent
            {
                id       = GetOrGenerateId(),
                name     = string.IsNullOrWhiteSpace(information.label)
                           ? "Unnamed Event"
                           : information.label,
                week     = information.week,
                icon     = information.icon,
                location = information.location,
                unlocked = true
            };

            GameEvents.RegisterOrUpdateCustomEvent(ev);
            Debug.Log(
                $"[NodeEvent] Registered custom agenda event '{ev.name}' " +
                $"(id={ev.id}, week={ev.week}, location={ev.location})");
            StatsManager.Set_Boolean_Stat("PhoneHasNewActivity", true);
            Finish_Node();
        }

        /// <summary>
        /// Generate a stable-ish ID if the writer didn’t supply one.
        /// </summary>
        private string GetOrGenerateId()
        {
            if (!string.IsNullOrEmpty(customId))
                return customId;

            // Sanitize label to something ID-friendly
            string labelPart = string.IsNullOrEmpty(information.label)
                ? "Event"
                : information.label;

            labelPart = new string(labelPart
                .Where(c => char.IsLetterOrDigit(c) || c == '_')
                .ToArray());

            if (string.IsNullOrEmpty(labelPart))
                labelPart = "Event";

            return $"Custom_{information.week}_{labelPart}";
        }

        // No click behavior – this node is fire-and-forget
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
