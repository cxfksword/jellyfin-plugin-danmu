## 支持弹幕网站
目前只支持以下站点，后续有空再更新
```
弹弹play、爱奇艺、腾讯、优酷
```
设置方式一样和jellyfin一样，但是api接口基本没有支持




## 支持的api操作
```shell
## 刷新某个媒体的弹幕
/api/danmu/{媒体id}?option=Refresh&X-Emby-Token=xxx
## 获取媒体对应json格式弹幕
/api/danmu/{媒体id}?option=GetJsonById&X-Emby-Token=xxx

```


## 遗留问题
1. 配置暂不支持顺序调整



## 下载链接
[Emby.Plugin.Danmu.dll](./Doc/Emby.Plugin.Danmu.dll) <p/>
[Emby.AndroidTV.2.1.14.弹幕.apk](./Doc/Emby.AndroidTV.2.1.14.弹幕.apk)

