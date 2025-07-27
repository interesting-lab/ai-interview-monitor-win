using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;

namespace AudioCaptureApp.Services
{
    public class HttpsService
    {
        private readonly ILogger<HttpsService> _logger;
        private const string CertificatePassword = "AudioCaptureApp2024";
        private const string CertificateFileName = "localhost.pfx";

        public HttpsService(ILogger<HttpsService> logger)
        {
            _logger = logger;
        }

        public X509Certificate2 GetOrCreateCertificate()
        {
            var certPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, CertificateFileName);

            try
            {
                // 如果证书文件存在，尝试加载
                if (File.Exists(certPath))
                {
                    var existingCert = new X509Certificate2(certPath, CertificatePassword);
                    
                    // 检查证书是否即将过期（30天内）
                    if (existingCert.NotAfter > DateTime.Now.AddDays(30))
                    {
                        _logger.LogInformation("使用现有SSL证书，有效期至: {ExpiryDate}", existingCert.NotAfter);
                        return existingCert;
                    }
                    else
                    {
                        _logger.LogWarning("SSL证书即将过期，正在重新生成...");
                        File.Delete(certPath);
                    }
                }

                // 创建新的自签名证书
                _logger.LogInformation("正在生成新的SSL自签名证书...");
                var newCert = CreateSelfSignedCertificate(certPath);
                _logger.LogInformation("SSL证书生成成功，有效期至: {ExpiryDate}", newCert.NotAfter);
                
                return newCert;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SSL证书处理失败");
                throw;
            }
        }

        private X509Certificate2 CreateSelfSignedCertificate(string certPath)
        {
            using (var rsa = RSA.Create(2048))
            {
                var request = new CertificateRequest(
                    "CN=localhost, O=AudioCaptureApp, OU=Development, C=US",
                    rsa,
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1);

                // 添加密钥用途扩展
                request.CertificateExtensions.Add(
                    new X509KeyUsageExtension(
                        X509KeyUsageFlags.DataEncipherment | 
                        X509KeyUsageFlags.KeyEncipherment | 
                        X509KeyUsageFlags.DigitalSignature,
                        false));

                // 添加增强密钥用途扩展（服务器身份验证）
                request.CertificateExtensions.Add(
                    new X509EnhancedKeyUsageExtension(
                        new OidCollection { new Oid("1.3.6.1.5.5.7.3.1") }, // Server Authentication
                        false));

                // 添加Subject Alternative Names
                var sanBuilder = new SubjectAlternativeNameBuilder();
                sanBuilder.AddDnsName("localhost");
                sanBuilder.AddDnsName("127.0.0.1");
                sanBuilder.AddIpAddress(System.Net.IPAddress.Loopback);
                sanBuilder.AddIpAddress(System.Net.IPAddress.IPv6Loopback);
                
                // 添加本机所有IP地址
                try
                {
                    var networkInterfaces = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces();
                    foreach (var ni in networkInterfaces)
                    {
                        if (ni.OperationalStatus == System.Net.NetworkInformation.OperationalStatus.Up)
                        {
                            var ipProperties = ni.GetIPProperties();
                            foreach (var addr in ipProperties.UnicastAddresses)
                            {
                                if (addr.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork &&
                                    !addr.Address.ToString().StartsWith("127."))
                                {
                                    sanBuilder.AddIpAddress(addr.Address);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "无法获取网络接口信息，使用默认配置");
                }

                request.CertificateExtensions.Add(sanBuilder.Build());

                // 创建证书（有效期1年）
                var certificate = request.CreateSelfSigned(
                    DateTimeOffset.Now.AddDays(-1),
                    DateTimeOffset.Now.AddYears(1));

                // 导出为PFX格式并保存
                var pfxData = certificate.Export(X509ContentType.Pfx, CertificatePassword);
                File.WriteAllBytes(certPath, pfxData);

                return new X509Certificate2(certPath, CertificatePassword);
            }
        }

        public string GetCertificateInfo()
        {
            try
            {
                var cert = GetOrCreateCertificate();
                return $"SSL证书信息 - 主题: {cert.Subject}, 有效期: {cert.NotBefore:yyyy-MM-dd} 至 {cert.NotAfter:yyyy-MM-dd}";
            }
            catch (Exception ex)
            {
                return $"SSL证书信息获取失败: {ex.Message}";
            }
        }
    }
}