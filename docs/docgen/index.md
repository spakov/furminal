# API Documentation Index
This is the API documentation for **Furminal** and **Terminal**.

## Quick links
- [AnsiProcessor](xref:Spakov.AnsiProcessor)
- [ConPTY](xref:Spakov.ConPTY)
- [EmojiGenerator](xref:Spakov.EmojiGenerator)
- [Terminal](xref:Spakov.Terminal)
- [Furminal](xref:Spakov.Furminal)
- [WideCharacter](xref:Spakov.WideCharacter)

## Getting started with **TerminalControl**
[**TerminalControl**](xref:Spakov.Terminal.TerminalControl) is the WinUI 3 user control exposed by **Terminal**.

If you're not familiar with WinUI 3, see [Create your first WinUI3 (Windows App SDK) project](https://learn.microsoft.com/en-us/windows/apps/winui/winui3/create-your-first-winui3-app) for a solid introduction.

### Creating a project
This demo is a stripped-down version of [**Furminal**](xref:Spakov.Furminal), focusing on just the essential pieces you need to get started. Let's create a **WinUI Blank App (Packaged)** project in Visual Studio. We'll call it **TerminalDemo**. If we run the project at this point, we get a blank window that does nothing.

### Adding the Terminal project reference
We'll need to make sure we have a reference to **Terminal**—by far the easiest way to do this is to add a project reference in Visual Studio. Right click **TerminalDemo** and select **Add** > **Project Reference…**, then browse to `Terminal.csproj`.

**Terminal** appears in the **Solution Explorer**.

### Building `utf8proc.dll`
`utf8proc.dll` does not get built automatically since it uses Visual Studio's CMake infrastructure and there's no apparent way to automate CMake builds. Open the `utf8proc` directory in Visual Studio with **File** > **Open** > **Folder…**. The **CMake in Visual Studio** overview page should be displayed. Click **Open CMake Settings Editor** and click **Edit JSON** near the top. Copy and paste the contents of `utf8proc.CMakePresets.json` from the Furminal repository (link) and save the file. This creates the `arm64-Release` and `x64-Release` configurations. Build the project for the desired architectures. `utf8proc.dll` is produced in `out/build/$(Platform)-$(Configuration)/`.

At this point, we can close the `utf8proc` folder and reopen **TerminalDemo**.

### Referencing `utf8proc.dll`
`utf8proc.dll` is a native library and is not added as a reference in Visual Studio. Instead, it must be packaged into the packaged MSIX.

We'll need to ensure that `utf8proc.dll` is present in our package at runtime, since it's needed by **WideCharacter**, so let's modify our `TerminalDemo.vsproj`. The easiest way to edit the project configuration is to simply double click **TerminalDemo** in the **Solution Explorer**. Add this XML:

```xml
<PropertyGroup>
  <Utf8ProcDll>utf8proc.dll</Utf8ProcDll>
  <Utf8ProcNativeArch Condition="'$(Platform)'=='x64'">x64</Utf8ProcNativeArch>
  <Utf8ProcNativeArch Condition="'$(Platform)'=='ARM64'">arm64</Utf8ProcNativeArch>
  <Utf8ProcInputPath>$(ProjectDir)..\..\Furminal\utf8proc\out\build\$(Utf8ProcNativeArch)-Release\$(Utf8ProcDll)</Utf8ProcInputPath>
  <Utf8ProcOutputPath>$(ProjectDir)$(Utf8ProcDll)</Utf8ProcOutputPath>
</PropertyGroup>

<ItemGroup>
  <None Include="$(Utf8ProcInputPath)">
    <Link>$(Utf8ProcOutputPath)</Link>
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </None>
</ItemGroup>
```

### Adding a resource file
**Terminal** uses the **Windows.ApplicationModel.Resources** framework for localization, so we need to add a new resource file to our project. Create a **Strings** folder containing an **en-US** folder in the **Solution Explorer** under **TerminalDemo**, right click **en-US**, select **Add** > **New Item…**, select a **WinUI** **Resources File (.resw)**, and click **Add** to add `Resources.resw` to our project.

