using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace RadialQuickMessage
{
    class RadialMenuManager : MonoBehaviour
    {
        // Events
        public UnityEvent<string?> OnMessageClicked = new UnityEvent<string?>();

        // Configuration
        public float RadialSize = 250;
        public float InnerCircleRange = .2f;

        // Visual
        public Color MenuColor = new Color(0, 0, 0, 0.6039216f);
        public Color HoverColor = new Color(0.250980392f, 0.549019608f, 1f, 1f);

        public Sprite RadialSprite;
        public Sprite CenterSprite;
        public Sprite SeparatorSprite;

        public TextMeshProUGUI ReferenceLabel;

        // Radial Menu
        private GameObject outerButtonRoot;
        private RadialButton radialCenterButton;
        private RadialButton[] outerButtons = new RadialButton[0];
        private int sliceHovered = -1;

        // Radial Navigation
        RadialContent[] currentContent;
        private Stack<RadialContent[]> menuStack = new Stack<RadialContent[]>();
        private List<string?> selectedMessageStack = new List<string?>();

        /// <summary>
        /// Opens the radial menu with the provided menu content
        /// </summary>
        /// <param name="content"></param>
        public void Open(RadialContent[] content)
        {
            menuStack.Clear();
            selectedMessageStack.Clear();
            GenerateMenu(content, false);
        }

        /// <summary>
        /// Closes the current radial menu
        /// </summary>
        public void Close()
        {
            if (radialCenterButton != null)
                Destroy(radialCenterButton.gameObject);
            Destroy(outerButtonRoot);
        }

        /// <summary>
        /// Returns if there's currently a radial menu open
        /// </summary>
        public bool IsOpen()
        {
            return radialCenterButton != null || outerButtonRoot != null;
        }

        private void GenerateMenu(RadialContent[] newContent, bool pushToStack = true)
        {
            if (!RadialSprite || !CenterSprite || !SeparatorSprite)
                return;

            // Center cursor for now (having to aim on the buttons makes quickly selecting options more difficult)
            StartCoroutine(CenterCursor());

            if (pushToStack && currentContent != null)
            {
                menuStack.Push(currentContent);
            }

            currentContent = newContent;

            sliceHovered = -1;

            if (menuStack.Count > 0)
                GenerateCenterButton();
            else if (radialCenterButton != null)
                Destroy(radialCenterButton.gameObject);

            GenerateRadialSlices(currentContent);

            Utils.SetLayerRecursively(gameObject, LayerMask.NameToLayer("UI"));
        }

        private void GenerateCenterButton()
        {
            if (radialCenterButton != null)
                Destroy(radialCenterButton.gameObject);

            // Add the center object
            GameObject radialCenterObj = new GameObject("Center", typeof(RectTransform), typeof(Image), typeof(RadialButton));
            radialCenterObj.transform.SetParent(transform, false);
            radialCenterObj.GetComponent<RectTransform>().sizeDelta = new Vector2(RadialSize, RadialSize);

            // Add image to center object
            Image centerImg = radialCenterObj.GetComponent<Image>();
            centerImg.sprite = CenterSprite;
            centerImg.color = MenuColor;

            // Setup radial button interactions
            RadialButton centerButton = radialCenterObj.GetComponent<RadialButton>();
            centerButton.image = centerImg;
            radialCenterButton = centerButton;

            if (ReferenceLabel != null)
            {
                RectTransform rect = transform as RectTransform;
                Vector2 center = rect != null ? rect.position : transform.position;

                TextMeshProUGUI labelInstance = Instantiate(ReferenceLabel, radialCenterObj.transform);
                labelInstance.text = "BACK";
                labelInstance.enabled = true;
                labelInstance.alignment = TextAlignmentOptions.Center;
                labelInstance.maskable = false;
                labelInstance.margin = Vector4.zero;

                RectTransform labelRect = labelInstance.GetComponent<RectTransform>();

                labelRect.pivot = new Vector2(0.5f, 0.5f);
                labelRect.position = center;
                labelRect.rotation = Quaternion.identity;
                labelRect.localScale = Vector3.one;

                labelRect.gameObject.SetActive(true);
            }
        }

        private void GenerateRadialSlices(params RadialContent[] content)
        {
            int numberOfSlices = content.Length;

            if (numberOfSlices <= 0)
            {
                RadialQuickMessage.Logger.LogWarning("Number of slices must be greater than 0.");
                return;
            }

            Destroy(outerButtonRoot);

            // Create the separator chain and assign root
            Transform separatorParent = CreateSeparatorMasks(numberOfSlices);

            // Create the radial slices
            outerButtons = new RadialButton[numberOfSlices];
            for (int i = 0; i < numberOfSlices; i++)
            {
                outerButtons[i] = CreateOuterButton(i, numberOfSlices, separatorParent, content[i]);
            }
        }

        private Transform CreateSeparatorMasks(int numberOfSlices)
        {
            Transform parent = null;
            for (int i = 0; i < numberOfSlices; i++)
            {
                GameObject separator = new GameObject($"Separator_{i}", typeof(RectTransform), typeof(Image), typeof(Mask));
                separator.transform.SetParent(parent == null ? transform : parent, false);

                if (i == 0)
                    outerButtonRoot = separator;

                RectTransform rectTransform = separator.GetComponent<RectTransform>();
                rectTransform.sizeDelta = new Vector2(RadialSize, RadialSize);
                rectTransform.localRotation = Quaternion.Euler(0, 0, (-360f / numberOfSlices) + (numberOfSlices == 2 && i == 0 ? 90 : 0));

                Image image = separator.GetComponent<Image>();
                image.sprite = SeparatorSprite;
                separator.GetComponent<Mask>().showMaskGraphic = false;

                parent = separator.transform;
            }

            return parent;
        }

        private RadialButton CreateOuterButton(int index, int totalSlices, Transform parent, RadialContent content)
        {
            GameObject slice = new GameObject($"Slice_{index}", typeof(RectTransform), typeof(Image), typeof(RadialButton));
            slice.transform.SetParent(parent, false);

            RectTransform rectTransform = slice.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(RadialSize, RadialSize);
            rectTransform.localRotation = Quaternion.Euler(0, 0, -360f * index / totalSlices);

            Image image = slice.GetComponent<Image>();
            image.sprite = RadialSprite;
            image.color = MenuColor;
            image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Radial360;
            image.fillOrigin = (int)Image.Origin360.Top;
            image.fillAmount = 1f / totalSlices;

            RadialButton button = slice.GetComponent<RadialButton>();
            button.image = image;
            button.label = content.Label ?? content.Message ?? "Error";
            button.message = content.Message;
            button.radialContent = content.Children;

            if (ReferenceLabel != null)
            {
                RectTransform rect = transform as RectTransform;
                Vector2 center = rect != null ? rect.position : transform.position;

                float radius = RadialSize * 0.35f;
                float sliceAngle = -360f / totalSlices;

                float angle = (index + 0.5f) * sliceAngle + (totalSlices == 2 ? -180 : 90);
                float rad = angle * Mathf.Deg2Rad;

                Vector2 direction = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
                Vector2 endPoint = center + direction * radius;

                TextMeshProUGUI labelInstance = Instantiate(ReferenceLabel, slice.transform);
                labelInstance.text = (content.Label ?? content.Message ?? "Error").ToUpper();
                labelInstance.enabled = true;
                labelInstance.alignment = TextAlignmentOptions.Center;
                labelInstance.maskable = false;
                labelInstance.margin = Vector4.zero;
                labelInstance.lineSpacing = -50;

                RectTransform labelRect = labelInstance.GetComponent<RectTransform>();

                labelRect.pivot = new Vector2(0.5f, 0.5f);
                labelRect.position = endPoint;
                labelRect.rotation = Quaternion.identity;
                labelRect.localScale = Vector3.one;

                labelRect.gameObject.SetActive(true);
            }

            return button;
        }

        /// <summary>
        /// Hacky way to center the mouse cursor
        /// </summary>
        /// <returns></returns>
        IEnumerator CenterCursor()
        {
            // Lock mouse to center of screen
            Cursor.lockState = CursorLockMode.Locked;

            // Wait a frame for unity to apply lock mode
            yield return null;

            // Release mouse cursor again
            Cursor.lockState = CursorLockMode.None;
        }

        private void Update()
        {
            // If there are no buttons, don't check mouse input
            if (radialCenterButton == null && outerButtonRoot == null)
                return;

            // Get local mouse position
            Vector2 localPos = SemiFunc.UIMousePosToUIPos() - (Vector2)transform.position;

            // Calculate magnitute and perform distance check
            float distance = localPos.magnitude;
            if (distance > RadialSize * 0.5f)  // Outside the circle
            {
                sliceHovered = -1;
                UpdateSliceColors();
                return;
            }

            int hoveredIndex = -1;
            if (distance < RadialSize * InnerCircleRange) // Inside inner circle
            {
                hoveredIndex = 0;
            }
            else if (outerButtons.Length > 0) // If there are radial buttons
            {
                float angle = Mathf.Atan2(-localPos.y, localPos.x) * Mathf.Rad2Deg + (outerButtons.Length == 2 ? 180 : 90);
                angle = (angle + 360f) % 360f;  // Normalize the angle

                // Determine which slice the pointer is hovering over
                float sliceAngle = 360f / outerButtons.Length;
                hoveredIndex = Mathf.FloorToInt(angle / sliceAngle) + 1;
            }

            // Update hover effect
            if (sliceHovered != hoveredIndex)
            {
                sliceHovered = hoveredIndex;
                if (UpdateSliceColors())
                    MenuManager.instance.MenuEffectClick(MenuManager.MenuClickEffectType.Tick, null, -1f, -1f, true);
            }

            if (Input.GetMouseButtonUp(0))
            {
                MenuManager.instance.MenuEffectClick(MenuManager.MenuClickEffectType.Confirm, null, 1f, 1f, true);
                RadialButton clickedButton = GetRadialButton(sliceHovered);
                if (sliceHovered == 0)
                {
                    // Center button clicked
                    GoBack();
                }
                else
                {
                    // Get corresponding RadialContent
                    if (clickedButton.radialContent != null && clickedButton.radialContent.Length > 0)
                    {
#if DEBUG
                        RadialQuickMessage.Logger.LogInfo($"Clicked: \"{clickedButton.message ?? clickedButton.label}\"");
#endif

                        // Push current label/message to stack if needed
                        selectedMessageStack.Add(clickedButton.message);

                        // Load child menu
                        GenerateMenu(clickedButton.radialContent);
                    }
                    else
                    {
                        // Final message selected, now build it with full template stack
                        string? finalMessage = clickedButton.message;

                        // Apply message templates from list in reverse order (like a stack)
                        for (int i = selectedMessageStack.Count - 1; i >= 0; i--)
                        {
                            string template = selectedMessageStack[i];

                            if (!string.IsNullOrEmpty(template) && template.Contains("{$}"))
                                finalMessage = template.Replace("{$}", finalMessage);
                            else if (!string.IsNullOrEmpty(template))
                                finalMessage = template + finalMessage; // fallback if no placeholder
                        }

#if DEBUG
                        RadialQuickMessage.Logger.LogInfo($"Final message: \"{finalMessage}\"");
#endif

                        // Trigger message clicked event
                        if (OnMessageClicked != null)
                            OnMessageClicked.Invoke(finalMessage);
                    }
                }
            }
        }

        private void GoBack()
        {
            if (menuStack.Count > 0)
            {
                RadialContent[] previousMenu = menuStack.Pop();
                if (selectedMessageStack.Count > 0)
                    selectedMessageStack.RemoveAt(selectedMessageStack.Count - 1);
                GenerateMenu(previousMenu, false);
            }
        }

        private bool UpdateSliceColors()
        {
            // Reset all slices to their normal color
            for (int i = 0; i < outerButtons.Length; i++)
            {
                if (outerButtons[i] == null)
                {
                    Close();
                    return false;
                }

                Image img = outerButtons[i].GetComponent<Image>();
                img.color = MenuColor;
            }

            if (radialCenterButton != null)
                radialCenterButton.image.color = MenuColor;


            // Highlight the hovered slice
            if (sliceHovered != -1)
            {
                RadialButton btn = GetRadialButton(sliceHovered);

                if (btn == null)
                    return false;

                btn.image.color = HoverColor;
            }

            return true;
        }

        private RadialButton GetRadialButton(int index)
        {
            if (outerButtons.Length == 0 || index == 0)
                return radialCenterButton;
            else
                return outerButtons[index - 1];
        }
    }
}