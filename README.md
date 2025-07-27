# Audio Capture App - Windows版本

这是一个Windows平台下的音频捕获应用程序，使用C#和WPF开发。

## 功能特性

### HTTP API接口
- `GET /health` - 健康检查接口
- `GET /config` - 配置信息接口  
- `GET /` - 服务状态页面

### WebSocket接口
- `/ws` - 主WebSocket端点
- `/audio` - 音频WebSocket端点

### 核心功能
- 系统音频捕获（使用WASAPI）
- 麦克风音频捕获
- 屏幕截图功能
- 剪贴板监控
- 实时音频数据传输
- 系统托盘集成

### 权限要求
- 麦克风访问权限
- 屏幕录制权限（可能需要管理员权限）

## 构建和运行

### 自动构建
```bash
./build_app.bat
```

### 手动构建
```bash
dotnet restore
dotnet build --configuration Release
dotnet run
```

### 发布
```bash
dotnet publish --configuration Release --output ./publish --self-contained true --runtime win-x64
```

## 服务端口
- HTTP服务：端口 9047
- HTTPS服务：端口 9048

## 网络访问
- 本地访问：http://localhost:9047
- 局域网访问：http://[本机IP]:9047

## 技术栈
- .NET 8.0
- WPF (Windows Presentation Foundation)
- ASP.NET Core
- SignalR (WebSocket)
- NAudio (音频处理)
- System.Drawing (屏幕截图)

## 使用说明

1. 运行应用程序
2. 应用会自动启动HTTP服务器和WebSocket服务器
3. 可以通过浏览器访问 http://localhost:9047 查看服务状态
4. 使用WebSocket客户端连接到 ws://localhost:9047/ws 进行实时通信
5. 应用支持最小化到系统托盘

## API使用示例

### 健康检查
```bash
curl http://localhost:9047/health
```

### 获取配置
```bash
curl http://localhost:9047/config
```

### WebSocket连接示例（JavaScript）
```javascript
const ws = new WebSocket('ws://localhost:9047/ws');

ws.onmessage = function(event) {
    const data = JSON.parse(event.data);
    console.log('Received:', data);
};

// 发送截图命令
ws.send(JSON.stringify({
    type: "client-screenshot-command",
    wsEventType: "client-screenshot-command",
    payload: "screenshot",
    id: "cmd-" + Date.now()
}));
```

## 项目结构