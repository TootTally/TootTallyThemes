using BaboonAPI.Hooks.Initializer;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TootTallyCore.Utils.Assets;
using TootTallyCore.Utils.TootTallyModules;
using TootTallySettings;
using UnityEngine;

namespace TootTallyThemes
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInDependency("TootTallyCore", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("TootTallySettings", BepInDependency.DependencyFlags.HardDependency)]
    public class Plugin : BaseUnityPlugin, ITootTallyModule
    {
        public static Plugin Instance;

        internal const string CONFIG_NAME = "TootTally.cfg";
        private const string CONFIG_FIELD = "Themes";
        private const string DEFAULT_THEME = "Default";
        private Harmony _harmony;
        public ConfigEntry<bool> ModuleConfigEnabled { get; set; }
        public bool IsConfigInitialized { get; set; }

        //Change this name to whatever you want
        public string Name { get => "Themes"; set => Name = value; }

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
            ModuleConfigEnabled = TootTallyCore.Plugin.Instance.Config.Bind("Modules", "Themes", true, "Enable color customization for TootTally");
            TootTallyModuleManager.AddModule(this);
            TootTallySettings.Plugin.Instance.AddModuleToSettingPage(this);
        }

        public void LoadModule()
        {
            string configPath = Path.Combine(Paths.BepInExRootPath, "config/");
            ConfigFile config = new ConfigFile(configPath + CONFIG_NAME, true) { SaveOnConfigSet = true };
            ThemeName = config.Bind(CONFIG_FIELD, "ThemeName", DEFAULT_THEME.ToString());
            config.SettingChanged += ThemeManager.Config_SettingChanged;

            AssetManager.LoadAssets(Path.Combine(Path.GetDirectoryName(Instance.Info.Location), "Assets"));

            string targetThemePath = Path.Combine(Paths.BepInExRootPath, "Themes");
            if (!Directory.Exists(targetThemePath))
            {
                string sourceThemePath = Path.Combine(Path.GetDirectoryName(Instance.Info.Location), "Themes");
                //TootTallyLogger.LogInfo("Theme folder not found. Attempting to move folder from " + sourceThemePath + " to " + targetThemePath);
                if (Directory.Exists(sourceThemePath))
                    Directory.Move(sourceThemePath, targetThemePath);
                else
                {
                    //TootTallyLogger.LogError("Source Theme Folder Not Found. Cannot Create Theme Folder. Download the mod again to fix the issue.");
                    return;
                }
            }

            TootTallySettingPage mainPage = TootTallySettingsManager.GetSettingPageByName("TootTally");
            var filePaths = Directory.GetFiles(targetThemePath);
            List<string> fileNames = new List<string>();
            fileNames.AddRange(new string[] { "Day", "Night", "Random", "Default" });
            filePaths.ToList().ForEach(path => fileNames.Add(Path.GetFileNameWithoutExtension(path)));
            mainPage.AddLabel("GameThemesLabel", "Game Theme", 24f, TMPro.FontStyles.Normal, TMPro.TextAlignmentOptions.BottomLeft);
            mainPage.AddDropdown("Themes", ThemeName, fileNames.ToArray()); //Have to fix dropdown default value not working
            mainPage.AddButton("ResetThemeButton", new Vector2(350, 50), "Refresh Theme", ThemeManager.RefreshTheme);

            _harmony.PatchAll(typeof(ThemeManager));
            LogInfo($"Module loaded!");
        }

        public void UnloadModule()
        {
            _harmony.UnpatchSelf();
            settingPage.Remove();
            LogInfo($"Module unloaded!");
        }

        public static ConfigEntry<string> ThemeName { get; set; }
    }
}