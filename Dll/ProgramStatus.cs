using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace UniEnumExtension
{
    public class ProgramStatus : ScriptableObject
    {
        public bool ShouldProcessAllAssemblies;
        [SerializeField] public bool[] Enables;
        [SerializeField] public string[] Names;
        [SerializeField] public string[] OutputPaths;
        
        private static ProgramStatus instance;

        public static ProgramStatus Instance
        {
            get
            {
                if (instance != null) goto RETURN;
                const string Assets = nameof(Assets);
                const string Plugins = nameof(Plugins);
                const string Assets_Plugins = Assets + "/" + Plugins;
                const string UEE = nameof(UEE);
                const string Assets_Plugins_UEE = Assets_Plugins + "/" + UEE;
                const string Dll = nameof(Dll);
                const string Assets_Plugins_UEE_Dll = Assets_Plugins_UEE + "/" + Dll;
                const string assetPath = Assets_Plugins_UEE_Dll + "/" + nameof(ProgramStatus) + ".asset";
                instance = AssetDatabase.LoadAssetAtPath<ProgramStatus>(assetPath);
                if (instance != null) goto RETURN;
                if (!AssetDatabase.IsValidFolder(Assets_Plugins_UEE_Dll))
                {
                    if (!AssetDatabase.IsValidFolder(Assets_Plugins_UEE))
                    {
                        if (!AssetDatabase.IsValidFolder(Assets_Plugins))
                        {
                            AssetDatabase.CreateFolder(Assets, Plugins);
                        }
                        AssetDatabase.CreateFolder(Assets_Plugins, UEE);
                    }
                    AssetDatabase.CreateFolder(Assets_Plugins_UEE, Dll);
                }

                instance = CreateInstance<ProgramStatus>();
                AssetDatabase.CreateAsset(instance, assetPath);
                RETURN:
                instance.Initialize();
                return instance;
            }
        }

        private bool ToQuit()
        {
            EditorPrefs.DeleteKey(nameof(UniEnumExtension) + nameof(ProgramStatus) + "IsFirst");
            return true;
        }

        private void Initialize()
        {
            if (Enables == null)
                Enables = Array.Empty<bool>();
            if (Names == null)
                Names = Array.Empty<string>();
            if (OutputPaths == null)
                OutputPaths = Array.Empty<string>();
            if (!EditorPrefs.HasKey(nameof(UniEnumExtension) + nameof(ProgramStatus) + "IsFirst"))
            {
                EditorApplication.wantsToQuit += ToQuit; 
                return;
            }
            var playerAssemblies = CompilationPipeline.GetAssemblies(AssembliesType.Player);
            if (ShouldProcessAllAssemblies)
            {
                Enables = new bool[playerAssemblies.Length];
                for (var i = 0; i < Enables.Length; i++)
                {
                    Enables[i] = true;
                }
                Names = new string[Enables.Length];
                OutputPaths = new string[Enables.Length];
                for (var i = 0; i < playerAssemblies.Length; i++)
                {
                    Names[i] = playerAssemblies[i].name;
                    OutputPaths[i] = playerAssemblies[i].outputPath;
                }
                Array.Sort(Names, OutputPaths);
            }
            else
            {
                var dic = playerAssemblies.ToDictionary(assembly => assembly.name, assembly => (true, assembly.outputPath));
                for (var i = 0; i < Enables.Length; i++)
                {
                    if (dic.TryGetValue(Names[i], out var pair))
                    {
                        dic[Names[i]] = (Enables[i], pair.Item2);
                    }
                }
                Enables = new bool[playerAssemblies.Length];
                Names = new string[Enables.Length];
                OutputPaths = new string[Enables.Length];
                var index = 0;
                foreach (var tuple in dic)
                {
                    Names[index] = tuple.Key;
                    (Enables[index], OutputPaths[index]) = tuple.Value;
                    ++index;
                }
            }
        }
    }
}