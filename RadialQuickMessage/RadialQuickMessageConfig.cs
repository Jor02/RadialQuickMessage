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
            OpenMenuKey = plugin.Config.Bind("Radial Menu", "Open Menu Key", KeyCode.F, "Key used to open the radial menu.");
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
                    Content = GetSyntaxErrorRadialContent(ex.Message);
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

        private RadialContent[] GetSyntaxErrorRadialContent(string error) => new[]
        {
            new RadialContent
            {
                Message = "Syntax Error"
            },
            new RadialContent
            {
                Message = "Full Error..",
                Children = new[]
                {
                    new RadialContent { Message = error }
                }
            }
        };

        private RadialContent[] GetDefaultRadialContent() => new[]
        {
            new RadialContent
            {
                Label = "HI..",
                Children = new[]
                {
                    new RadialContent { Message = "Hi" },
                    new RadialContent { Message = "Hello" },
                    new RadialContent { Message = "Heya" },
                    new RadialContent { Message = "Greetings" }
                }
            },
            new RadialContent
            {
                Label = "Reactions..",
                Children = new[]
                {
                    new RadialContent { Label = "Thanks", Message = "Thanks!" },
                    new RadialContent { Message = "LOL" },
                    new RadialContent { Label = "Haha", Message = "Haha!" },
                    new RadialContent { Label = "Wow", Message = "Wow!" },
                    new RadialContent { Message = "LMAO" }
                }
            },
            new RadialContent
            {
                Label = "I spot..",
                Message = "I spot {$}.",
                Children = new[]
                {
                    new RadialContent
                    {
                        Label = "Valuable..",
                        Message = "{$} valuable",
                        Children = new[]
                        {
                            new RadialContent { Message = "Heavy" },
                            new RadialContent { Message = "Expensive" },
                            new RadialContent { Message = "Small" },
                            new RadialContent { Message = "Big" }
                        }
                    },
                    new RadialContent
                    {
                        Label = "Common..",
                        Children = new[]
                        {
                            new RadialContent { Message = "Animal" },
                            new RadialContent { Message = "Duck" },
                            new RadialContent { Message = "Gnome" },
                            new RadialContent { Message = "Eye" },
                            new RadialContent { Message = "Skull" },
                            new RadialContent { Message = "Head" }
                        }
                    },
                    new RadialContent
                    {
                        Label = "Human-like..",
                        Children = new[]
                        {
                            new RadialContent { Message = "Baby" },
                            new RadialContent { Message = "Big guy" },
                            new RadialContent { Message = "Stick lady" },
                            new RadialContent { Label = "Blind", Message = "Blind guy" },
                            new RadialContent { Message = "Upscream" }
                        }
                    },
                    new RadialContent
                    {
                        Label = "Supernatural..",
                        Children = new[]
                        {
                            new RadialContent { Message = "Shadow Child" },
                            new RadialContent { Message = "Hugger" },
                            new RadialContent { Message = "Spewer" },
                            new RadialContent { Label = "Invisible", Message = "Invisible dude" },
                            new RadialContent { Message = "Laser guy" },
                            new RadialContent { Message = "Alien" }
                        }
                    },
                    new RadialContent
                    {
                        Label = "Other..",
                        Children = new[]
                        {
                            new RadialContent { Message = "Frog" },
                            new RadialContent { Label = "Blowey", Message = "Blowey guy" }
                        }
                    }
                }
            },
            new RadialContent { Message = "MONSTER" },
            new RadialContent { Message = "RUN" },
            new RadialContent
            {
                Label = "HELP..",
                Message = "HELP {$}",
                Children = new[]
                {
                    new RadialContent { Label = "." },
                    new RadialContent { Label = "carry", Message = "carry please" },
                    new RadialContent { Message = "kill" },
                    new RadialContent { Message = "reach" }
                }
            },
            new RadialContent
            {
                Label = "Bye..",
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
