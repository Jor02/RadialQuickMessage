namespace RadialQuickMessage
{
    /// <summary>
    /// Represents a single item in the radial menu, including potential children for submenus.
    /// </summary>
    [System.Serializable]
    public class RadialContent
    {
        // Label that shows in the menu
        public string Label = string.Empty;

        // Target chat message
        public string Message = string.Empty;

        // Menu content that shows when you click this option
        public RadialContent[] Children = new RadialContent[0];
    }
}
