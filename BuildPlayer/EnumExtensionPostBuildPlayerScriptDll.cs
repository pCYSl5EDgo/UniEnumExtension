using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace UniEnumExtension
{
    public sealed class EnumExtensionPostBuildPlayerScriptDll : IPostBuildPlayerScriptDLLs
    {
        private static readonly MethodInfo BeginBuildStep;
        private static readonly MethodInfo EndBuildStep;
        private readonly object[] uniEnumExtension = { nameof(uniEnumExtension) };
        private readonly object[] step = new object[1];
        static EnumExtensionPostBuildPlayerScriptDll()
        {
            BeginBuildStep = typeof(BuildReport).GetMethod(nameof(BeginBuildStep), BindingFlags.Instance | BindingFlags.NonPublic);
            EndBuildStep = typeof(BuildReport).GetMethod(nameof(EndBuildStep), BindingFlags.Instance | BindingFlags.NonPublic);
        }

        public int callbackOrder => -1;
        public void OnPostBuildPlayerScriptDLLs(BuildReport report)
        {
            step[0] = BeginBuildStep.Invoke(report, uniEnumExtension);
            try
            {
                Implement(report);
            }
            finally
            {
                EndBuildStep.Invoke(report, step);
            }
        }

        private void Implement(BuildReport report)
        {
            var guidArray = AssetDatabase.FindAssets("t:" + nameof(ProgramStatus));
            var programStatus = AssetDatabase.LoadAssetAtPath<ProgramStatus>(AssetDatabase.GUIDToAssetPath(guidArray[0]));
            programStatus.Initialize();
            var targetNames = programStatus.Enables.Zip(programStatus.OutputPaths, (enable, outputPath) => (enable, Path.GetFileName(outputPath))).ToArray();
            var assemblyPaths = report.files.Where(buildFile =>
            {
                if (buildFile.role != "ManagedLibrary")
                {
                    return false;
                }
                if (string.IsNullOrWhiteSpace(buildFile.path)) return false;
                var buildName = Path.GetFileName(buildFile.path);
                return targetNames.All(pair => pair.Item2 != buildName) || targetNames.First(pair => pair.Item2 == buildName).Item1;
            }).Select(buildFile => buildFile.path);
            var directoryName = Path.GetDirectoryName(report.files[0].path);
            Debug.Log(directoryName);
            using (var extender = new EnumExtender(new[] { directoryName }))
            {
                extender.Extend(assemblyPaths);
            }
        }
    }
}
