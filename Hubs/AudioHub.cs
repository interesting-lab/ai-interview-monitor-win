using Microsoft.AspNetCore.SignalR;
using AudioCaptureApp.Models;
using AudioCaptureApp.Services;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace AudioCaptureApp.Hubs
{
    public class AudioHub : Hub
    {
        private readonly AudioCaptureService _audioCaptureService;
        private readonly ScreenshotService _screenshotService;
        private readonly ClipboardService _clipboardService;
        private readonly ILogger<AudioHub> _logger;

        public AudioHub(AudioCaptureService audioCaptureService, 
                       ScreenshotService screenshotService,
                       ClipboardService clipboardService,
                       ILogger<AudioHub> logger)
        {
            _audioCaptureService = audioCaptureService;
            _screenshotService = screenshotService;
            _clipboardService = clipboardService;
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            _logger.LogInformation($"WebSocket client connected: {Context.ConnectionId}");
            _logger.LogInformation($"User Agent: {Context.GetHttpContext()?.Request.Headers["User-Agent"]}");
            _logger.LogInformation($"Origin: {Context.GetHttpContext()?.Request.Headers["Origin"]}");
            
            try
            {
                await base.OnConnectedAsync();
                
                // 开始音频捕获
                _audioCaptureService.StartCapture(Context.ConnectionId);
                
                // 开始剪贴板监控
                _clipboardService.StartMonitoring(Context.ConnectionId);
                
                _logger.LogInformation($"Services started for connection: {Context.ConnectionId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in OnConnectedAsync for {Context.ConnectionId}");
                throw;
            }
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            _logger.LogInformation($"WebSocket client disconnected: {Context.ConnectionId}");
            if (exception != null)
            {
                _logger.LogError(exception, $"Disconnection error for {Context.ConnectionId}");
            }
            
            try
            {
                // 停止音频捕获
                _audioCaptureService.StopCapture(Context.ConnectionId);
                
                // 停止剪贴板监控
                _clipboardService.StopMonitoring(Context.ConnectionId);
                
                await base.OnDisconnectedAsync(exception);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in OnDisconnectedAsync for {Context.ConnectionId}");
            }
        }

        public async Task SendMessage(string message)
        {
            try
            {
                _logger.LogInformation($"Received message from {Context.ConnectionId}: {message}");
                
                var wsMessage = JsonConvert.DeserializeObject<WebSocketMessage>(message);
                
                if (wsMessage?.WsEventType == "client-screenshot-command")
                {
                    var screenshot = await _screenshotService.TakeScreenshotAsync();
                    var response = new WebSocketMessage
                    {
                        Id = Guid.NewGuid().ToString(),
                        Payload = new ScreenshotPayload { Base64 = screenshot },
                        Type = null,
                        WsEventType = "clipboard-image-event"
                    };
                    
                    await Clients.Caller.SendAsync("ReceiveMessage", JsonConvert.SerializeObject(response));
                }
                else if (wsMessage?.WsEventType == "test-audio-command")
                {
                    // 发送测试音频数据
                    await _audioCaptureService.SendTestAudioData();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing message from {Context.ConnectionId}: {message}");
            }
        }
    }
}