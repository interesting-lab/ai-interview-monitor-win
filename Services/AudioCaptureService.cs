using NAudio.Wave;
using NAudio.CoreAudioApi;
using Microsoft.AspNetCore.SignalR;
using AudioCaptureApp.Hubs;
using AudioCaptureApp.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System.Net.WebSockets;
using System.Text;
using System.Linq;

namespace AudioCaptureApp.Services
{
    public class AudioCaptureService
    {
        private readonly IHubContext<AudioHub> _hubContext;
        private readonly Dictionary<string, bool> _activeConnections = new();
        private readonly Dictionary<string, WebSocket> _webSocketConnections = new();
        private WaveInEvent? _microphoneCapture;
        private WasapiLoopbackCapture? _systemAudioCapture;
        private Timer? _testDataTimer;

        public AudioCaptureService(IHubContext<AudioHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public void AddWebSocketConnection(string connectionId, WebSocket webSocket)
        {
            _webSocketConnections[connectionId] = webSocket;
            Console.WriteLine($"Added WebSocket connection: {connectionId}");
        }

        public void RemoveWebSocketConnection(string connectionId)
        {
            _webSocketConnections.Remove(connectionId);
            Console.WriteLine($"Removed WebSocket connection: {connectionId}");
        }

        public async Task SendScreenshotToAllClients(string base64Image)
        {
            if (_webSocketConnections.Count == 0) return;

            try
            {
                var message = new WebSocketMessage
                {
                    Id = GenerateRandomId(),
                    Payload = new ScreenshotPayload
                    {
                        Base64 = base64Image
                    },
                    Type = null,
                    WsEventType = "clipboard-image-event"
                };

                await SendToAllWebSockets(message);
                Console.WriteLine("截图已发送到所有WebSocket客户端");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"发送截图失败: {ex.Message}");
            }
        }

        public async Task SendKeydownEventToAllClients()
        {
            if (_webSocketConnections.Count == 0) return;

            try
            {
                var message = new WebSocketMessage
                {
                    Id = GenerateRandomId(),
                    Payload = new KeyboardEventPayload
                    {
                        KeyEventType = "primary"
                    },
                    Type = "keydown-event",
                    WsEventType = "keydown-event"
                };

                await SendToAllWebSockets(message);
                Console.WriteLine("键盘事件已发送到所有WebSocket客户端");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"发送键盘事件失败: {ex.Message}");
            }
        }

