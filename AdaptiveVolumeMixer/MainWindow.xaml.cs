using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Application = System.Windows.Application;
using Button = System.Windows.Controls.Button;
using DataObject = System.Windows.DataObject;
using DragDrop = System.Windows.DragDrop;
using DragDropEffects = System.Windows.DragDropEffects;
using DragEventArgs = System.Windows.DragEventArgs;
using ListBox = System.Windows.Controls.ListBox;
using MouseButtonEventArgs = System.Windows.Input.MouseButtonEventArgs;
using MouseButtonState = System.Windows.Input.MouseButtonState;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using Point = System.Windows.Point;
using AdaptiveVolumeMixer.Models;
using AdaptiveVolumeMixer.ViewModels;

namespace AdaptiveVolumeMixer;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    /// <summary>
    /// 是否允许窗口真正关闭（由托盘"退出"菜单设置）
    /// </summary>
    public bool AllowClose { get; set; }

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    /// <summary>
    /// 窗口关闭时拦截：隐藏到托盘而非退出
    /// </summary>
    private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (AllowClose)
        {
            // 允许真正关闭（托盘"退出"触发）
            AllowClose = false;
            return;
        }

        // 取消关闭，改为隐藏到托盘
        e.Cancel = true;
        Hide();
        (Application.Current as App)?.ShowMinimizeBalloonTip();
    }

    /// <summary>
    /// 窗口最小化时隐藏到托盘
    /// </summary>
    private void MainWindow_StateChanged(object? sender, EventArgs e)
    {
        if (WindowState == WindowState.Minimized)
        {
            Hide();
            (Application.Current as App)?.ShowMinimizeBalloonTip();
        }
    }

    /// <summary>
    /// 删除进程按钮点击事件
    /// 使用事件处理而非 Command 绑定，避免嵌套 ItemsControl 中绑定失效的问题
    /// </summary>
    private void RemoveProcessButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is LevelItemViewModel item)
        {
            if (DataContext is MainViewModel vm)
            {
                vm.RemoveProcessCommand.Execute(item);
            }
        }
    }

    // ==================== 拖拽相关 ====================

    private Point _dragStartPoint;
    private bool _isDragging;

    /// <summary>
    /// 可用进程列表鼠标移动 — 检测拖拽启动
    /// </summary>
    private void AvailableProcessListBox_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || _isDragging)
            return;

        var listBox = sender as ListBox;
        if (listBox?.SelectedItem is not AudioProcess process)
            return;

        var currentPosition = e.GetPosition(listBox);
        var diff = currentPosition - _dragStartPoint;

        // 超过阈值才启动拖拽，避免误触
        if (Math.Abs(diff.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(diff.Y) < SystemParameters.MinimumVerticalDragDistance)
            return;

        _isDragging = true;
        try
        {
            var data = new DataObject("AudioProcess", process);
            DragDrop.DoDragDrop(listBox, data, DragDropEffects.Move);
        }
        finally
        {
            _isDragging = false;
        }
    }

    /// <summary>
    /// 可用进程列表鼠标按下 — 记录起始位置
    /// </summary>
    protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
    {
        base.OnPreviewMouseDown(e);
        if (e.OriginalSource is DependencyObject source)
        {
            var listBox = FindParent<ListBox>(source);
            if (listBox?.Name == "AvailableProcessListBox")
            {
                _dragStartPoint = e.GetPosition(listBox);
            }
        }
    }

    /// <summary>
    /// 层级区域拖拽进入 — 高亮边框
    /// </summary>
    private void LevelBorder_DragEnter(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent("AudioProcess"))
        {
            e.Effects = DragDropEffects.Move;
            if (sender is Border border)
                border.Tag = "DragOver";
        }
        else
        {
            e.Effects = DragDropEffects.None;
        }
    }

    /// <summary>
    /// 层级区域拖拽经过 — 持续显示高亮
    /// </summary>
    private void LevelBorder_DragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent("AudioProcess"))
        {
            e.Effects = DragDropEffects.Move;
        }
        else
        {
            e.Effects = DragDropEffects.None;
        }
    }

    /// <summary>
    /// 层级区域拖拽离开 — 恢复边框
    /// </summary>
    private void LevelBorder_DragLeave(object sender, DragEventArgs e)
    {
        if (sender is Border border)
            border.Tag = null;
    }

    /// <summary>
    /// 层级区域放下 — 将进程添加到目标层级
    /// </summary>
    private void LevelBorder_Drop(object sender, DragEventArgs e)
    {
        if (sender is Border border)
            border.Tag = null;

        if (!e.Data.GetDataPresent("AudioProcess"))
            return;

        var process = e.Data.GetData("AudioProcess") as AudioProcess;
        if (process == null)
            return;

        // 获取目标层级的 DataContext
        if (sender is FrameworkElement element && element.DataContext is LevelViewModel levelVm)
        {
            if (DataContext is MainViewModel vm)
            {
                vm.AddProcessToLevel(process, levelVm.Level);
            }
        }
    }

    /// <summary>
    /// 辅助方法：向上查找指定类型的父元素
    /// </summary>
    private static T? FindParent<T>(DependencyObject child) where T : DependencyObject
    {
        var parent = VisualTreeHelper.GetParent(child);
        while (parent != null)
        {
            if (parent is T typed)
                return typed;
            parent = VisualTreeHelper.GetParent(parent);
        }
        return null;
    }
}
