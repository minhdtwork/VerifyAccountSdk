using System;
using System.Collections.Generic;
using UnityEngine;

namespace OnDi.VerifyAccount
{
    internal static class SettingsLocator
    {
        internal const string LogPrefix = "[VerifyAccount] ";
        const string ResourcesFolder = "Resources";
        const string EditorFolder = "Editor";

        internal static VerifyAccountSettings Load()
        {
            var atRoot = Resources.Load<VerifyAccountSettings>(VerifyAccountSettings.ResourceName);
            if (atRoot != null) return atRoot;

            var sdkDefault = Resources.Load<VerifyAccountSettings>(VerifyAccountSettings.DefaultResourcePath);
            var everywhere = Resources.LoadAll<VerifyAccountSettings>(string.Empty);

            var picked = Pick(everywhere, sdkDefault, out var warning);
            if (warning != null) Debug.LogWarning(LogPrefix + warning);

            if (picked == null)
            {
                Debug.LogWarning(LogPrefix + "Found neither a " + VerifyAccountSettings.ResourceName +
                                 " asset in any Resources folder nor the SDK default. Running on freshly " +
                                 "constructed values. Create the asset via Create > OnDi > Verify Account " +
                                 "Settings and put it under a Resources folder.");
                return ScriptableObject.CreateInstance<VerifyAccountSettings>();
            }

            if (picked != sdkDefault)
                Debug.Log(LogPrefix + "Using " + VerifyAccountSettings.ResourceName +
                          " found in a Resources subfolder.");
            return picked;
        }

        internal static VerifyAccountSettings Pick(IList<VerifyAccountSettings> candidates,
            VerifyAccountSettings sdkDefault, out string warning)
        {
            VerifyAccountSettings own = null;
            var ownCount = 0;
            List<string> otherNames = null;

            if (candidates != null)
            {
                foreach (var candidate in candidates)
                {
                    if (candidate == null || candidate == sdkDefault) continue;
                    if (candidate.name != VerifyAccountSettings.ResourceName)
                    {
                        if (otherNames == null) otherNames = new List<string>();
                        otherNames.Add(candidate.name);
                        continue;
                    }
                    if (own == null) own = candidate;
                    ownCount++;
                }
            }

            if (ownCount > 1)
            {
                warning = "Found " + ownCount + " assets named " + VerifyAccountSettings.ResourceName +
                          " across Resources folders; using the first one. Keep a single copy.";
                return own;
            }

            if (own != null)
            {
                warning = null;
                return own;
            }

            var hint = otherNames == null
                ? ""
                : " Assets of this type named " + string.Join(", ", otherNames) +
                  " were found but ignored: rename the game copy to " +
                  VerifyAccountSettings.ResourceName + ".";
            warning = "No " + VerifyAccountSettings.ResourceName + " asset found in any Resources folder; " +
                      "using the SDK default (" + VerifyAccountSettings.DefaultResourcePath +
                      "), whose policy links are empty. Create > OnDi > Verify Account Settings, put " +
                      "the asset under a Resources folder (subfolders are fine) and keep its name." + hint;
            return sdkDefault;
        }

        internal static string DescribePlacementProblem(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath)) return null;

            var normalized = assetPath.Replace('\\', '/');
            var segments = normalized.Split('/');
            var fileName = segments[segments.Length - 1];
            var name = fileName.EndsWith(".asset", StringComparison.OrdinalIgnoreCase)
                ? fileName.Substring(0, fileName.Length - ".asset".Length)
                : fileName;

            var inResources = false;
            var inEditor = false;
            for (var i = 0; i < segments.Length - 1; i++)
            {
                if (string.Equals(segments[i], ResourcesFolder, StringComparison.OrdinalIgnoreCase)) inResources = true;
                if (string.Equals(segments[i], EditorFolder, StringComparison.OrdinalIgnoreCase)) inEditor = true;
            }

            if (!inResources)
                return "\"" + normalized + "\" sits outside every Resources folder, so the SDK cannot load " +
                       "it and will fall back to its bundled default. Move it under a Resources folder " +
                       "(subfolders are fine).";

            if (inEditor)
                return "\"" + normalized + "\" is under an Editor folder, which is stripped from builds, " +
                       "so the SDK will not find it on device. Move it under a Resources folder outside Editor.";

            if (name != VerifyAccountSettings.ResourceName)
                return "\"" + normalized + "\" is named \"" + name + "\"; the SDK only picks up an asset " +
                       "named " + VerifyAccountSettings.ResourceName + ". Rename it.";

            return null;
        }
    }
}
