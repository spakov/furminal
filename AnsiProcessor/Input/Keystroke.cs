namespace Spakov.AnsiProcessor.Input
{
    /// <summary>
    /// Represents a keystroke.
    /// </summary>
    public struct Keystroke
    {
        /// <summary>
        /// A <see cref="Input.Key"/>, representing the pressed key.
        /// </summary>
        public Key Key;

        /// <summary>
        /// A <see cref="Input.ModifierKeys"/>, representing modifier key
        /// states.
        /// </summary>
        public ModifierKeys ModifierKeys;

        /// <summary>
        /// Whether the key is a repeated key press (i.e., holding down a key).
        /// </summary>
        public bool IsRepeat;

        /// <summary>
        /// Whether auto-repeat keys (DECARM) is in effect.
        /// </summary>
        public bool AutoRepeatKeys;

        /// <summary>
        /// Whether application cursor keys mode (DECCKM) is in effect.
        /// </summary>
        public bool ApplicationCursorKeys;

        /// <summary>
        /// Whether Caps Lock is enabled.
        /// </summary>
        public bool CapsLock;
    }
}
