using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace UniEnumExtension
{
    public sealed class PostProcessHook : IPostBuildPlayerScriptDLLs
    {
        private static readonly MethodInfo BeginBuildStep;
        private static readonly MethodInfo EndBuildStep;
        private static readonly object[] UniEnumExtension = { nameof(UniEnumExtension) };
        private static readonly object[] Step = new object[1];
        static PostProcessHook()
        {
            BeginBuildStep = typeof(BuildReport).GetMethod(nameof(BeginBuildStep), BindingFlags.Instance | BindingFlags.NonPublic);
            EndBuildStep = typeof(BuildReport).GetMethod(nameof(EndBuildStep), BindingFlags.Instance | BindingFlags.NonPublic);
        }

        public int callbackOrder => 1;
        public void OnPostBuildPlayerScriptDLLs(BuildReport report)
        {
            Step[0] = BeginBuildStep.Invoke(report, UniEnumExtension);
            try
            {
                Impl(report);
            }
            finally
            {
                EndBuildStep.Invoke(report, Step);
            }
        }

        private void Impl(BuildReport report)
        {
            var guidArray = AssetDatabase.FindAssets("t:" + nameof(ProgramStatus));
            var programStatus = AssetDatabase.LoadAssetAtPath<ProgramStatus>(AssetDatabase.GUIDToAssetPath(guidArray[0]));
            programStatus.Initialize();
            var targetNames = programStatus.OutputPaths.Where((_, i) => programStatus.Enables[i]).Select(Path.GetFileName).ToArray();
            var assemblyPaths = report.files.Where(buildFile =>
            {
                if (string.IsNullOrWhiteSpace(buildFile.path)) return false;
                return targetNames.Contains(Path.GetFileName(buildFile.path));
            }).Select(buildFile => buildFile.path);
            EnumExtender.Execute(assemblyPaths);
        }
    }
}
