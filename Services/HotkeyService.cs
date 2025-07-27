using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows;
using AudioCaptureApp.Services;
using System.Threading.Tasks;

namespace AudioCaptureApp.Services
{
    public class HotkeyService : IDisposable
    {
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private const int HOTKEY_ID_SCREENSHOT = 9000;
        private const int HOTKEY_ID_KEYDOWN = 9001;

        private IntPtr _windowHandle;
        private HwndSource? _source;
        private readonly ScreenshotService _screenshotService;
        private readonly AudioCaptureService _audioCaptureService;
        private readonly ConfigService _configService;

        public HotkeyService(ScreenshotService screenshotService, AudioCaptureService audioCaptureService, ConfigService configService)
        {
            _screenshotService = screenshotService;
            _audioCaptureService = audioCaptureService;
            _configService = configService;
        }

        public void Initialize(Window window)
        {
            var helper = new WindowInteropHelper(window);
            _windowHandle = helper.Handle;

            if (_windowHandle == IntPtr.Zero)
            {
                window.SourceInitialized += (s, e) =>
                {
                    var newHelper = new WindowInteropHelper(window);
                    _windowHandle = newHelper.Handle;
                    RegisterHotkeys();
                };
            }
            else
            {
                RegisterHotkeys();
            }

            _source = HwndSource.FromHwnd(_windowHandle);
            _source?.AddHook(WndProc);
        }

        private void RegisterHotkeys()
        {
            if (_windowHandle != IntPtr.Zero)
            {
                var config = _configService.GetConfig();
                
                // 注册截图快捷键
                var (screenshotModifiers, screenshotKey) = ConfigService.ParseHotkey(config.Hotkeys.ScreenshotHotkey);
                if (screenshotKey != 0)
                {
                    bool screenshotSuccess = RegisterHotKey(_windowHandle, HOTKEY_ID_SCREENSHOT, screenshotModifiers, screenshotKey);
                    Console.WriteLine($"截图快捷键注册{(screenshotSuccess ? "成功" : "失败")}: {config.Hotkeys.ScreenshotHotkey}");
                }

                // 注册键盘事件快捷键
                var (keydownModifiers, keydownKey) = ConfigService.ParseHotkey(config.Hotkeys.KeydownEventHotkey);
                if (keydownKey != 0)
                {
                    bool keydownSuccess = RegisterHotKey(_windowHandle, HOTKEY_ID_KEYDOWN, keydownModifiers, keydownKey);
                    Console.WriteLine($"键盘事件快捷键注册{(keydownSuccess ? "成功" : "失败")}: {config.Hotkeys.KeydownEventHotkey}");
                }
            }
        }

        public void ReregisterHotkeys()
        {
            // 先注销现有快捷键
            if (_windowHandle != IntPtr.Zero)
            {
                UnregisterHotKey(_windowHandle, HOTKEY_ID_SCREENSHOT);
                UnregisterHotKey(_windowHandle, HOTKEY_ID_KEYDOWN);
            }
            
            // 重新注册
            RegisterHotkeys();
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_HOTKEY = 0x0312;

            if (msg == WM_HOTKEY)
            {
                int id = wParam.ToInt32();
                if (id == HOTKEY_ID_SCREENSHOT)
                {
                    // 触发截图
                    Task.Run(async () => await TakeScreenshotAndSend());
                    handled = true;
                }
                else if (id == HOTKEY_ID_KEYDOWN)
                {
                    // 触发键盘事件
                    Task.Run(async () => await SendKeydownEvent());
                    handled = true;
                }
            }

            return IntPtr.Zero;
        }

        public async Task TakeScreenshotAndSend()
        {
            try
            {
                Console.WriteLine("截图快捷键被触发");
                var base64Image = await _screenshotService.TakeScreenshotAsync();
                
                if (!string.IsNullOrEmpty(base64Image))
                {
                    await _audioCaptureService.SendScreenshotToAllClients(base64Image);
                    Console.WriteLine("截图已发送到所有WebSocket客户端");
                }
                else
                {
                    Console.WriteLine("截图失败");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"截图处理错误: {ex.Message}");
            }
        }

        public async Task SendKeydownEvent()
        {
            try
            {
                Console.WriteLine("键盘事件快捷键被触发");
                await _audioCaptureService.SendKeydownEventToAllClients();
                Console.WriteLine("键盘事件已发送到所有WebSocket客户端");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"键盘事件处理错误: {ex.Message}");
            }
        }

        public void Dispose()
        {
            if (_windowHandle != IntPtr.Zero)
            {
                UnregisterHotKey(_windowHandle, HOTKEY_ID_SCREENSHOT);
                UnregisterHotKey(_windowHandle, HOTKEY_ID_KEYDOWN);
            }
            _source?.RemoveHook(WndProc);
            _source?.Dispose();
        }
    }
}