using System.Linq;
using UnityEditor;
using UnityEngine;
// ReSharper disable UnusedMember.Local

namespace UniEnumExtension
{
    public sealed class Program : EditorWindow
    {
        [MenuItem("Window/UniEnumExtension")]
        public static void Open() => GetWindow<Program>(typeof(SceneView));

        private SerializedObject serializedObject;
        private SerializedProperty enablesProperty;
        ProgramStatus programStatus;

        public void OnEnable()
        {
            if (serializedObject != null) return;

            var guidArray = AssetDatabase.FindAssets("t:" + nameof(ProgramStatus));
            programStatus = AssetDatabase.LoadAssetAtPath<ProgramStatus>(AssetDatabase.GUIDToAssetPath(guidArray[0]));
            serializedObject = new SerializedObject(programStatus);
            enablesProperty = serializedObject.FindProperty(nameof(ProgramStatus.Enables));
        }

        [InitializeOnLoadMethod]
        private static void PostCompiled()
        {
            var guidArray = AssetDatabase.FindAssets("t:" + nameof(ProgramStatus));
            var programStatus = AssetDatabase.LoadAssetAtPath<ProgramStatus>(AssetDatabase.GUIDToAssetPath(guidArray[0]));
            EnumExtender.Main(programStatus.OutputPaths.Where((_, index) => programStatus.Enables[index]));
        }

        // ReSharper disable once InconsistentNaming
        public void OnGUI()
        {
            bool changed = false;
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            if (GUILayout.Button("Update Assemblies"))
            {
                programStatus.Initialize();
                serializedObject = new SerializedObject(programStatus);
                enablesProperty = serializedObject.FindProperty(nameof(ProgramStatus.Enables));
            }
            serializedObject.Update();
            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Select All"))
                {
                    changed = true;
                    for (var i = 0; i < programStatus.Enables.Length; i++)
                    {
                        enablesProperty.GetArrayElementAtIndex(i).boolValue = true;
                    }
                }
                if (GUILayout.Button("Deselect All"))
                {
                    changed = true;
                    for (var i = 0; i < programStatus.Enables.Length; i++)
                    {
                        enablesProperty.GetArrayElementAtIndex(i).boolValue = false;
                    }
                }
            }
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Target Assemblies", EditorStyles.boldLabel);
            for (var i = 0; i < programStatus.Enables.Length; i++)
            {
                ref var programStatusEnable = ref programStatus.Enables[i];
                var enabled = EditorGUILayout.ToggleLeft(new GUIContent(programStatus.Names[i], programStatus.OutputPaths[i]), programStatusEnable, "button");
                if (!(enabled ^ programStatusEnable)) continue;
                enablesProperty.GetArrayElementAtIndex(i).boolValue = enabled;
                changed = true;
            }
            if (changed)
            {
                serializedObject.ApplyModifiedProperties();
            }
        }
    }
}
