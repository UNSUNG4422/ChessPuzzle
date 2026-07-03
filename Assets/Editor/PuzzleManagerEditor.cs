using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PuzzleManager))]
public class PuzzleManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Developer Debug", EditorStyles.boldLabel);

        PuzzleManager puzzleManager = (PuzzleManager)target;

        using (new EditorGUI.DisabledScope(!Application.isPlaying))
        {
            if (GUILayout.Button("Debug Clear Current Stage"))
            {
                puzzleManager.DebugClearCurrentStage();
            }
        }

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Debug Clear Current Stage は再生中のみ使用できます。", MessageType.Info);
        }
    }
}
