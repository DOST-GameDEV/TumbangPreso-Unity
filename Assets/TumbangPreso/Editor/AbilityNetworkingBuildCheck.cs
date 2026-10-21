using System;
using TumbangPreso.Abilities;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace TumbangPreso.EditorTools
{
    public sealed class AbilityNetworkingBuildCheck : IPreprocessBuildWithReport
    {
        public int callbackOrder => -1000;

        public void OnPreprocessBuild(BuildReport report)
        {
            try { AbilityNetworking.ValidateRoster(); }
            catch (InvalidOperationException error)
            {
                throw new BuildFailedException("Ability networking contract: " + error.Message);
            }
        }
    }
}
