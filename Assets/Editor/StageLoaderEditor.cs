using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(StageLoader))]
public class StageLoaderEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();

        if (GUILayout.Button("Unlock All Stages"))
        {
            StageLoader stageLoader = (StageLoader)target;
            stageLoader.UnlockAllStagesForDebug();
            EditorUtility.SetDirty(stageLoader);
        }

        if (GUILayout.Button("Reset Stage Progress"))
        {
            StageLoader stageLoader = (StageLoader)target;
            stageLoader.ResetStageProgress();
            EditorUtility.SetDirty(stageLoader);
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("Reset Help Unlocks"))
        {
            StageLoader stageLoader = (StageLoader)target;
            stageLoader.ResetHelpUnlocks();
            EditorUtility.SetDirty(stageLoader);
        }

        if (GUILayout.Button("Reset Help Shown Flags"))
        {
            StageLoader stageLoader = (StageLoader)target;
            stageLoader.ResetHelpShownFlags();
            EditorUtility.SetDirty(stageLoader);
        }

        if (GUILayout.Button("Reset All Help Progress"))
        {
            StageLoader stageLoader = (StageLoader)target;
            stageLoader.ResetAllHelpProgress();
            EditorUtility.SetDirty(stageLoader);
        }
    }
}
