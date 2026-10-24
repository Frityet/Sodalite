using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FistVR;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Sodalite.Api
{
    public static class GameAPI
    {
        private static readonly List<Object> InjectedAssets = new();
        public static string BetaName { get; internal set; } = "native-unity2021";
        public static int BuildId { get; internal set; } = -10;

        public static void InjectResource(Object resource)
        {
            if (!resource) throw new ArgumentNullException(nameof(resource));
            if (!InjectedAssets.Contains(resource)) InjectedAssets.Add(resource);
        }

        public static void PreloadAllAssets(string assetBundle)
        {
            var bundle = AssetBundle.LoadFromFile(Path.GetFullPath(assetBundle));
            if (!bundle) throw new InvalidDataException("Unable to load resource bundle: " + assetBundle);
            try
            {
                foreach (var asset in bundle.LoadAllAssets())
                {
                    if (asset is FVRObject item)
                    {
                        var field = AccessTools.Field(typeof(AnvilAsset), "m_anvilPrefab");
                        var prefab = (Anvil.AssetID)field.GetValue(item);
                        prefab.Bundle = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(assetBundle))!, prefab.Bundle);
                        field.SetValue(item, prefab);
                        item.IsModContent = true;
                    }
                    InjectResource(asset);
                }
            }
            finally { bundle.Unload(false); }
        }

        internal static void Append(string path, Type systemTypeInstance, ref Object[] __result)
        {
            // Match Resources.LoadAll's all-resources query. Do not inject into unrelated subfolders.
            if (path.Length != 0) return;
            __result = __result.Concat(InjectedAssets.Where(x => x && systemTypeInstance.IsInstanceOfType(x))).Distinct().ToArray();
        }
    }
}

namespace Sodalite
{
    internal static class GameHooks
    {
        internal static void Install(Harmony harmony)
        {
            // Unity 2021 has a managed ResourcesAPI seam; avoid detouring an engine icall.
            var method = AccessTools.Method(typeof(ResourcesAPI), "LoadAll", new[] { typeof(string), typeof(Type) });
            if (method == null) throw new MissingMethodException("Unity 2021 ResourcesAPI.LoadAll");
            harmony.Patch(method, postfix: new HarmonyMethod(typeof(Api.GameAPI), nameof(Api.GameAPI.Append)));
        }
    }
}
