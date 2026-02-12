using UnityEngine;
using System.Collections;
using System.Collections.Generic;
namespace VNEngine
{
    public class NodeMessage : Node
    {
        public TextMessage textMessage;

        // New: configure this per-node in the inspector
        public int showAfterWeek = 0; // 0 => show immediately

        public override void Run_Node()
        {
            int currentWeek = Mathf.RoundToInt(StatsManager.Get_Numbered_Stat("Week"));

            // Stamp the message with its unlock condition
            if (textMessage != null)
                textMessage.unlockWeek = showAfterWeek;

            List<TextMessage> messages = PlayerPrefsExtra.GetList<TextMessage>("messages", new List<TextMessage>());

            if (!messages.Contains(textMessage))
            {
                Debug.Log("New Text Message From : " + textMessage.from);
                messages.Add(textMessage);
                PlayerPrefsExtra.SetList("messages", messages);

                // Only trigger "new activity" if it's actually visible now
                if (showAfterWeek <= 0 || currentWeek >= showAfterWeek)
                    StatsManager.Set_Boolean_Stat("PhoneHasNewActivity", true);
            }
            else
            {
                Debug.Log("Duplicate Message From : " + textMessage.from);
            }

            Finish_Node();
        }
    }
}
