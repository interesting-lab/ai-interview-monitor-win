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
            var html = @"
<!DOCTYPE html>
<html>
<head>
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
    </div>
</body>
</html>";
            return Content(html, "text/html");
        }
    }
}