The resource file must have at least one entry for the ResourceMap to be built, so let's double click `Resources.resw` and add a string named `LaunchTerminal.Text` with value `Launch Terminal`. We'll use this later for the button that launches the terminal.

### Referencing the **Terminal** resource file
We'll need to edit our `TerminalDemo.vsproj` to reference Terminal's resource file so it gets packaged into the ResourceMap:

```xml
<ItemGroup>
  <PRIResource Include="..\..\Furminal\Terminal\Strings\**\*.resw" />
</ItemGroup>
```

### Adding NuGet packages
**TerminalControl** uses the **Microsoft.Graphics.Win2D** NuGet package. Because of the way its libraries are referenced at runtime, we must add this NuGet package to **TerminalDemo** to ensure the proper DLLs are included in our MSIX package.

I find the **CommunityToolkit.Mvvm** NuGet package to be helpful for minimizing some of the **INotifyPropertyChanged** repetition that's common in MVVM, so let's add a reference to that NuGet package.

### Creating the viewmodel
First, let's create a viewmodel to support a **TerminalControl**. The viewmodel does several important things:
- Initializes a new [Palette](xref:Spakov.AnsiProcessor.AnsiColors.Palette)
- Creates a [Pseudoconsole](xref:Spakov.ConPTY.Pseudoconsole)
- Sets up **Pseudoconsole** event handlers
- Starts the **Pseudoconsole**
- Exposes the **AnsiColors**, **ConsoleOutput**, **ConsoleInput**, **Rows**, and **Columns** properties to the view
- Exposes **PseudoconsoleDied** to the view, allowing some cleanup to happen in case the pseudoconsole dies

It's good practice to keep viewmodels in a separate namespace, so let's create a **ViewModels** folder in our project. We'll add a `TerminalViewModel.cs` inside that folder:

