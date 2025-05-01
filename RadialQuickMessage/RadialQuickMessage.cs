using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace RadialQuickMessage;

[BepInPlugin("Jor02.RadialQuickMessage", "RadialQuickMessage", "1.0")]
public class RadialQuickMessage : BaseUnityPlugin
{
    internal static RadialQuickMessage Instance { get; private set; } = null!;
    internal new static ManualLogSource Logger => Instance._logger;
    private ManualLogSource _logger => base.Logger;
    internal Harmony? Harmony { get; set; }

    private RadialMenuManager radialMenu;

    private void Awake()
    {
        Instance = this;
        
        // Prevent the plugin from being deleted
        this.gameObject.transform.parent = null;
        this.gameObject.hideFlags = HideFlags.HideAndDontSave;

        Logger.LogInfo($"{Info.Metadata.GUID} v{Info.Metadata.Version} has loaded!");
    }

    // Predefined content for now
    RadialContent[] content = new RadialContent[]
    {
        new RadialContent
        {
            Label = "Greetings",
            Children = new RadialContent[]
            {
                new RadialContent
                {
                    Label = "Hello",
                    Message = "Hello!"
                }
            }
        },
        new RadialContent
        {
            Label = "Reactions",
            Children = new RadialContent[]
            {
                new RadialContent
                {
                    Label = "LOL",
                    Message = "LOL"
                },
                new RadialContent
                {
                    Label = "Haha",
                    Message = "Haha!"
                },
                new RadialContent
                {
                    Label = "Wow",
                    Message = "Wow!"
                }
                ,
                new RadialContent
                {
                    Label = "LMAO",
                    Message = "LMAO!"
                }
            }
        },
        new RadialContent
        {
            Label = "Farewells",
            Children = new RadialContent[]
            {
                new RadialContent
                {
                    Label = "Goodbye",
                    Message = "Goodbye!"
                },
                new RadialContent
                {
                    Label = "Bye",
                    Message = "Bye!"
                }
            }
        }
    };

    CursorLockMode prevLockMode;
    private void Update()
    {
        if (ChatManager.instance.localPlayerAvatarFetched) {
            if (radialMenu != null)
            {
                if (!radialMenu.IsOpen() && UnityInput.Current.GetKey(KeyCode.K))
                {
                    prevLockMode = Cursor.lockState;

                    radialMenu.Open(content);
                }
                else if (radialMenu.IsOpen())
                {
                    SemiFunc.CameraOverrideStopAim();
                    GameObject.Find("UI/HUD/HUD Canvas/HUD/Cursor/").GetComponent<MenuCursor>().Show();
                    if (!UnityInput.Current.GetKey(KeyCode.K))
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
        }
    }

    private void CreateRadialMenuManager()
    {
        GameObject canvas = GameObject.Find("UI/HUD/HUD Canvas/HUD/");
        TextMeshProUGUI labelComponent = GameObject.Find("UI/HUD/HUD Canvas/HUD/Chat/Chat Text/").GetComponent<TextMeshProUGUI>();

        GameObject radialMenuObj = new GameObject("RadialMenu", typeof(RectTransform), typeof(RadialMenuManager));
        radialMenuObj.transform.SetParent(canvas.transform, false);
        radialMenuObj.transform.SetSiblingIndex(canvas.transform.childCount - 2);

        RectTransform rectTransform = radialMenuObj.GetComponent<RectTransform>();

        // Center the RadialMenu
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;

        radialMenu = radialMenuObj.GetComponent<RadialMenuManager>();

        radialMenu.OnMessageClicked.AddListener(RelayMessage);

        // Convert resources to sprites
        radialMenu.RadialSprite = Utils.CreateSpriteFromBytes(Resources.RadialCircle);
        radialMenu.CenterSprite = Utils.CreateSpriteFromBytes(Resources.RadialCenter);
        radialMenu.SeparatorSprite = Utils.CreateSpriteFromBytes(Resources.RadialSeparator);

        radialMenu.ReferenceLabel = labelComponent;
    }

    void RelayMessage(string message)
    {
        ChatManager.instance.ForceSendMessage(message);
    }
}