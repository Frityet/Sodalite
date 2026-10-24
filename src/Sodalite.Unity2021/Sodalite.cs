using FistVR;
using HarmonyLib;
using UnityEngine;

namespace Sodalite;

public static class SodaliteConstants
{
    public const string Guid = "nrgill28.Sodalite";
    public const string Name = "Sodalite";
    public const string Version = "1.5.1";
}

/// <summary>Unity 2021 host for Sodalite's game, player, firearm, vault, Sosig and wrist APIs.</summary>
public static class Sodalite
{
    internal static NativeLogger Logger { get; } = new("Sodalite");
    private static Harmony? harmony;

    private static void MarkModded() => GM.SetRunningModded();

    public static bool IsInitialized => harmony != null;

    public static void Initialize()
    {
        if (IsInitialized) return;
        var candidate = new Harmony(SodaliteConstants.Guid + ".unity2021");
        try
        {
            candidate.CreateClassProcessor(typeof(Hooks)).Patch();
            GameHooks.Install(candidate);
            candidate.Patch(AccessTools.Method(typeof(GM), "Awake"), postfix: new HarmonyMethod(typeof(Sodalite), nameof(MarkModded)));
            UiWidgets.WidgetStyle.DefaultTextFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += (scene, mode) =>
            {
                var button = GameObject.Find("MainMenuSceneProtoBase/LevelLoadScreen/LevelLoadHolder/Canvas/Button");
                if (!button) return;
                UiWidgets.WidgetStyle.DefaultButtonSprite = button.GetComponent<UnityEngine.UI.Image>().sprite;
                UiWidgets.WidgetStyle.DefaultTextFont = button.GetComponentInChildren<UnityEngine.UI.Text>().font;
            };
            harmony = candidate;
            Logger.LogInfo("Unity 2021 APIs initialized; Harmony " + typeof(Harmony).Assembly.GetName().Version);
        }
        catch
        {
            candidate.UnpatchAll(candidate.Id);
            throw;
        }
    }
}

public sealed class NativeLogger
{
    private readonly string name;
    public NativeLogger(string name) => this.name = name;
    public void LogInfo(object value) => Debug.Log("[" + name + "] " + value);
    public void LogDebug(object value) => Debug.Log("[" + name + "] " + value);
    public void LogWarning(object value) => Debug.LogWarning("[" + name + "] " + value);
    public void LogError(object value) => Debug.LogError("[" + name + "] " + value);
}