#### `TerminalViewModel.cs`
```cs
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Spakov.AnsiProcessor.AnsiColors;
using Spakov.ConPTY;
using System;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using TerminalDemo.Views;

namespace TerminalDemo.ViewModels {
  internal partial class TerminalViewModel : ObservableObject {
    private readonly TerminalView terminalView;

    private readonly DispatcherQueue dispatcherQueue;

    private readonly Pseudoconsole pseudoconsole;

    private Palette _palette;

    private FileStream? _consoleOutput;
    private FileStream? _consoleInput;
    private int _rows;
    private int _columns;

    /// <summary>
    /// Callback for handling the case in which the pseudoconsole dies.
    /// </summary>
    public delegate void OnPseudoconsoleDied(Exception e);

    /// <summary>
    /// Invoked if the pseudoconsole dies.
    /// </summary>
    public event OnPseudoconsoleDied? PseudoconsoleDied;

    /// <summary>
    /// The <see cref="Palette"/> used for ANSI colors.
    /// </summary>
    public Palette AnsiColors {
      get => _palette;
      set => SetProperty(ref _palette, value);
    }

    /// <summary>
    /// The console's output <see cref="FileStream"/>.
    /// </summary>
    public FileStream? ConsoleOutput {
      get => _consoleOutput;
      set => SetProperty(ref _consoleOutput, value);
    }

    /// <summary>
    /// The console's input <see cref="FileStream"/>.
    /// </summary>
    public FileStream? ConsoleInput {
      get => _consoleInput;
      set => SetProperty(ref _consoleInput, value);
    }

    /// <summary>
    /// The number of console rows.
    /// </summary>
    /// <remarks>It's important to make sure we invoke <see
    /// cref="ObservableObject.SetProperty"/> before we tell the pseudoconsole
    /// about the change to ensure scrollback is handled gracefully!</remarks>
    public int Rows {
      get => _rows;

      set {
        int oldRows = _rows;

        SetProperty(ref _rows, value);

        if (oldRows != value) {
          pseudoconsole.Rows = (uint) value;
        }
      }
    }

    /// <summary>
    /// The number of console columns.
    /// </summary>
    public int Columns {
      get => _columns;

      set {
        int oldColumns = _columns;

        SetProperty(ref _columns, value);

        if (oldColumns != value) {
          pseudoconsole.Columns = (uint) value;
        }
      }
    }

    /// <summary>
    /// Initializes a <see cref="TerminalViewModel"/>.
    /// </summary>
    /// <param name="terminalView">A <see cref="TerminalView"/>.</param>
    /// <param name="startDirectory">The directory in which to start the
    /// shell.</param>
    /// <param name="command">The command to execute in the
    /// pseudoconsole.</param>
    internal TerminalViewModel(TerminalView terminalView, string? startDirectory, string command) {
      this.terminalView = terminalView;

      dispatcherQueue = DispatcherQueue.GetForCurrentThread();

      _palette = new();

      _rows = 24;
      _columns = 80;

      if (startDirectory is not null) {
        string expandedStartDirectory = Environment.ExpandEnvironmentVariables(startDirectory);

        if (Directory.Exists(expandedStartDirectory) && Directory.GetCurrentDirectory() == Environment.SystemDirectory) {
          Directory.SetCurrentDirectory(expandedStartDirectory);
        }
      }

      pseudoconsole = new(command, (uint) _rows, (uint) _columns);
      pseudoconsole.Ready += Pseudoconsole_Ready;
      pseudoconsole.Done += Pseudoconsole_Done;

      StartPseudoconsole();
    }

    /// <summary>
    /// Starts the pseudoconsole, checking for error conditions.
    /// </summary>
    /// <exception cref="ArgumentException"></exception>
    private async void StartPseudoconsole() {
      try {
        await Task.Run(pseudoconsole.Start);
      } catch (Win32Exception e) {
        PseudoconsoleDied?.Invoke(new ArgumentException($"Failed to start pseudoconsole with command \"{pseudoconsole.Command}\".", e));
      }
    }

    /// <summary>
    /// Invoked when the pseudoconsole is ready.
    /// </summary>
    private void Pseudoconsole_Ready() {
      ConsoleOutput = pseudoconsole.ConsoleOutStream;
      ConsoleInput = pseudoconsole.ConsoleInStream;
    }

    /// <summary>
    /// Invoked when the pseudoconsole is being disposed.
    /// </summary>
    /// <param name="exitCode">The exit code of the command that
    /// executed.</param>
    private void Pseudoconsole_Done(uint exitCode) {
      if (exitCode == 0) {
        dispatcherQueue.TryEnqueue(Application.Current.Exit);
      } else {
        terminalView.Write($"{pseudoconsole.Command} exited with {exitCode}");
      }
    }
  }
}
```

### Creating the view
Next, we'll create a view that presents the **TerminalControl** to the user. Let's create a **Views** folder in our project and add a **WinUI** **Blank Window** called `TerminalView.xaml`:

#### `TerminalView.xaml`
```xml
<?xml version="1.0" encoding="utf-8"?>
<Window
  x:Class="TerminalDemo.Views.TerminalView"
  xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
  xmlns:local="using:TerminalDemo.Views"
  xmlns:terminal="using:Spakov.Terminal"
  xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
  xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
  mc:Ignorable="d"
  Title="TerminalDemo"
  Activated="Window_Activated">

  <Window.SystemBackdrop>
    <MicaBackdrop />
  </Window.SystemBackdrop>

  <Grid
    d:DataContext="{x:Bind ViewModel}">
    <Grid.RowDefinitions>
      <RowDefinition
        x:Name="AppTitleBarRow"
        Height="Auto"/>
      <RowDefinition
        x:Name="ContentRow"
        Height="*"/>
    </Grid.RowDefinitions>
    <Grid
      x:Name="AppTitleBarGrid"
      ColumnSpacing="8"
      Height="32">
      <Grid.ColumnDefinitions>
        <ColumnDefinition
          x:Name="LeftPaddingColumn"
          Width="Auto"/>
        <ColumnDefinition
          x:Name="IconColumn"
          Width="Auto"/>
        <ColumnDefinition
          x:Name="TitleColumn"
          Width="Auto"/>
        <ColumnDefinition
          x:Name="VisualBellColumn"
          Width="Auto"/>
        <ColumnDefinition
          x:Name="SpaceColumn"
          Width="*"/>
      </Grid.ColumnDefinitions>
      <Image
        x:Name="IconImage"
        Source="/Assets/StoreLogo.png"
        Width="16"
        Height="16"
        Grid.Column="1"/>
      <TextBlock
        x:Name="TitleTextBlock"
        Text="Furminal"
        VerticalAlignment="Center"
        Grid.Column="2"/>
      <FontIcon
        x:Name="VisualBellFontIcon"
        Glyph="&#xea8f;"
        Visibility="Collapsed"
        Grid.Column="3"/>
    </Grid>
    <terminal:TerminalControl
      x:Name="TerminalControl"
      DefaultWindowTitle="TerminalDemo"
      AnsiColors="{x:Bind ViewModel.AnsiColors}"
      ConsoleOutput="{x:Bind ViewModel.ConsoleOutput}"
      ConsoleInput="{x:Bind ViewModel.ConsoleInput}"
      Rows="{x:Bind ViewModel.Rows, Mode=TwoWay}"
      Columns="{x:Bind ViewModel.Columns, Mode=TwoWay}"
      Grid.Row="1"
      Grid.Column="0"/>
  </Grid>

</Window>
```

