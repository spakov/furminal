using Microsoft.UI.Xaml;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Spakov.Terminal.Settings {
  public class SettingsViewModel {
    private const string windowsLineEnding = "\r\n";
    private const string unixLineEnding = "\n";

    private readonly Dictionary<DependencyProperty, long> callbackTokens;

    /// <summary>
    /// Settings groups and items.
    /// </summary>
    public ObservableCollection<SettingsGroup> Groups { get; }

    /// <summary>
    /// A dictionary of tokens returned by <see
    /// cref="DependencyObject.RegisterPropertyChangedCallback"/> that
    /// must be unregistered.
    /// </summary>
    public Dictionary<DependencyProperty, long> CallbackTokens => callbackTokens;

    /// <summary>
    /// Initializes a <see cref="SettingsViewModel"/>.
    /// </summary>
    /// <remarks>
    /// <para><see cref="Groups"/> can be customized after instantiation but
    /// before display by creating a callback for <see
    /// cref="TerminalControl.CustomizeSettingsWindowSettings"/>. If
    /// doing so, be sure to complete the following steps in the callback
    /// method:</para>
    /// <list type="number">
    /// <item>Create objects that are subclasses of <see
    /// cref="SettingsItem"/> or objects of type <see cref="SettingsGroup"/>
    /// containing such objects.</item>
    /// <item>Add them to <see cref="Groups"/>, optionally using <see
    /// cref="SettingsGroup.Key"/> to locate the desired section.</item>
    /// <item>Register <see cref="DependencyPropertyChangedCallback"/>s for
    /// applicable objects you created.</item>
    /// <item>Add the tokens returned by <see
    /// cref="DependencyObject.RegisterPropertyChangedCallback"/> to <see
    /// cref="CallbackTokens"/>.</item>
    /// </list>
    /// </remarks>
    /// <param name="terminalControl">A <see cref="TerminalControl"/>.</param>
    internal SettingsViewModel(TerminalControl terminalControl) {
      callbackTokens = [];

      // Basics

      TextSettingsItem defaultWindowTitle = new() {
        Key = nameof(terminalControl.DefaultWindowTitle),
        Name = terminalControl.ResourceLoader.GetString("DefaultWindowTitleName"),
        Getter = () => terminalControl.DefaultWindowTitle,
        Setter = (defaultWindowTitle) => terminalControl.DefaultWindowTitle = defaultWindowTitle
      };

      IntegerSettingsItem rows = new() {
        Key = nameof(terminalControl.Rows),
        Name = terminalControl.ResourceLoader.GetString("RowsName"),
        Getter = () => terminalControl.Rows,
        Setter = (rows) => terminalControl.Rows = rows,
        LargeChange = 8
      };

      IntegerSettingsItem columns = new() {
        Key = nameof(terminalControl.Columns),
        Name = terminalControl.ResourceLoader.GetString("ColumnsName"),
        Getter = () => terminalControl.Columns,
        Setter = (columns) => terminalControl.Columns = columns,
        LargeChange = 10
      };

      IntegerSettingsItem tabWidth = new() {
        Key = nameof(terminalControl.TabWidth),
        Name = terminalControl.ResourceLoader.GetString("TabWidthName"),
        Getter = () => terminalControl.TabWidth,
        Setter = (tabWidth) => terminalControl.TabWidth = tabWidth,
        LargeChange = 2
      };

      // Appearance

      FontFamilyPickerSettingsItem fontFamily = new() {
        Key = nameof(terminalControl.FontFamily),
        Name = terminalControl.ResourceLoader.GetString("FontFamilyName"),
        Getter = () => terminalControl.FontFamily,
        Setter = (fontFamily) => terminalControl.FontFamily = fontFamily,
        MonospaceOnlyName = terminalControl.ResourceLoader.GetString("MonospaceOnlyName"),
        DefaultFontFamily = "0xProto Nerd Font Propo"
      };

      NumberSettingsItem fontSize = new() {
        Key = nameof(terminalControl.FontSize),
        Name = terminalControl.ResourceLoader.GetString("FontSizeName"),
        Getter = () => terminalControl.FontSize,
        Setter = (fontSize) => terminalControl.FontSize = fontSize,
        SmallChange = 0.5,
        LargeChange = 1.0
      };

      RadioSettingsItem noneTextAntialiasing = new() {
        Key = "TextAntialiasing",
        Name = terminalControl.ResourceLoader.GetString("NoneTextAntialiasingName")
      };

      RadioSettingsItem grayscaleTextAntialiasing = new() {
        Key = "GrayscaleTextAntialiasing",
        Name = terminalControl.ResourceLoader.GetString("GrayscaleTextAntialiasingName")
      };

      RadioSettingsItem clearTypeTextAntialiasing = new() {
        Key = "ClearTypeTextAntialiasing",
        Name = terminalControl.ResourceLoader.GetString("ClearTypeTextAntialiasingName")
      };

      RadioCollectionSettingsItem textAntialiasing = new() {
        Key = nameof(terminalControl.TextAntialiasing),
        Name = terminalControl.ResourceLoader.GetString("TextAntialiasingName"),
        Items = [
          noneTextAntialiasing,
          grayscaleTextAntialiasing,
          clearTypeTextAntialiasing
        ],
        Getter = () => (int) terminalControl.TextAntialiasing,
        Setter = (textAntialiasing) => terminalControl.TextAntialiasing = (TextAntialiasingStyles) textAntialiasing
      };

      BooleanSettingsItem fullColorEmoji = new() {
        Key = nameof(terminalControl.FullColorEmoji),
        Name = terminalControl.ResourceLoader.GetString("FullColorEmojiName"),
        Getter = () => terminalControl.FullColorEmoji,
        Setter = (fullColorEmoji) => terminalControl.FullColorEmoji = fullColorEmoji
      };

      BooleanSettingsItem useBackgroundColorErase = new() {
        Key = nameof(terminalControl.UseBackgroundColorErase),
        Name = terminalControl.ResourceLoader.GetString("UseBackgroundColorEraseName"),
        Getter = () => terminalControl.UseBackgroundColorErase,
        Setter = (useBackgroundColorErase) => terminalControl.UseBackgroundColorErase = useBackgroundColorErase
      };

      BooleanSettingsItem backgroundIsInvisible = new() {
        Key = nameof(terminalControl.BackgroundIsInvisible),
        Name = terminalControl.ResourceLoader.GetString("BackgroundIsInvisibleName"),
        Getter = () => terminalControl.BackgroundIsInvisible,
        Setter = (backgroundIsInvisible) => terminalControl.BackgroundIsInvisible = backgroundIsInvisible
      };

      // Behavior

      BooleanSettingsItem useVisualBell = new() {
        Key = nameof(terminalControl.UseVisualBell),
        Name = terminalControl.ResourceLoader.GetString("UseVisualBellName"),
        Getter = () => terminalControl.UseVisualBell,
        Setter = (useVisualBell) => terminalControl.UseVisualBell = useVisualBell
      };

      BooleanSettingsItem useContextMenu = new() {
        Key = nameof(terminalControl.UseContextMenu),
        Name = terminalControl.ResourceLoader.GetString("UseContextMenuName"),
        Getter = () => terminalControl.UseContextMenu,
        Setter = (useContextMenu) => {
          terminalControl.UseContextMenu = useContextMenu;

          if (!useContextMenu) {
            terminalControl.UseExtendedContextMenu = false;
          }
        }
      };

      BooleanSettingsItem useExtendedContextMenu = new() {
        Key = nameof(terminalControl.UseExtendedContextMenu),
        Name = terminalControl.ResourceLoader.GetString("UseExtendedContextMenuName"),
        Getter = () => terminalControl.UseExtendedContextMenu,
        Setter = (useExtendedContextMenu) => {
          terminalControl.UseExtendedContextMenu = useExtendedContextMenu;

          if (useExtendedContextMenu) {
            terminalControl.UseContextMenu = true;
          }
        }
      };

      // Cursor

      BooleanSettingsItem cursorStyleBlock = new() {
        Key = "CursorStyleBlock",
        Name = terminalControl.ResourceLoader.GetString("CursorStyleBlockName"),
        Getter = () => terminalControl.CursorStyle == CursorStyles.Block,
        Setter = (cursorStyleBlock) => {
          terminalControl.CursorStyle = cursorStyleBlock
            ? CursorStyles.Block
            : terminalControl.CursorStyle == CursorStyles.Bar
              ? CursorStyles.Bar
              : CursorStyles.Underline;
        }
      };

      BooleanSettingsItem cursorStyleUnderline = new() {
        Key = "CursorStyleUnderline",
        Name = terminalControl.ResourceLoader.GetString("CursorStyleUnderlineName"),
        Getter = () => terminalControl.CursorStyle == CursorStyles.Underline,
        Setter = (cursorStyleUnderline) => {
          terminalControl.CursorStyle = cursorStyleUnderline
            ? CursorStyles.Underline
            : terminalControl.CursorStyle == CursorStyles.Block
              ? CursorStyles.Block
              : CursorStyles.Bar;
        }
      };

      BooleanSettingsItem cursorStyleBar = new() {
        Key = "CursorStyleBar",
        Name = terminalControl.ResourceLoader.GetString("CursorStyleBarName"),
        Getter = () => terminalControl.CursorStyle == CursorStyles.Bar,
        Setter = (cursorStyleBar) => {
          terminalControl.CursorStyle = cursorStyleBar
            ? CursorStyles.Bar
            : terminalControl.CursorStyle == CursorStyles.Underline
              ? CursorStyles.Underline
              : CursorStyles.Block;
        }
      };

      NumberSettingsItem cursorThickness = new() {
        Key = nameof(terminalControl.CursorThickness),
        Name = terminalControl.ResourceLoader.GetString("CursorThicknessName"),
        Getter = () => terminalControl.CursorThickness,
        Setter = (cursorThickness) => terminalControl.CursorThickness = cursorThickness,
        SmallChange = 0.05,
        LargeChange = 0.1
      };

      BooleanSettingsItem cursorBlink = new() {
        Key = nameof(terminalControl.CursorBlink),
        Name = terminalControl.ResourceLoader.GetString("CursorBlinkName"),
        Getter = () => terminalControl.CursorBlink,
        Setter = (cursorBlink) => terminalControl.CursorBlink = cursorBlink
      };

      IntegerSettingsItem cursorBlinkRate = new() {
        Key = nameof(terminalControl.CursorBlinkRate),
        Name = terminalControl.ResourceLoader.GetString("CursorBlinkRateName"),
        Getter = () => terminalControl.CursorBlinkRate,
        Setter = (cursorBlinkRate) => terminalControl.CursorBlinkRate = cursorBlinkRate,
        SmallChange = 25,
        LargeChange = 100
      };

      ColorSettingsItem cursorColor = new() {
        Key = nameof(terminalControl.CursorColor),
        Name = terminalControl.ResourceLoader.GetString("CursorColorName"),
        Getter = () => terminalControl.CursorColor,
        Setter = (cursorColor) => terminalControl.CursorColor = cursorColor
      };

      // Scrollback

      IntegerSettingsItem scrollback = new() {
        Key = nameof(terminalControl.Scrollback),
        Name = terminalControl.ResourceLoader.GetString("ScrollbackName"),
        Getter = () => terminalControl.Scrollback,
        Setter = (scrollback) => terminalControl.Scrollback = scrollback,
        SmallChange = 100,
        LargeChange = 1000
      };

      IntegerSettingsItem linesPerScrollback = new() {
        Key = nameof(terminalControl.LinesPerScrollback),
        Name = terminalControl.ResourceLoader.GetString("LinesPerScrollbackName"),
        Getter = () => terminalControl.LinesPerScrollback,
        Setter = (linesPerScrollback) => terminalControl.LinesPerScrollback = linesPerScrollback,
        SmallChange = 4,
        LargeChange = 8
      };

      IntegerSettingsItem linesPerSmallScrollback = new() {
        Key = nameof(terminalControl.LinesPerSmallScrollback),
        Name = terminalControl.ResourceLoader.GetString("LinesPerSmallScrollbackName"),
        Getter = () => terminalControl.LinesPerSmallScrollback,
        Setter = (linesPerSmallScrollback) => terminalControl.LinesPerSmallScrollback = linesPerSmallScrollback,
        SmallChange = 1,
        LargeChange = 4
      };

      IntegerSettingsItem linesPerWheelScrollback = new() {
        Key = nameof(terminalControl.LinesPerWheelScrollback),
        Name = terminalControl.ResourceLoader.GetString("LinesPerWheelScrollbackName"),
        Getter = () => terminalControl.LinesPerWheelScrollback,
        Setter = (linesPerWheelScrollback) => terminalControl.LinesPerWheelScrollback = linesPerWheelScrollback,
        SmallChange = 1,
        LargeChange = 4
      };

      // Copy and Paste

      BooleanSettingsItem copyOnMouseUp = new() {
        Key = nameof(terminalControl.CopyOnMouseUp),
        Name = terminalControl.ResourceLoader.GetString("CopyOnMouseUpName"),
        Getter = () => terminalControl.CopyOnMouseUp,
        Setter = (copyOnMouseUp) => terminalControl.CopyOnMouseUp = copyOnMouseUp
      };

      BooleanSettingsItem pasteOnMiddleClick = new() {
        Key = nameof(terminalControl.PasteOnMiddleClick),
        Name = terminalControl.ResourceLoader.GetString("PasteOnMiddleClickName"),
        Getter = () => terminalControl.PasteOnMiddleClick,
        Setter = (pasteOnMiddleClick) => terminalControl.PasteOnMiddleClick = pasteOnMiddleClick
      };

      BooleanSettingsItem pasteOnRightClick = new() {
        Key = nameof(terminalControl.PasteOnRightClick),
        Name = terminalControl.ResourceLoader.GetString("PasteOnRightClickName"),
        Getter = () => terminalControl.PasteOnRightClick,
        Setter = (pasteOnRightClick) => terminalControl.PasteOnRightClick = pasteOnRightClick
      };

      BooleanSettingsItem copyNewlineWindows = new() {
        Key = "CopyNewlineWindows",
        Name = terminalControl.ResourceLoader.GetString("CopyNewlineWindowsName"),
        Getter = () => terminalControl.CopyNewline == windowsLineEnding,
        Setter = (copyNewlineWindows) => {
          terminalControl.CopyNewline = copyNewlineWindows
            ? windowsLineEnding
            : unixLineEnding;
        }
      };

      BooleanSettingsItem copyNewlineUnix = new() {
        Key = "CopyNewlineUnix",
        Name = terminalControl.ResourceLoader.GetString("CopyNewlineUnixName"),
        Getter = () => terminalControl.CopyNewline == unixLineEnding,
        Setter = (copyNewlineUnix) => {
          terminalControl.CopyNewline = copyNewlineUnix
            ? unixLineEnding
            : windowsLineEnding;
        }
      };

      // Key Bindings

      KeyBindingSettingsItem scrollBack = new() {
        Key = "ScrollBackKeyBinding",
        Name = terminalControl.ResourceLoader.GetString("ScrollBackKeyBindingName"),
        Getter = () => terminalControl.ResourceLoader.GetString("ScrollBackKeyBinding")
      };

      KeyBindingSettingsItem scrollForward = new() {
        Key = "ScrollForwardKeyBinding",
        Name = terminalControl.ResourceLoader.GetString("ScrollForwardKeyBindingName"),
        Getter = () => terminalControl.ResourceLoader.GetString("ScrollForwardKeyBinding")
      };

      KeyBindingSettingsItem smallScrollBack = new() {
        Key = "SmallScrollBackKeyBinding",
        Name = terminalControl.ResourceLoader.GetString("SmallScrollBackKeyBindingName"),
        Getter = () => terminalControl.ResourceLoader.GetString("SmallScrollBackKeyBinding")
      };

      KeyBindingSettingsItem smallScrollForward = new() {
        Key = "SmallScrollForwardKeyBinding",
        Name = terminalControl.ResourceLoader.GetString("SmallScrollForwardKeyBindingName"),
        Getter = () => terminalControl.ResourceLoader.GetString("SmallScrollForwardKeyBinding")
      };

      KeyBindingSettingsItem copy = new() {
        Key = "CopyKeyBinding",
        Name = terminalControl.ResourceLoader.GetString("CopyKeyBindingName"),
        Getter = () => terminalControl.ResourceLoader.GetString("CopyKeyBinding")
      };

      KeyBindingSettingsItem paste = new() {
        Key = "PasteKeyBinding",
        Name = terminalControl.ResourceLoader.GetString("PasteKeyBindingName"),
        Getter = () => terminalControl.ResourceLoader.GetString("PasteKeyBinding")
      };

      KeyBindingSettingsItem sendNul = new() {
        Key = "SendNulKeyBinding",
        Name = terminalControl.ResourceLoader.GetString("SendNulKeyBindingName"),
        Getter = () => terminalControl.ResourceLoader.GetString("SendNulKeyBinding")
      };

      KeyBindingSettingsItem sendBs = new() {
        Key = "SendBsKeyBinding",
        Name = terminalControl.ResourceLoader.GetString("SendBsKeyBindingName"),
        Getter = () => terminalControl.ResourceLoader.GetString("SendBsKeyBinding")
      };

      KeyBindingSettingsItem sendEsc = new() {
        Key = "SendEscKeyBinding",
        Name = terminalControl.ResourceLoader.GetString("SendEscKeyBindingName"),
        Getter = () => terminalControl.ResourceLoader.GetString("SendEscKeyBinding")
      };

      KeyBindingSettingsItem sendFs = new() {
        Key = "SendFsKeyBinding",
        Name = terminalControl.ResourceLoader.GetString("SendFsKeyBindingName"),
        Getter = () => terminalControl.ResourceLoader.GetString("SendFsKeyBinding")
      };

      KeyBindingSettingsItem sendGs = new() {
        Key = "SendGsKeyBinding",
        Name = terminalControl.ResourceLoader.GetString("SendGsKeyBindingName"),
        Getter = () => terminalControl.ResourceLoader.GetString("SendGsKeyBinding")
      };

      KeyBindingSettingsItem sendRs = new() {
        Key = "SendRsKeyBinding",
        Name = terminalControl.ResourceLoader.GetString("SendRsKeyBindingName"),
        Getter = () => terminalControl.ResourceLoader.GetString("SendRsKeyBinding")
      };

      KeyBindingSettingsItem sendUs = new() {
        Key = "SendUsKeyBinding",
        Name = terminalControl.ResourceLoader.GetString("SendUsKeyBindingName"),
        Getter = () => terminalControl.ResourceLoader.GetString("SendUsKeyBinding")
      };

      Groups = [
        new() {
          Key = "Basics",
          Name = terminalControl.ResourceLoader.GetString("BasicsName"),
          Items = [
            defaultWindowTitle,
            rows,
            columns,
            tabWidth
          ]
        },

        new() {
          Key = "Appearance",
          Name = terminalControl.ResourceLoader.GetString("AppearanceName"),
          Items = [
            fontFamily,
            fontSize,
            textAntialiasing,
            new CaptionSettingsItem() { Key = "TextAntialiasingCaption1", Getter = () => terminalControl.ResourceLoader.GetString("TextAntialiasingExplanation1") },
            new CaptionSettingsItem() { Key = "TextAntialiasingCaption2", Getter = () => terminalControl.ResourceLoader.GetString("TextAntialiasingExplanation2") },
            fullColorEmoji,
            new CaptionSettingsItem() { Key = "FullColorEmojiCaption", Getter = () => terminalControl.ResourceLoader.GetString("FullColorEmojiExplanation") },
            useBackgroundColorErase,
            backgroundIsInvisible
          ]
        },

        new() {
          Key = "Behavior",
          Name = terminalControl.ResourceLoader.GetString("BehaviorName"),
          Items = [
            useVisualBell,
            useContextMenu,
            useExtendedContextMenu
          ]
        },

        new() {
          Key = "Cursor",
          Name = terminalControl.ResourceLoader.GetString("CursorName"),
          Items = [
            new GroupSettingsItem() {
              Key = "CursorStyle",
              Name = terminalControl.ResourceLoader.GetString("CursorStyleName"),
              Items = [
                cursorStyleBlock,
                cursorStyleUnderline,
                cursorStyleBar
              ]
            },
            cursorThickness,
            new CaptionSettingsItem() { Key = "CursorThicknessCaption", Getter = () => terminalControl.ResourceLoader.GetString("CursorThicknessExplanation") },
            cursorBlink,
            cursorBlinkRate,
            new CaptionSettingsItem() { Key = "CursorBlinkRateCaption", Getter = () => terminalControl.ResourceLoader.GetString("CursorBlinkRateExplanation") },
            cursorColor
          ]
        },

        new() {
          Key = "Scrollback",
          Name = terminalControl.ResourceLoader.GetString("ScrollbackGroupName"),
          Items = [
            scrollback,
            linesPerScrollback,
            linesPerSmallScrollback,
            linesPerWheelScrollback
          ]
        },

        new() {
          Key = "CopyAndPaste",
          Name = terminalControl.ResourceLoader.GetString("CopyAndPasteName"),
          Items = [
            copyOnMouseUp,
            pasteOnMiddleClick,
            pasteOnRightClick,
            new CaptionSettingsItem() { Key = "PasteOnRightClickCaption", Getter = () => terminalControl.ResourceLoader.GetString("PasteOnRightClickExplanation") },
            new GroupSettingsItem() {
              Key = "CopyAndPasteLineEndingForCopiedLines",
              Name = terminalControl.ResourceLoader.GetString("CopyAndPasteLineEndingForCopiedLinesName"),
              Items = [
                copyNewlineWindows,
                copyNewlineUnix
              ]
            }
          ]
        },

        new() {
          Key = "KeyBindings",
          Name = terminalControl.ResourceLoader.GetString("KeyBindingsName"),
          Items = [
            scrollBack,
            scrollForward,
            smallScrollBack,
            smallScrollForward,
            copy,
            paste,
            sendNul,
            sendBs,
            sendEsc,
            sendFs,
            sendGs,
            sendRs,
            sendUs
          ]
        }
      ];

      callbackTokens.Add(
        TerminalControl.DefaultWindowTitleProperty,
        terminalControl.RegisterPropertyChangedCallback(
          TerminalControl.DefaultWindowTitleProperty,
          (_, _) => defaultWindowTitle.BoundValue = terminalControl.DefaultWindowTitle
        )
      );

      callbackTokens.Add(
        TerminalControl.RowsProperty,
        terminalControl.RegisterPropertyChangedCallback(
          TerminalControl.RowsProperty,
          (_, _) => rows.BoundValue = terminalControl.Rows
        )
      );

      callbackTokens.Add(
        TerminalControl.ColumnsProperty,
        terminalControl.RegisterPropertyChangedCallback(
          TerminalControl.ColumnsProperty,
          (_, _) => columns.BoundValue = terminalControl.Columns
        )
      );

      callbackTokens.Add(
        TerminalControl.TabWidthProperty,
        terminalControl.RegisterPropertyChangedCallback(
          TerminalControl.TabWidthProperty,
          (_, _) => tabWidth.BoundValue = terminalControl.TabWidth
        )
      );

      callbackTokens.Add(
        TerminalControl.FontFamilyProperty,
        terminalControl.RegisterPropertyChangedCallback(
          TerminalControl.FontFamilyProperty,
          (_, _) => fontFamily.BoundValue = terminalControl.FontFamily
        )
      );

      callbackTokens.Add(
        TerminalControl.FontSizeProperty,
        terminalControl.RegisterPropertyChangedCallback(
          TerminalControl.FontSizeProperty,
          (_, _) => fontSize.BoundValue = terminalControl.FontSize
        )
      );

      callbackTokens.Add(
        TerminalControl.TextAntialiasingProperty,
        terminalControl.RegisterPropertyChangedCallback(
          TerminalControl.TextAntialiasingProperty,
          (_, _) => textAntialiasing.BoundValue = (int) terminalControl.TextAntialiasing
        )
      );

      callbackTokens.Add(
        TerminalControl.FullColorEmojiProperty,
        terminalControl.RegisterPropertyChangedCallback(
          TerminalControl.FullColorEmojiProperty,
          (_, _) => fullColorEmoji.BoundValue = terminalControl.FullColorEmoji
        )
      );

      callbackTokens.Add(
        TerminalControl.UseBackgroundColorEraseProperty,
        terminalControl.RegisterPropertyChangedCallback(
          TerminalControl.UseBackgroundColorEraseProperty,
          (_, _) => useBackgroundColorErase.BoundValue = terminalControl.UseBackgroundColorErase
        )
      );

      callbackTokens.Add(
        TerminalControl.BackgroundIsInvisibleProperty,
        terminalControl.RegisterPropertyChangedCallback(
          TerminalControl.BackgroundIsInvisibleProperty,
          (_, _) => backgroundIsInvisible.BoundValue = terminalControl.BackgroundIsInvisible
        )
      );

      callbackTokens.Add(
        TerminalControl.UseVisualBellProperty,
        terminalControl.RegisterPropertyChangedCallback(
          TerminalControl.UseVisualBellProperty,
          (_, _) => useVisualBell.BoundValue = terminalControl.UseVisualBell
        )
      );

      callbackTokens.Add(
        TerminalControl.UseContextMenuProperty,
        terminalControl.RegisterPropertyChangedCallback(
          TerminalControl.UseContextMenuProperty,
          (_, _) => useContextMenu.BoundValue = terminalControl.UseContextMenu
        )
      );

      callbackTokens.Add(
        TerminalControl.UseExtendedContextMenuProperty,
        terminalControl.RegisterPropertyChangedCallback(
          TerminalControl.UseExtendedContextMenuProperty,
          (_, _) => useExtendedContextMenu.BoundValue = terminalControl.UseExtendedContextMenu
        )
      );

      callbackTokens.Add(
        TerminalControl.CursorStyleProperty,
        terminalControl.RegisterPropertyChangedCallback(
          TerminalControl.CursorStyleProperty,
          (_, _) => {
            cursorStyleBlock.BoundValue = terminalControl.CursorStyle == CursorStyles.Block;
            cursorStyleUnderline.BoundValue = terminalControl.CursorStyle == CursorStyles.Underline;
            cursorStyleBar.BoundValue = terminalControl.CursorStyle == CursorStyles.Bar;
          }
        )
      );

      callbackTokens.Add(
        TerminalControl.CursorThicknessProperty,
        terminalControl.RegisterPropertyChangedCallback(
          TerminalControl.CursorThicknessProperty,
          (_, _) => cursorThickness.BoundValue = terminalControl.CursorThickness
        )
      );

      callbackTokens.Add(
        TerminalControl.CursorBlinkProperty,
        terminalControl.RegisterPropertyChangedCallback(
          TerminalControl.CursorBlinkProperty,
          (_, _) => cursorBlink.BoundValue = terminalControl.CursorBlink
        )
      );

      callbackTokens.Add(
        TerminalControl.CursorBlinkRateProperty,
        terminalControl.RegisterPropertyChangedCallback(
          TerminalControl.CursorBlinkRateProperty,
          (_, _) => cursorBlinkRate.BoundValue = terminalControl.CursorBlinkRate
        )
      );

      callbackTokens.Add(
        TerminalControl.CursorColorProperty,
        terminalControl.RegisterPropertyChangedCallback(
          TerminalControl.CursorColorProperty,
          (_, _) => cursorColor.BoundValue = terminalControl.CursorColor
        )
      );

      callbackTokens.Add(
        TerminalControl.ScrollbackProperty,
        terminalControl.RegisterPropertyChangedCallback(
          TerminalControl.ScrollbackProperty,
          (_, _) => scrollback.BoundValue = terminalControl.Scrollback
        )
      );

      callbackTokens.Add(
        TerminalControl.LinesPerScrollbackProperty,
        terminalControl.RegisterPropertyChangedCallback(
          TerminalControl.LinesPerScrollbackProperty,
          (_, _) => linesPerScrollback.BoundValue = terminalControl.LinesPerScrollback
        )
      );

      callbackTokens.Add(
        TerminalControl.LinesPerSmallScrollbackProperty,
        terminalControl.RegisterPropertyChangedCallback(
          TerminalControl.LinesPerSmallScrollbackProperty,
          (_, _) => linesPerSmallScrollback.BoundValue = terminalControl.LinesPerSmallScrollback
        )
      );

      callbackTokens.Add(
        TerminalControl.LinesPerWheelScrollbackProperty,
        terminalControl.RegisterPropertyChangedCallback(
          TerminalControl.LinesPerWheelScrollbackProperty,
          (_, _) => linesPerWheelScrollback.BoundValue = terminalControl.LinesPerWheelScrollback
        )
      );

      callbackTokens.Add(
        TerminalControl.CopyOnMouseUpProperty,
        terminalControl.RegisterPropertyChangedCallback(
          TerminalControl.CopyOnMouseUpProperty,
          (_, _) => copyOnMouseUp.BoundValue = terminalControl.CopyOnMouseUp
        )
      );

      callbackTokens.Add(
        TerminalControl.PasteOnMiddleClickProperty,
        terminalControl.RegisterPropertyChangedCallback(
          TerminalControl.PasteOnMiddleClickProperty,
          (_, _) => pasteOnMiddleClick.BoundValue = terminalControl.PasteOnMiddleClick
        )
      );

      callbackTokens.Add(
        TerminalControl.PasteOnRightClickProperty,
        terminalControl.RegisterPropertyChangedCallback(
          TerminalControl.PasteOnRightClickProperty,
          (_, _) => pasteOnRightClick.BoundValue = terminalControl.PasteOnRightClick
        )
      );

      callbackTokens.Add(
        TerminalControl.CopyNewlineProperty,
        terminalControl.RegisterPropertyChangedCallback(
          TerminalControl.CopyNewlineProperty,
          (_, _) => {
            copyNewlineWindows.BoundValue = terminalControl.CopyNewline == windowsLineEnding;
            copyNewlineUnix.BoundValue = terminalControl.CopyNewline == unixLineEnding;
          }
        )
      );
    }
  }
}
