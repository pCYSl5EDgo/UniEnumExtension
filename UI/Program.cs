using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
// ReSharper disable UnusedMember.Local

namespace UniEnumExtension
{
    public sealed class Program : EditorWindow
    {
        [MenuItem("Tools/UniEnumExtension/Settings")]
        public static void Open() => GetWindow<Program>();

        private SerializedObject serializedObject;
        private SerializedProperty enablesProperty;
        private SerializedProperty shouldProcessAllProperty;
        private ProgramStatus programStatus;

        public void OnEnable()
        {
            var guidArray = AssetDatabase.FindAssets("t:" + nameof(ProgramStatus));
            if (programStatus == null)
            {
                programStatus = AssetDatabase.LoadAssetAtPath<ProgramStatus>(AssetDatabase.GUIDToAssetPath(guidArray[0]));
            }
            programStatus.Initialize();
            serializedObject = new SerializedObject(programStatus);
            enablesProperty = serializedObject.FindProperty(nameof(ProgramStatus.Enables));
            shouldProcessAllProperty = serializedObject.FindProperty(nameof(programStatus.ShouldProcessAllAssemblies));
        }

        [InitializeOnLoadMethod]
        private static void PostCompiled()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) { return; }

            var guidArray = AssetDatabase.FindAssets("t:" + nameof(ProgramStatus));
            if (guidArray.Length == 0) return;
            var programStatus = AssetDatabase.LoadAssetAtPath<ProgramStatus>(AssetDatabase.GUIDToAssetPath(guidArray[0]));
            var assemblyPaths = programStatus.OutputPaths.Where((_, index) => programStatus.Enables[index]);
            using (var extender = new EnumExtender(searchDirectory: new[] { Path.GetDirectoryName(UnityEditorInternal.InternalEditorUtility.GetEngineCoreModuleAssemblyPath()) }))
            {
                extender.Extend(assemblyPaths);
            }
        }

        public void OnGUI()
        {
            var changed = false;
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            serializedObject.Update();
            EditorGUILayout.Space();
            var shouldProcess = shouldProcessAllProperty.boolValue;
            var newShould = EditorGUILayout.ToggleLeft("ProcessRewriteToString All Assemblies", shouldProcess, "button");
            if (newShould ^ shouldProcess)
            {
                shouldProcessAllProperty.boolValue = newShould;
                if (newShould)
                {
                    for (var i = 0; i < programStatus.Enables.Length; i++)
                    {
                        enablesProperty.GetArrayElementAtIndex(i).boolValue = true;
                    }
                }
                changed = true;
            }
            if (!newShould)
            {
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
            }
            if (changed)
            {
                serializedObject.ApplyModifiedProperties();
            }
        }
    }
}
