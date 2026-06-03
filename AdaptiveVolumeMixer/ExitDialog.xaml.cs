using System.Windows;

namespace AdaptiveVolumeMixer;

/// <summary>
/// 退出选择弹窗：退出程序 / 最小化到托盘
/// </summary>
public partial class ExitDialog : Window
{
    /// <summary>
    /// true=退出程序, false=最小化到托盘
    /// </summary>
    public bool ShouldExit { get; private set; }

    /// <summary>
    /// 是否勾选了"记住选择"
    /// </summary>
    public bool RememberChoice => RememberCheckBox.IsChecked == true;

    public ExitDialog()
    {
        InitializeComponent();
    }

    private void ExitButton_Click(object sender, RoutedEventArgs e)
    {
        ShouldExit = true;
        DialogResult = true;
        Close();
    }

    private void TrayButton_Click(object sender, RoutedEventArgs e)
    {
        ShouldExit = false;
        DialogResult = true;
        Close();
    }
}
