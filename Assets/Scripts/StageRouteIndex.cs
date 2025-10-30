using System;
using System.Collections.Generic;
using UnityEngine;
using VNEngine;

[CreateAssetMenu(fileName = "StageRouteIndex", menuName = "AnswerVerse/Stage Route Index")]
public class StageRouteIndex : ScriptableObject
{
    [Serializable]
    public class StageRouteMeta
    {
        public Character character;         // e.g., Breanna
        public LocationData location;                // e.g., "DiningHall"
        public int stage;                   // e.g., 3
        [Min(0)] public int unlockWeek;     // e.g., 8 (0 = always)
    }

    public List<StageRouteMeta> routes = new();
}