If you're unfamiliar with XAML at this point, each `.xaml` file has a C# code-behind file associated with it. Expand the arrow next to `TerminalView.xaml` in the **Solution Explorer** to see it.

#### `TerminalView.xaml.cs`
```cs
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Spakov.Terminal;
using System;
using System.Text;
using TerminalDemo.ViewModels;
using Windows.Foundation;

namespace TerminalDemo.Views {
  /// <summary>
  /// A demo <see cref="TerminalControl"/> terminal.
  /// </summary>
  public sealed partial class TerminalView : Window {
    private readonly DispatcherQueueTimer visualBellTimer;

    private TerminalViewModel? viewModel;

    /// <summary>
    /// The <see cref="TerminalViewModel"/>.
    /// </summary>
    private TerminalViewModel? ViewModel {
      get => viewModel;
      set => viewModel = value;
    }

    /// Initializes a <see cref="TerminalView"/>.
    /// </summary>
    /// <param name="startCommand">The command to run in the terminal.</param>
    public TerminalView(string startCommand) {
      visualBellTimer = DispatcherQueue.CreateTimer();
      visualBellTimer.Interval = TimeSpan.FromSeconds(1);
      visualBellTimer.Tick += VisualBellTimer_Tick;

      ViewModel = new(this, @"C:\", startCommand);
      ViewModel.PseudoconsoleDied += ViewModel_PseudoconsoleDied;
      InitializeComponent();

      ExtendsContentIntoTitleBar = true;

      TerminalControl.Focus(FocusState.Programmatic);
      TerminalControl.WindowTitleChanged += TerminalControl_WindowTitleChanged;
      TerminalControl.VisualBellRinging += TerminalControl_VisualBellRinging;
    }

    /// <summary>
    /// Writes <paramref name="message"/> to the terminal.
    /// </summary>
    /// <param name="message">The message to write.</param>
    internal void Write(string message) => TerminalControl.Write(message);

    /// <summary>
    /// Handles the case in which the <see cref="ViewModel"/>'s <see
    /// cref="ConPTY.Pseudoconsole"/> dies.
    /// </summary>
    private void ViewModel_PseudoconsoleDied(Exception e) {
      StringBuilder message = new();

      message.Append(e.Message);

      if (e.InnerException is not null) {
        message.Append("\r\n");
        message.Append(e.InnerException.Message);
      }

      TerminalControl.WriteError(message.ToString());
    }

    /// <summary>
    /// Invoked when the terminal's window title changes.
    /// </summary>
    private void TerminalControl_WindowTitleChanged() {
      TitleTextBlock.Text = TerminalControl.WindowTitle;
      Title = TerminalControl.WindowTitle;
    }

    /// <summary>
    /// Invoked when the terminal's visual bell is ringing.
    /// </summary>
    private void TerminalControl_VisualBellRinging() {
      VisualBellFontIcon.Visibility = Visibility.Visible;
      visualBellTimer.Start();
    }

    /// <summary>
    /// Invoked when the window is activated.
    /// </summary>
    /// <param name="sender"><inheritdoc
    /// cref="TypedEventHandler{TSender, TResult}"
    /// path="/param[@name='sender']"/></param>
    /// <param name="args"><inheritdoc
    /// cref="TypedEventHandler{TSender, TResult}"
    /// path="/param[@name='args']"/></param>
    private void Window_Activated(object sender, WindowActivatedEventArgs args) {
      TitleTextBlock.Foreground = args.WindowActivationState == WindowActivationState.Deactivated
        ? (SolidColorBrush) App.Current.Resources["WindowCaptionForegroundDisabled"]
        : (SolidColorBrush) App.Current.Resources["WindowCaptionForeground"];
    }

    /// <summary>
    /// Invoked when the <see cref="visualBellTimer"/> goes off.
    /// </summary>
    /// <remarks>Hides the visual bell.</remarks>
    /// <param name="sender"><inheritdoc
    /// cref="TypedEventHandler{TSender, TResult}"
    /// path="/param[@name='sender']"/></param>
    /// <param name="args"><inheritdoc
    /// cref="TypedEventHandler{TSender, TResult}"
    /// path="/param[@name='args']"/></param>
    private void VisualBellTimer_Tick(DispatcherQueueTimer sender, object args) {
      VisualBellFontIcon.Visibility = Visibility.Collapsed;
      TerminalControl.VisualBell = false;
    }
  }
}
```

