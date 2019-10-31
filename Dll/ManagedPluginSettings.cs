using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UniEnumExtension
{
    public sealed class ManagedPluginSettings : ScriptableObject
    {
        [Serializable]
        public struct Tuple : IComparable<Tuple>
        {
            public string ManagedPluginPath;
            public long ByteSize;
            public bool ShouldAutoProcess;

            public int CompareTo(Tuple other) => string.CompareOrdinal(ManagedPluginPath, other.ManagedPluginPath);
        }

        public Tuple[] Tuples;

        private static ManagedPluginSettings instance;

        public static ManagedPluginSettings Instance
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
                const string assetPath = Assets_Plugins_UEE_Dll + "/" + nameof(ManagedPluginSettings) + ".asset";
                instance = AssetDatabase.LoadAssetAtPath<ManagedPluginSettings>(assetPath);
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

                instance = CreateInstance<ManagedPluginSettings>();
                AssetDatabase.CreateAsset(instance, assetPath);
                RETURN:
                instance.Initialize();
                return instance;
            }
        }

        private void Initialize()
        {
            var guidArray = AssetDatabase.FindAssets("");
            var dllArray = guidArray.Distinct().Select(AssetDatabase.GUIDToAssetPath).Where(path => Path.GetExtension(path) == ".dll").ToArray();
            Array.Sort(dllArray, new Comparer());
            if (Tuples == null)
            {
                Tuples = Array.Empty<Tuple>();
            }
            var length = Tuples.Length;
            for (var i = Tuples.Length - 1; i >= 0; i--)
            {
                ref var tuple = ref Tuples[i];
                if (dllArray.Contains(tuple.ManagedPluginPath)) continue;
                tuple = Tuples[--length];
            }
            var enumerable = dllArray.Except(Tuples.Where(x => !(x.ManagedPluginPath is null)).Select(x => x.ManagedPluginPath)).ToArray();
            Array.Resize(ref Tuples, length + enumerable.Length);
            for (var i = 0; i < enumerable.Length; i++)
            {
                Tuples[length + i] = new Tuple
                {
                    ManagedPluginPath = enumerable[i],
                    ShouldAutoProcess = enumerable[i].StartsWith("Assets"),
                    ByteSize = new FileInfo(enumerable[i]).Length,
                };
            }
            Array.Sort(Tuples);
        }

        private sealed class Comparer : IComparer<string>
        {
            public int Compare(string x, string y) => string.CompareOrdinal(Path.GetFileNameWithoutExtension(x), Path.GetFileNameWithoutExtension(y));
        }
    }
}