using Microsoft.AspNetCore.Http;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AudioCaptureApp.Services;
using Newtonsoft.Json;
using AudioCaptureApp.Models;
using System;

namespace AudioCaptureApp.Middleware
{
    public class WebSocketMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly AudioCaptureService _audioCaptureService;
        private readonly ScreenshotService _screenshotService;
        private readonly ClipboardService _clipboardService;

        public WebSocketMiddleware(RequestDelegate next, AudioCaptureService audioCaptureService, ScreenshotService screenshotService, ClipboardService clipboardService)
        {
            _next = next;
            _audioCaptureService = audioCaptureService;
            _screenshotService = screenshotService;
            _clipboardService = clipboardService;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.Request.Path == "/ws")
            {
                if (context.WebSockets.IsWebSocketRequest)
                {
                    var webSocket = await context.WebSockets.AcceptWebSocketAsync();
                    var connectionId = Guid.NewGuid().ToString();
                    
                    // 将连接添加到所有相关服务
                    _audioCaptureService.AddWebSocketConnection(connectionId, webSocket);
                    _clipboardService.AddWebSocketConnection(connectionId, webSocket);
                    
                    Console.WriteLine($"WebSocket连接已建立: {connectionId}");

                    try
                    {
                        // 发送连接成功消息
                        var welcomeMessage = "Connected successfully";
                        var welcomeBytes = Encoding.UTF8.GetBytes(welcomeMessage);
                        await webSocket.SendAsync(new ArraySegment<byte>(welcomeBytes), WebSocketMessageType.Text, true, CancellationToken.None);

                        // 开始音频捕获
                        _audioCaptureService.StartCapture(connectionId);

                        // 发送初始测试音频数据
                        await _audioCaptureService.SendTestAudioData();

                        // 处理WebSocket消息
                        await HandleWebSocketMessages(webSocket, connectionId);
                    }
                    finally
                    {
                        // 清理连接
                        _audioCaptureService.RemoveWebSocketConnection(connectionId);
                        _audioCaptureService.StopCapture(connectionId);
                        _clipboardService.RemoveWebSocketConnection(connectionId);
                        Console.WriteLine($"WebSocket连接已断开: {connectionId}");
                    }
                }
                else
                {
                    context.Response.StatusCode = 400;
                }
            }
            else
            {
                await _next(context);
            }
        }

        private async Task HandleWebSocketMessages(WebSocket webSocket, string connectionId)
        {
            var buffer = new byte[1024 * 4];
            
            while (webSocket.State == WebSocketState.Open)
            {
                try
                {
                    var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    
                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        Console.WriteLine($"收到WebSocket消息: {message}");
                        
                        try
                        {
                            var wsMessage = JsonConvert.DeserializeObject<WebSocketMessage>(message);
                            
                            // 处理截图命令
                            if (wsMessage?.WsEventType == "client-screenshot-command")
                            {
                                Console.WriteLine("收到截图命令，正在处理...");
                                await TakeScreenshotAndSend();
                            }
                        }
                        catch (JsonException ex)
                        {
                            Console.WriteLine($"解析WebSocket消息失败: {ex.Message}");
                        }
                    }
                    else if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Connection closed", CancellationToken.None);
                        break;
                    }
                }
                catch (WebSocketException ex)
                {
                    Console.WriteLine($"WebSocket异常: {ex.Message}");
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"处理WebSocket消息时发生错误: {ex.Message}");
                    break;
                }
            }
        }

        private async Task TakeScreenshotAndSend()
        {
            try
            {
                var base64Image = await _screenshotService.TakeScreenshotAsync();
                await _audioCaptureService.SendScreenshotToAllClients(base64Image);
                Console.WriteLine("截图已拍摄并发送");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"拍摄和发送截图失败: {ex.Message}");
            }
        }
    }
}