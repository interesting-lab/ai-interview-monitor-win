using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AudioCaptureApp.Services
{
    public class ScreenshotService
    {
        public async Task<string> TakeScreenshotAsync()
        {
            try
            {
                await Task.Run(() => { }); // 确保异步执行
                
                // 获取主屏幕的边界
                var bounds = Screen.PrimaryScreen?.Bounds ?? new Rectangle(0, 0, 1920, 1080);
                
                using var bitmap = new Bitmap(bounds.Width, bounds.Height);
                using var graphics = Graphics.FromImage(bitmap);
                
                // 截取屏幕
                graphics.CopyFromScreen(bounds.X, bounds.Y, 0, 0, bounds.Size, CopyPixelOperation.SourceCopy);
                
                // 转换为base64
                using var stream = new MemoryStream();
                bitmap.Save(stream, ImageFormat.Jpeg);
                var imageBytes = stream.ToArray();
                var base64String = Convert.ToBase64String(imageBytes);
                
                Console.WriteLine($"截图完成，大小: {imageBytes.Length} bytes");
                return $"data:image/jpeg;base64,{base64String}";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"截图失败: {ex.Message}");
                return "";
            }
        }
    }
}