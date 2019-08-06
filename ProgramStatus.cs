using UnityEditor.Compilation;
using UnityEngine;

namespace UniEnumExtension
{
    public class ProgramStatus : ScriptableObject
    {
        [SerializeField] public bool[] Enables;
        [SerializeField] public string[] Names;
        [SerializeField] public string[] OutputPaths;

        public void Initialize()
        {
            Assembly[] playerAssemblies = CompilationPipeline.GetAssemblies(AssembliesType.Player);
            Enables = new bool[playerAssemblies.Length];
            Names = new string[Enables.Length];
            OutputPaths = new string[Enables.Length];
            for (int i = 0; i < playerAssemblies.Length; i++)
            {
                Names[i] = playerAssemblies[i].name;
                OutputPaths[i] = playerAssemblies[i].outputPath;
            }
        }
    }
}
