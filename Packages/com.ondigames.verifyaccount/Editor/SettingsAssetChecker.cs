using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace OnDi.VerifyAccount.Editor
{
    sealed class SettingsAssetChecker : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets,
            string[] movedAssets, string[] movedFromAssetPaths)
        {
            var seen = new HashSet<string>();
            foreach (var path in importedAssets) if (seen.Add(path)) Check(path);
            foreach (var path in movedAssets) if (seen.Add(path)) Check(path);
        }

        static void Check(string path)
        {
            if (!path.EndsWith(".asset", StringComparison.OrdinalIgnoreCase)) return;
            if (!path.StartsWith("Assets/", StringComparison.Ordinal)) return;
            if (AssetDatabase.GetMainAssetTypeAtPath(path) != typeof(VerifyAccountSettings)) return;

            var problem = SettingsLocator.DescribePlacementProblem(path);
            if (problem == null) return;
            Debug.LogWarning(SettingsLocator.LogPrefix + problem, AssetDatabase.LoadMainAssetAtPath(path));
        }
    }
}
