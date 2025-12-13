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
    public StageRouteMeta FindRoute(Character character, string sceneName, int stage)
    {
        if (Routes == null) return null;

        foreach (var meta in Routes)
        {
            if (meta == null || meta.location == null) continue;

            // Adjust the property if your LocationData calls it something else
            var locName = meta.location.sceneName;
            if (meta.character == character &&
                meta.stage == stage &&
                locName == sceneName)
            {
                return meta;
            }
        }

        return null;
    }
    public IReadOnlyList<StageRouteMeta> GetRoutesForScene(string sceneName)
    {
        if (Routes == null || string.IsNullOrEmpty(sceneName))
            return Array.Empty<StageRouteMeta>();

        var list = new List<StageRouteMeta>();
        foreach (var meta in Routes)
        {
            if (meta == null || meta.location == null) continue;

            // Adjust this if your LocationData uses a different field name
            var locName = meta.location.sceneName;
            if (string.Equals(locName, sceneName, StringComparison.Ordinal))
            {
                list.Add(meta);
            }
        }
        return list;
    }

    public bool HasRoute(Character character, string sceneName, int stage)
    {
        return FindRoute(character, sceneName, stage) != null;
    }

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