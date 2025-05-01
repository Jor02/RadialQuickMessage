using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

namespace RadialQuickMessage
{
    class RadialQuickMessageConfig
    {
        // Plugin related
        private readonly RadialQuickMessage plugin;
        private readonly ManualLogSource logger;
        
        // Json
        private FileSystemWatcher? contentWatcher;
        private static readonly JsonSerializerSettings serializerSettings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };
        public string ContentFilePath { get; private set; } = null!;

        // Radial key
        public ConfigEntry<KeyCode> OpenMenuKey { get; private set; } = null!;
        
        // Color options
        public ConfigEntry<Color> MenuColor { get; private set; } = null!;
        public ConfigEntry<Color> HoverColor { get; private set; } = null!;

        // Radial content
        public RadialContent[] Content { get; private set; } = null!;

        // Events
        public event Action<RadialContent[]>? OnContentChanged;

        public RadialQuickMessageConfig(RadialQuickMessage plugin)
        {
            this.plugin = plugin;
            this.logger = RadialQuickMessage.Logger;

            string configFileName = $"{plugin.Info.Metadata.GUID}.content.json";
            ContentFilePath = Path.Combine(Paths.ConfigPath, configFileName);

            BindConfig();
            LoadRadialContent();
            StartWatchingContentFile();
        }

        private void BindConfig()
        {
            // Setup config bindings
            OpenMenuKey = plugin.Config.Bind("Radial Menu", "Open Menu Key", KeyCode.K, "Key used to open the radial menu.");
            MenuColor = plugin.Config.Bind("Radial Menu", "Menu Color", new Color(0, 0, 0, 0.6f), "Background color of the radial menu.");
            HoverColor = plugin.Config.Bind("Radial Menu", "Hover Color", new Color(0.25f, 0.55f, 1f, 1f), "Hover color of the radial menu.");

            // Subscribe to config updates
            OpenMenuKey.SettingChanged += (_, _) => OnContentChanged?.Invoke(Content);
            MenuColor.SettingChanged += (_, _) => OnContentChanged?.Invoke(Content);
            HoverColor.SettingChanged += (_, _) => OnContentChanged?.Invoke(Content);
        }

        private void LoadRadialContent()
        {
            logger.LogInfo($"Loading radial content from {ContentFilePath}.");
            if (!File.Exists(ContentFilePath))
            {
                logger.LogWarning("Radial content file not found. Creating default.");
                Content = GetDefaultRadialContent();
                SaveRadialContent();
            }
            else
            {
                try
                {
                    string json = File.ReadAllText(ContentFilePath);
                    Content = JsonConvert.DeserializeObject<RadialContent[]>(json)!;
                }
                catch (Exception ex)
                {
                    logger.LogError($"Failed to load radial content: {ex.Message}");
                    Content = GetDefaultRadialContent();
                }
            }
        }

        private void SaveRadialContent()
        {
            try
            {
                string json = JsonConvert.SerializeObject(Content, Formatting.Indented, serializerSettings);
                File.WriteAllText(ContentFilePath, json);
                logger.LogInfo("Radial content saved successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError($"Failed to save radial content: {ex.Message}");
            }
        }

        private void StartWatchingContentFile()
        {
            string directory = Path.GetDirectoryName(ContentFilePath)!;
            string fileName = Path.GetFileName(ContentFilePath);

            contentWatcher = new FileSystemWatcher(directory, fileName)
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName
            };

            contentWatcher.Changed += (_, _) => ReloadContentDelayed();
            contentWatcher.Renamed += (_, _) => ReloadContentDelayed();
            contentWatcher.EnableRaisingEvents = true;
        }

        private void ReloadContentDelayed()
        {
            Task.Delay(200).ContinueWith(_ =>
            {
                try
                {
                    LoadRadialContent();
                    OnContentChanged?.Invoke(Content);
                    logger.LogInfo("Radial content reloaded from file.");
                }
                catch (Exception ex)
                {
                    logger.LogError($"Error reloading radial content: {ex.Message}");
                }
            });
        }

        private RadialContent[] GetDefaultRadialContent() => new[]
        {
            new RadialContent
            {
                Label = "Greetings",
                Children = new[]
                {
                    new RadialContent { Label = "Hello", Message = "Hello!" }
                }
            },
            new RadialContent
            {
                Label = "Reactions",
                Children = new[]
                {
                    new RadialContent { Label = "LOL", Message = "LOL" },
                    new RadialContent { Label = "Haha", Message = "Haha!" },
                    new RadialContent { Label = "Wow", Message = "Wow!" },
                    new RadialContent { Label = "LMAO", Message = "LMAO!" }
                }
            },
            new RadialContent
            {
                Label = "Farewells",
                Children = new[]
                {
                    new RadialContent { Label = "Goodbye", Message = "Goodbye!" },
                    new RadialContent { Label = "Bye", Message = "Bye!" }
                }
            }
        };

        public void Dispose() => contentWatcher?.Dispose();
    }
}
