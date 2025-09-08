// Editor/FootballRequirementDrawer.cs
using UnityEditor;
using UnityEngine;
using VNEngine;

[CustomPropertyDrawer(typeof(FootballRequirement))]
public class FootballRequirementDrawer : PropertyDrawer
{
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        var checkProp = property.FindPropertyRelative("check");
        bool showThreshold = ShouldShowThreshold((FootballCheckType)checkProp.enumValueIndex);

        // One line for "check"; add a second line if threshold is visible
        int lines = showThreshold ? 2 : 1;
        return lines * (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing);
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var checkProp = property.FindPropertyRelative("check");
        var thresholdProp = property.FindPropertyRelative("threshold");

        // Draw "check"
        Rect row = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        EditorGUI.PropertyField(row, checkProp);

        // Draw "threshold" only when needed
        var mode = (FootballCheckType)checkProp.enumValueIndex;
        if (ShouldShowThreshold(mode))
        {
            row.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            string thresholdLabel = (mode == FootballCheckType.WinsAtLeast)
                ? "Wins ≥"
                : "Win Rate ≥ (0..1)";
            EditorGUI.PropertyField(row, thresholdProp, new GUIContent(thresholdLabel));
        }
    }

    private bool ShouldShowThreshold(FootballCheckType mode)
    {
        return mode == FootballCheckType.WinsAtLeast || mode == FootballCheckType.WinRateAtLeast;
    }
}