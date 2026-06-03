using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Application = System.Windows.Application;
using DataObject = System.Windows.DataObject;
using DragDrop = System.Windows.DragDrop;
using DragDropEffects = System.Windows.DragDropEffects;
using ListBox = System.Windows.Controls.ListBox;
using MouseButtonEventArgs = System.Windows.Input.MouseButtonEventArgs;
using MouseButtonState = System.Windows.Input.MouseButtonState;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using Point = System.Windows.Point;
using AdaptiveVolumeMixer.Models;
using AdaptiveVolumeMixer.ViewModels;

namespace AdaptiveVolumeMixer;

/// <summary>
/// 主窗口：工具栏、层级列表、可用进程面板、状态栏
/// 仅负责窗口级别行为和拖拽源检测；拖拽放置和进程删除由 LevelItemControl 处理
/// </summary>
public partial class MainWindow : Window
{
    /// <summary>
    /// 是否允许窗口真正关闭（由托盘"退出"菜单设置后触发）
    /// </summary>
    public bool AllowClose { get; set; }

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    /// <summary>
    /// 窗口关闭拦截：取消关闭并隐藏到系统托盘
    /// </summary>
    private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (AllowClose)
        {
            AllowClose = false;
            return;
        }

        e.Cancel = true;
        Hide();
        (Application.Current as App)?.ShowMinimizeBalloonTip();
    }

    /// <summary>
    /// 窗口最小化时隐藏到系统托盘
    /// </summary>
    private void MainWindow_StateChanged(object? sender, EventArgs e)
    {
        if (WindowState == WindowState.Minimized)
        {
            Hide();
            (Application.Current as App)?.ShowMinimizeBalloonTip();
        }
    }

    // ==================== 拖拽源检测（进程列表 → 层级区域） ====================

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
