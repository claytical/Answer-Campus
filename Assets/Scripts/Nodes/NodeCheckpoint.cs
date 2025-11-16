using UnityEngine;

namespace VNEngine
{
    public class NodeCheckpoint : Node
    {
        // Minimum week this checkpoint should bump the player to.
        // If current week is already >= this, we just +1 instead.
        public int week;

        public override void Run_Node()
        {
            // 1) Read current week as int
            float storedWeek = StatsManager.Get_Numbered_Stat("Week");
            int currentWeek = Mathf.FloorToInt(storedWeek);

            Debug.Log($"[NodeCheckpoint] Current week: {currentWeek}, checkpoint min week: {week}");

            int newWeek;

            if (week <= 0)
            {
                // Pure +1 advancement mode
                newWeek = currentWeek + 1;
                Debug.Log($"[NodeCheckpoint] No minimum week set → advancing to {newWeek}");
            }
            else if (currentWeek < week)
            {
                newWeek = week;
                Debug.Log($"[NodeCheckpoint] Forcing advancement to checkpoint week {newWeek}");
            }
            else
            {
                newWeek = currentWeek + 1;
                Debug.Log($"[NodeCheckpoint] Normal advancement → week {newWeek}");
            }
            
            StatsManager.Set_Numbered_Stat("Week", newWeek);

            // Save after we’ve updated week
            CheckpointManager.SaveCheckpoint();

            Finish_Node();
        }

        public override void Button_Pressed()
        {
            // Intentionally no-op; this node runs instantly.
        }

        public override void Finish_Node()
        {
            StopAllCoroutines();
            base.Finish_Node();
        }
    }
}