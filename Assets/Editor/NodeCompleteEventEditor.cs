#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using VNEngine; // adjust if your namespace is different

[CustomEditor(typeof(NodeCompleteEvent))]
public class NodeCompleteEventEditor : Editor
{
    SerializedProperty idSourceProp;
    SerializedProperty customIdProp;

    SerializedProperty stageRouteIndexProp;
    SerializedProperty characterProp;
    SerializedProperty sceneProp;
    SerializedProperty useCurrentStageProp;
    SerializedProperty explicitStageProp;

    void OnEnable()
    {
        idSourceProp        = serializedObject.FindProperty("idSource");
        customIdProp        = serializedObject.FindProperty("customId");

        stageRouteIndexProp = serializedObject.FindProperty("stageRouteIndex");
        characterProp       = serializedObject.FindProperty("character");
        sceneProp           = serializedObject.FindProperty("scene");
        useCurrentStageProp = serializedObject.FindProperty("useCurrentStage");
        explicitStageProp   = serializedObject.FindProperty("explicitStage");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Event ID Source (always visible)
        EditorGUILayout.PropertyField(idSourceProp, new GUIContent("ID Source"));

        // Cast the enum for clarity
        var mode = (NodeCompleteEvent.IdSourceMode)idSourceProp.enumValueIndex;

        EditorGUILayout.Space();

        if (mode == NodeCompleteEvent.IdSourceMode.CustomId)
        {
            // Only show the custom ID field
            EditorGUILayout.PropertyField(customIdProp, new GUIContent("Custom Event Id"));
        }
        else // StageRoute mode
        {
            EditorGUILayout.LabelField("Stage Route Lookup", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(stageRouteIndexProp, new GUIContent("Stage Route Index"));
            EditorGUILayout.PropertyField(characterProp,       new GUIContent("Character"));
            EditorGUILayout.PropertyField(sceneProp,           new GUIContent("Scene"));

            EditorGUILayout.PropertyField(
                useCurrentStageProp,
                new GUIContent("Use Current Stage Stat",
                    "If true, read the current stage from StatsManager for this character/scene. " +
                    "If false, use the explicit stage below.")
            );

            if (!useCurrentStageProp.boolValue)
            {
                EditorGUILayout.PropertyField(explicitStageProp, new GUIContent("Explicit Stage"));
            }
        }

        serializedObject.ApplyModifiedProperties();
    }
}
#endif
