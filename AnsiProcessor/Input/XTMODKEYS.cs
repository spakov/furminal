namespace Spakov.AnsiProcessor.Input
{
    /// <summary>
    /// The xterm XTMODKEYS values.
    /// </summary>
    /// <remarks>
    /// <para>Source:</para>
    /// <see href="https://invisible-island.net/xterm/ctlseqs/ctlseqs.html"/>
    /// </remarks>
    public struct XTMODKEYS
    {
        /// <summary>
        /// The <c>modifyCursorKeys</c> value.
        /// </summary>
        public ModifyCursorKeysValue ModifyCursorKeysValue = ModifyCursorKeysValue.Enabled;

        /// <summary>
        /// The <c>modifyFunctionKeys</c> value.
        /// </summary>
        public ModifyFunctionKeysValue ModifyFunctionKeysValue = ModifyFunctionKeysValue.Enabled;

        /// <summary>
        /// The <c>modifyKeypadKeys</c> value.
        /// </summary>
        public ModifyKeypadKeysValue ModifyKeypadKeysValue = ModifyKeypadKeysValue.Disabled;

        /// <summary>
        /// The <c>modifyOtherKeys</c> value.
        /// </summary>
        public ModifyOtherKeysValue ModifyOtherKeysValue = ModifyOtherKeysValue.Disabled;

        /// <summary>
        /// Initializes an <see cref="XTMODKEYS"/>.
        /// </summary>
        public XTMODKEYS()
        {
        }
    }

    /// <summary>
    /// The xterm <c>modifyCursorKeys</c> values.
    /// </summary>
    /// <remarks>
    /// <para>Source:</para>
    /// <see
    /// href="https://invisible-island.net/xterm/manpage/xterm.html#VT100-Widget-Resources:modifyCursorKeys"
    /// />
    /// <para>Tells how to handle the special case where Control-, Shift-,
    /// Alt- or Meta-modifiers are used to add a parameter to the escape
    /// sequence returned by a cursor-key. X11 cursor keys are the four keys
    /// with arrow symbols:</para>
    /// <para>Left Right Up Down</para>
    /// </remarks>
    public enum ModifyCursorKeysValue
    {
        /// <summary>
        /// forces the modifier to be the second parameter if it would
        /// otherwise be the first.
        /// </summary>
        Enabled = 2,

        /// <summary>
        /// changes the format to match <see cref="ModifyOtherKeysValue"/> 3,
        /// sending an escape sequence according to formatCursorKeys.
        /// </summary>
        All = 4
    }

    /// <summary>
    /// The xterm <c>modifyFunctionKeys</c> values.
    /// </summary>
    /// <remarks>
    /// <para>Source:</para>
    /// <see
    /// href="https://invisible-island.net/xterm/manpage/xterm.html#VT100-Widget-Resources:modifyFunctionKeys"
    /// />
    /// <para>Tells how to handle the special case where Control-, Shift-,
    /// Alt- or Meta-modifiers are used to add a parameter to the escape
    /// sequence returned by a (numbered) function-key. The default is "2". The
    /// resource values are similar to <see
    /// cref="ModifyCursorKeysValue"/>:</para>
    /// </remarks>
    public enum ModifyFunctionKeysValue
    {
        /// <summary>
        /// forces the modifier to be the second parameter if it would
        /// otherwise be the first.
        /// </summary>
        Enabled = 2,

        /// <summary>
        /// changes the format to match <see cref="ModifyOtherKeysValue"/> 3,
        /// sending an escape sequence according to formatFunctionKeys.
        /// </summary>
        All = 4
    }

    /// <summary>
    /// The xterm <c>modifyKeypadKeys</c> values.
    /// </summary>
    /// <remarks>
    /// <para>Source:</para>
    /// <see
    /// href="https://invisible-island.net/xterm/manpage/xterm.html#VT100-Widget-Resources:modifyKeypadKeys"
    /// />
    /// <para>Like <see cref="ModifyCursorKeysValue"/> "4", tells xterm to
    /// construct an escape sequence for numeric keypad keys. The default is
    /// "0".</para>
    /// </remarks>
    public enum ModifyKeypadKeysValue
    {
        Disabled = 0,
        All = 4
    }

    /// <summary>
    /// The xterm <c>modifyOtherKeys</c> values.
    /// </summary>
    /// <remarks>
    /// <para>Source:</para>
    /// <see
    /// href="https://invisible-island.net/xterm/manpage/xterm.html#VT100-Widget-Resources:modifyOtherKeys"
    /// />
    /// <para>Like <see cref="ModifyCursorKeysValue"/> "4", tells xterm to
    /// construct an escape sequence for ordinary (i.e., "other") keys (such as
    /// "2") when modified by Shift-, Control-, Alt- or Meta-modifiers. This
    /// feature does not apply to special keys, i.e., cursor-, keypad-,
    /// function- or control-keys which are labeled on your keyboard. Those
    /// have key symbols which XKB identifies uniquely.</para>
    /// <para>The default is "0":</para>
    /// </remarks>
    public enum ModifyOtherKeysValue
    {
        /// <summary>
        /// disables this feature.
        /// </summary>
        Disabled = 0,

        /// <summary>
        /// enables this feature for keys including the exceptions listed.
        /// Xterm ignores the special cases built into the X11 library. Any
        /// shifted (modified) ordinary key sends an escape sequence. The Alt-
        /// and Meta- modifiers cause xterm to send escape sequences.
        /// </summary>
        Enabled = 2,

        /// <summary>
        /// extends the feature to send unmodified keys as escape sequences.
        /// </summary>
        All = 3
    }
}
