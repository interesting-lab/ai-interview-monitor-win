using AudioCaptureApp.Models;
using System;

namespace AudioCaptureApp.Services
{
    public class DeviceInfoService
    {
        public DeviceInfoService()
        {
            // 无参构造函数，确保依赖注入正常工作
        }

        public DeviceInfo GetDeviceInfo()
        {
            try
            {
                var deviceInfo = new DeviceInfo
                {
                    Platform = "windows",
                    Version = "1.0.0",
                    Build = "1"
                };

                // 获取计算机名称
                deviceInfo.Name = Environment.MachineName ?? "Unknown";
                deviceInfo.Id = Environment.MachineName ?? "Unknown";

                return deviceInfo;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting device info: {ex.Message}");
                
                // 返回默认值
                return new DeviceInfo
                {
                    Platform = "windows",
                    Version = "1.0.0",
                    Build = "1",
                    Name = "Unknown",
                    Id = "Unknown"
                };
            }
        }
    }
}