At this point, our project file is in its final state and should contain a reference to `Views\TerminalView.xaml`:

#### The completed `TerminalDemo.vsproj`
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0-windows10.0.19041.0</TargetFramework>
    <TargetPlatformMinVersion>10.0.17763.0</TargetPlatformMinVersion>
    <RootNamespace>TerminalDemo</RootNamespace>
    <ApplicationManifest>app.manifest</ApplicationManifest>
    <Platforms>x86;x64;ARM64</Platforms>
    <RuntimeIdentifiers>win-x86;win-x64;win-arm64</RuntimeIdentifiers>
    <PublishProfile>win-$(Platform).pubxml</PublishProfile>
    <UseWinUI>true</UseWinUI>
    <EnableMsixTooling>true</EnableMsixTooling>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <PropertyGroup>
    <Utf8ProcDll>utf8proc.dll</Utf8ProcDll>
    <Utf8ProcNativeArch Condition="'$(Platform)'=='x64'">x64</Utf8ProcNativeArch>
    <Utf8ProcNativeArch Condition="'$(Platform)'=='ARM64'">arm64</Utf8ProcNativeArch>
    <Utf8ProcInputPath>$(ProjectDir)..\..\Furminal\utf8proc\out\build\$(Utf8ProcNativeArch)-Release\$(Utf8ProcDll)</Utf8ProcInputPath>
    <Utf8ProcOutputPath>$(ProjectDir)$(Utf8ProcDll)</Utf8ProcOutputPath>
  </PropertyGroup>

  <ItemGroup>
    <None Include="$(Utf8ProcInputPath)">
      <Link>$(Utf8ProcOutputPath)</Link>
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </None>
  </ItemGroup>

  <ItemGroup>
    <Content Include="Assets\SplashScreen.scale-200.png" />
    <Content Include="Assets\LockScreenLogo.scale-200.png" />
    <Content Include="Assets\Square150x150Logo.scale-200.png" />
    <Content Include="Assets\Square44x44Logo.scale-200.png" />
    <Content Include="Assets\Square44x44Logo.targetsize-24_altform-unplated.png" />
    <Content Include="Assets\StoreLogo.png" />
    <Content Include="Assets\Wide310x150Logo.scale-200.png" />
  </ItemGroup>

  <ItemGroup>
    <Manifest Include="$(ApplicationManifest)" />
  </ItemGroup>

  <!--
    Defining the "Msix" ProjectCapability here allows the Single-project MSIX Packaging
    Tools extension to be activated for this project even if the Windows App SDK Nuget
    package has not yet been restored.
  -->
  <ItemGroup Condition="'$(DisableMsixProjectCapabilityAddedByProject)'!='true' and '$(EnableMsixTooling)'=='true'">
    <ProjectCapability Include="Msix" />
  </ItemGroup>
  
  <ItemGroup>
    <PackageReference Include="CommunityToolkit.Mvvm" Version="8.4.0" />
    <PackageReference Include="Microsoft.Graphics.Win2D" Version="1.3.2" />
    <PackageReference Include="Microsoft.Windows.SDK.BuildTools" Version="10.0.26100.4654" />
    <PackageReference Include="Microsoft.WindowsAppSDK" Version="1.7.250606001" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\Furminal\Terminal\Terminal.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PRIResource Include="..\Furminal\Terminal\Strings\**\*.resw" />
  </ItemGroup>
  
  <ItemGroup>
    <Page Update="Views\TerminalView.xaml">
      <Generator>MSBuild:Compile</Generator>
    </Page>
  </ItemGroup>

  <!--
    Defining the "HasPackageAndPublishMenuAddedByProject" property here allows the Solution
    Explorer "Package and Publish" context menu entry to be enabled for this project even if
    the Windows App SDK Nuget package has not yet been restored.
  -->
  <PropertyGroup Condition="'$(DisableHasPackageAndPublishMenuAddedByProject)'!='true' and '$(EnableMsixTooling)'=='true'">
    <HasPackageAndPublishMenu>true</HasPackageAndPublishMenu>
  </PropertyGroup>

  <!-- Publish Properties -->
  <PropertyGroup>
    <PublishReadyToRun Condition="'$(Configuration)' == 'Debug'">False</PublishReadyToRun>
    <PublishReadyToRun Condition="'$(Configuration)' != 'Debug'">True</PublishReadyToRun>
    <PublishTrimmed Condition="'$(Configuration)' == 'Debug'">False</PublishTrimmed>
    <PublishTrimmed Condition="'$(Configuration)' != 'Debug'">True</PublishTrimmed>
  </PropertyGroup>
