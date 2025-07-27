using Microsoft.AspNetCore.SignalR;
using AudioCaptureApp.Hubs;
using AudioCaptureApp.Models;
using Newtonsoft.Json;
using System.Windows;
using System.Windows.Threading;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System.Net.WebSockets;
using System.Text;
using System.Linq;

namespace AudioCaptureApp.Services
{
    public class ClipboardService
    {
        private readonly IHubContext<AudioHub> _hubContext;
        private readonly Dictionary<string, bool> _activeConnections = new();
        private readonly Dictionary<string, WebSocket> _webSocketConnections = new();
        private string _lastClipboardText = "";
        private Timer? _clipboardMonitorTimer;
        private readonly AudioCaptureService _audioCaptureService;

        public ClipboardService(IHubContext<AudioHub> hubContext, AudioCaptureService audioCaptureService)
        {
            _hubContext = hubContext;
            _audioCaptureService = audioCaptureService;
        }

        public void AddWebSocketConnection(string connectionId, WebSocket webSocket)
        {
            _webSocketConnections[connectionId] = webSocket;
            Console.WriteLine($"Added WebSocket connection to ClipboardService: {connectionId}");
            
            // 如果是第一个连接，开始监控剪切板
            if (_webSocketConnections.Count == 1)
            {
                StartClipboardMonitoring();
            }
        }

        public void RemoveWebSocketConnection(string connectionId)
        {
            _webSocketConnections.Remove(connectionId);
            Console.WriteLine($"Removed WebSocket connection from ClipboardService: {connectionId}");
            
            // 如果没有连接了，停止监控剪切板
            if (_webSocketConnections.Count == 0)
            {
                StopClipboardMonitoring();
            }
        }

        private void StartClipboardMonitoring()
        {
            try
            {
                // 获取初始剪切板内容
                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (Clipboard.ContainsText())
                    {
                        _lastClipboardText = Clipboard.GetText();
                    }
                });

                // 每500毫秒检查一次剪切板变化
                _clipboardMonitorTimer = new Timer(async _ =>
                {
                    await CheckClipboardChanges();
                }, null, TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(500));

                Console.WriteLine("剪切板监控已启动");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"启动剪切板监控失败: {ex.Message}");
            }
        }

        private void StopClipboardMonitoring()
        {
            try
            {
                _clipboardMonitorTimer?.Dispose();
                _clipboardMonitorTimer = null;
                Console.WriteLine("剪切板监控已停止");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"停止剪切板监控失败: {ex.Message}");
            }
        }

        private async Task CheckClipboardChanges()
        {
            try
            {
                string currentText = "";
                
                // 在UI线程中获取剪切板内容
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    if (Clipboard.ContainsText())
                    {
                        currentText = Clipboard.GetText();
                    }
                });

                // 检查是否有变化
                if (!string.IsNullOrEmpty(currentText) && currentText != _lastClipboardText)
                {
                    _lastClipboardText = currentText;
                    await SendClipboardTextEvent(currentText);
                    Console.WriteLine($"检测到剪切板变化: {currentText.Substring(0, Math.Min(50, currentText.Length))}...");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"检查剪切板变化失败: {ex.Message}");
            }
        }

        private async Task SendClipboardTextEvent(string text)
        {
            if (_webSocketConnections.Count == 0) return;

            try
            {
                var message = new WebSocketMessage
                {
                    Id = GenerateRandomId(),
                    Payload = new ClipboardTextPayload { Text = text },
                    Type = "clipboard-text-event",
                    WsEventType = "clipboard-text-event"
                };

                await SendToAllWebSockets(message);
                Console.WriteLine($"剪切板文本事件已发送到所有WebSocket客户端");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"发送剪切板文本事件失败: {ex.Message}");
            }
        }

        private async Task SendToAllWebSockets(WebSocketMessage message)
        {
            var json = JsonConvert.SerializeObject(message);
            var bytes = Encoding.UTF8.GetBytes(json);

            var tasks = new List<Task>();
            foreach (var kvp in _webSocketConnections.ToArray())
            {
                if (kvp.Value.State == WebSocketState.Open)
                {
                    tasks.Add(kvp.Value.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None));
                }
            }

            if (tasks.Count > 0)
            {
                await Task.WhenAll(tasks);
            }
        }

        private string GenerateRandomId()
        {
            // 生成类似 "3FXYLSDONwIZuYYMGkCNv" 的随机ID
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var random = new Random();
            var result = new char[21];
            
            for (int i = 0; i < 21; i++)
            {
                result[i] = chars[random.Next(chars.Length)];
            }
            
            return new string(result);
        }

        public void StartMonitoring(string connectionId)
        {
            try
            {
                Console.WriteLine($"Started clipboard monitoring for connection: {connectionId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error starting clipboard monitoring: {ex.Message}");
            }
        }

        public void StopMonitoring(string connectionId)
        {
            try
            {
                Console.WriteLine($"Stopped clipboard monitoring for connection: {connectionId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error stopping clipboard monitoring: {ex.Message}");
            }
        }
    }
}