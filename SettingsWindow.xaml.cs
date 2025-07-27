using System;
using System.Windows;
using AudioCaptureApp.Services;

namespace AudioCaptureApp
{
    public partial class SettingsWindow : Window
    {
        private readonly ConfigService _configService;
        private AppConfig _currentConfig;
        private bool _isCapturingScreenshotHotkey = false;
        private bool _isCapturingKeydownEventHotkey = false;

        public event EventHandler? ConfigChanged;

        public SettingsWindow(ConfigService configService)
        {
            InitializeComponent();
            _configService = configService;
            LoadCurrentConfig();
        }

        private void LoadCurrentConfig()
        {
            _currentConfig = _configService.GetConfig();
            ScreenshotHotkeyTextBox.Text = _currentConfig.Hotkeys.ScreenshotHotkey;
            KeydownEventHotkeyTextBox.Text = _currentConfig.Hotkeys.KeydownEventHotkey;
        }

        private void SetScreenshotHotkey_Click(object sender, RoutedEventArgs e)
        {
            if (_isCapturingScreenshotHotkey)
            {
                StopCapturingHotkey();
                return;
            }

            _isCapturingScreenshotHotkey = true;
            SetScreenshotHotkeyButton.Content = "按下快捷键...";
            ScreenshotHotkeyTextBox.Text = "请按下新的快捷键组合";
            
            // 监听键盘事件
            KeyDown += SettingsWindow_KeyDown;
            Focus();
        }

        private void SetKeydownEventHotkey_Click(object sender, RoutedEventArgs e)
        {
            if (_isCapturingKeydownEventHotkey)
            {
                StopCapturingHotkey();
                return;
            }

            _isCapturingKeydownEventHotkey = true;
            SetKeydownEventHotkeyButton.Content = "按下快捷键...";
            KeydownEventHotkeyTextBox.Text = "请按下新的快捷键组合";
            
            // 监听键盘事件
            KeyDown += SettingsWindow_KeyDown;
            Focus();
        }

        private void SettingsWindow_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (!_isCapturingScreenshotHotkey && !_isCapturingKeydownEventHotkey)
                return;

            var modifiers = System.Windows.Input.Keyboard.Modifiers;
            var key = e.Key;

            // 忽略单独的修饰键
            if (key == System.Windows.Input.Key.LeftCtrl || key == System.Windows.Input.Key.RightCtrl ||
                key == System.Windows.Input.Key.LeftShift || key == System.Windows.Input.Key.RightShift ||
                key == System.Windows.Input.Key.LeftAlt || key == System.Windows.Input.Key.RightAlt)
            {
                return;
            }

            // 构建快捷键字符串
            var hotkeyString = "";
            if (modifiers.HasFlag(System.Windows.Input.ModifierKeys.Control))
                hotkeyString += "Ctrl+";
            if (modifiers.HasFlag(System.Windows.Input.ModifierKeys.Shift))
                hotkeyString += "Shift+";
            if (modifiers.HasFlag(System.Windows.Input.ModifierKeys.Alt))
                hotkeyString += "Alt+";

            // 转换按键名称
            var keyName = ConvertKeyToString(key);
            if (!string.IsNullOrEmpty(keyName))
            {
                hotkeyString += keyName;

                if (_isCapturingScreenshotHotkey)
                {
                    ScreenshotHotkeyTextBox.Text = hotkeyString;
                    _currentConfig.Hotkeys.ScreenshotHotkey = hotkeyString;
                }
                else if (_isCapturingKeydownEventHotkey)
                {
                    KeydownEventHotkeyTextBox.Text = hotkeyString;
                    _currentConfig.Hotkeys.KeydownEventHotkey = hotkeyString;
                }

                StopCapturingHotkey();
            }

            e.Handled = true;
        }

        private string ConvertKeyToString(System.Windows.Input.Key key)
        {
            switch (key)
            {
                case System.Windows.Input.Key.Space:
                    return "Space";
                case System.Windows.Input.Key.Enter:
                    return "Enter";
                case System.Windows.Input.Key.F1:
                    return "F1";
                case System.Windows.Input.Key.F2:
                    return "F2";
                case System.Windows.Input.Key.F3:
                    return "F3";
                case System.Windows.Input.Key.F4:
                    return "F4";
                case System.Windows.Input.Key.F5:
                    return "F5";
                case System.Windows.Input.Key.F6:
                    return "F6";
                case System.Windows.Input.Key.F7:
                    return "F7";
                case System.Windows.Input.Key.F8:
                    return "F8";
                case System.Windows.Input.Key.F9:
                    return "F9";
                case System.Windows.Input.Key.F10:
                    return "F10";
                case System.Windows.Input.Key.F11:
                    return "F11";
                case System.Windows.Input.Key.F12:
                    return "F12";
                default:
                    if (key >= System.Windows.Input.Key.A && key <= System.Windows.Input.Key.Z)
                    {
                        return key.ToString();
                    }
                    return "";
            }
        }

        private void StopCapturingHotkey()
        {
            _isCapturingScreenshotHotkey = false;
            _isCapturingKeydownEventHotkey = false;
            SetScreenshotHotkeyButton.Content = "设置";
            SetKeydownEventHotkeyButton.Content = "设置";
            KeyDown -= SettingsWindow_KeyDown;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _configService.SaveConfig(_currentConfig);
                ConfigChanged?.Invoke(this, EventArgs.Empty);
                MessageBox.Show("设置已保存！重启应用程序后生效。", "保存成功", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存设置失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            StopCapturingHotkey();
            base.OnClosed(e);
        }
    }
}