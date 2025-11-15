using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "StageRouteIndex", menuName = "AnswerVerse/Stage Route Index")]
public class StageRouteIndex : ScriptableObject
{
    [Serializable]
    public class StageRouteMeta
    {
        public Character character;
        public LocationData location;
        public int stage;
        [Min(0)] public int unlockWeek;
    }

    // Keep it serialized, give new assets a default list,
    // but don't nuke existing serialized data.
    [SerializeField]
    private List<StageRouteMeta> routes = new List<StageRouteMeta>();
    public IReadOnlyList<StageRouteMeta> Routes => routes;
#if UNITY_EDITOR
    private void OnValidate()
    {
        // Repair old assets that might have null here
        if (routes == null)
            routes = new List<StageRouteMeta>();
    }
    public void EditorReplaceRoutes(List<StageRouteMeta> newRoutes)
    {
        routes.Clear();
        if (newRoutes != null)
            routes.AddRange(newRoutes);
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif
}