# 迷你世界多服务器客户端

这是一个用于管理游戏多服务器的客户端程序，可以连接到主服务器并接受启动、停止服务器等命令。

## 功能特性

- 🔌 **SignalR 连接**: 连接到主服务器，接收实时命令
- 🎮 **服务器管理**: 启动、停止、查询 TShock 游戏服务器
- 🗺️ **地图管理**: 自动复制默认地图，支持地图复用
- 🔧 **端口管理**: 自动分配可用端口
- 📊 **状态监控**: 实时监控节点和服务器状态
- 💬 **控制台管理**: 本地命令行界面管理
- 📝 **日志记录**: 详细的日志记录和存储

## 项目结构

```
MiniWorldClient/
├── Models/                    # 数据模型
│   ├── ServerSettings.cs     # 服务器配置
│   ├── GameServerInfo.cs     # 游戏服务器信息
│   └── Commands.cs           # 命令模型
├── Services/                  # 业务服务
│   ├── GameServerManager.cs  # 游戏服务器管理
│   ├── SignalRClientService.cs # SignalR客户端
│   ├── NodeInfoService.cs    # 节点信息服务
│   └── ConsoleCommandService.cs # 控制台命令服务
├── Maps/                     # 地图存储目录
│   └── DefaultMap/           # 默认地图目录
├── Logs/                     # 日志存储目录
├── TShock/                   # TShock服务器程序目录
├── appsettings.json         # 配置文件
└── Program.cs               # 主程序入口
```

## 安装和配置

### 1. 环境要求

- .NET 9.0 或更高版本
- Windows 10/11 (支持性能计数器)
- TShock 服务器程序

### 2. 配置文件

编辑 `appsettings.json` 文件：

```json
{
  "ServerSettings": {
    "HubUrl": "https://your-main-server.com/serverhub",
    "NodeId": "Node1",
    "NodeName": "游戏服务器节点1",
    "DefaultMapPath": "Maps/DefaultMap",
    "MapsDirectory": "Maps",
    "LogsDirectory": "Logs",
    "TShockPath": "TShock/TerrariaServer.exe",
    "BasePort": 7777,
    "MaxRetryAttempts": 3,
    "RetryDelaySeconds": 5
  }
}
```

### 3. 目录准备

```bash
# 创建必要的目录
mkdir Maps
mkdir Maps\DefaultMap
mkdir Logs
mkdir TShock

# 将默认地图文件放置到 Maps/DefaultMap/ 目录
# 将TShock服务器程序放置到 TShock/ 目录
```

### 4. 运行程序

```bash
dotnet run
```

## 使用方法

### 控制台命令

程序启动后，可以使用以下命令：

| 命令                | 说明               | 示例                                   |
| ------------------- | ------------------ | -------------------------------------- |
| `help`, `h`         | 显示帮助信息       | `help`                                 |
| `status`, `s`       | 显示节点状态       | `status`                               |
| `list`, `l`         | 显示服务器列表     | `list`                                 |
| `start <id> <map>`  | 启动服务器         | `start server1 world1`                 |
| `stop <id> [force]` | 停止服务器         | `stop server1` 或 `stop server1 force` |
| `info <id>`         | 显示服务器详细信息 | `info server1`                         |
| `node`              | 显示节点信息       | `node`                                 |
| `clear`, `cls`      | 清屏               | `clear`                                |
| `exit`, `quit`, `q` | 退出程序           | `exit`                                 |

### SignalR 命令

主服务器可以发送以下命令：

#### 启动服务器

```json
{
  "ServerId": "server1",
  "ServerName": "测试服务器",
  "MapName": "world1",
  "MaxPlayers": 8,
  "Password": "123456",
  "AdditionalArgs": {}
}
```

#### 停止服务器

```json
{
  "ServerId": "server1",
  "ForceStop": false
}
```

#### 查询服务器

```json
{
  "ServerId": null // null表示查询所有服务器
}
```

## 工作流程

### 启动服务器流程

1. 收到启动命令
2. 检查地图文件（存在则使用，不存在则复制默认地图）
3. 分配可用端口
4. 生成日志文件路径
5. 构建 TShock 启动参数
6. 启动 TShock 进程
7. 监控进程状态
8. 向主服务器报告启动状态

### 地图管理

- 默认地图位置：`Maps/DefaultMap/`
- 新地图存储：`Maps/{MapName}.wld`
- 如果指定名称的地图已存在，直接使用
- 如果不存在，从默认地图复制

### 端口分配

- 从基础端口开始（默认 7777）
- 依次向上查找可用端口
- 支持最多 100 个端口范围

## 故障排除

### 常见问题

1. **无法连接到主服务器**

   - 检查网络连接
   - 确认服务器地址配置正确
   - 检查防火墙设置

2. **TShock 启动失败**

   - 确认 TShock 程序路径正确
   - 检查地图文件是否存在
   - 查看日志文件了解详细错误

3. **端口冲突**

   - 程序会自动查找可用端口
   - 如果所有端口都被占用，会启动失败

4. **性能计数器错误**
   - Windows 系统可能需要管理员权限
   - 系统会自动使用替代方法获取性能信息

### 日志查看

- 应用程序日志：控制台输出
- 服务器日志：`Logs/{ServerId}_{timestamp}.log`

## 开发说明

### 项目架构

- **依赖注入**: 使用 Microsoft.Extensions.DependencyInjection
- **配置管理**: 使用 Microsoft.Extensions.Configuration
- **日志系统**: 使用 Microsoft.Extensions.Logging
- **后台服务**: 使用 Microsoft.Extensions.Hosting
- **实时通信**: 使用 Microsoft.AspNetCore.SignalR.Client

### 扩展开发

1. **添加新命令**：在`SignalRClientService`中注册新的事件处理器
2. **扩展服务器参数**：修改`StartServerCommand`模型
3. **添加新的监控指标**：扩展`NodeInfoService`
4. **自定义日志格式**：修改`GameServerManager`中的日志处理

## 许可证

本项目采用 MIT 许可证。
