# NetEaseMusic-MCP

NetEaseMusic-MCP 是一个配合网易云音乐客户端使用的 Model Context Protocol (MCP) 工具，可以让 AI 对网易云音乐进行有限的控制。

## 支持平台
- Windows

## 功能

- **播放/暂停**：控制音乐播放或暂停。
- **切歌功能**：切换上一曲或下一曲。
- **喜欢/取消喜欢**：红心歌曲，或取消红心。
- **音量调节**：调整音量。
- **搜索歌曲**：搜索歌曲，目前仅支持单曲搜索。
- **每日推荐**：播放每日推荐歌单。

## 运行环境要求

- .NET 9.0 SDK 或运行时
- 网易云音乐客户端（测试版本为 3.1.7）

## 使用指南

1. 从 Release 下载最新版本，解压到任意目录待后续使用。
2. 配置你的 MCP Client（Visual Studio Code、Claude Desktop、...）。

### 配置 Visual Studio Code（Windows）

1. 打开 Visual Studio Code 的设置项（Ctrl + ,）
2. 在设置搜索中，搜索 `mcp`
3. 在搜索结果中，点击 `Edit in settings.json`
4. 添加 mcp 服务器
```JSON
"netease-music-mcp": {
    "command": "PATH/TO/NetEaseMusic-MCP.exe",
    "args": [],
}
```
5. 确保网易云音乐未在运行。
6. 点击编辑器中，netease-music-mcp 上方显示的 `Start`。
7. 在 Copilot Agent 模式下使用。

以下为完整 settings.json 示例
```JSON
{
    "security.allowedUNCHosts": [
        "wsl.localhost"
    ],
    "github.copilot.nextEditSuggestions.enabled": true,
    "mcp": {
        
        "inputs": [],
        "servers": {
            "netease-music-mcp": {
                "command": "D:\\NetEaseMusic-MCP\\NetEaseMusic-MCP.exe",
                "args": [],
            }
        }
    }
}
```

## 配置项

配置项位于可执行文件相同目录下，appsettings.json。

### UseDynamicPort

是否使用动态端口。<br/>
开启后自动寻找可用端口，在本地环境无法保证特定端口可用的情况下开启。

### StaticPort

静态端口号。<br/>
UseDynamicPort 为 false 时使用。

### NetEaseMusicPath

网易云音乐客户端路径。<br/>
如果使用默认安装，则不用填写。

### ChromeDriverPath

ChromeDriver 可执行文件所在目录。<br/>
如果不需要自定义 ChromeDriver，则不用填写。

## 常见问题

### 它能做什么？

单独使用此应用时，应用场景有限，比如可以让 AI 帮助你：

**"播放每日推荐"**<br/>
**"静音"**<br/>
**"音量 15%"**<br/>
**"播放 XXX 的歌"**<br/>

欢迎提供更多的使用场景。

### macOS 版本？

功能相对完善后再考虑跨平台。

### 数据安全吗？

应用本身不会上传任何数据。<br/>
播放状态、音量、歌单等数据在需要时会提供给 AI，如果不同意此数据的共享，请不要使用。

## 开源协议

除以下情况外，本项目基于 MIT 协议开源。
- **禁止**用于木马病毒等非法用途。
- **禁止**用于任何危害网易云音乐权益的项目。