# MiniWorldPlugin 重构说明

## 重构概述

根据 MiniWorld.Shared/Architecture.md 中定义的新架构，完成了 MiniWorldPlugin 的全面重构。

## 主要变更

### 1. 通信方式改变

- **之前**: 使用 WebSocket (WatsonWebsocket) 作为服务器接收节点连接
- **现在**: 使用 StreamJsonRpc + HttpClient 作为客户端连接到节点的 RPC 服务

### 2. 项目依赖更新

- 移除: `WatsonWebsocket` 依赖
- 新增: `StreamJsonRpc` 和 `MiniWorld.Shared` 项目引用

### 3. 配置变更

- **之前**: `HubUrl` - WebSocket 服务器监听地址
- **现在**: `RpcUrl` - 节点 RPC 服务器地址 (默认: `http://localhost:8080/rpc/`)

### 4. 架构简化

- **之前**: 支持多节点管理，复杂的连接管理
- **现在**: 单节点架构，简化的节点状态管理

### 5. 删除的文件

- `MiniWorldPlugin/MWServer.cs` - WebSocket 服务器实现
- `MiniWorldPlugin/Models/MiniWorld.cs` - 本地模型定义
- `MiniWorldPlugin/Models/NodeInfo.cs` - 本地模型定义

### 6. 新增的文件

- `MiniWorldPlugin/Services/RpcClientService.cs` - RPC 客户端服务

### 7. 重构的文件

- `MiniWorldPlugin.cs` - 主插件文件，移除 WebSocket 服务器启动逻辑
- `Config.cs` - 配置文件，更新配置项
- `Managers/NodeManager.cs` - 简化为单节点管理
- `Managers/WorldManager.cs` - 完全重写，使用 RPC 调用
- `Commands/MWCommands.cs` - 更新命令以使用新架构

## 技术细节

### RPC 客户端连接

- 使用 HttpClient 发送 POST 请求到节点的 RPC 端点
- 通过 StreamJsonRpc 建立双向通信通道
- 实现自动重连机制确保连接稳定性

### 错误处理

- 在 RPC 调用失败时提供清晰的错误信息
- 自动处理连接断开和重连
- 保护性的异常处理避免插件崩溃

### 兼容性

- 数据库模型保持兼容
- 用户命令接口基本不变
- 配置文件需要手动更新

## 使用说明

### 配置文件示例

```json
{
  "MaxWorldsPerPlayer": 3,
  "DefaultWorldPortStart": 27000,
  "RpcUrl": "http://localhost:8080/rpc/"
}
```

### 启动顺序

1. 先启动 MiniWorldNode (RPC 服务器)
2. 再启动 TShock 服务器 (加载 MiniWorldPlugin)
3. 插件会自动连接到节点并同步状态

### 故障排查

- 检查 RpcUrl 配置是否正确
- 确保 MiniWorldNode 已启动并监听正确端口
- 查看插件日志了解连接状态
- 使用 `/mw nodes` 命令检查节点状态

## 向后兼容性说明

- 数据库结构保持不变，现有世界数据会自动迁移
- 用户命令保持兼容，功能基本一致
- 管理员需要更新配置文件格式
- 需要同时升级 MiniWorldNode 到新版本
