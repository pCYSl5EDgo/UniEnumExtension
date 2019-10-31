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

        private ManagedPluginSettings settings;

        [InitializeOnLoadMethod]
        private static void PostCompiled()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }
            var settings = ManagedPluginSettings.Instance;
            var assemblyPaths = new List<string>();
            for (var i = 0; i < settings.Tuples.Length; i++)
            {
                ref var tuple = ref settings.Tuples[i];
                if(!tuple.ShouldAutoProcess) continue;
                var fileInfo = new FileInfo(tuple.ManagedPluginPath);
                if(tuple.ByteSize == fileInfo.Length) continue;
                tuple.ByteSize = fileInfo.Length;
                assemblyPaths.Add(tuple.ManagedPluginPath);
            }
            using (var extender = new EnumExtender(searchDirectory: new[] { Path.GetDirectoryName(UnityEditorInternal.InternalEditorUtility.GetEngineCoreModuleAssemblyPath()) }))
            {
                extender.Extend(assemblyPaths);
            }
        }

        private void OnEnable()
        {
            settings = ManagedPluginSettings.Instance;
        }

        public void OnGUI()
        {
            for (var i = 0; i < settings.Tuples.Length; i++)
            {
                ref var t = ref settings.Tuples[i];
                ref var shouldProcessAutomatically = ref t.ShouldAutoProcess;
                var changed = EditorGUILayout.ToggleLeft(
                    new GUIContent(Path.GetFileNameWithoutExtension(t.ManagedPluginPath), 
                        (shouldProcessAutomatically ? "Process : " : "Do not process : ")+ t.ManagedPluginPath + "\nSize : " + t.ByteSize
                    ),
                    shouldProcessAutomatically,
                    "button"
                );
                if (changed == shouldProcessAutomatically) continue;
                EditorUtility.SetDirty(settings);
                shouldProcessAutomatically = changed;
            }
        }
    }
}