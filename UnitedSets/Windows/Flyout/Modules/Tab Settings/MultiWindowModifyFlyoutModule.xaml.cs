using EasyCSharp;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using UnitedSets.Classes;
using WinUI3HwndHostPlus;
namespace UnitedSets.Windows.Flyout.Modules;

public sealed partial class MultiWindowModifyFlyoutModule
{
    public MultiWindowModifyFlyoutModule(HwndHost[] HwndHosts)
    {
        this.HwndHosts = HwndHosts;
        InitializeComponent();
        foreach (var hwndhost in HwndHosts)
        {
            HwndHostSelector.Items.Add(hwndhost.HostedWindow.TitleText);
        }
    }
    readonly HwndHost[] HwndHosts;

    [Event(typeof(SelectionChangedEventHandler))]
    void HwndHostSelector_SelectionChanged()
    {
        if (HwndHostSelector.SelectedIndex != -1)
            ModifyWindowFlyoutModulePlace.Child = new ModifyWindowFlyoutModule(
                HwndHosts[HwndHostSelector.SelectedIndex]
            );
        else
            ModifyWindowFlyoutModulePlace.Child = null;
    }
}
