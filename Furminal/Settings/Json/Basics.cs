using Spakov.Furminal.Settings.Json.SchemaAttributes;

namespace Spakov.Furminal.Settings.Json
{
    [Description("The Furminal basic settings.")]
    internal class Basics
    {
        [Description("The command to run in the terminal.")]
        [DefaultString("powershell")]
        public string? Command { get; set; }

        [Description("The default window title.")]
        [DefaultString("Furminal")]
        public string? DefaultWindowTitle { get; set; }

        [Description("The number of terminal rows.")]
        [DefaultIntNumber(24)]
        [MinimumInt(1)]
        public int? Rows { get; set; }

        [Description("The number of terminal columns.")]
        [DefaultIntNumber(80)]
        [MinimumInt(1)]
        public int? Columns { get; set; }

        [Description("The width of a tab character, in cells.")]
        [DefaultIntNumber(8)]
        [MinimumInt(1)]
        public int? TabWidth { get; set; }

        [Description("The directory to change to when starting. Set to null to inherit the parent process's working directory.")]
        [DefaultString("%USERPROFILE%")]
        public string? StartDirectory { get; set; }
    }
}