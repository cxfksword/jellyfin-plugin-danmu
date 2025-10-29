# Jellyfin Plugin Danmu - AI 编码助手指南

## 项目概述
一个为 Jellyfin 开发的弹幕自动下载插件，支持从多个视频平台（B站、优酷、爱奇艺、腾讯视频、芒果TV）自动下载和管理中文弹幕。支持 XML 和 ASS 字幕格式。

**核心技术栈**: C# .NET 9.0, Jellyfin Plugin API, ILRepack（用于依赖合并）

## 架构：多弹幕源设计模式

### 弹幕源系统 (`Jellyfin.Plugin.Danmu/Scrapers/`)
核心架构是一个**可插拔的弹幕源系统**，每个视频平台都实现 `AbstractScraper`：

```csharp
// 每个平台（Bilibili/、Youku/、Iqiyi/ 等）需要实现：
public abstract class AbstractScraper {
    Task<List<ScraperSearchInfo>> Search(BaseItem item);
    Task<string?> SearchMediaId(BaseItem item);
    Task<ScraperMedia?> GetMedia(BaseItem item, string id);
    Task<ScraperEpisode?> GetMediaEpisode(BaseItem item, string id);
    Task<ScraperDanmaku?> GetDanmuContent(BaseItem item, string commentId);
}
```

**重要说明**: 
- 弹幕源通过 `Plugin.cs` 中的 `IApplicationHost.GetExports<AbstractScraper>()` 自动发现
- 顺序由 `DefaultOrder` 属性和用户配置控制
- 每个弹幕源有唯一的 `ProviderId`（如 `BilibiliID`），用于 Jellyfin 元数据存储
- 参考 `Scrapers/Bilibili/Bilibili.cs` 的实现示例

### 事件驱动的弹幕处理
插件使用 Jellyfin 的媒体库事件和**防抖队列处理**机制：

1. `PluginStartup.cs` 订阅 `ILibraryManager.ItemAdded/ItemUpdated` 事件
2. 事件在 `LibraryManagerEventsHelper` 中排队，使用 10 秒防抖定时器
3. 批量处理：匹配媒体 → 搜索弹幕源 → 下载弹幕
4. 结果保存为 `.xml` 文件，与视频文件同目录（如 `movie.mp4` → `movie.xml`）

**关键类**:
- `LibraryManagerEventsHelper.QueueItem()` - 添加项目到处理队列
- `LibraryManagerEventsHelper.ProcessQueuedMovieEvents()` - 批量处理电影
- `LibraryManagerEventsHelper.ProcessQueuedSeasonEvents()` - 处理电视剧季

## 开发工作流

### 构建项目
```bash
dotnet restore
dotnet publish --configuration=Release Jellyfin.Plugin.Danmu/Jellyfin.Plugin.Danmu.csproj
```

输出位置: `Jellyfin.Plugin.Danmu/bin/Release/net9.0/Jellyfin.Plugin.Danmu.dll`

**ILRepack 集成**: Release 构建会通过 `ILRepack.targets` 自动将依赖（RateLimiter、ComposableAsync、Google.Protobuf、SharpZipLib）合并到单个 DLL。这对 Jellyfin 插件部署至关重要。

### 运行测试
```bash
dotnet test --no-restore --verbosity normal
```

测试使用 MSTest 框架和 Moq 进行模拟：
- `*ApiTest.cs` - API 响应解析测试
- `*Test.cs` - 端到端弹幕源测试
- 模拟 `ILibraryManager`、`IFileSystem` 以实现隔离测试

### 开发环境安装
1. 构建 Release 配置
2. 创建 `danmu/` 文件夹并复制 `Jellyfin.Plugin.Danmu.dll` 到其中
3. 将文件夹移动到 Jellyfin 的 `data/plugins/` 目录
4. 重启 Jellyfin

## 关键模式与约定

### Provider ID 映射
每个弹幕源使用其 `ProviderId` 在 Jellyfin 元数据中存储匹配结果：
```csharp
item.ProviderIds["BilibiliID"] = "123456";  // 季/媒体 ID
// 对于剧集，使用格式："seasonId_episodeId"
```