</Project>
```

### Opening the terminal window
If we run our project now, we'll see no change at all. We need to implement a way to open the terminal window, so let's modify `MainWindow.xaml` to add a button:

#### `MainWindow.xaml`
```xml
<?xml version="1.0" encoding="utf-8"?>
<Window
  x:Class="TerminalDemo.MainWindow"
  xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
  xmlns:local="using:TerminalDemo"
  xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
  xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
  mc:Ignorable="d"
  Title="TerminalDemo">

  <Window.SystemBackdrop>
      <MicaBackdrop />
  </Window.SystemBackdrop>

  <Grid>
    <Button
      x:Name="LaunchTerminal"
      Click="LaunchTerminal_Click">
      <TextBlock
        x:Uid="LaunchTerminal"/>
    </Button>
  </Grid>
</Window>
```

And, in its code-behind file:

#### `MainWindow.xaml.cs`
```cs
using Microsoft.UI.Xaml;
using TerminalDemo.Views;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace TerminalDemo {
  /// <summary>
  /// An empty window that can be used on its own or navigated to within a Frame.
  /// </summary>
  public sealed partial class MainWindow : Window {
    public MainWindow() {
      InitializeComponent();
    }

    /// <summary>
    /// Launches the terminal.
    /// </summary>
    /// <param name="sender"><inheritdoc cref="RoutedEventHandler"
    /// path="/param[@name='sender']"/></param>
    /// <param name="e"><inheritdoc cref="RoutedEventHandler"
    /// path="/param[@name='e']"/></param>
    private void LaunchTerminal_Click(object sender, RoutedEventArgs e) {
      TerminalView terminalView = new("powershell");
      terminalView.Activate();
    }
  }
}
```

### Running the project
We should be ready to use the terminal. Run the project. We'll see our blank window with a **Launch Terminal** button. Click it, and we see our terminal displayed in a new window!

## Next steps
There are at least two major things you may want to do:
1. Allow the user to interact with the terminal configuration (though the settings window should work fine at this point), and
2. Improve the visuals and user experience

Take a look at the source code of Furminal (link) and TermBar (link) for two different approaches to both.
