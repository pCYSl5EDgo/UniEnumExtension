using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UniEnumExtension
{
    public sealed class ManagedPluginPreprocessor : EditorWindow
    {
        [MenuItem("Tools/UniEnumExtension/Open Managed Plugin Process Window")]
        public static void Open() => GetWindow<ManagedPluginPreprocessor>();

        private static string[] dllArray;

        [InitializeOnLoadMethod]
        private static void PostCompiled()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }
            InitializeDllArray();
        }

        private sealed class Comparer : IComparer<string>
        {
            public int Compare(string x, string y) => string.CompareOrdinal(Path.GetFileNameWithoutExtension(x), Path.GetFileNameWithoutExtension(y));
        }

        private static void InitializeDllArray()
        {
            var guidArray = AssetDatabase.FindAssets("");
            dllArray = guidArray.Distinct().Select(AssetDatabase.GUIDToAssetPath).Where(path => Path.GetExtension(path) == ".dll").ToArray();
            Array.Sort(dllArray, new Comparer());
            foreach (var s in dllArray)
            {
                Debug.Log(s);
            }
        }

        public void OnGUI()
        {
            if (GUILayout.Button("Process All Managed Plugins"))
            {
                Process(dllArray);
            }
            foreach (var dllPath in dllArray)
            {
                EditorGUILayout.ToggleLeft(Path.GetFileNameWithoutExtension(dllPath), false, "button");
            }
        }

        private void Process(IEnumerable<string> dllPaths)
        {
            var searchDirectory = new[] { Path.GetDirectoryName(UnityEditorInternal.InternalEditorUtility.GetEngineCoreModuleAssemblyPath()) };
            using (var extender = new EnumExtender(searchDirectory: searchDirectory))
            {
                extender.Extend(dllPaths);
            }
        }
    }
}