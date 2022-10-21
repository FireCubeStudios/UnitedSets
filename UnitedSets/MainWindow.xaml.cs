﻿using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;
using System.Linq;
using WinRT.Interop;
using WinUIEx;
using Microsoft.UI.Xaml;
using Windows.UI.ViewManagement;
using Windows.UI;
using Microsoft.UI;
using Windows.ApplicationModel.DataTransfer;
using System.Numerics;
using Microsoft.UI.Xaml.Media;
using System;
using WindowEx = WinWrapper.Window;
using UnitedSets.Classes;
using UnitedSets.Interfaces;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.System;
using Windows.Win32;
using Windows.Win32.UI.WindowsAndMessaging;
using UnitedSets.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Media.Imaging;

namespace UnitedSets;

/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class MainWindow
{
    public SettingsService Settings = App.Current.Services.GetService<SettingsService>();
    public ObservableCollection<HwndHostTab> Tabs { get; } = new();
    readonly WindowEx WindowEx;
    public MainWindow()
    {
        Title = "UnitedSets";
        InitializeComponent();
        WindowEx = WindowEx.FromWindowHandle(WindowNative.GetWindowHandle(this));
      /*  var paint = new HwndHostTab(this, WindowEx.GetAllWindows().First(x => x.TitleText.Contains("Paint")).Root);
        paint.Icon = new BitmapImage(new Uri("https://media.discordapp.net/attachments/757560235144642577/1030621242975342612/unknown.png"));
        Tabs.Add(paint);
        paint.Closed += () => Tabs.Remove(paint);
        var vscode = new HwndHostTab(this, WindowEx.GetAllWindows().First(x => x.TitleText.Contains("Notepad")).Root);
        vscode.Icon = new BitmapImage(new Uri("https://media.discordapp.net/attachments/757560235144642577/1030621196972216421/unknown.png"));
        Tabs.Add(vscode);
        vscode.Closed += () => Tabs.Remove(vscode);*/
        AppWindow.Closing += async (o, e) =>
        {
            e.Cancel = true;
            Dialog.XamlRoot = Content.XamlRoot;
            var item = TabView.SelectedItem;
            TabView.SelectedIndex = -1;
            TabView.Visibility = Visibility.Collapsed;
            WindowEx.Focus();
            ContentDialogResult result;
            try
            {
                result = await Dialog.ShowAsync();
            } catch
            {
                result = ContentDialogResult.None;
            }
            switch (result)
            {
                case ContentDialogResult.Primary:
                    // Release all windows
                    while (Tabs.Count > 0)
                    {
                        var Tab = Tabs[0];
                        Tabs.RemoveAt(0);
                        Tab.DetachAndDispose();
                    }
                    Environment.Exit(0);
                    return;
                case ContentDialogResult.Secondary:
                    // Close all windows
                    TabView.Visibility = Visibility.Visible;
                    await Task.Delay(100);
                    foreach (var Tab in Tabs.ToArray()) // ToArray = Clone Collection
                    {
                        try
                        {
                            _ = Tab.TryCloseAsync();
                            // Try closing tab in 3 second, otherwise give up
                            for (int i = 0; i < 30; i++)
                            {
                                await Task.Delay(100);
                                if (Tab.Window.IsValid) continue;
                            }
                            if (Tab.Window.IsValid) break;
                        }
                        catch
                        {
                            Tab.DetachAndDispose();
                        }
                    }
                    if (Tabs.Count == 0)
                    {
                        Environment.Exit(0);
                        return;
                    }
                    goto default;
                default:
                    // Do not close window
                    try
                    {
                        TabView.SelectedItem = item;
                    } catch
                    {
                        if (Tabs.Count > 0)
                            TabView.SelectedIndex = 0;
                    }
                    TabView.Visibility = Visibility.Visible;
                    break;
            }
        };
        Activated += FirstRun;
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(CustomDragRegion);
    }

    private void FirstRun(object sender, WindowActivatedEventArgs args)
    {
        Activated -= FirstRun;
        var icon = PInvoke.LoadImage(
            hInst: null,
            name: $@"{Package.Current.InstalledLocation.Path}\Assets\UnitedSets.ico",
            type: GDI_IMAGE_TYPE.IMAGE_ICON,
        cx: 0,
        cy: 0,
            fuLoad: IMAGE_FLAGS.LR_LOADFROMFILE | IMAGE_FLAGS.LR_DEFAULTSIZE | IMAGE_FLAGS.LR_SHARED
        );
        bool success = false;
        icon.DangerousAddRef(ref success);
        PInvoke.SendMessage(WindowEx.Handle, PInvoke.WM_SETICON, 1, icon.DangerousGetHandle());
        PInvoke.SendMessage(WindowEx.Handle, PInvoke.WM_SETICON, 0, icon.DangerousGetHandle());
    }

    ContentDialog Dialog = new()
    {
        Title = "Closing UnitedSets",
        Content = "How do you want to close the app?",
        PrimaryButtonText = "Release all Windows",
        SecondaryButtonText = "Close all Windows",
        CloseButtonText = "Cancel"
    };
    readonly AddTabFlyout AddTabFlyout = new();
    private async void AddTab(object sender, RoutedEventArgs e)
    {
        WindowEx.Minimize();
        //this.Hide();
        await AddTabFlyout.ShowAtCursorAsync();
        //this.Show();
        WindowEx.Restore();
        var result = AddTabFlyout.Result;
        if (!result.IsValid) 
            return;
        result = result.Root;
        if (result.Handle == IntPtr.Zero) 
            return;
        if (result.Handle == AddTabFlyout.GetWindowHandle()) 
            return;
        if (result.Handle == WindowEx.Handle) 
            return;
        if (result.ClassName is
            "Shell_TrayWnd" // Taskbar
            or "Chrome_WidgetWin_1" // Desktop
            or "Progman" // Desktop
            or "WindowsDashboard" // I forget
            or "Windows.UI.Core.CoreWindow" // Quick Settings and Notification Center (other uwp apps should already be ApplicationFrameHost)
            )
            return;
        if (Tabs.FirstOrDefault(x => x.Window.Handle == result.Handle) is not null) 
            return;
            var newTab = new HwndHostTab(this, result);
        Tabs.Add(newTab);
        TabView.SelectedItem = newTab;
    }

    private void TabDroppedOutside(TabView sender, TabViewTabDroppedOutsideEventArgs args)
    {
        if (args.Tab.Tag is HwndHostTab Tab)
        {
            Tab.DetachAndDispose();
        }
    }

    private void TabDragStarting(TabView sender, TabViewTabDragStartingEventArgs args)
    {
        var firstItem = args.Tab;
        args.Data.Properties.Add("UnitedSetsTab", firstItem);
        args.Data.RequestedOperation = DataPackageOperation.Move;
    }
}
