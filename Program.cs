using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using System.Windows;
using AudioCaptureApp.Services;

namespace AudioCaptureApp
{
    public class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            var app = new App();
            app.Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                    webBuilder.UseUrls("http://0.0.0.0:9047", "https://0.0.0.0:9048"); // HTTP和HTTPS端口
                    webBuilder.ConfigureKestrel((context, options) =>
                    {
                        options.ListenAnyIP(9047); // HTTP端口
                        options.ListenAnyIP(9048, listenOptions =>
                        {
                            // HTTPS配置 - 延迟配置证书
                            listenOptions.UseHttps(httpsOptions =>
                            {
                                httpsOptions.ServerCertificateSelector = (connectionContext, name) =>
                                {
                                    // 创建一个临时的logger factory来获取证书
                                    using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
                                    var logger = loggerFactory.CreateLogger<HttpsService>();
                                    var httpsService = new HttpsService(logger);
                                    return httpsService.GetOrCreateCertificate();
                                };
                            });
                        });
                    });
                });
    }
}