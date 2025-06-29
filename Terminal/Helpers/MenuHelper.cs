using Microsoft.UI.Xaml.Controls;
using System;

namespace Terminal.Helpers {
  /// <summary>
  /// Methods to initialize and modify the context menu and menu items.
  /// </summary>
  internal static class MenuHelper {
    /// <summary>
    /// Initializes the terminal context menu.
    /// </summary>
    /// <param name="terminalControl">A <see cref="TerminalControl"/>.</param>
    internal static void InitializeContextMenu(TerminalControl terminalControl) {
      if (terminalControl.UseContextMenu) {
        terminalControl.ContextMenu = new();

        terminalControl.CopyMenuItem = (MenuFlyoutItem) InitializeMenuItem(
          terminalControl,
          nameof(terminalControl.CopyMenuItem)
        );

        terminalControl.PasteMenuItem = (MenuFlyoutItem) InitializeMenuItem(
          terminalControl,
          nameof(terminalControl.PasteMenuItem)
        );

        terminalControl.ContextMenu.Items.Add(terminalControl.PasteMenuItem);

        if (terminalControl.UseExtendedContextMenu) {
          terminalControl.SmallerTextMenuItem = (MenuFlyoutItem) InitializeMenuItem(
            terminalControl,
            nameof(terminalControl.SmallerTextMenuItem)
          );

          terminalControl.LargerTextMenuItem = (MenuFlyoutItem) InitializeMenuItem(
            terminalControl,
            nameof(terminalControl.LargerTextMenuItem)
          );

          terminalControl.BackgroundIsInvisibleMenuItem = (ToggleMenuFlyoutItem) InitializeMenuItem(
            terminalControl,
            nameof(terminalControl.BackgroundIsInvisibleMenuItem)
          );

          terminalControl.UseVisualBellMenuItem = (ToggleMenuFlyoutItem) InitializeMenuItem(
            terminalControl,
            nameof(terminalControl.UseVisualBellMenuItem)
          );

          terminalControl.CopyOnMouseUpMenuItem = (ToggleMenuFlyoutItem) InitializeMenuItem(
            terminalControl,
            nameof(terminalControl.CopyOnMouseUpMenuItem)
          );

          terminalControl.PasteOnRightClickMenuItem = (ToggleMenuFlyoutItem) InitializeMenuItem(
            terminalControl,
            nameof(terminalControl.PasteOnRightClickMenuItem)
          );

          terminalControl.PasteOnMiddleClickMenuItem = (ToggleMenuFlyoutItem) InitializeMenuItem(
            terminalControl,
            nameof(terminalControl.PasteOnMiddleClickMenuItem)
          );

          terminalControl.CursorMenuItem = (MenuFlyoutSubItem) InitializeMenuItem(
            terminalControl,
            nameof(terminalControl.CursorMenuItem)
          );

          terminalControl.BlockCursorMenuItem = (ToggleMenuFlyoutItem) InitializeMenuItem(
            terminalControl,
            nameof(terminalControl.BlockCursorMenuItem)
          );

          terminalControl.UnderlineCursorMenuItem = (ToggleMenuFlyoutItem) InitializeMenuItem(
            terminalControl,
            nameof(terminalControl.UnderlineCursorMenuItem)
          );

          terminalControl.BarCursorMenuItem = (ToggleMenuFlyoutItem) InitializeMenuItem(
            terminalControl,
            nameof(terminalControl.BarCursorMenuItem)
          );

          terminalControl.CursorBlinkMenuItem = (ToggleMenuFlyoutItem) InitializeMenuItem(
            terminalControl,
            nameof(terminalControl.CursorBlinkMenuItem)
          );

          terminalControl.ContextMenu.Items.Add(new MenuFlyoutSeparator());
          terminalControl.ContextMenu.Items.Add(terminalControl.SmallerTextMenuItem);
          terminalControl.ContextMenu.Items.Add(terminalControl.LargerTextMenuItem);
          terminalControl.ContextMenu.Items.Add(new MenuFlyoutSeparator());
          terminalControl.ContextMenu.Items.Add(terminalControl.BackgroundIsInvisibleMenuItem);
          terminalControl.ContextMenu.Items.Add(terminalControl.UseVisualBellMenuItem);
          terminalControl.ContextMenu.Items.Add(new MenuFlyoutSeparator());
          terminalControl.ContextMenu.Items.Add(terminalControl.CopyOnMouseUpMenuItem);
          terminalControl.ContextMenu.Items.Add(terminalControl.PasteOnRightClickMenuItem);
          terminalControl.ContextMenu.Items.Add(terminalControl.PasteOnMiddleClickMenuItem);
          terminalControl.ContextMenu.Items.Add(new MenuFlyoutSeparator());
          terminalControl.ContextMenu.Items.Add(terminalControl.CursorMenuItem);
          terminalControl.CursorMenuItem.Items.Add(terminalControl.BlockCursorMenuItem);
          terminalControl.CursorMenuItem.Items.Add(terminalControl.UnderlineCursorMenuItem);
          terminalControl.CursorMenuItem.Items.Add(terminalControl.BarCursorMenuItem);
          terminalControl.CursorMenuItem.Items.Add(new MenuFlyoutSeparator());
          terminalControl.CursorMenuItem.Items.Add(terminalControl.CursorBlinkMenuItem);
        }

        terminalControl.SettingsMenuItem = (MenuFlyoutItem) InitializeMenuItem(
          terminalControl,
          nameof(terminalControl.SettingsMenuItem)
        );

        terminalControl.ContextMenu.Items.Add(new MenuFlyoutSeparator());
        terminalControl.ContextMenu.Items.Add(terminalControl.SettingsMenuItem);
      } else {
        if (
          terminalControl.ContextMenu is not null
          && terminalControl.ContextMenu.Items.Contains(terminalControl.SettingsMenuItem)
        ) {
          if (terminalControl.SettingsMenuItem is not null) {
            terminalControl.SettingsMenuItem.Click -= terminalControl.SettingsMenuItem_Click;
          }

          terminalControl.ContextMenu.Items.Remove(terminalControl.SettingsMenuItem);
        }

        if (
          terminalControl.CursorMenuItem is not null
          && terminalControl.CursorMenuItem.Items.Contains(terminalControl.CursorBlinkMenuItem)
        ) {
          if (terminalControl.CursorBlinkMenuItem is not null) {
            terminalControl.CursorBlinkMenuItem.Click -= terminalControl.CursorBlinkMenuItem_Click;
          }

          terminalControl.CursorMenuItem.Items.Remove(terminalControl.CursorBlinkMenuItem);
        }

        if (
          terminalControl.CursorMenuItem is not null
          && terminalControl.CursorMenuItem.Items.Contains(terminalControl.BlockCursorMenuItem)
        ) {
          if (terminalControl.BlockCursorMenuItem is not null) {
            terminalControl.BlockCursorMenuItem.Click -= terminalControl.BlockCursorMenuItem_Click;
          }

          terminalControl.CursorMenuItem.Items.Remove(terminalControl.BlockCursorMenuItem);
        }

        if (
          terminalControl.CursorMenuItem is not null
          && terminalControl.CursorMenuItem.Items.Contains(terminalControl.UnderlineCursorMenuItem)
        ) {
          if (terminalControl.UnderlineCursorMenuItem is not null) {
            terminalControl.UnderlineCursorMenuItem.Click -= terminalControl.UnderlineCursorMenuItem_Click;
          }

          terminalControl.CursorMenuItem.Items.Remove(terminalControl.UnderlineCursorMenuItem);
        }

        if (
          terminalControl.CursorMenuItem is not null
          && terminalControl.CursorMenuItem.Items.Contains(terminalControl.BarCursorMenuItem)
        ) {
          if (terminalControl.BarCursorMenuItem is not null) {
            terminalControl.BarCursorMenuItem.Click -= terminalControl.BarCursorMenuItem_Click;
          }

          terminalControl.CursorMenuItem.Items.Remove(terminalControl.BarCursorMenuItem);
        }

        if (
          terminalControl.ContextMenu is not null
          && terminalControl.ContextMenu.Items.Contains(terminalControl.CursorMenuItem)
        ) {
          terminalControl.ContextMenu.Items.Remove(terminalControl.CursorMenuItem);
        }

        if (
          terminalControl.ContextMenu is not null
          && terminalControl.ContextMenu.Items.Contains(terminalControl.PasteOnMiddleClickMenuItem)
        ) {
          if (terminalControl.PasteOnMiddleClickMenuItem is not null) {
            terminalControl.PasteOnMiddleClickMenuItem.Click -= terminalControl.PasteOnMiddleClickMenuItem_Click;
          }

          terminalControl.ContextMenu.Items.Remove(terminalControl.PasteOnMiddleClickMenuItem);
        }

        if (
          terminalControl.ContextMenu is not null
          && terminalControl.ContextMenu.Items.Contains(terminalControl.PasteOnRightClickMenuItem)
        ) {
          if (terminalControl.PasteOnRightClickMenuItem is not null) {
            terminalControl.PasteOnRightClickMenuItem.Click -= terminalControl.PasteOnRightClickMenuItem_Click;
          }

          terminalControl.ContextMenu.Items.Remove(terminalControl.PasteOnRightClickMenuItem);
        }

        if (
          terminalControl.ContextMenu is not null
          && terminalControl.ContextMenu.Items.Contains(terminalControl.CopyOnMouseUpMenuItem)
        ) {
          if (terminalControl.CopyOnMouseUpMenuItem is not null) {
            terminalControl.CopyOnMouseUpMenuItem.Click -= terminalControl.CopyOnMouseUpMenuItem_Click;
          }

          terminalControl.ContextMenu.Items.Remove(terminalControl.CopyOnMouseUpMenuItem);
        }

        if (
          terminalControl.ContextMenu is not null
          && terminalControl.ContextMenu.Items.Contains(terminalControl.UseVisualBellMenuItem)
        ) {
          if (terminalControl.UseVisualBellMenuItem is not null) {
            terminalControl.UseVisualBellMenuItem.Click -= terminalControl.UseVisualBellMenuItem_Click;
          }

          terminalControl.ContextMenu.Items.Remove(terminalControl.UseVisualBellMenuItem);
        }

        if (
          terminalControl.ContextMenu is not null
          && terminalControl.ContextMenu.Items.Contains(terminalControl.BackgroundIsInvisibleMenuItem)
        ) {
          if (terminalControl.BackgroundIsInvisibleMenuItem is not null) {
            terminalControl.BackgroundIsInvisibleMenuItem.Click -= terminalControl.BackgroundIsInvisibleMenuItem_Click;
          }

          terminalControl.ContextMenu.Items.Remove(terminalControl.BackgroundIsInvisibleMenuItem);
        }

        if (
          terminalControl.ContextMenu is not null
          && terminalControl.ContextMenu.Items.Contains(terminalControl.LargerTextMenuItem)
        ) {
          if (terminalControl.LargerTextMenuItem is not null) {
            terminalControl.LargerTextMenuItem.Click -= terminalControl.LargerTextMenuItem_Click;
          }

          terminalControl.ContextMenu.Items.Remove(terminalControl.LargerTextMenuItem);
        }

        if (
          terminalControl.ContextMenu is not null
          && terminalControl.ContextMenu.Items.Contains(terminalControl.SmallerTextMenuItem)
        ) {
          if (terminalControl.SmallerTextMenuItem is not null) {
            terminalControl.SmallerTextMenuItem.Click -= terminalControl.SmallerTextMenuItem_Click;
          }

          terminalControl.ContextMenu.Items.Remove(terminalControl.SmallerTextMenuItem);
        }

        if (
          terminalControl.ContextMenu is not null
          && terminalControl.ContextMenu.Items.Contains(terminalControl.PasteMenuItem)
        ) {
          if (terminalControl.PasteMenuItem is not null) {
            terminalControl.PasteMenuItem.Click -= terminalControl.PasteMenuItem_Click;
          }

          terminalControl.ContextMenu.Items.Remove(terminalControl.PasteMenuItem);
        }

        if (
          terminalControl.ContextMenu is not null
          && terminalControl.ContextMenu.Items.Contains(terminalControl.CopyMenuItem)
        ) {
          if (terminalControl.CopyMenuItem is not null) {
            terminalControl.CopyMenuItem.Click -= terminalControl.CopyMenuItem_Click;
          }

          terminalControl.ContextMenu.Items.Remove(terminalControl.CopyMenuItem);
        }

        terminalControl.SettingsMenuItem = null;
        terminalControl.CursorBlinkMenuItem = null;
        terminalControl.BarCursorMenuItem = null;
        terminalControl.UnderlineCursorMenuItem = null;
        terminalControl.BlockCursorMenuItem = null;
        terminalControl.CursorMenuItem = null;
        terminalControl.PasteOnMiddleClickMenuItem = null;
        terminalControl.PasteOnRightClickMenuItem = null;
        terminalControl.CopyOnMouseUpMenuItem = null;
        terminalControl.UseVisualBellMenuItem = null;
        terminalControl.BackgroundIsInvisibleMenuItem = null;
        terminalControl.LargerTextMenuItem = null;
        terminalControl.SmallerTextMenuItem = null;
        terminalControl.PasteMenuItem = null;
        terminalControl.CopyMenuItem = null;
        terminalControl.ContextMenu = null;
      }
    }