**特殊处理**: B站支持 `BV` 视频 ID（用户上传内容）和 `av` ID（UGC视频），以及番剧的 season ID。

### 弹幕 → ASS 转换 (`Core/Danmaku2Ass/`)
`Creater` 类将 XML 弹幕转换为 ASS 字幕，具有：
- 碰撞检测（`Collision.cs`）- 使用基于行的跟踪防止重叠
- 显示模式（`Display.cs`）- 滚动、顶部锚定、底部锚定
- 可配置的字体、速度、透明度、行数

**重要**: ASS 生成是可选的，通过插件配置 `ToAss = true` 启用。

### API 端点 (`Controllers/DanmuController.cs`)
为外部播放器提供的公共 REST API：
- `GET /api/danmu/{id}` - 返回弹幕 URL 元数据
- `GET /api/danmu/{id}/raw` - 下载 XML 弹幕文件
- `GET /api/danmu/search?keyword=` - 跨所有弹幕源搜索
- `GET /api/{site}/danmu/{id}/episodes` - 获取剧集列表
- `GET /api/{site}/danmu/{cid}/download` - 通过评论 ID 下载弹幕

### 配置系统
`PluginConfiguration.cs` 使用 XML 序列化，特殊的 `Scrapers` 属性会：
1. 合并用户配置和新发现的弹幕源
2. 删除已废弃的弹幕源
3. 尊重用户定义的顺序和启用/禁用状态

### 异常处理
- `CanIgnoreException` - 表示预期的失败（如未找到弹幕），不应记录错误日志
- `FrequentlyRequestException` - 视频平台的速率限制异常

## 外部依赖与集成

### Jellyfin Plugin API
- 实现 `ISubtitleProvider` 用于字幕搜索 UI
- `IPluginServiceRegistrator` 用于依赖注入注册
- `BasePlugin<PluginConfiguration>` 用于配置管理
- `IScheduledTask` 用于定期媒体库扫描（`ScheduledTasks/ScanLibraryTask.cs`）

### .NET 包
- `RateLimiter` - API 请求节流
- `Google.Protobuf` - B站 API protobuf 响应解析
- `SharpZipLib` - 弹幕数据解压缩
- `ComposableAsync.Core` - 异步工具库

### 平台专用 API
每个弹幕源都有一个 `*Api.cs`（如 `BilibiliApi.cs`）处理：
- HTTP 客户端和重试逻辑
- 响应反序列化（JSON/Protobuf）
- 速率限制和错误处理

## 命名约定
- 弹幕源：小写名称（如 `bilibili`、`youku`）用于用户界面显示
- Provider ID：驼峰式 + `ID` 后缀（如 `BilibiliID`）
- 事件类型：`EventType.Add`、`EventType.Update`、`EventType.Force`
- 文件扩展名：`.xml` 用于弹幕，`.ass` 用于字幕

## 测试视频平台 API
使用测试类如 `BilibiliApiTest.cs` 在集成前验证 API 响应。模拟 `ILibraryManager` 以避免单元测试中的 Jellyfin 依赖。

## 发布流程
1. 打标签：`git tag -a v1.2.3 -m "Release notes"`
2. GitHub Actions 自动构建和发布
3. `scripts/generate_manifest.py` 更新插件清单并生成校验和
4. 通过 `CN_DOMAIN` 环境变量支持国内镜像

## 常见陷阱
- **不要在字幕搜索中直接修改 `item.ProviderIds`** - 使用临时 item 副本以避免持久化错误的元数据
- **弹幕源必须在 `ServiceRegistrator.cs` 中注册**才能被发现
- **季匹配需要正确的 `IndexNumber`** 用于多季系列
- **ILRepack 仅在 Release 构建中运行** - Debug 构建有独立的 DLL
- **Jellyfin 的 `LocationType.Virtual`** 项目（没有季文件夹的系列）需要特殊处理
