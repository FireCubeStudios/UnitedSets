using System.ComponentModel;
using Microsoft.UI.Xaml;
using System.Linq;
using System;
using Windows.ApplicationModel.DataTransfer;
using Windows.Win32;
using Window = WinWrapper.Window;
using EasyCSharp;
using UnitedSets.Windows;
namespace UnitedSets.Classes;
public abstract partial class ICell : INotifyPropertyChanged
{
    [Event(typeof(DragEventHandler), Name = "DragOverEv")]
    public void OnDragOver(DragEventArgs e)
    {
        // There MUST BE NO SUBCELL AND CURRNETCELL
        if (!Empty) return;
        DataPackageView dataview = e.DataView;
        var formats = dataview.AvailableFormats.ToList();
        if (formats.Contains("UnitedSetsTabWindow"))
            e.AcceptedOperation = DataPackageOperation.Move;
    }

    [Event(typeof(DragEventHandler), Name = "DropEv")]
    public async void OnItemDrop(DragEventArgs e)
    {
        // There MUST BE NO SUBCELL AND CURRNETCELL
        if (!Empty) return;
        DataPackageView dataview = e.DataView;
        var formats = dataview.AvailableFormats.ToList();
        if (formats.Contains("UnitedSetsTabWindow"))
        {
            var hwnd = (long)await e.DataView.GetDataAsync("UnitedSetsTabWindow");
            var window = Window.FromWindowHandle((nint)hwnd);
            var ret = PInvoke.SendMessage(window.Owner, MainWindow.UnitedSetCommunicationChangeWindowOwnership, new(), new(window));
            RegisterWindow(window);
        }
    }

    public int CellAddCount = 2;
    public string CellAddCountAsString => CellAddCount.ToString();
    [Event(typeof(RoutedEventHandler), Name = "AddCellAddCountClickEv")]
    public void AddCellAddCount()
    {
        CellAddCount += 1;
        _PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CellAddCount)));
        _PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CellAddCountAsString)));
    }

    [Event(typeof(RoutedEventHandler), Name = "SubtractCellAddCountClickEv")]
    public void SubtractCellAddCount()
    {
        if (CellAddCount <= 2) return;
        CellAddCount -= 1;
        _PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CellAddCount)));
        _PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CellAddCountAsString)));
    }

    [Event(typeof(RoutedEventHandler), Name = "SplitVerticallyClickEv", Visibility = GeneratorVisibility.Public)]
    void SplitVerticallyCellAddCount()
    {
        SplitVertically(CellAddCount);
    }

    [Event(typeof(RoutedEventHandler), Name = "SplitHorizontallyClickEv", Visibility = GeneratorVisibility.Public)]
    void SplitHorizontallyCellAddCount()
    {
        SplitHorizontally(CellAddCount);
    }

}