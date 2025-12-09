using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading.Tasks;
using System.Windows;
using AudioCaptureApp.Services;

namespace AudioCaptureApp
{
    public partial class App : Application
    {
        private IHost? _host;
        private MainWindow? _mainWindow;
        public IServiceProvider? ServiceProvider { get; private set; }

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                // 构建和启动Web服务器
                _host = Program.CreateHostBuilder(Array.Empty<string>()).Build();
                ServiceProvider = _host.Services;
                
                // 在后台任务中启动Web服务器
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _host.RunAsync();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Web服务器启动失败: {ex.Message}");
                    }
                });

                // 等待一小段时间确保服务器启动
                await Task.Delay(1000);
                
                Console.WriteLine("应用程序启动完成");
                Console.WriteLine("Web服务器正在运行在 http://0.0.0.0:9047");
                
                // 显示主窗口
                _mainWindow = new MainWindow();
                _mainWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"应用程序启动失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            try
            {
                if (_host != null)
                {
                    await _host.StopAsync();
                    _host.Dispose();
                }
            }
            catch
            {
                // 忽略关闭错误
            }
            
            base.OnExit(e);
        }

        // 提供一个方法来激活主窗口（如果需要的话）
        public void ActivateMainWindow()
        {
            if (_mainWindow != null)
            {
                if (_mainWindow.WindowState == WindowState.Minimized)
                {
                    _mainWindow.WindowState = WindowState.Normal;
                }
                _mainWindow.Activate();
                _mainWindow.Topmost = true;
                _mainWindow.Topmost = false;
            }
        }
    }
}