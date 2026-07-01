using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BoardGenerator))]
public class BoardGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        BoardGenerator boardGenerator = (BoardGenerator)target;

        EditorGUILayout.Space();

        if (GUILayout.Button("Generate Board"))
        {
            Undo.RecordObject(boardGenerator, "Generate Board");
            boardGenerator.GenerateBoard();
            EditorUtility.SetDirty(boardGenerator);
        }

        if (GUILayout.Button("Clear Board"))
        {
            Undo.RecordObject(boardGenerator, "Clear Board");
            boardGenerator.ClearBoard();
            EditorUtility.SetDirty(boardGenerator);
        }
    }
}
