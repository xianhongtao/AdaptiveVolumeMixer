using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AdaptiveVolumeMixer.Models;
using AdaptiveVolumeMixer.ViewModels;
using Button = System.Windows.Controls.Button;
using DragDropEffects = System.Windows.DragDropEffects;
using DragEventArgs = System.Windows.DragEventArgs;
using RoutedEventArgs = System.Windows.RoutedEventArgs;
using UserControl = System.Windows.Controls.UserControl;

namespace AdaptiveVolumeMixer.Controls;

/// <summary>
/// 层级项控件：展示单个层级的标题栏、进程列表和空状态
/// </summary>
public partial class LevelItemControl : UserControl
{
    public LevelItemControl()
    {
        InitializeComponent();
    }

    /// <summary>
    /// 删除进程按钮点击事件
    /// </summary>
    private void RemoveProcessButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is LevelItemViewModel item)
        {
            // 向上查找 MainViewModel
            var window = Window.GetWindow(this);
            if (window?.DataContext is MainViewModel vm)
            {
                vm.RemoveProcessCommand.Execute(item);
            }
        }
    }

    // ==================== 拖拽相关 ====================

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
        if (DataContext is LevelViewModel levelVm)
        {
            var window = Window.GetWindow(this);
            if (window?.DataContext is MainViewModel vm)
            {
                vm.AddProcessToLevel(process, levelVm.Level);
            }
        }
    }
}