        public void StartCapture(string connectionId)
        {
            try
            {
                _activeConnections[connectionId] = true;
                Console.WriteLine($"Started capture for connection: {connectionId}");

                // 如果是第一个连接，开始音频捕获
                if (_activeConnections.Count == 1)
                {
                    StartMicrophoneCapture();
                    StartSystemAudioCapture();
                    StartTestDataTimer(); // 临时添加测试数据定时器
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error starting capture: {ex.Message}");
            }
        }

        public void StopCapture(string connectionId)
        {
            try
            {
                _activeConnections.Remove(connectionId);
                Console.WriteLine($"Stopped capture for connection: {connectionId}");

                // 如果没有活跃连接，停止音频捕获
                if (_activeConnections.Count == 0)
                {
                    StopMicrophoneCapture();
                    StopSystemAudioCapture();
                    StopTestDataTimer();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error stopping capture: {ex.Message}");
            }
        }

        private void StartTestDataTimer()
        {
            // 每500毫秒发送一次测试数据
            _testDataTimer = new Timer(async _ =>
            {
                await SendTestAudioDataPeriodically();
            }, null, TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(500));
            
            Console.WriteLine("Test data timer started");
        }

        private void StopTestDataTimer()
        {
            _testDataTimer?.Dispose();
            _testDataTimer = null;
            Console.WriteLine("Test data timer stopped");
        }

        private async Task SendTestAudioDataPeriodically()
        {
            if (_webSocketConnections.Count == 0) return;

            try
            {
                var random = new Random();
                
                // 生成随机的麦克风数据
                var micData = new double[100];
                for (int i = 0; i < micData.Length; i++)
                {
                    micData[i] = (random.NextDouble() - 0.5) * 0.01; // 小幅度随机数据
                }

                // 生成随机的系统音频数据
                var systemData = new double[100];
                for (int i = 0; i < systemData.Length; i++)
                {
                    systemData[i] = (random.NextDouble() - 0.5) * 0.005; // 更小幅度的随机数据
                }

                var micMessage = new WebSocketMessage
                {
                    Id = GenerateRandomId(),
                    Payload = new AudioDataPayload
                    {
                        AudioType = "mic",
                        Data = micData
                    },
                    Type = null,
                    WsEventType = "audio-data-event"
                };

                var systemMessage = new WebSocketMessage
                {
                    Id = GenerateRandomId(),
                    Payload = new AudioDataPayload
                    {
                        AudioType = "system",
                        Data = systemData
                    },
                    Type = null,
                    WsEventType = "audio-data-event"
                };

                await SendToAllWebSockets(micMessage);
                await SendToAllWebSockets(systemMessage);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending periodic test data: {ex.Message}");
            }
        }

        private void StartMicrophoneCapture()
        {
            try
            {
                _microphoneCapture = new WaveInEvent
                {
                    WaveFormat = new WaveFormat(48000, 1) // 48kHz, mono
                };

                _microphoneCapture.DataAvailable += async (sender, e) =>
                {
                    await ProcessMicrophoneData(e.Buffer, e.BytesRecorded);
                };

                _microphoneCapture.StartRecording();
                Console.WriteLine("Microphone capture started with 48kHz sample rate");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error starting microphone capture: {ex.Message}");
            }
        }

        private void StartSystemAudioCapture()
        {
            try
            {
                _systemAudioCapture = new WasapiLoopbackCapture();
                
                // 设置系统音频采样率为16kHz
                _systemAudioCapture.WaveFormat = new WaveFormat(16000, 1);

                _systemAudioCapture.DataAvailable += async (sender, e) =>
                {
                    await ProcessSystemAudioData(e.Buffer, e.BytesRecorded);
                };

                _systemAudioCapture.StartRecording();
                Console.WriteLine("System audio capture started with 16kHz sample rate");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error starting system audio capture: {ex.Message}");
            }
        }

        private void StopMicrophoneCapture()
        {
            try
            {
                _microphoneCapture?.StopRecording();
                _microphoneCapture?.Dispose();
                _microphoneCapture = null;
                Console.WriteLine("Microphone capture stopped");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error stopping microphone capture: {ex.Message}");
            }
        }

        private void StopSystemAudioCapture()
        {
            try
            {
                _systemAudioCapture?.StopRecording();
                _systemAudioCapture?.Dispose();
                _systemAudioCapture = null;
                Console.WriteLine("System audio capture stopped");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error stopping system audio capture: {ex.Message}");
            }
        }

        private async Task ProcessMicrophoneData(byte[] buffer, int bytesRecorded)
        {
            if (_webSocketConnections.Count == 0) return;

            try
            {
                // 将字节数据转换为浮点数组
                var audioData = ConvertBytesToFloatArray(buffer, bytesRecorded);

                var message = new WebSocketMessage
                {
                    Id = GenerateRandomId(),
                    Payload = new AudioDataPayload
                    {
                        AudioType = "mic",
                        Data = audioData
                    },
                    Type = null,
                    WsEventType = "audio-data-event"
                };

                await SendToAllWebSockets(message);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing microphone data: {ex.Message}");
            }
        }

        private async Task ProcessSystemAudioData(byte[] buffer, int bytesRecorded)
        {
            if (_webSocketConnections.Count == 0) return;

            try
            {
                // 将字节数据转换为浮点数组
                var audioData = ConvertBytesToFloatArray(buffer, bytesRecorded);

                var message = new WebSocketMessage
                {
                    Id = GenerateRandomId(),
                    Payload = new AudioDataPayload
                    {
                        AudioType = "system",
                        Data = audioData
                    },
                    Type = null,
                    WsEventType = "audio-data-event"
                };

                await SendToAllWebSockets(message);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing system audio data: {ex.Message}");
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

        private double[] ConvertBytesToFloatArray(byte[] buffer, int bytesRecorded)
        {
            // 假设是16位PCM音频数据
            var sampleCount = bytesRecorded / 2;
            var audioData = new double[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                // 将16位PCM转换为-1.0到1.0的浮点数
                short sample = BitConverter.ToInt16(buffer, i * 2);
                audioData[i] = sample / 32768.0; // 归一化到-1.0到1.0
            }

            return audioData;
        }

        private string GenerateRandomId()
        {
            // 生成类似 "GAocFtaxX6X2Lc_xAi8Ev" 的随机ID
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789_";
            var random = new Random();
            var result = new char[21];
            
            for (int i = 0; i < 21; i++)
            {
                result[i] = chars[random.Next(chars.Length)];
            }
            
            return new string(result);
        }

        public async Task SendTestAudioData()
        {
            if (_webSocketConnections.Count == 0) return;

            try
            {
                // 发送测试麦克风数据
                var micMessage = new WebSocketMessage
                {
                    Id = GenerateRandomId(),
                    Payload = new AudioDataPayload
                    {
                        AudioType = "mic",
                        Data = new double[] { -0.0028614969924092293, -0.0029907075222581625, -0.0030055076349526644 }
                    },
                    Type = null,
                    WsEventType = "audio-data-event"
                };

                // 发送测试系统音频数据
                var systemMessage = new WebSocketMessage
                {
                    Id = GenerateRandomId(),
                    Payload = new AudioDataPayload
                    {
                        AudioType = "system",
                        Data = new double[] { 0.0, 0.0, 0.0, 0.0 }
                    },
                    Type = null,
                    WsEventType = "audio-data-event"
                };

                await SendToAllWebSockets(micMessage);
                await SendToAllWebSockets(systemMessage);
                
                Console.WriteLine("Test audio data sent");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending test audio data: {ex.Message}");
            }
        }
    }
}