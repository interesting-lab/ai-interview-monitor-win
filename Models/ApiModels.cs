using Newtonsoft.Json;
using System;

namespace AudioCaptureApp.Models
{
    public class ApiResponse<T>
    {
        [JsonProperty("data")]
        public T? Data { get; set; }

        [JsonProperty("success")]
        public bool Success { get; set; }
    }

    public class DeviceInfo
    {
        [JsonProperty("build")]
        public string Build { get; set; } = "1";

        [JsonProperty("id")]
        public string Id { get; set; } = "";

        [JsonProperty("name")]
        public string Name { get; set; } = "";

        [JsonProperty("platform")]
        public string Platform { get; set; } = "windows";

        [JsonProperty("version")]
        public string Version { get; set; } = "1.0.0";
    }

    public class WebSocketMessage
    {
        [JsonProperty("id")]
        public string Id { get; set; } = "";

        [JsonProperty("payload")]
        public object? Payload { get; set; }

        [JsonProperty("type")]
        public string? Type { get; set; }

        [JsonProperty("wsEventType")]
        public string WsEventType { get; set; } = "";
    }

    public class AudioDataPayload
    {
        [JsonProperty("audioType")]
        public string AudioType { get; set; } = "";

        [JsonProperty("data")]
        public double[] Data { get; set; } = Array.Empty<double>();
    }

    public class ScreenshotPayload
    {
        [JsonProperty("base64")]
        public string Base64 { get; set; } = "";
    }

    public class ClipboardTextPayload
    {
        [JsonProperty("text")]
        public string Text { get; set; } = "";
    }

    public class KeyboardEventPayload
    {
        [JsonProperty("keyEventType")]
        public string KeyEventType { get; set; } = "";
    }
}