    /// <summary>
    /// Returns a <see cref="MenuFlyoutItemBase"/> corresponding to <paramref
    /// name="menuItemName"/>.
    /// </summary>
    /// <param name="terminalControl">A <see cref="TerminalControl"/>.</param>
    /// <param name="menuItemName">The name of a menu item.</param>
    /// <returns>An object that is a subclass of <see
    /// cref="MenuFlyoutItemBase"/>.</returns>
    /// <exception cref="ArgumentException"><paramref name="menuItemName"/>
    /// referred to an unknown menu item.</exception>
    internal static MenuFlyoutItemBase InitializeMenuItem(TerminalControl terminalControl, string menuItemName) {
      MenuFlyoutItemBase menuItem;

      // Initialize
      menuItem = menuItemName switch {
        "CopyMenuItem" => new MenuFlyoutItem() {
          Icon = new FontIcon() { Glyph = "\xe8c8" },
          Text = terminalControl.ResourceLoader.GetString("CopyName")
        },

        "PasteMenuItem" => new MenuFlyoutItem() {
          Icon = new FontIcon() { Glyph = "\xe77f" },
          Text = terminalControl.ResourceLoader.GetString("PasteName")
        },

        "SmallerTextMenuItem" => new MenuFlyoutItem() {
          Icon = new FontIcon() { Glyph = "\xe8e7" },
          Text = terminalControl.ResourceLoader.GetString("SmallerTextName")
        },

        "LargerTextMenuItem" => new MenuFlyoutItem() {
          Icon = new FontIcon() { Glyph = "\xe8e8" },
          Text = terminalControl.ResourceLoader.GetString("LargerTextName")
        },

        "BackgroundIsInvisibleMenuItem" => new ToggleMenuFlyoutItem() {
          Icon = new FontIcon() { Glyph = "\xea61" },
          Text = terminalControl.ResourceLoader.GetString("BackgroundIsInvisibleName"),
          IsChecked = terminalControl.BackgroundIsInvisible
        },

        "UseVisualBellMenuItem" => new ToggleMenuFlyoutItem() {
          Icon = new FontIcon() { Glyph = "\xea8f" },
          Text = terminalControl.ResourceLoader.GetString("UseVisualBellName"),
          IsChecked = terminalControl.UseVisualBell
        },

        "CopyOnMouseUpMenuItem" => new ToggleMenuFlyoutItem() {
          Icon = new FontIcon() { Glyph = "\xf683" },
          Text = terminalControl.ResourceLoader.GetString("CopyOnMouseUpName"),
          IsChecked = terminalControl.CopyOnMouseUp
        },

        "PasteOnRightClickMenuItem" => new ToggleMenuFlyoutItem() {
          Icon = new FontIcon() { Glyph = "\xf148" },
          Text = terminalControl.ResourceLoader.GetString("PasteOnRightClickName"),
          IsChecked = terminalControl.PasteOnRightClick
        },

        "PasteOnMiddleClickMenuItem" => new ToggleMenuFlyoutItem() {
          Icon = new FontIcon() { Glyph = "\xf147" },
          Text = terminalControl.ResourceLoader.GetString("PasteOnMiddleClickName"),
          IsChecked = terminalControl.PasteOnMiddleClick
        },

        "CursorMenuItem" => new MenuFlyoutSubItem() {
          Icon = new FontIcon() { Glyph = "\xe8ac" },
          Text = terminalControl.ResourceLoader.GetString("CursorName")
        },

        "BlockCursorMenuItem" => new ToggleMenuFlyoutItem() {
          Icon = new FontIcon() { Glyph = "\xe8a8" },
          Text = terminalControl.ResourceLoader.GetString("CursorStyleBlockName"),
          IsChecked = terminalControl.CursorStyle == CursorStyles.Block
        },

        "UnderlineCursorMenuItem" => new ToggleMenuFlyoutItem() {
          Icon = new FontIcon() { Glyph = "\xe90e" },
          Text = terminalControl.ResourceLoader.GetString("CursorStyleUnderlineName"),
          IsChecked = terminalControl.CursorStyle == CursorStyles.Underline
        },

        "BarCursorMenuItem" => new ToggleMenuFlyoutItem() {
          Icon = new FontIcon() { Glyph = "\xe90c" },
          Text = terminalControl.ResourceLoader.GetString("CursorStyleBarName"),
          IsChecked = terminalControl.CursorStyle == CursorStyles.Bar
        },

        "CursorBlinkMenuItem" => new ToggleMenuFlyoutItem() {
          Icon = new FontIcon() { Glyph = "\xe9a9" },
          Text = terminalControl.ResourceLoader.GetString("CursorBlinkName"),
          IsChecked = terminalControl.CursorBlink
        },

        "SettingsMenuItem" => new MenuFlyoutItem() {
          Icon = new FontIcon() { Glyph = "\xe713" },
          Text = terminalControl.ResourceLoader.GetString("SettingsWindowTitle")
        },

        _ => throw new ArgumentException($"Unknown menu item \"{menuItemName}\".", nameof(menuItemName)),
      };

      // Attach a click handler
      switch (menuItemName) {
        case "CopyMenuItem":
          ((MenuFlyoutItem) menuItem).Click += terminalControl.CopyMenuItem_Click;

          break;

        case "PasteMenuItem":
          ((MenuFlyoutItem) menuItem).Click += terminalControl.PasteMenuItem_Click;

          break;

        case "SmallerTextMenuItem":
          ((MenuFlyoutItem) menuItem).Click += terminalControl.SmallerTextMenuItem_Click;

          break;

        case "LargerTextMenuItem":
          ((MenuFlyoutItem) menuItem).Click += terminalControl.LargerTextMenuItem_Click;

          break;

        case "BackgroundIsInvisibleMenuItem":
          ((ToggleMenuFlyoutItem) menuItem).Click += terminalControl.BackgroundIsInvisibleMenuItem_Click;

          break;

        case "UseVisualBellMenuItem":
          ((ToggleMenuFlyoutItem) menuItem).Click += terminalControl.UseVisualBellMenuItem_Click;

          break;

        case "CopyOnMouseUpMenuItem":
          ((ToggleMenuFlyoutItem) menuItem).Click += terminalControl.CopyOnMouseUpMenuItem_Click;

          break;

        case "PasteOnRightClickMenuItem":
          ((ToggleMenuFlyoutItem) menuItem).Click += terminalControl.PasteOnRightClickMenuItem_Click;

          break;

        case "PasteOnMiddleClickMenuItem":
          ((ToggleMenuFlyoutItem) menuItem).Click += terminalControl.PasteOnMiddleClickMenuItem_Click;

          break;

        case "BlockCursorMenuItem":
          ((ToggleMenuFlyoutItem) menuItem).Click += terminalControl.BlockCursorMenuItem_Click;

          break;

        case "UnderlineCursorMenuItem":
          ((ToggleMenuFlyoutItem) menuItem).Click += terminalControl.UnderlineCursorMenuItem_Click;

          break;

        case "BarCursorMenuItem":
          ((ToggleMenuFlyoutItem) menuItem).Click += terminalControl.BarCursorMenuItem_Click;

          break;

        case "CursorBlinkMenuItem":
          ((ToggleMenuFlyoutItem) menuItem).Click += terminalControl.CursorBlinkMenuItem_Click;

          break;

        case "SettingsMenuItem":
          ((MenuFlyoutItem) menuItem).Click += terminalControl.SettingsMenuItem_Click;

          break;
      }

      return menuItem;
    }
  }
}
