using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using AudioCaptureApp.Services;
using AudioCaptureApp.Hubs;
using AudioCaptureApp.Middleware;
using System;
using System.Linq;

namespace AudioCaptureApp
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.AddSignalR(options =>
            {
                options.EnableDetailedErrors = true;
            });

            // 注册服务 - 注意依赖顺序
            services.AddSingleton<ConfigService>(); // 配置服务优先注册
            services.AddSingleton<HttpsService>(); // 添加HTTPS服务
            services.AddSingleton<AudioCaptureService>();
            services.AddSingleton<ScreenshotService>();
            services.AddSingleton<DeviceInfoService>();
            services.AddSingleton<HotkeyService>();
            // ClipboardService依赖AudioCaptureService，所以要在之后注册
            services.AddSingleton<ClipboardService>();

            // 配置CORS
            services.AddCors(options =>
            {
                options.AddDefaultPolicy(builder =>
                {
                    builder.AllowAnyOrigin()
                           .AllowAnyMethod()
                           .AllowAnyHeader();
                });
            });

            // 配置HTTPS重定向
            services.AddHttpsRedirection(options =>
            {
                options.RedirectStatusCode = StatusCodes.Status307TemporaryRedirect;
                options.HttpsPort = 9048;
            });

            // 配置日志
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Information);
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILogger<Startup> logger)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            // 启用HTTPS重定向
            app.UseHttpsRedirection();

            // 启用CORS
            app.UseCors();

            // 启用WebSocket支持
            app.UseWebSockets();

            // 添加请求日志
            app.Use(async (context, next) =>
            {
                var scheme = context.Request.IsHttps ? "HTTPS" : "HTTP";
                logger.LogInformation($"Request: {scheme} {context.Request.Method} {context.Request.Path}");
                logger.LogInformation($"Headers: {string.Join(", ", context.Request.Headers.Select(h => $"{h.Key}={h.Value}"))}");
                await next();
            });

            // 使用WebSocket中间件
            app.UseMiddleware<WebSocketMiddleware>();

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHub<AudioHub>("/audio");

                // 默认路由 - 移除了/config路由，让ApiController处理
                endpoints.MapGet("/", async context =>
                {
                    var scheme = context.Request.IsHttps ? "HTTPS" : "HTTP";
                    await context.Response.WriteAsync($"Audio Capture Service is running on {scheme}");
                });
            });

            logger.LogInformation("Application configured successfully with HTTPS support");
            
            // 记录SSL证书信息
            var httpsService = app.ApplicationServices.GetService<HttpsService>();
            if (httpsService != null)
            {
                logger.LogInformation(httpsService.GetCertificateInfo());
            }
        }
    }
}