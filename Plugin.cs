using BaboonAPI.Hooks.Initializer;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.IO;
using TootTallyCore.Utils.TootTallyModules;
using TootTallySettings;
using UnityEngine;
using static TootTallyAutoToot.EasingHelper;

namespace TootTallyAutoToot
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInDependency("TootTallyCore", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("TootTallySettings", BepInDependency.DependencyFlags.HardDependency)]
    public class Plugin : BaseUnityPlugin, ITootTallyModule
    {
        public static Plugin Instance;

        private const string CONFIG_NAME = "TTAutoToot.cfg";
        private Harmony _harmony;
        public ConfigEntry<bool> ModuleConfigEnabled { get; set; }
        public bool IsConfigInitialized { get; set; }

        //Change this name to whatever you want
        public string Name { get => PluginInfo.PLUGIN_NAME; set => Name = value; }

        public static TootTallySettingPage settingPage;

        public static void LogInfo(string msg) => Instance.Logger.LogInfo(msg);
        public static void LogError(string msg) => Instance.Logger.LogError(msg);

        private void Awake()
        {
            if (Instance != null) return;
            Instance = this;
            _harmony = new Harmony(Info.Metadata.GUID);

            GameInitializationEvent.Register(Info, TryInitialize);
        }

        private void TryInitialize()
        {
            // Bind to the TTModules Config for TootTally
            ModuleConfigEnabled = TootTallyCore.Plugin.Instance.Config.Bind("Module", "TTAutoToot", true, "Bot that automatically plays the song for you.");
            TootTallyModuleManager.AddModule(this);
            TootTallySettings.Plugin.Instance.AddModuleToSettingPage(this);
        }

        public void LoadModule()
        {
            string configPath = Path.Combine(Paths.BepInExRootPath, "config/");
            ConfigFile config = new ConfigFile(configPath + CONFIG_NAME, true) { SaveOnConfigSet = true };

            ToggleKey = config.Bind("General", nameof(ToggleKey), KeyCode.F1, "Enable / Disable AutoToot.");
            EasingType = config.Bind("General", nameof(EasingType), EasingHelper.EasingType.OutQuad, "Easing function for transitions. Recommended to use EaseOut only smoothing functions for better results.");
            SyncTootWithSong = config.Bind("General", nameof(SyncTootWithSong), false, "Sync toot with the song instead of notes.");
            PerfectPlay = config.Bind("General", nameof(PerfectPlay), false, "Forces perfect score on every notes.");
            settingPage = TootTallySettingsManager.AddNewPage("TTAutoToot", "TTAutoToot", 40f, new Color(0,0,0,0));

            settingPage.AddLabel("Toggle Key");
            settingPage.AddDropdown("Toggle Key", ToggleKey);
            settingPage.AddLabel("Easing Type");
            settingPage.AddDropdown("Easing Type", EasingType);
            settingPage.AddToggle("Sync toot with song", SyncTootWithSong);
            settingPage.AddToggle("Perfect Play", PerfectPlay);

            _harmony.PatchAll(typeof(AutoTootManager));
            LogInfo($"Module loaded!");
        }

        public void UnloadModule()
        {
            _harmony.UnpatchSelf();
            settingPage.Remove();
            LogInfo($"Module unloaded!");
        }

        public ConfigEntry<KeyCode> ToggleKey { get; set; }
        public ConfigEntry<EasingType> EasingType { get; set; }
        public ConfigEntry<bool> SyncTootWithSong { get; set; }
        public ConfigEntry<bool> PerfectPlay { get; set; }
    }
}