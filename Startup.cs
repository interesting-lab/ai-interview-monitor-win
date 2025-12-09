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
                    builder
                        .SetIsOriginAllowed(_ => true) // allow any origin explicitly (no wildcard to support private network)
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials()
                        .WithExposedHeaders("Access-Control-Allow-Private-Network");
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

            // CORS（默认策略）
            app.UseCors();

            // 私网请求兼容（放在UseCors之后、Endpoints之前，避免覆盖）
            app.Use(async (context, next) =>
            {
                var origin = context.Request.Headers["Origin"].ToString();
                var isPreflight = string.Equals(context.Request.Method, "OPTIONS", StringComparison.OrdinalIgnoreCase);
                var reqPrivateNetwork = context.Request.Headers["Access-Control-Request-Private-Network"].ToString();

                // 明确允许私网
                context.Response.Headers["Access-Control-Allow-Private-Network"] = "true";

                // 对所有跨域请求回写 Origin，避免被判定为无地址空间
                if (!string.IsNullOrEmpty(origin))
                {
                    context.Response.Headers["Access-Control-Allow-Origin"] = origin;
                    context.Response.Headers["Vary"] = "Origin";
                    context.Response.Headers["Access-Control-Allow-Credentials"] = "true";
                }

                if (isPreflight)
                {
                    var reqMethods = context.Request.Headers["Access-Control-Request-Method"];
                    var reqHeaders = context.Request.Headers["Access-Control-Request-Headers"];
                    if (!string.IsNullOrEmpty(reqMethods))
                        context.Response.Headers["Access-Control-Allow-Methods"] = reqMethods;
                    if (!string.IsNullOrEmpty(reqHeaders))
                        context.Response.Headers["Access-Control-Allow-Headers"] = reqHeaders;

                    // 如果浏览器传了 Access-Control-Request-Private-Network，则显式响应
                    if (!string.IsNullOrEmpty(reqPrivateNetwork))
                    {
                        context.Response.Headers["Access-Control-Allow-Private-Network"] = "true";
                    }

                    context.Response.StatusCode = StatusCodes.Status204NoContent;
                    return;
                }

                await next();
            });

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHub<AudioHub>("/audio");
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