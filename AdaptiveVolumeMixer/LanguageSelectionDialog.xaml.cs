using System.Windows;

namespace AdaptiveVolumeMixer;

/// <summary>
/// 语言选择弹窗
/// </summary>
public partial class LanguageSelectionDialog : Window
{
    /// <summary>
    /// 用户选择的语言（"zh" 或 "en"）
    /// </summary>
    public string SelectedLanguage { get; private set; } = "zh";

    /// <summary>
    /// 是否勾选了"记住选择"
    /// </summary>
    public bool RememberChoice => RememberCheckBox.IsChecked == true;

    public LanguageSelectionDialog()
    {
        InitializeComponent();
    }

    private void ChineseButton_Click(object sender, RoutedEventArgs e)
    {
        SelectedLanguage = "zh";
        DialogResult = true;
        Close();
    }

    private void EnglishButton_Click(object sender, RoutedEventArgs e)
    {
        SelectedLanguage = "en";
        DialogResult = true;
        Close();
    }
}
