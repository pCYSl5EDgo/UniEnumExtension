using System.Collections.Generic;
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

        public void Initialize()
        {
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
            }
            else
            {
                var dic = new Dictionary<string, (bool, string)>();
                foreach (var assembly in playerAssemblies)
                {
                    dic.Add(assembly.name, (true, assembly.outputPath));
                }
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
