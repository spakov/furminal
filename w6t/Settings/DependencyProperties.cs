using Microsoft.UI.Xaml;
using Spakov.Terminal;
using System;
using Windows.UI;

namespace Spakov.W6t.Settings {
  /// <summary>
  /// A wrapper for <see cref="DependencyProperty"/>s, since <see
  /// href="https://github.com/microsoft/microsoft-ui-xaml/issues/7305">Window
  /// is not a DependencyObject</see>.
  /// </summary>
  /// <param name="terminal">A <see cref="Views.Terminal"/>.</param>
  public class DependencyProperties(Views.Terminal terminal) : DependencyObject {
    private readonly Views.Terminal _terminal = terminal;

    /// <summary>
    /// <inheritdoc cref="CommandProperty"/>
    /// </summary>
    public string Command {
      get => (string) GetValue(CommandProperty);
      set => SetValue(CommandProperty, value);
    }

    /// <summary>
    /// The command to run in the terminal.
    /// </summary>
    /// <remarks>This is typically a shell.</remarks>
    public static readonly DependencyProperty CommandProperty = DependencyProperty.Register(
      nameof(Command),
      typeof(string),
      typeof(TerminalControl),
      new PropertyMetadata("powershell")
    );

    /// <summary>
    /// <inheritdoc cref="StartDirectoryProperty"/>
    /// </summary>
    public string? StartDirectory {
      get => (string?) GetValue(StartDirectoryProperty);
      set => SetValue(StartDirectoryProperty, value);
    }

    /// <summary>
    /// The start working directory, to be used if the inherited current
    /// working directory is the system directory.
    /// </summary>
    /// <remarks>Set to <see langword="null"/> to inherit the current working
    /// directory from the launching process.</remarks>
    public static readonly DependencyProperty StartDirectoryProperty = DependencyProperty.Register(
      nameof(StartDirectory),
      typeof(string),
      typeof(TerminalControl),
      new PropertyMetadata("%USERPROFILE%")
    );

    /// <summary>
    /// <inheritdoc cref="VisualBellDisplayTimeProperty"/>
    /// </summary>
    public int VisualBellDisplayTime {
      get => (int) GetValue(VisualBellDisplayTimeProperty);
      set => SetValue(VisualBellDisplayTimeProperty, value);
    }

    /// <summary>
    /// The visual bell display time, in seconds.
    /// </summary>
    public static readonly DependencyProperty VisualBellDisplayTimeProperty = DependencyProperty.Register(
      nameof(VisualBellDisplayTime),
      typeof(int),
      typeof(TerminalControl),
      new PropertyMetadata(1, OnVisualBellDisplayTimeChanged)
    );

    /// <summary>
    /// Invoked when the visual bell display time changes.
    /// </summary>
    /// <param name="d"><inheritdoc
    /// cref="DependencyPropertyChangedEventHandler"
    /// path="/param[@name='sender']"/></param>
    /// <param name="e"><inheritdoc
    /// cref="DependencyPropertyChangedEventHandler"
    /// path="/param[@name='e']"/></param>
    private static void OnVisualBellDisplayTimeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
      DependencyProperties dependencyProperties = (DependencyProperties) d;

      dependencyProperties.Terminal.UpdateVisualBellTimerInterval();
    }

    /// <summary>
    /// <inheritdoc cref="WindowBackdropProperty"/>
    /// </summary>
    public WindowBackdrops WindowBackdrop {
      get => (WindowBackdrops) GetValue(WindowBackdropProperty);
      set => SetValue(WindowBackdropProperty, value);
    }

    /// <summary>
    /// The terminal default window title.
    /// </summary>
    public static readonly DependencyProperty WindowBackdropProperty = DependencyProperty.Register(
      nameof(WindowBackdrop),
      typeof(WindowBackdrops),
      typeof(TerminalControl),
      new PropertyMetadata(WindowBackdrops.Mica, OnWindowBackdropChanged)
    );

    /// <summary>
    /// Invoked when the window backdrop changes.
    /// </summary>
    /// <param name="d"><inheritdoc
    /// cref="DependencyPropertyChangedEventHandler"
    /// path="/param[@name='sender']"/></param>
    /// <param name="e"><inheritdoc
    /// cref="DependencyPropertyChangedEventHandler"
    /// path="/param[@name='e']"/></param>
    /// <exception cref="InvalidOperationException"></exception>
    private static void OnWindowBackdropChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
      DependencyProperties dependencyProperties = (DependencyProperties) d;

      // Sometimes we get a transient state
      if (dependencyProperties.WindowBackdrop == (WindowBackdrops) (-1)) {
        return;
      }

      dependencyProperties.Terminal.SystemBackdrop = dependencyProperties.WindowBackdrop switch {
        WindowBackdrops.DefaultBackgroundColor => new SolidColorBackdrop(dependencyProperties.SolidColorWindowBackdropColor),
        WindowBackdrops.Mica => Views.Terminal.MicaWindowBackdrop,
        WindowBackdrops.Acrylic => Views.Terminal.AcrylicWindowBackdrop,
        WindowBackdrops.Blurred => Views.Terminal.BlurredWindowBackdrop,
        WindowBackdrops.Transparent => Views.Terminal.TransparentWindowBackdrop,
        WindowBackdrops.SolidColor => new SolidColorBackdrop(dependencyProperties.SolidColorWindowBackdropColor),
        _ => throw new InvalidOperationException($"Invalid WindowBackdrops {dependencyProperties.WindowBackdrop}.")
      };
    }

    /// <summary>
    /// <inheritdoc cref="SolidColorWindowBackdropColorProperty"/>
    /// </summary>
    public Color SolidColorWindowBackdropColor {
      get => (Color) GetValue(SolidColorWindowBackdropColorProperty);
      set => SetValue(SolidColorWindowBackdropColorProperty, value);
    }

    /// <summary>
    /// The solid color window backdrop color.
    /// </summary>
    public static readonly DependencyProperty SolidColorWindowBackdropColorProperty = DependencyProperty.Register(
      nameof(SolidColorWindowBackdropColor),
      typeof(Color),
      typeof(TerminalControl),
      new PropertyMetadata(new Color() { A = 0xff, R = 0x00, G = 0x00, B = 0x00 }, OnWindowBackdropChanged)
    );

    /// <summary>
    /// The <see cref="Views.Terminal"/> associated with this <see
    /// cref="DependencyProperties"/>.
    /// </summary>
    internal Views.Terminal Terminal => _terminal;
  }
}
