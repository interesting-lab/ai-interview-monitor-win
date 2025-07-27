using System;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;

namespace AudioCaptureApp.Services
{
    public class HotkeyConfig
    {
        public string ScreenshotHotkey { get; set; } = "Shift+Ctrl+Space";
        public string KeydownEventHotkey { get; set; } = "Ctrl+Shift+Enter";
    }

    public class AppConfig
    {
        public HotkeyConfig Hotkeys { get; set; } = new HotkeyConfig();
    }

    public class ConfigService
    {
        private readonly string _configPath;
        private AppConfig _config;

        public ConfigService()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var appFolder = Path.Combine(appDataPath, "AudioCaptureApp");
            Directory.CreateDirectory(appFolder);
            _configPath = Path.Combine(appFolder, "config.json");
            LoadConfig();
        }

        public AppConfig GetConfig()
        {
            return _config;
        }

        public void SaveConfig(AppConfig config)
        {
            try
            {
                _config = config;
                var json = JsonSerializer.Serialize(config, new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });
                File.WriteAllText(_configPath, json);
                Console.WriteLine($"配置已保存到: {_configPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"保存配置失败: {ex.Message}");
            }
        }

        private void LoadConfig()
        {
            try
            {
                if (File.Exists(_configPath))
                {
                    var json = File.ReadAllText(_configPath);
                    _config = JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
                    Console.WriteLine($"配置已从 {_configPath} 加载");
                }
                else
                {
                    _config = new AppConfig();
                    SaveConfig(_config); // 创建默认配置文件
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"加载配置失败: {ex.Message}，使用默认配置");
                _config = new AppConfig();
            }
        }

        public static Dictionary<string, uint> GetKeyCodeMap()
        {
            return new Dictionary<string, uint>
            {
                { "Space", 0x20 },
                { "Enter", 0x0D },
                { "F1", 0x70 },
                { "F2", 0x71 },
                { "F3", 0x72 },
                { "F4", 0x73 },
                { "F5", 0x74 },
                { "F6", 0x75 },
                { "F7", 0x76 },
                { "F8", 0x77 },
                { "F9", 0x78 },
                { "F10", 0x79 },
                { "F11", 0x7A },
                { "F12", 0x7B },
                { "A", 0x41 },
                { "B", 0x42 },
                { "C", 0x43 },
                { "D", 0x44 },
                { "E", 0x45 },
                { "F", 0x46 },
                { "G", 0x47 },
                { "H", 0x48 },
                { "I", 0x49 },
                { "J", 0x4A },
                { "K", 0x4B },
                { "L", 0x4C },
                { "M", 0x4D },
                { "N", 0x4E },
                { "O", 0x4F },
                { "P", 0x50 },
                { "Q", 0x51 },
                { "R", 0x52 },
                { "S", 0x53 },
                { "T", 0x54 },
                { "U", 0x55 },
                { "V", 0x56 },
                { "W", 0x57 },
                { "X", 0x58 },
                { "Y", 0x59 },
                { "Z", 0x5A }
            };
        }

        public static (uint modifiers, uint key) ParseHotkey(string hotkeyString)
        {
            uint modifiers = 0;
            uint key = 0;
            
            var parts = hotkeyString.Split('+');
            var keyCodeMap = GetKeyCodeMap();
            
            foreach (var part in parts)
            {
                var trimmedPart = part.Trim();
                switch (trimmedPart.ToLower())
                {
                    case "ctrl":
                        modifiers |= 0x0002; // MOD_CTRL
                        break;
                    case "shift":
                        modifiers |= 0x0004; // MOD_SHIFT
                        break;
                    case "alt":
                        modifiers |= 0x0001; // MOD_ALT
                        break;
                    default:
                        if (keyCodeMap.ContainsKey(trimmedPart))
                        {
                            key = keyCodeMap[trimmedPart];
                        }
                        break;
                }
            }
            
            return (modifiers, key);
        }
    }
}