using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace RadialQuickMessage;

[BepInPlugin("Jor02.RadialQuickMessage", "RadialQuickMessage", "1.0")]
public class RadialQuickMessage : BaseUnityPlugin
{
    // Plugin related stuff
    internal static RadialQuickMessage Instance { get; private set; } = null!;
    internal new static ManualLogSource Logger => Instance._logger;
    private ManualLogSource _logger => base.Logger;
    internal Harmony? Harmony { get; set; }

    // Reference to radial menu
    private RadialMenuManager radialMenu = null!;

    // Configuration
    private RadialQuickMessageConfig config = null!;


    private void Awake()
    {
        // Save reference current insance of plugin
        Instance = this;
        
        // Prevent the plugin from being deleted
        this.gameObject.transform.parent = null;
        this.gameObject.hideFlags = HideFlags.HideAndDontSave;

        // Setup plugin config
        config = new RadialQuickMessageConfig(this);
        config.OnContentChanged += OnConfigChanged;

        // We've loaded the mod :D
        Logger.LogInfo($"{Info.Metadata.GUID} v{Info.Metadata.Version} has loaded!");
    }

    private void OnConfigChanged(RadialContent[] _)
    {
        // Update radial menu colors
        radialMenu.MenuColor = config.MenuColor.Value;
        radialMenu.HoverColor = config.HoverColor.Value;
    }

    CursorLockMode prevLockMode;
    private void Update()
    {
#if !DEBUG
        // Only allow radial menu to be opened when TTS is available
        if (ChatManager.instance.localPlayerAvatarFetched) {
#endif
            if (radialMenu != null)
            {
                if (!radialMenu.IsOpen() && UnityInput.Current.GetKey(config.OpenMenuKey.Value))
                {
                    prevLockMode = Cursor.lockState;
                    radialMenu.Open(config.Content);
                }
                else if (radialMenu.IsOpen())
                {
                    // Prevent camera from rotating while having menu open
                    SemiFunc.CameraOverrideStopAim();

                    // Show the cursor while menu is open
                    MenuCursor.instance.Show();

                    // Close menu when open menu key has been released
                    if (!UnityInput.Current.GetKey(config.OpenMenuKey.Value))
                    {
                        radialMenu.Close();
                        Cursor.lockState = prevLockMode;
                    }
                }
            }
            else
            {
                CreateRadialMenuManager();
            }
#if !DEBUG
        }
#endif
    }

    private void CreateRadialMenuManager()
    {
        // Make sure we don't accidentally create multiple radial menu managers
        if (radialMenu != null)
            Destroy(radialMenu);

        // Get hud canvas
        GameObject canvas = GameObject.Find("UI/HUD/HUD Canvas/HUD/");
        TextMeshProUGUI labelComponent = GameObject.Find("UI/HUD/HUD Canvas/HUD/Chat/Chat Text/").GetComponent<TextMeshProUGUI>();

        // Create radial menu instance
        GameObject radialMenuObj = new GameObject("RadialMenu", typeof(RectTransform), typeof(RadialMenuManager));
        radialMenuObj.transform.SetParent(canvas.transform, false);
        radialMenuObj.transform.SetSiblingIndex(canvas.transform.childCount - 2);

        // Center the RadialMenu
        RectTransform rectTransform = radialMenuObj.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;
        
        // Get radial menu behaviour
        radialMenu = radialMenuObj.GetComponent<RadialMenuManager>();
        radialMenu.OnMessageClicked.AddListener(RelayMessage);

        // Convert resources to sprites
        radialMenu.RadialSprite = Utils.CreateSpriteFromBytes(Resources.RadialCircle);
        radialMenu.CenterSprite = Utils.CreateSpriteFromBytes(Resources.RadialCenter);
        radialMenu.SeparatorSprite = Utils.CreateSpriteFromBytes(Resources.RadialSeparator);

        // Setup radial menu colors
        radialMenu.MenuColor = config.MenuColor.Value;
        radialMenu.HoverColor = config.HoverColor.Value;

        // Setup radial menu label
        radialMenu.ReferenceLabel = labelComponent;
    }

    void RelayMessage(string message)
    {
        ChatManager.instance.ForceSendMessage(message);
    }

    private void OnDestroy()
    {
        config?.Dispose();
    }
}