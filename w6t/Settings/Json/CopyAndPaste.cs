using Spakov.W6t.Settings.Json.SchemaAttributes;

namespace Spakov.W6t.Settings.Json {
  [Description("The w6t copy and paste settings.")]
  internal class CopyAndPaste {
    [Description("Whether to copy text to the clipboard when releasing the mouse button after selecting text.")]
    [DefaultBoolean(false)]
    public bool? CopyOnMouseUp { get; set; }

    [Description("Whether to paste text when middle clicking the terminal.")]
    [DefaultBoolean(true)]
    public bool? PasteOnMiddleClick { get; set; }

    [Description("Whether to paste text when right clicking the terminal. If this is true, the context menu is displayed with Ctrl-Right Click.")]
    [DefaultBoolean(false)]
    public bool? PasteOnRightClick { get; set; }

    [Description("The string to use to separate lines copied to the clipboard.")]
    [DefaultString(@"\r\n")]
    public string? CopyNewline { get; set; }
  }
}
