﻿using ColossalFramework.Packaging;
using ColossalFramework.UI;
using Klyte.Commons.Extensions;
using Klyte.Commons.Utils;
using System.IO;
using System.Linq;

namespace Klyte.Commons.Redirectors
{
    public class UIWorkshopAssetRedirector : Redirector, IRedirectable
    {
        public Redirector RedirectorInstance => this;
        public void Awake()
        {
            if (CommonProperties.AssetExtraFileNames?.Length > 0 || CommonProperties.AssetExtraDirectoryNames?.Length > 0)
            {
                AddRedirect(typeof(WorkshopAssetUploadPanel).GetMethod("PrepareStagingArea", RedirectorUtils.allFlags), null, GetType().GetMethod("AfterPrepareStagingArea", RedirectorUtils.allFlags));
            }
        }

        public static void AfterPrepareStagingArea(WorkshopAssetUploadPanel __instance)
        {
            var m_ContentPath = __instance.GetType().GetField("m_ContentPath", RedirectorUtils.allFlags).GetValue(__instance) as string;
            var m_TargetAsset = __instance.GetType().GetField("m_TargetAsset", RedirectorUtils.allFlags).GetValue(__instance) as Package.Asset;
            var rootAssetFolder = Path.GetDirectoryName(m_TargetAsset.package.packagePath);
            LogUtils.DoErrorLog($"rootAssetFolder2: {rootAssetFolder}; ");
            bool bundledAnyFile = false;
            if (!(CommonProperties.AssetExtraFileNames is null))
            {
                foreach (string filename in CommonProperties.AssetExtraFileNames)
                {
                    var targetFilename = Path.Combine(rootAssetFolder, filename);
                    if (File.Exists(targetFilename))
                    {
                        File.Copy(targetFilename, Path.Combine(m_ContentPath, filename));
                        bundledAnyFile = true;
                    }
                }
            }
            if (!(CommonProperties.AssetExtraDirectoryNames is null))
            {
                foreach (string directory in CommonProperties.AssetExtraDirectoryNames)
                {
                    var targetFolder = Path.Combine(rootAssetFolder, directory);
                    if (Directory.Exists(targetFolder))
                    {
                        WorkshopHelper.DirectoryCopy(targetFolder, Path.Combine(m_ContentPath, directory), true);
                        bundledAnyFile = true;
                    }
                }
            }

            if (bundledAnyFile)
            {
                var tagsField = (__instance.GetType().GetField("m_Tags", RedirectorUtils.allFlags));
                tagsField.SetValue(__instance, (tagsField.GetValue(__instance) as string[]).Union(new string[] { CommonProperties.ModName, $"K45 {CommonProperties.Acronym}" }).Distinct().ToArray());
            }

        }
    }
}