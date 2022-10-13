# jellyfin-plugin-danmu

[![Danmu](https://img.shields.io/github/v/release/cxfksword/jellyfin-plugin-danmu)](https://github.com/cxfksword/jellyfin-plugin-danmu/releases)
[![Danmu](https://img.shields.io/badge/jellyfin-10.8.x-lightgrey)](https://github.com/cxfksword/jellyfin-plugin-danmu/releases)
[![Danmu](https://img.shields.io/github/license/cxfksword/jellyfin-plugin-danmu)](https://github.com/cxfksword/jellyfin-plugin-danmu/main/LICENSE) 

jellyfin的b站弹幕自动下载插件，会匹配b站番剧和电影视频，自动下载对应弹幕，并定时更新。

支持功能：

* 自动下载xml格式弹幕
* 生成ass格式弹幕
* 定时更新
* 支持api访问弹幕

![preview](doc/logo.png)

## 安装插件

只支持最新的`jellyfin 10.8.x`版本

添加插件存储库：

国内加速：https://ghproxy.com/https://github.com/cxfksword/jellyfin-plugin-danmu/releases/download/manifest/manifest_cn.json

国外访问：https://github.com/cxfksword/jellyfin-plugin-danmu/releases/download/manifest/manifest.json

## 如何使用

* 新加入的影片会自动获取弹幕（只匹配番剧和电影视频），旧影片可以通过计划任务**扫描媒体库匹配弹幕**手动执行获取
* 可以在元数据中手动指定匹配的视频ID，如播放链接`https://www.bilibili.com/bangumi/play/ep682965`，对应的视频ID就是`682965`
* 对于电视剧和动画，可以在元数据中指定季ID，如播放链接`https://www.bilibili.com/bangumi/play/ss1564`，对应的季ID就是`1564`，只要集数和b站的集数的一致，每季视频的弹幕会自动获取
* 同时生成ass弹幕，需要在插件配置中打开，默认是关闭的

## 支持的api接口

* `/plugin/danmu/{id}`:  获取影片或剧集的xml弹幕链接，不存在时，url为空
* `/plugin/danmu/raw/{id}`:  获取影片或剧集的xml弹幕文件内容


## 如何播放

xml格式：

* [弹弹play](https://www.dandanplay.com/) (Windows/Mac/Android)
* [fileball](https://fileball.app/) (iOS/iPadOS/AppleTV)

ass格式：

* PotPlayer (Windows)
* IINA (Mac)
* Infuse (Mac/iOS/iPadOS/AppleTV)




## How to build

1. Clone or download this repository

2. Ensure you have .NET Core SDK setup and installed

3. Build plugin with following command.

```sh
$ dotnet restore 
$ dotnet publish -c Release Jellyfin.Plugin.Danmu/Jellyfin.Plugin.Danmu.csproj
```


## How to test

1. Build the plugin

2. Create a folder, like `Danmu` and copy  `bin/Release/Jellyfin.Plugin.Danmu.dll` into it

3. Move folder `Danmu` to jellyfin `data/plugin` folder

## Thanks

[downkyi](https://github.com/leiurayer/downkyi)


## License

Apache License Version 2.0