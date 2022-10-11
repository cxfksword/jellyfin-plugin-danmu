# jellyfin-plugin-danmu

jellyfin的b站弹幕自动下载插件，会匹配b站番剧/电影视频，自动下载对应弹幕，并定时更新。

## 安装插件

只支持最新的`jellyfin 10.8.x`版本

添加插件存储库：

`https://github.com/cxfksword/jellyfin-plugin-danmu/raw/main/manifest.json`


## How to test

1. Build the plugin

2. Create a folder, like `Danmu` and copy  `bin/Release/Jellyfin.Plugin.Danmu.dll` into it

3. Move folder `Danmu` to jellyfin `data/plugin` folder


## How to build

1. Clone or download this repository

2. Ensure you have .NET Core SDK setup and installed

3. Build plugin with following command.

```sh
$ dotnet restore Jellyfin.Plugin.Danmu/Jellyfin.Plugin.Danmu.csproj
$ dotnet publish -c Release Jellyfin.Plugin.Danmu/Jellyfin.Plugin.Danmu.csproj
```