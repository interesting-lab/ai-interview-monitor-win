using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Threading;
using System.Diagnostics;
using System.Runtime.InteropServices;
using AudioCaptureApp.Services;

namespace AudioCaptureApp
{
    public class Program
    {
        private static Mutex? _mutex;
        private const string MutexName = "AudioCaptureApp_SingleInstance_Mutex";

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);

        private const int SW_RESTORE = 9;

        [STAThread]
        public static void Main(string[] args)
        {
            // 检查是否已有实例在运行
            _mutex = new Mutex(true, MutexName, out bool createdNew);

            if (!createdNew)
            {
                // 已有实例在运行，尝试激活现有窗口
                ActivateExistingInstance();
                return;
            }

            try
            {
                var app = new App();
                app.Run();
            }
            finally
            {
                _mutex?.ReleaseMutex();
                _mutex?.Dispose();
            }
        }

        private static void ActivateExistingInstance()
        {
            try
            {
                // 查找现有的应用程序窗口
                var currentProcess = Process.GetCurrentProcess();
                var processes = Process.GetProcessesByName(currentProcess.ProcessName);

                foreach (var process in processes)
                {
                    if (process.Id != currentProcess.Id && process.MainWindowHandle != IntPtr.Zero)
                    {
                        // 如果窗口被最小化，先恢复
                        if (IsIconic(process.MainWindowHandle))
                        {
                            ShowWindow(process.MainWindowHandle, SW_RESTORE);
                        }

                        // 将窗口置于前台
                        SetForegroundWindow(process.MainWindowHandle);
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                // 如果激活失败，显示消息框
                MessageBox.Show($"应用程序已在运行中。\n激活现有窗口时出错: {ex.Message}", 
                    "Audio Capture Service", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                    webBuilder.UseUrls("http://0.0.0.0:9047", "https://0.0.0.0:9048"); // HTTP和HTTPS端口
                    webBuilder.ConfigureKestrel((context, options) =>
                    {
                        options.ListenAnyIP(9047); // HTTP端口
                        options.ListenAnyIP(9048, listenOptions =>
                        {
                            // HTTPS配置 - 延迟配置证书
                            listenOptions.UseHttps(httpsOptions =>
                            {
                                httpsOptions.ServerCertificateSelector = (connectionContext, name) =>
                                {
                                    // 创建一个临时的logger factory来获取证书
                                    using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
                                    var logger = loggerFactory.CreateLogger<HttpsService>();
                                    var httpsService = new HttpsService(logger);
                                    return httpsService.GetOrCreateCertificate();
                                };
                            });
                        });
                    });
                });
    }
}