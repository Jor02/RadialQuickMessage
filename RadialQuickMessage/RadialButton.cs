using UnityEngine;
using UnityEngine.UI;

namespace RadialQuickMessage
{
    public class RadialButton : MonoBehaviour
    {
        // Label that shows in the menu
        public string label;

        // Target chat message
        public string? message;

        // Menu content that shows when you click this button
        public RadialContent[] radialContent;

        // Reference to the image component of the slice (used for highlight color)
        public Image image;
    }
}
