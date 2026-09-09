using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(LevelThreeTypingGameplay))]
public sealed class LevelThreeTypingGameplayEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DrawPropertiesExcluding(serializedObject, "m_Script", "mainLineDurations");

        SerializedProperty lines = serializedObject.FindProperty("mainLines");
        SerializedProperty durations = serializedObject.FindProperty("mainLineDurations");
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Per-Line Speech Timing", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Each duration matches the speech line with the same number. A missing entry uses Main Seconds Per Line.",
            MessageType.Info);

        if (durations.arraySize != lines.arraySize) durations.arraySize = lines.arraySize;
        for (int index = 0; index < lines.arraySize; index++)
        {
            string preview = lines.GetArrayElementAtIndex(index).stringValue
                .Replace("[TYPE:", string.Empty).Replace("]", string.Empty);
            if (preview.Length > 48) preview = preview.Substring(0, 45) + "...";
            SerializedProperty duration = durations.GetArrayElementAtIndex(index);
            duration.floatValue = Mathf.Max(0.1f,
                EditorGUILayout.FloatField($"Line {index + 1:00} — {preview}", Mathf.Max(0.1f, duration.floatValue)));
        }

        serializedObject.ApplyModifiedProperties();
    }
}
