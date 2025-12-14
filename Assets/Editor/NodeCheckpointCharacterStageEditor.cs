#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(VNEngine.NodeCheckpointCharacterStage))]
public class NodeCheckpointCharacterStageEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Draw everything except 'stage' and 'useManualStage' first, in a controlled order
        EditorGUILayout.PropertyField(serializedObject.FindProperty("week"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("skipWeeks"));

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("character"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("scene"));

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("stageRouteIndex"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("completeAgendaEvent"));

        EditorGUILayout.Space();
        // Draw the checkbox
        var useManualProp = serializedObject.FindProperty("useManualStage");
        EditorGUILayout.PropertyField(useManualProp, new GUIContent("Use Manual Stage"));

        // Only show 'stage' when useManualStage == true
        if (useManualProp.boolValue)
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("stage"));
        }

        serializedObject.ApplyModifiedProperties();
    }
}
#endif