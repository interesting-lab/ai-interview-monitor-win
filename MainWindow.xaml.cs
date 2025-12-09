using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Windows;
using System.Windows.Threading;
using NAudio.Wave;
using System;
using System.Linq;
using System.Threading.Tasks;
using AudioCaptureApp.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Windows.Input;

namespace AudioCaptureApp
{
    public partial class MainWindow : Window
    {
        private DispatcherTimer? _updateTimer;
        private WasapiLoopbackCapture? _systemAudioMonitor;
        private WaveInEvent? _micAudioMonitor;
        private double _systemAudioLevel = 0;
        private double _micAudioLevel = 0;
        private readonly object _audioLock = new object();
        private HotkeyService? _hotkeyService;
        private ConfigService? _configService;

        public MainWindow()
        {
            InitializeComponent();
            InitializeWindow();
            UpdateNetworkInfo();
            StartUpdateTimer();
            
            // 延迟启动音频监控，避免阻塞UI
            Dispatcher.BeginInvoke(new Action(async () =>
            {
                await Task.Delay(1000); // 等待1秒让UI完全加载
                StartAudioMonitoring();
                InitializeHotkeyService();
            }), DispatcherPriority.Background);
        }

        private void InitializeHotkeyService()
        {
            try
            {
                var serviceProvider = ((App)Application.Current).ServiceProvider;
                _hotkeyService = serviceProvider?.GetService(typeof(HotkeyService)) as HotkeyService;
                _configService = serviceProvider?.GetService(typeof(ConfigService)) as ConfigService;
                
                if (_hotkeyService != null && _configService != null)
                {
                    _hotkeyService.Initialize(this);
                    var config = _configService.GetConfig();
                    LogTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] 截图快捷键已注册: {config.Hotkeys.ScreenshotHotkey}\n");
                    LogTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] 键盘事件快捷键已注册: {config.Hotkeys.KeydownEventHotkey}\n");
                }
                else
                {
                    LogTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] 快捷键服务初始化失败\n");
                }
            }
            catch (Exception ex)
            {
                LogTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] 快捷键服务初始化错误: {ex.Message}\n");
            }
        }

        private void InitializeWindow()
        {
            try
            {
                // 设置窗口图标
                var iconUri = new Uri("pack://application:,,,/icon.ico");
                var iconBitmap = new System.Windows.Media.Imaging.BitmapImage(iconUri);
                Icon = iconBitmap;
                
                // 设置托盘图标
                if (TrayIcon != null)
                {
                    TrayIcon.IconSource = iconBitmap;
                }
            }
            catch (Exception ex)
            {
                // 如果图标加载失败，记录错误但不影响程序运行
                LogTextBox?.AppendText($"[{DateTime.Now:HH:mm:ss}] 图标加载失败: {ex.Message}\n");
            }
        
            // 最小化到系统托盘
            StateChanged += MainWindow_StateChanged;
            
            // 添加窗口拖拽功能
            MouseLeftButtonDown += (s, e) => 
            {
                if (e.GetPosition(this).Y < 60) // 只在标题栏区域允许拖拽
                {
                    DragMove();
                }
            };
            
            // 添加窗口激活支持
            Activated += (s, e) =>
            {
                // 当窗口被激活时，确保它在前台
                Topmost = true;
                Topmost = false;
            };
            
            // 添加未处理异常处理
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                LogTextBox?.Dispatcher.BeginInvoke(new Action(() =>
                {
                    LogTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] 未处理异常: {e.ExceptionObject}\n");
                }));
            };
        }

        private void MainWindow_StateChanged(object? sender, EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
            {
                Hide();
                try
                {
                    TrayIcon?.ShowBalloonTip("Audio Capture Service", "应用程序已最小化到系统托盘", Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Info);
                }
                catch { }
            }
        }

        private void UpdateNetworkInfo()
        {
            try
            {
                var localIPs = NetworkInterface.GetAllNetworkInterfaces()
                    .Where(ni => ni.OperationalStatus == OperationalStatus.Up)
                    .SelectMany(ni => ni.GetIPProperties().UnicastAddresses)
                    .Where(addr => addr.Address.AddressFamily == AddressFamily.InterNetwork && 
                                   !addr.Address.ToString().StartsWith("127."))
                    .Select(addr => addr.Address.ToString())
                    .ToList();

                if (localIPs.Any())
                {
                    var networkText = string.Join(", ", localIPs.Select(ip => $"http://{ip}:9047"));
                    LocalNetworkText.Text = networkText;
                    
                    // 更新状态栏显示HTTPS信息
                    NetworkInfo.Text = "HTTP: localhost:9047 | HTTPS: localhost:9048";
                }
                else
                {
                    LocalNetworkText.Text = "未检测到可用网络";
                    NetworkInfo.Text = "HTTP: localhost:9047 | HTTPS: localhost:9048";
                }
            }
            catch (Exception ex)
            {
                LocalNetworkText.Text = $"网络信息获取失败 - {ex.Message}";
                NetworkInfo.Text = "HTTP: localhost:9047 | HTTPS: localhost:9048";
            }
        }

        private void StartAudioMonitoring()
        {
            Task.Run(() =>
            {
                try
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        LogTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] 正在启动音频监控...\n");
                    }));

                    // 启动系统音频监控
                    try
                    {
                        _systemAudioMonitor = new WasapiLoopbackCapture();
                        _systemAudioMonitor.DataAvailable += OnSystemAudioData;
                        _systemAudioMonitor.StartRecording();
                        
                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            LogTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] 系统音频监控已启动\n");
                        }));
                    }
                    catch (Exception ex)
                    {
                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            LogTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] 系统音频监控启动失败: {ex.Message}\n");
                        }));
                    }

                    // 启动麦克风监控
                    try
                    {
                        _micAudioMonitor = new WaveInEvent
                        {
                            BufferMilliseconds = 100
                        };
                        _micAudioMonitor.DataAvailable += OnMicAudioData;
                        _micAudioMonitor.StartRecording();
                        
                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            LogTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] 麦克风监控已启动\n");
                        }));
                    }
                    catch (Exception ex)
                    {
                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            LogTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] 麦克风监控启动失败: {ex.Message}\n");
                        }));
                    }
                }
                catch (Exception ex)
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        LogTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] 音频监控启动异常: {ex.Message}\n");
                    }));
                }
            });
        }

        private void OnSystemAudioData(object? sender, WaveInEventArgs e)
        {
            try
            {
                // 在后台线程计算音频电平
                double sum = 0;
                int sampleCount = e.BytesRecorded / 2;
                
                for (int i = 0; i < e.BytesRecorded; i += 2)
                {
                    short sample = BitConverter.ToInt16(e.Buffer, i);
                    sum += Math.Abs(sample);
                }
                
                lock (_audioLock)
                {
                    _systemAudioLevel = Math.Min((sum / sampleCount) / 32768.0 * 100, 100);
                }
            }
            catch
            {
                // 忽略音频处理错误
            }
        }

        private void OnMicAudioData(object? sender, WaveInEventArgs e)
        {
            try
            {
                // 在后台线程计算音频电平
                double sum = 0;
                int sampleCount = e.BytesRecorded / 2;
                
                for (int i = 0; i < e.BytesRecorded; i += 2)
                {
                    short sample = BitConverter.ToInt16(e.Buffer, i);
                    sum += Math.Abs(sample);
                }
                
                lock (_audioLock)
                {
                    _micAudioLevel = Math.Min((sum / sampleCount) / 32768.0 * 100, 100);
                }
            }
            catch
            {
                // 忽略音频处理错误
            }
        }

        private void StartUpdateTimer()
        {
            _updateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(200) // 降低更新频率
            };
            _updateTimer.Tick += UpdateTimer_Tick;
            _updateTimer.Start();
        }

        private void UpdateTimer_Tick(object? sender, EventArgs e)
        {
            try
            {
                // 更新音频电平显示
                lock (_audioLock)
                {
                    SystemAudioLevel.Value = _systemAudioLevel;
                    MicAudioLevel.Value = _micAudioLevel;
                }
                
                // 减少日志更新频率
                if (DateTime.Now.Second % 10 == 0 && DateTime.Now.Millisecond < 300)
                {
                    if (LogTextBox.LineCount > 30)
                    {
                        LogTextBox.Clear();
                    }
                    
                    lock (_audioLock)
                    {
                        LogTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] 系统音频: {_systemAudioLevel:F1}%, 麦克风: {_micAudioLevel:F1}%\n");
                    }
                    LogTextBox.ScrollToEnd();
                }
            }
            catch
            {
                // 忽略UI更新错误
            }
        }

        // 新增的窗口控制事件处理方法
        private void MinimizeWindow_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void CloseWindow_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_configService != null)
                {
                    var settingsWindow = new SettingsWindow(_configService);
                    settingsWindow.Owner = this;
                    settingsWindow.ConfigChanged += (s, args) =>
                    {
                        // 配置更改后重新注册快捷键
                        _hotkeyService?.ReregisterHotkeys();
                        var config = _configService.GetConfig();
                        LogTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] 快捷键配置已更新\n");
                        LogTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] 截图快捷键: {config.Hotkeys.ScreenshotHotkey}\n");
                        LogTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] 键盘事件快捷键: {config.Hotkeys.KeydownEventHotkey}\n");
                    };
                    settingsWindow.ShowDialog();
                }
                else
                {
                    MessageBox.Show("配置服务未初始化", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打开设置窗口失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShowWindow_Click(object sender, RoutedEventArgs e)
        {
            Show();
            WindowState = WindowState.Normal;
            Activate();
        }

        private void ExitApplication_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void TrayIcon_TrayMouseDoubleClick(object sender, RoutedEventArgs e)
        {
            ShowWindow_Click(sender, e);
        }

        protected override void OnClosed(EventArgs e)
        {
            try
            {
                _updateTimer?.Stop();
                
                _systemAudioMonitor?.StopRecording();
                _systemAudioMonitor?.Dispose();
                
                _micAudioMonitor?.StopRecording();
                _micAudioMonitor?.Dispose();
                
                _hotkeyService?.Dispose();
                
                TrayIcon?.Dispose();
            }
            catch
            {
                // 忽略清理错误
            }
            
            base.OnClosed(e);
        }
    }
}