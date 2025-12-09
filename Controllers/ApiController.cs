using Microsoft.AspNetCore.Mvc;
using AudioCaptureApp.Services;
using AudioCaptureApp.Models;
using System;
using System.IO;

namespace AudioCaptureApp.Controllers
{
    [ApiController]
    public class ApiController : ControllerBase
    {
        private readonly DeviceInfoService _deviceInfoService;

        public ApiController(DeviceInfoService deviceInfoService)
        {
            _deviceInfoService = deviceInfoService;
        }

        private void LogError(string message)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var logMessage = $"[{timestamp}] {message}";
            
            // 尝试多种日志输出方式
            try
            {
                // 1. 输出到控制台
                Console.WriteLine(logMessage);
                
                // 2. 尝试写入exe目录
                var exeDirectory = AppContext.BaseDirectory;
                var logPath = Path.Combine(exeDirectory, "AudioCaptureApp.log");
                System.IO.File.AppendAllText(logPath, logMessage + "\n");
            }
            catch (Exception ex1)
            {
                try
                {
                    // 3. 如果exe目录失败，尝试写入临时目录
                    var tempPath = Path.Combine(Path.GetTempPath(), "AudioCaptureApp.log");
                    System.IO.File.AppendAllText(tempPath, logMessage + "\n");
                    Console.WriteLine($"Log written to temp: {tempPath}");
                }
                catch (Exception ex2)
                {
                    // 4. 最后只输出到控制台
                    Console.WriteLine($"Failed to write log: {ex1.Message}, {ex2.Message}");
                    Console.WriteLine(logMessage);
                }
            }
        }

        [HttpGet("/health")]
        public IActionResult GetHealth()
        {
            LogError("Health endpoint called");
            return Ok(new ApiResponse<object>
            {
                Data = new { ok = true },
                Success = true
            });
        }

        [HttpGet("/config")]
        public IActionResult GetConfig()
        {
            LogError("=== GetConfig endpoint called ===");
            
            try
            {
                LogError("Step 1: Starting GetConfig");
                
                LogError("Step 2: Getting device info service");
                var deviceInfo = _deviceInfoService?.GetDeviceInfo();
                LogError($"Step 3: DeviceInfo retrieved: {deviceInfo?.Name ?? "NULL"}");
                
                LogError("Step 4: Creating config object");
                var config = new
                {
                    audioConfig = new
                    {
                        bufferDurationMs = 50.0,
                        sampleRate = 16000.0
                    },
                    deviceInfo = deviceInfo ?? new DeviceInfo
                    {
                        Platform = "windows",
                        Version = "1.0.0",
                        Build = "1",
                        Name = "Unknown",
                        Id = "Unknown"
                    }
                };

                LogError("Step 5: Config created successfully, returning response");
                var response = new ApiResponse<object>
                {
                    Data = config,
                    Success = true
                };
                
                LogError("Step 6: Response object created, returning OK");
                return Ok(response);
            }
            catch (Exception ex)
            {
                LogError($"=== ERROR in GetConfig ===");
                LogError($"Exception Type: {ex.GetType().Name}");
                LogError($"Exception Message: {ex.Message}");
                LogError($"Stack trace: {ex.StackTrace}");
                LogError($"Inner exception: {ex.InnerException?.Message ?? "None"}");
                
                // 返回一个简化的配置
                LogError("Creating fallback config");
                var fallbackConfig = new
                {
                    audioConfig = new
                    {
                        bufferDurationMs = 50.0,
                        sampleRate = 16000.0
                    },
                    deviceInfo = new
                    {
                        platform = "windows",
                        version = "1.0.0",
                        build = "1",
                        name = "Unknown",
                        id = "Unknown"
                    },
                    error = ex.Message
                };

                LogError("Returning fallback config");
                return Ok(new ApiResponse<object>
                {
                    Data = fallbackConfig,
                    Success = true
                });
            }
        }

        [HttpGet("/")]
        public IActionResult GetRoot()
        {
            LogError("Root endpoint called");
            var isHttpsOr9048 = HttpContext.Request.IsHttps || HttpContext.Request.Host.Port == 9048;
            var html = isHttpsOr9048 
                ? @"
<!DOCTYPE html>
<html lang=""zh-CN"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>证书安装成功</title>
    <style>
        body { margin:0; background:#f7f7f7; font-family: 'Segoe UI', 'Helvetica Neue', Arial, 'Microsoft YaHei', sans-serif; color:#111; }
        .container { min-height:100vh; display:flex; align-items:center; justify-content:center; padding:40px; box-sizing:border-box; }
        .card { background:#fff; padding:56px 64px; border-radius:18px; box-shadow:0 24px 60px rgba(0,0,0,0.08); text-align:center; max-width:520px; width:100%; }
        h1 { margin:0 0 16px; font-size:28px; font-weight:700; }
        .subtitle { margin:0 0 32px; font-size:16px; color:#4a4a4a; }
        .btn { display:inline-block; margin-top:4px; padding:14px 36px; background:#111; color:#fff; border-radius:999px; font-size:16px; font-weight:600; text-decoration:none; transition:background 0.2s ease, transform 0.2s ease; }
        .btn:hover { background:#000; transform:translateY(-1px); }
        .note { margin-top:24px; font-size:13px; color:#7a7a7a; }
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""card"">
            <h1>证书安装成功</h1>
            <p class=""subtitle"">拾间局域网服务已可用</p>
            <a class=""btn"" href=""#"">我知道了</a>
            <div class=""note"">若点击上方按钮无反应，可直接手动关闭此页面</div>
        </div>
    </div>
</body>
</html>"
                : @"
<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Audio Capture Service</title>
    <style>
        body { font-family: Arial, sans-serif; margin: 40px; }
        .status { color: green; font-weight: bold; }
        .info { background: #f5f5f5; padding: 20px; border-radius: 5px; }
    </style>
</head>
<body>
    <h1>Audio Capture Service</h1>
    <div class='status'>✓ Service is running</div>
    <div class='info'>
        <h3>Available Endpoints:</h3>
        <ul>
            <li>GET /health - Health check</li>
            <li>GET /config - Configuration info</li>
            <li>WebSocket /ws - Main WebSocket endpoint</li>
            <li>WebSocket /audio - Audio WebSocket endpoint</li>
        </ul>
        <p><strong>Log locations:</strong></p>
        <ul>
            <li>Primary: " + AppContext.BaseDirectory + @"AudioCaptureApp.log</li>
            <li>Fallback: " + Path.GetTempPath() + @"AudioCaptureApp.log</li>
        </ul>
        <p><strong>Tip:</strong> Access <a href=""https://localhost:9048"" target=""_blank"">https://localhost:9048</a> to verify HTTPS certificate.</p>
    </div>
</body>
</html>";
            return Content(html, "text/html; charset=utf-8");
        }
    }
}