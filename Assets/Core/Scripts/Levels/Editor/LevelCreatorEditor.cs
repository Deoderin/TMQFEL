using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TMQFEL.Levels.Editor
{
    [CustomEditor(typeof(LevelCreator))]
    public sealed class LevelCreatorEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(8f);

            var levelCreator = (LevelCreator)target;

            if (GUILayout.Button("Build Level", GUILayout.Height(28f)))
            {
                levelCreator.BuildLevel();
                EditorSceneManager.MarkSceneDirty(levelCreator.gameObject.scene);
            }

            if (GUILayout.Button("Clear Generated", GUILayout.Height(24f)))
            {
                levelCreator.ClearGenerated();
                EditorSceneManager.MarkSceneDirty(levelCreator.gameObject.scene);
            }
        }
    }
}
