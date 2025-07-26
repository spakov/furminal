using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using Spakov.Terminal;
using Spakov.Terminal.Helpers;
using Spakov.Terminal.Settings;
using Spakov.Furminal.Settings;
using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Timers;
using Windows.UI;

namespace Spakov.Furminal.Views
{
    public sealed partial class Terminal : Window
    {
        /// <summary>
        /// The delay (in milliseconds) after being notified the settings file
        /// was written to wait before attempting to read it.
        /// </summary>
        private const int SettleDelay = 100;

        /// <summary>
        /// The default start directory.
        /// </summary>
        private const string DefaultStartDirectory = "%USERPROFILE%";

        /// <summary>
        /// The Explorer process name.
        /// </summary>
        private const string Explorer = "explorer";

        /// <summary>
        /// The string to pass to <see cref="Explorer"/> to facilitate
        /// selecting a file in a folder.
        /// </summary>
        private const string ExplorerSelect = "/select,\"{0}\"";

        private readonly FileSystemWatcher _settingsJsonWatcher;
        private readonly Timer _settingsSaveTimer;

        /// <summary>
        /// Loads and applies settings, including from the command line and
        /// from <see cref="SettingsHelper.SettingsPath"/>.
        /// </summary>
        /// <param name="initialLoad">Whether this is the initial loading of
        /// settings.</param>
        /// <param name="terminalIsInitialized">Whether the <see
        /// cref="TerminalControl"/> is ready.</param>
#pragma warning disable IDE0079 // Remove unnecessary suppression
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "Types are preserved, reflection required")]
#pragma warning restore IDE0079 // Remove unnecessary suppression
        public void LoadSettings(bool initialLoad = false, bool terminalIsInitialized = false)
        {
            if (_startCommand is not null && _startCommand.Length > 0 && initialLoad)
            {
                _dependencyProperties.Command = _startCommand;
            }

            if (_startRows is not null && initialLoad && terminalIsInitialized)
            {
                TerminalControl.Rows = (int)_startRows;
            }

            if (_startColumns is not null && initialLoad && terminalIsInitialized)
            {
                TerminalControl.Columns = (int)_startColumns;
            }

            if (!File.Exists(SettingsHelper.SettingsPath))
            {
                return;
            }

            Settings.Json.Settings? settings = null;
            bool readSucceeded = false;

            try
            {
                using StreamReader settingsJsonFile = new(SettingsHelper.SettingsPath, new FileStreamOptions() { Access = FileAccess.Read, Mode = FileMode.Open });

                settings = JsonSerializer.Deserialize<Settings.Json.Settings>(settingsJsonFile.BaseStream, SettingsHelper.JsonSerializerOptions)!;
                readSucceeded = true;
            }
            catch (IOException e)
            {
                if (terminalIsInitialized)
                {
                    ShowErrorDialog(TerminalControl.XamlRoot, _resources.GetString("CouldNotReadSettings"), e);
                }
            }
            catch (JsonException e)
            {
                if (terminalIsInitialized)
                {
                    ShowErrorDialog(TerminalControl.XamlRoot, _resources.GetString("CouldNotLoadSettings"), e);
                }
            }

            if (!readSucceeded || settings is null)
            {
                return;
            }

            if (settings.Basics is not null)
            {
                if (_startCommand is null || _startCommand.Length == 0)
                {
                    if (settings.Basics.Command is not null)
                    {
                        _dependencyProperties.Command = settings.Basics.Command;
                    }
                }

                if (terminalIsInitialized)
                {
                    if (settings.Basics.DefaultWindowTitle is not null)
                    {
                        TerminalControl.DefaultWindowTitle = settings.Basics.DefaultWindowTitle;
                    }

                    if (_startRows is null || !initialLoad)
                    {
                        if (settings.Basics.Rows is not null)
                        {
                            TerminalControl.Rows = (int)settings.Basics.Rows;
                        }
                    }

                    if (_startColumns is null || !initialLoad)
                    {
                        if (settings.Basics.Columns is not null)
                        {
                            TerminalControl.Columns = (int)settings.Basics.Columns;
                        }
                    }

                    if (settings.Basics.TabWidth is not null)
                    {
                        TerminalControl.TabWidth = (int)settings.Basics.TabWidth;
                    }
                }

                _dependencyProperties.StartDirectory = settings.Basics.StartDirectory;
            }

            if (settings.Appearance is not null)
            {
                if (settings.Appearance.WindowBackdrop is not null)
                {
                    _dependencyProperties.WindowBackdrop = (WindowBackdrop)settings.Appearance.WindowBackdrop;
                }

                if (settings.Appearance.SolidColorWindowBackdropColor is not null)
                {
                    _dependencyProperties.SolidColorWindowBackdropColor = (Color)settings.Appearance.SolidColorWindowBackdropColor;
                }

                if (terminalIsInitialized)
                {
                    if (settings.Appearance.FontFamily is not null)
                    {
                        TerminalControl.FontFamily = settings.Appearance.FontFamily;
                    }

                    if (settings.Appearance.FontSize is not null)
                    {
                        TerminalControl.FontSize = (double)settings.Appearance.FontSize;
                    }

                    if (settings.Appearance.TextAntialiasing is not null)
                    {
                        TerminalControl.TextAntialiasing = (TextAntialiasingStyle)settings.Appearance.TextAntialiasing;
                    }

                    if (settings.Appearance.FullColorEmoji is not null)
                    {
                        TerminalControl.FullColorEmoji = (bool)settings.Appearance.FullColorEmoji;
                    }

                    if (settings.Appearance.UseBackgroundColorErase is not null)
                    {
                        TerminalControl.UseBackgroundColorErase = (bool)settings.Appearance.UseBackgroundColorErase;
                    }

                    if (settings.Appearance.BackgroundIsInvisible is not null)
                    {
                        TerminalControl.BackgroundIsInvisible = (bool)settings.Appearance.BackgroundIsInvisible;
                    }
                }
            }

            if (settings.Behavior is not null)
            {
                if (terminalIsInitialized)
                {
                    if (settings.Behavior.UseVisualBell is not null)
                    {
                        TerminalControl.UseVisualBell = (bool)settings.Behavior.UseVisualBell;
                    }
                }

                if (settings.Behavior.VisualBellDisplayTime is not null)
                {
                    _dependencyProperties.VisualBellDisplayTime = (int)settings.Behavior.VisualBellDisplayTime;
                }

                if (terminalIsInitialized)
                {
                    if (settings.Behavior.UseContextMenu is not null)
                    {
                        TerminalControl.UseContextMenu = (bool)settings.Behavior.UseContextMenu;
                    }

                    if (settings.Behavior.UseExtendedContextMenu is not null)
                    {
                        TerminalControl.UseExtendedContextMenu = (bool)settings.Behavior.UseExtendedContextMenu;
                    }
                }
            }

            if (settings.Cursor is not null)
            {
                if (terminalIsInitialized)
                {
                    if (settings.Cursor.CursorStyle is not null)
                    {
                        TerminalControl.CursorStyle = (CursorStyle)settings.Cursor.CursorStyle;
                    }

                    if (settings.Cursor.CursorThickness is not null)
                    {
                        TerminalControl.CursorThickness = (double)settings.Cursor.CursorThickness;
                    }

                    if (settings.Cursor.CursorBlink is not null)
                    {
                        TerminalControl.CursorBlink = (bool)settings.Cursor.CursorBlink;
                    }

                    if (settings.Cursor.CursorBlinkRate is not null)
                    {
                        TerminalControl.CursorBlinkRate = (int)settings.Cursor.CursorBlinkRate;
                    }

                    if (settings.Cursor.CursorColor is not null)
                    {
                        TerminalControl.CursorColor = (Color)settings.Cursor.CursorColor;
                    }
                }
            }

            if (settings.Scrollback is not null)
            {
                if (terminalIsInitialized)
                {
                    if (settings.Scrollback.ScrollbackLines is not null)
                    {
                        TerminalControl.Scrollback = (int)settings.Scrollback.ScrollbackLines;
                    }

                    if (settings.Scrollback.LinesPerScrollback is not null)
                    {
                        TerminalControl.LinesPerScrollback = (int)settings.Scrollback.LinesPerScrollback;
                    }

                    if (settings.Scrollback.LinesPerSmallScrollback is not null)
                    {
                        TerminalControl.LinesPerSmallScrollback = (int)settings.Scrollback.LinesPerSmallScrollback;
                    }

                    if (settings.Scrollback.LinesPerWheelScrollback is not null)
                    {
                        TerminalControl.LinesPerWheelScrollback = (int)settings.Scrollback.LinesPerWheelScrollback;
                    }
                }
            }

            if (settings.CopyAndPaste is not null)
            {
                if (terminalIsInitialized)
                {
                    if (settings.CopyAndPaste.CopyOnMouseUp is not null)
                    {
                        TerminalControl.CopyOnMouseUp = (bool)settings.CopyAndPaste.CopyOnMouseUp;
                    }

                    if (settings.CopyAndPaste.PasteOnMiddleClick is not null)
                    {
                        TerminalControl.PasteOnMiddleClick = (bool)settings.CopyAndPaste.PasteOnMiddleClick;
                    }

                    if (settings.CopyAndPaste.PasteOnRightClick is not null)
                    {
                        TerminalControl.PasteOnRightClick = (bool)settings.CopyAndPaste.PasteOnRightClick;
                    }

                    if (settings.CopyAndPaste.CopyNewline is not null)
                    {
                        TerminalControl.CopyNewline = settings.CopyAndPaste.CopyNewline;
                    }
                }
            }

            if (settings.ColorPalette is not null)
            {
                if (terminalIsInitialized)
                {
                    if (settings.ColorPalette.DefaultColors is not null)
                    {
                        if (settings.ColorPalette.DefaultColors.DefaultBackgroundColor is not null)
                        {
                            TerminalControl.AnsiColors.DefaultBackgroundColor = ((Color)settings.ColorPalette.DefaultColors.DefaultBackgroundColor).ToSystemDrawingColor();
                        }

                        if (settings.ColorPalette.DefaultColors.DefaultForegroundColor is not null)
                        {
                            TerminalControl.AnsiColors.DefaultForegroundColor = ((Color)settings.ColorPalette.DefaultColors.DefaultForegroundColor).ToSystemDrawingColor();
                        }

                        if (settings.ColorPalette.DefaultColors.DefaultUnderlineColor is not null)
                        {
                            TerminalControl.AnsiColors.DefaultUnderlineColor = ((Color)settings.ColorPalette.DefaultColors.DefaultUnderlineColor).ToSystemDrawingColor();
                        }
                    }

                    if (settings.ColorPalette.StandardColors is not null)
                    {
                        if (settings.ColorPalette.StandardColors.Black is not null)
                        {
                            TerminalControl.AnsiColors.Black = ((Color)settings.ColorPalette.StandardColors.Black).ToSystemDrawingColor();
                        }

                        if (settings.ColorPalette.StandardColors.Red is not null)
                        {
                            TerminalControl.AnsiColors.Red = ((Color)settings.ColorPalette.StandardColors.Red).ToSystemDrawingColor();
                        }

                        if (settings.ColorPalette.StandardColors.Green is not null)
                        {
                            TerminalControl.AnsiColors.Green = ((Color)settings.ColorPalette.StandardColors.Green).ToSystemDrawingColor();
                        }

                        if (settings.ColorPalette.StandardColors.Yellow is not null)
                        {
                            TerminalControl.AnsiColors.Yellow = ((Color)settings.ColorPalette.StandardColors.Yellow).ToSystemDrawingColor();
                        }

                        if (settings.ColorPalette.StandardColors.Blue is not null)
                        {
                            TerminalControl.AnsiColors.Blue = ((Color)settings.ColorPalette.StandardColors.Blue).ToSystemDrawingColor();
                        }

                        if (settings.ColorPalette.StandardColors.Magenta is not null)
                        {
                            TerminalControl.AnsiColors.Magenta = ((Color)settings.ColorPalette.StandardColors.Magenta).ToSystemDrawingColor();
                        }

                        if (settings.ColorPalette.StandardColors.Cyan is not null)
                        {
                            TerminalControl.AnsiColors.Cyan = ((Color)settings.ColorPalette.StandardColors.Cyan).ToSystemDrawingColor();
                        }

                        if (settings.ColorPalette.StandardColors.White is not null)
                        {
                            TerminalControl.AnsiColors.White = ((Color)settings.ColorPalette.StandardColors.White).ToSystemDrawingColor();
                        }
                    }

                    if (settings.ColorPalette.BrightColors is not null)
                    {
                        if (settings.ColorPalette.BrightColors.BrightBlack is not null)
                        {
                            TerminalControl.AnsiColors.BrightBlack = ((Color)settings.ColorPalette.BrightColors.BrightBlack).ToSystemDrawingColor();
                        }

                        if (settings.ColorPalette.BrightColors.BrightRed is not null)
                        {
                            TerminalControl.AnsiColors.BrightRed = ((Color)settings.ColorPalette.BrightColors.BrightRed).ToSystemDrawingColor();
                        }

                        if (settings.ColorPalette.BrightColors.BrightGreen is not null)
                        {
                            TerminalControl.AnsiColors.BrightGreen = ((Color)settings.ColorPalette.BrightColors.BrightGreen).ToSystemDrawingColor();
                        }

                        if (settings.ColorPalette.BrightColors.BrightYellow is not null)
                        {
                            TerminalControl.AnsiColors.BrightYellow = ((Color)settings.ColorPalette.BrightColors.BrightYellow).ToSystemDrawingColor();
                        }

                        if (settings.ColorPalette.BrightColors.BrightBlue is not null)
                        {
                            TerminalControl.AnsiColors.BrightBlue = ((Color)settings.ColorPalette.BrightColors.BrightBlue).ToSystemDrawingColor();
                        }

                        if (settings.ColorPalette.BrightColors.BrightMagenta is not null)
                        {
                            TerminalControl.AnsiColors.BrightMagenta = ((Color)settings.ColorPalette.BrightColors.BrightMagenta).ToSystemDrawingColor();
                        }

                        if (settings.ColorPalette.BrightColors.BrightCyan is not null)
                        {
                            TerminalControl.AnsiColors.BrightCyan = ((Color)settings.ColorPalette.BrightColors.BrightCyan).ToSystemDrawingColor();
                        }

                        if (settings.ColorPalette.BrightColors.BrightWhite is not null)
                        {
                            TerminalControl.AnsiColors.BrightWhite = ((Color)settings.ColorPalette.BrightColors.BrightWhite).ToSystemDrawingColor();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Invoked before the <see cref="TerminalControl"/>'s <see
        /// cref="SettingsWindow"/> is displayed.
        /// </summary>
        /// <param name="settingsGroups"></param>
        private void TerminalControl_CustomizeSettingsWindowSettings(SettingsViewModel settingsViewModel)
        {
            TextSettingsItem command = new()
            {
                Key = nameof(_dependencyProperties.Command),
                Name = _resources.GetString("CommandName"),
                Getter = () => _dependencyProperties.Command,
                Setter = (command) => _dependencyProperties.Command = command
            };

            TextSettingsItem startDirectory = new()
            {
                Key = nameof(_dependencyProperties.StartDirectory),
                Name = _resources.GetString("StartDirectoryName"),
                Getter = () => _dependencyProperties.StartDirectory is not null ? _dependencyProperties.StartDirectory : string.Empty,
                Setter = (startDirectory) => _dependencyProperties.StartDirectory = startDirectory == string.Empty ? null : startDirectory
            };

            CaptionSettingsItem startDirectoryCaption = new()
            {
                Key = "StartDirectoryCaption",
                Getter = () => _dependencyProperties.StartDirectory is null ? _resources.GetString("NullStartDirectoryExplanation") : _resources.GetString("NonNullStartDirectoryExplanation")
            };

            BooleanSettingsItem nullStartDirectory = new()
            {
                Key = "NullStartDirectory",
                Name = _resources.GetString("NullStartDirectoryName"),
                Getter = () => _dependencyProperties.StartDirectory is null,
                Setter = (startDirectory) =>
                {
                    _dependencyProperties.StartDirectory = startDirectory ? null : DefaultStartDirectory;
                    startDirectoryCaption.OnPropertyChanged(nameof(startDirectoryCaption.Caption));
                }
            };

            RadioSettingsItem defaultBackgroundColorWindowBackdrop = new()
            {
                Key = "DefaultBackgroundColorWindowBackdrop",
                Name = _resources.GetString("DefaultBackgroundColorWindowBackdropName")
            };

            RadioSettingsItem micaWindowBackdrop = new()
            {
                Key = "MicaWindowBackdrop",
                Name = _resources.GetString("MicaWindowBackdropName")
            };

            RadioSettingsItem acrylicWindowBackdrop = new()
            {
                Key = "AcrylicWindowBackdrop",
                Name = _resources.GetString("AcrylicWindowBackdropName")
            };

            RadioSettingsItem blurredWindowBackdrop = new()
            {
                Key = "BlurredWindowBackdrop",
                Name = _resources.GetString("BlurredWindowBackdropName")
            };

            RadioSettingsItem transparentWindowBackdrop = new()
            {
                Key = "TransparentWindowBackdrop",
                Name = _resources.GetString("TransparentWindowBackdropName")
            };

            RadioSettingsItem solidColorWindowBackdrop = new()
            {
                Key = "SolidColorWindowBackdrop",
                Name = _resources.GetString("SolidColorWindowBackdropName")
            };

            RadioCollectionSettingsItem windowBackdrop = new()
            {
                Key = nameof(_dependencyProperties.WindowBackdrop),
                Name = _resources.GetString("WindowBackdropName"),
                Items =
                [
                    defaultBackgroundColorWindowBackdrop,
                    micaWindowBackdrop,
                    acrylicWindowBackdrop,
                    blurredWindowBackdrop,
                    transparentWindowBackdrop,
                    solidColorWindowBackdrop
                ],
                Getter = () => (int)_dependencyProperties.WindowBackdrop,
                Setter = (windowBackdrop) =>
                {
                    if ((WindowBackdrop)windowBackdrop == WindowBackdrop.DefaultBackgroundColor)
                    {
                        _dependencyProperties.SolidColorWindowBackdropColor = TerminalControl.AnsiColors.DefaultBackgroundColor.ToWindowsUIColor();
                    }

                    _dependencyProperties.WindowBackdrop = (WindowBackdrop)windowBackdrop;
                }
            };

            ColorSettingsItem solidColorWindowBackdropColor = new()
            {
                Key = nameof(_dependencyProperties.SolidColorWindowBackdropColor),
                Name = _resources.GetString("SolidColorWindowBackdropColorName"),
                Getter = () => _dependencyProperties.SolidColorWindowBackdropColor,
                Setter = (solidColorWindowBackdropColor) => _dependencyProperties.SolidColorWindowBackdropColor = solidColorWindowBackdropColor
            };

            IntegerSettingsItem visualBellDisplayTime = new()
            {
                Key = nameof(_dependencyProperties.VisualBellDisplayTime),
                Name = _resources.GetString("VisualBellDisplayTimeName"),
                Getter = () => _dependencyProperties.VisualBellDisplayTime,
                Setter = (visualBellDisplayTime) => _dependencyProperties.VisualBellDisplayTime = visualBellDisplayTime,
                SmallChange = 1,
                LargeChange = 1
            };

            SettingsGroup advanced = new()
            {
                Key = "Advanced",
                Name = _resources.GetString("AdvancedName"),
                Items =
                [
                    new ButtonSettingsItem()
                    {
                        Key = "OpenSettingsJsonLocation",
                        Name = _resources.GetString("OpenSettingsJsonLocationName"),
                        Click = (_, _) => {
                            if (!File.Exists(SettingsHelper.SettingsPath)) {
                                Process.Start(Explorer, Windows.Storage.ApplicationData.Current.LocalFolder.Path);
                            } else {
                                Process.Start(Explorer, string.Format(ExplorerSelect, SettingsHelper.SettingsPath));
                            }
                        }
                    },
                    new CaptionSettingsItem()
                    {
                        Key = "SettingsJsonLocationCaption",
                        Getter = () => string.Format(_resources.GetString("SettingsJsonLocationExplanation"), SettingsHelper.SettingsPath)
                    }
                ]
            };

            ColorSettingsItem defaultBackgroundColor = new()
            {
                Key = "DefaultBackgroundColor",
                Name = _resources.GetString("DefaultBackgroundColorName"),
                Getter = () => TerminalControl.AnsiColors.DefaultBackgroundColor.ToWindowsUIColor(),
                Setter = (defaultBackgroundColor) =>
                {
                    TerminalControl.AnsiColors.DefaultBackgroundColor = defaultBackgroundColor.ToSystemDrawingColor();

                    if (_dependencyProperties.WindowBackdrop == WindowBackdrop.DefaultBackgroundColor)
                    {
                        SystemBackdrop = new SolidColorBackdrop(TerminalControl.AnsiColors.DefaultBackgroundColor.ToWindowsUIColor());
                    }
                }
            };

            ColorSettingsItem defaultForegroundColor = new()
            {
                Key = "DefaultForegroundColor",
                Name = _resources.GetString("DefaultForegroundColorName"),
                Getter = () => TerminalControl.AnsiColors.DefaultForegroundColor.ToWindowsUIColor(),
                Setter = (defaultForegroundColor) => TerminalControl.AnsiColors.DefaultForegroundColor = defaultForegroundColor.ToSystemDrawingColor()
            };

            ColorSettingsItem defaultUnderlineColor = new()
            {
                Key = "DefaultUnderlineColor",
                Name = _resources.GetString("DefaultUnderlineColorName"),
                Getter = () => TerminalControl.AnsiColors.DefaultUnderlineColor.ToWindowsUIColor(),
                Setter = (defaultUnderlineColor) => TerminalControl.AnsiColors.DefaultUnderlineColor = defaultUnderlineColor.ToSystemDrawingColor()
            };

            ColorSettingsItem blackStandardColor = new()
            {
                Key = "BlackStandardColor",
                Name = _resources.GetString("BlackStandardColorName"),
                Getter = () => TerminalControl.AnsiColors.Black.ToWindowsUIColor(),
                Setter = (blackStandardColor) => TerminalControl.AnsiColors.Black = blackStandardColor.ToSystemDrawingColor()
            };

            ColorSettingsItem redStandardColor = new()
            {
                Key = "RedStandardColor",
                Name = _resources.GetString("RedStandardColorName"),
                Getter = () => TerminalControl.AnsiColors.Red.ToWindowsUIColor(),
                Setter = (redStandardColor) => TerminalControl.AnsiColors.Red = redStandardColor.ToSystemDrawingColor()
            };

            ColorSettingsItem greenStandardColor = new()
            {
                Key = "GreenStandardColor",
                Name = _resources.GetString("GreenStandardColorName"),
                Getter = () => TerminalControl.AnsiColors.Green.ToWindowsUIColor(),
                Setter = (greenStandardColor) => TerminalControl.AnsiColors.Green = greenStandardColor.ToSystemDrawingColor()
            };

            ColorSettingsItem yellowStandardColor = new()
            {
                Key = "YellowStandardColor",
                Name = _resources.GetString("YellowStandardColorName"),
                Getter = () => TerminalControl.AnsiColors.Yellow.ToWindowsUIColor(),
                Setter = (yellowStandardColor) => TerminalControl.AnsiColors.Yellow = yellowStandardColor.ToSystemDrawingColor()
            };

            ColorSettingsItem blueStandardColor = new()
            {
                Key = "BlueStandardColor",
                Name = _resources.GetString("BlueStandardColorName"),
                Getter = () => TerminalControl.AnsiColors.Blue.ToWindowsUIColor(),
                Setter = (blueStandardColor) => TerminalControl.AnsiColors.Blue = blueStandardColor.ToSystemDrawingColor()
            };

            ColorSettingsItem magentaStandardColor = new()
            {
                Key = "MagentaStandardColor",
                Name = _resources.GetString("MagentaStandardColorName"),
                Getter = () => TerminalControl.AnsiColors.Magenta.ToWindowsUIColor(),
                Setter = (magentaStandardColor) => TerminalControl.AnsiColors.Magenta = magentaStandardColor.ToSystemDrawingColor()
            };

            ColorSettingsItem cyanStandardColor = new()
            {
                Key = "CyanStandardColor",
                Name = _resources.GetString("CyanStandardColorName"),
                Getter = () => TerminalControl.AnsiColors.Cyan.ToWindowsUIColor(),
                Setter = (cyanStandardColor) => TerminalControl.AnsiColors.Cyan = cyanStandardColor.ToSystemDrawingColor()
            };

            ColorSettingsItem whiteStandardColor = new()
            {
                Key = "WhiteStandardColor",
                Name = _resources.GetString("WhiteStandardColorName"),
                Getter = () => TerminalControl.AnsiColors.White.ToWindowsUIColor(),
                Setter = (whiteStandardColor) => TerminalControl.AnsiColors.White = whiteStandardColor.ToSystemDrawingColor()
            };

            ColorSettingsItem blackBrightColor = new()
            {
                Key = "BlackBrightColor",
                Name = _resources.GetString("BlackBrightColorName"),
                Getter = () => TerminalControl.AnsiColors.BrightBlack.ToWindowsUIColor(),
                Setter = (blackBrightColor) => TerminalControl.AnsiColors.BrightBlack = blackBrightColor.ToSystemDrawingColor()
            };

            ColorSettingsItem redBrightColor = new()
            {
                Key = "RedBrightColor",
                Name = _resources.GetString("RedBrightColorName"),
                Getter = () => TerminalControl.AnsiColors.BrightRed.ToWindowsUIColor(),
                Setter = (redBrightColor) => TerminalControl.AnsiColors.BrightRed = redBrightColor.ToSystemDrawingColor()
            };

            ColorSettingsItem greenBrightColor = new()
            {
                Key = "GreenBrightColor",
                Name = _resources.GetString("GreenBrightColorName"),
                Getter = () => TerminalControl.AnsiColors.BrightGreen.ToWindowsUIColor(),
                Setter = (greenBrightColor) => TerminalControl.AnsiColors.BrightGreen = greenBrightColor.ToSystemDrawingColor()
            };

            ColorSettingsItem yellowBrightColor = new()
            {
                Key = "YellowBrightColor",
                Name = _resources.GetString("YellowBrightColorName"),
                Getter = () => TerminalControl.AnsiColors.BrightYellow.ToWindowsUIColor(),
                Setter = (yellowBrightColor) => TerminalControl.AnsiColors.BrightYellow = yellowBrightColor.ToSystemDrawingColor()
            };

            ColorSettingsItem blueBrightColor = new()
            {
                Key = "BlueBrightColor",
                Name = _resources.GetString("BlueBrightColorName"),
                Getter = () => TerminalControl.AnsiColors.BrightBlue.ToWindowsUIColor(),
                Setter = (blueBrightColor) => TerminalControl.AnsiColors.BrightBlue = blueBrightColor.ToSystemDrawingColor()
            };

            ColorSettingsItem magentaBrightColor = new()
            {
                Key = "MagentaBrightColor",
                Name = _resources.GetString("MagentaBrightColorName"),
                Getter = () => TerminalControl.AnsiColors.BrightMagenta.ToWindowsUIColor(),
                Setter = (magentaBrightColor) => TerminalControl.AnsiColors.BrightMagenta = magentaBrightColor.ToSystemDrawingColor()
            };

            ColorSettingsItem cyanBrightColor = new()
            {
                Key = "CyanBrightColor",
                Name = _resources.GetString("CyanBrightColorName"),
                Getter = () => TerminalControl.AnsiColors.BrightCyan.ToWindowsUIColor(),
                Setter = (cyanBrightColor) => TerminalControl.AnsiColors.BrightCyan = cyanBrightColor.ToSystemDrawingColor()
            };

            ColorSettingsItem whiteBrightColor = new()
            {
                Key = "WhiteBrightColor",
                Name = _resources.GetString("WhiteBrightColorName"),
                Getter = () => TerminalControl.AnsiColors.BrightWhite.ToWindowsUIColor(),
                Setter = (whiteBrightColor) => TerminalControl.AnsiColors.BrightWhite = whiteBrightColor.ToSystemDrawingColor()
            };

            SettingsGroup colorPalette = new()
            {
                Key = "ColorPalette",
                Name = _resources.GetString("ColorPaletteName"),
                Items =
                [
                    new GroupSettingsItem()
                    {
                        Key = "DefaultColors",
                        Name = _resources.GetString("DefaultColorsName"),
                        Items =
                        [
                            defaultBackgroundColor,
                            defaultForegroundColor,
                            defaultUnderlineColor
                        ]
                    },
                    new GroupSettingsItem()
                    {
                        Key = "StandardColors",
                        Name = _resources.GetString("StandardColorsName"),
                        Items =
                        [
                            blackStandardColor,
                            redStandardColor,
                            greenStandardColor,
                            yellowStandardColor,
                            blueStandardColor,
                            magentaStandardColor,
                            cyanStandardColor,
                            whiteStandardColor
                        ]
                    },
                    new GroupSettingsItem()
                    {
                        Key = "BrightColors",
                        Name = _resources.GetString("BrightColorsName"),
                        Items =
                        [
                            blackBrightColor,
                            redBrightColor,
                            greenBrightColor,
                            yellowBrightColor,
                            blueBrightColor,
                            magentaBrightColor,
                            cyanBrightColor,
                            whiteBrightColor
                        ]
                    }
                ]
            };

            for (int i = 0; i < settingsViewModel.Groups.Count; i++)
            {
                SettingsGroup settingsGroup = settingsViewModel.Groups[i];

                if (settingsGroup.Key == "Basics")
                {
                    if (settingsGroup.Items is null)
                    {
                        continue;
                    }

                    settingsGroup.Items.Insert(0, command);
                    settingsGroup.Items.Insert(1, new CaptionSettingsItem() { Key = "CommandCaption", Getter = () => _resources.GetString("CommandExplanation") });

                    settingsGroup.Items.Add(
                        new GroupSettingsItem()
                        {
                            Key = "BasicsWorkingDirectory",
                            Name = _resources.GetString("BasicsWorkingDirectoryName"),
                            Items =
                            [
                                startDirectory,
                                nullStartDirectory,
                                startDirectoryCaption
                            ]
                        }
                    );
                }
                else if (settingsGroup.Key == "Appearance")
                {
                    if (settingsGroup.Items is null)
                    {
                        continue;
                    }

                    settingsGroup.Items.Insert(0, windowBackdrop);
                    settingsGroup.Items.Insert(1, solidColorWindowBackdropColor);
                }
                else if (settingsGroup.Key == "Behavior")
                {
                    if (settingsGroup.Items is null)
                    {
                        continue;
                    }

                    for (int j = 0; j < settingsGroup.Items.Count; j++)
                    {
                        if (settingsGroup.Items[j].Key == "UseVisualBell")
                        {
                            settingsGroup.Items.Insert(j + 1, visualBellDisplayTime);
                            settingsGroup.Items.Insert(j + 2, new CaptionSettingsItem() { Key = "VisualBellDisplayTimeCaption", Getter = () => _resources.GetString("VisualBellDisplayTimeExplanation") });
                        }
                    }
                }
                else if (settingsGroup.Key == "KeyBindings")
                {
                    settingsViewModel.Groups.Insert(i++, colorPalette);
                }
            }

            settingsViewModel.Groups.Add(advanced);

            settingsViewModel.CallbackTokens.Add(
                DependencyProperties.CommandProperty,
                _dependencyProperties.RegisterPropertyChangedCallback(
                    DependencyProperties.CommandProperty,
                    (_, _) => command.BoundValue = _dependencyProperties.Command
                )
            );

            settingsViewModel.CallbackTokens.Add(
                DependencyProperties.StartDirectoryProperty,
                _dependencyProperties.RegisterPropertyChangedCallback(
                    DependencyProperties.StartDirectoryProperty,
                    (_, _) =>
                    {
                        startDirectory.BoundValue = _dependencyProperties.StartDirectory is null ? string.Empty : _dependencyProperties.StartDirectory;
                        nullStartDirectory.BoundValue = _dependencyProperties.StartDirectory is null;
                    }
                )
            );

            settingsViewModel.CallbackTokens.Add(
                DependencyProperties.WindowBackdropProperty,
                _dependencyProperties.RegisterPropertyChangedCallback(
                    DependencyProperties.WindowBackdropProperty,
                    (_, _) => windowBackdrop.BoundValue = (int)_dependencyProperties.WindowBackdrop
                )
            );

            settingsViewModel.CallbackTokens.Add(
                DependencyProperties.SolidColorWindowBackdropColorProperty,
                _dependencyProperties.RegisterPropertyChangedCallback(
                    DependencyProperties.SolidColorWindowBackdropColorProperty,
                    (_, _) => solidColorWindowBackdropColor.BoundValue = _dependencyProperties.SolidColorWindowBackdropColor
                )
            );

            settingsViewModel.CallbackTokens.Add(
                DependencyProperties.VisualBellDisplayTimeProperty,
                _dependencyProperties.RegisterPropertyChangedCallback(
                    DependencyProperties.VisualBellDisplayTimeProperty,
                    (_, _) => visualBellDisplayTime.BoundValue = _dependencyProperties.VisualBellDisplayTime
                )
            );
        }

        /// <summary>
        /// Invoked when the user clicks the terminal's settings window's save
        /// as defaults button.
        /// </summary>
#pragma warning disable IDE0079 // Remove unnecessary suppression
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "Types are preserved")]
#pragma warning restore IDE0079 // Remove unnecessary suppression
        private void TerminalControl_SaveSettingsAsDefaults()
        {
            Settings.Json.Settings settings = new()
            {
                Basics = new()
                {
                    Command = _dependencyProperties.Command,
                    DefaultWindowTitle = TerminalControl.DefaultWindowTitle,
                    Rows = TerminalControl.Rows,
                    Columns = TerminalControl.Columns,
                    TabWidth = TerminalControl.TabWidth,
                    StartDirectory = _dependencyProperties.StartDirectory
                },

                Appearance = new()
                {
                    WindowBackdrop = _dependencyProperties.WindowBackdrop,
                    SolidColorWindowBackdropColor = _dependencyProperties.SolidColorWindowBackdropColor,
                    FontFamily = TerminalControl.FontFamily,
                    FontSize = TerminalControl.FontSize,
                    TextAntialiasing = TerminalControl.TextAntialiasing,
                    FullColorEmoji = TerminalControl.FullColorEmoji,
                    UseBackgroundColorErase = TerminalControl.UseBackgroundColorErase,
                    BackgroundIsInvisible = TerminalControl.BackgroundIsInvisible
                },

                Behavior = new()
                {
                    UseVisualBell = TerminalControl.UseVisualBell,
                    VisualBellDisplayTime = _dependencyProperties.VisualBellDisplayTime,
                    UseContextMenu = TerminalControl.UseContextMenu,
                    UseExtendedContextMenu = TerminalControl.UseExtendedContextMenu
                },

                Cursor = new()
                {
                    CursorStyle = TerminalControl.CursorStyle,
                    CursorThickness = TerminalControl.CursorThickness,
                    CursorBlink = TerminalControl.CursorBlink,
                    CursorBlinkRate = TerminalControl.CursorBlinkRate,
                    CursorColor = TerminalControl.CursorColor
                },

                Scrollback = new()
                {
                    ScrollbackLines = TerminalControl.Scrollback,
                    LinesPerScrollback = TerminalControl.LinesPerScrollback,
                    LinesPerSmallScrollback = TerminalControl.LinesPerSmallScrollback,
                    LinesPerWheelScrollback = TerminalControl.LinesPerWheelScrollback
                },

                CopyAndPaste = new()
                {
                    CopyOnMouseUp = TerminalControl.CopyOnMouseUp,
                    PasteOnMiddleClick = TerminalControl.PasteOnMiddleClick,
                    PasteOnRightClick = TerminalControl.PasteOnRightClick,
                    CopyNewline = TerminalControl.CopyNewline
                },

                ColorPalette = new()
                {
                    DefaultColors = new()
                    {
                        DefaultBackgroundColor = TerminalControl.AnsiColors.DefaultBackgroundColor.ToWindowsUIColor(),
                        DefaultForegroundColor = TerminalControl.AnsiColors.DefaultForegroundColor.ToWindowsUIColor(),
                        DefaultUnderlineColor = TerminalControl.AnsiColors.DefaultUnderlineColor.ToWindowsUIColor()
                    },

                    StandardColors = new()
                    {
                        Black = TerminalControl.AnsiColors.Black.ToWindowsUIColor(),
                        Red = TerminalControl.AnsiColors.Red.ToWindowsUIColor(),
                        Green = TerminalControl.AnsiColors.Green.ToWindowsUIColor(),
                        Yellow = TerminalControl.AnsiColors.Yellow.ToWindowsUIColor(),
                        Blue = TerminalControl.AnsiColors.Blue.ToWindowsUIColor(),
                        Magenta = TerminalControl.AnsiColors.Magenta.ToWindowsUIColor(),
                        Cyan = TerminalControl.AnsiColors.Cyan.ToWindowsUIColor(),
                        White = TerminalControl.AnsiColors.White.ToWindowsUIColor(),
                    },

                    BrightColors = new()
                    {
                        BrightBlack = TerminalControl.AnsiColors.BrightBlack.ToWindowsUIColor(),
                        BrightRed = TerminalControl.AnsiColors.BrightRed.ToWindowsUIColor(),
                        BrightGreen = TerminalControl.AnsiColors.BrightGreen.ToWindowsUIColor(),
                        BrightYellow = TerminalControl.AnsiColors.BrightYellow.ToWindowsUIColor(),
                        BrightBlue = TerminalControl.AnsiColors.BrightBlue.ToWindowsUIColor(),
                        BrightMagenta = TerminalControl.AnsiColors.BrightMagenta.ToWindowsUIColor(),
                        BrightCyan = TerminalControl.AnsiColors.BrightCyan.ToWindowsUIColor(),
                        BrightWhite = TerminalControl.AnsiColors.BrightWhite.ToWindowsUIColor(),
                    }
                }
            };

            bool writeSucceeded = false;

            try
            {
                using StreamWriter settingsJsonFile = new(SettingsHelper.SettingsPath, new FileStreamOptions() { Access = FileAccess.Write, Mode = FileMode.Create });

                settingsJsonFile.Write(JsonSerializer.Serialize(settings, SettingsHelper.JsonSerializerOptions));
                _settingsSaveTimer.Enabled = true;
                writeSucceeded = true;
            }
            catch (IOException e)
            {
                ShowErrorDialog(TerminalControl.SettingsWindow!.Content.XamlRoot, _resources.GetString("CouldNotSaveSettings"), e);
            }

            if (writeSucceeded)
            {
                AppNotificationManager.Default.Show(
                    new AppNotificationBuilder()
                        .AddText(_resources.GetString("SettingsSaved"))
                        .AddText(_resources.GetString("DefaultSettingsHaveBeenSaved"))
                        .BuildNotification()
                );
            }
        }

        /// <summary>
        /// Invoked when <see cref="_settingsJsonWatcher"/> detects a change to
        /// the settings JSON file.
        /// </summary>
        /// <param name="sender"><inheritdoc cref="FileSystemEventHandler"
        /// path="/param[@name='sender']"/></param>
        /// <param name="e"><inheritdoc cref="FileSystemEventHandler"
        /// path="/param[@name='e']"/></param>
        private void SettingsJsonWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            // Ignore events that were caused by us
            if (_settingsSaveTimer.Enabled)
            {
                return;
            }

            // Ignore events if the settings window is not open
            if (TerminalControl.SettingsWindow is null)
            {
                return;
            }

            Task.Delay(SettleDelay).ContinueWith(_ =>
            {
                // This does not arrive from the UI thread
                TerminalControl.DispatcherQueue.TryEnqueue(() => LoadSettings(initialLoad: false, terminalIsInitialized: true));
            });
        }

        /// <summary>
        /// Shows an error dialog.
        /// </summary>
        /// <param name="xamlRoot">The <see cref="XamlRoot"/> in which to
        /// present the dialog.</param>
        /// <param name="title">The dialog title.</param>
        /// <param name="e">The exception to describe in the dialog.</param>
        private async void ShowErrorDialog(XamlRoot xamlRoot, string title, Exception e)
        {
            StackPanel dialogContent = new();
            dialogContent.Children.Add(
                new TextBlock()
                {
                    Text = e.Message,
                    TextWrapping = TextWrapping.WrapWholeWords
                }
            );

            ContentDialog dialog = new()
            {
                XamlRoot = xamlRoot,
                Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
                Title = title,
                CloseButtonText = _resources.GetString("CloseButtonCaption"),
                DefaultButton = ContentDialogButton.Close,
                Content = dialogContent
            };

            await dialog.ShowAsync();
        }
    }
}