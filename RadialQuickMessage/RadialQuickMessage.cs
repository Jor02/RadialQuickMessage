using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace RadialQuickMessage;

[BepInPlugin("Jor02.RadialQuickMessage", "RadialQuickMessage", "1.0")]
public class RadialQuickMessage : BaseUnityPlugin
{
    internal static RadialQuickMessage Instance { get; private set; } = null!;
    internal new static ManualLogSource Logger => Instance._logger;
    private ManualLogSource _logger => base.Logger;
    internal Harmony? Harmony { get; set; }

    private void Awake()
    {
        Instance = this;
        
        // Prevent the plugin from being deleted
        this.gameObject.transform.parent = null;
        this.gameObject.hideFlags = HideFlags.HideAndDontSave;

        Patch();

        Logger.LogInfo($"{Info.Metadata.GUID} v{Info.Metadata.Version} has loaded!");
    }

    internal void Patch()
    {
        Harmony ??= new Harmony(Info.Metadata.GUID);
        Harmony.PatchAll();
    }

    internal void Unpatch()
    {
        Harmony?.UnpatchSelf();
    }

    private void Update()
    {

    }
}