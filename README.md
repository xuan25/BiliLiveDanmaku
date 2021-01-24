# BiliLiveDanmaku

显示及播报bilibili直播弹幕

测试项目 不定期维护 勿用于生产环境

使用 C#/WPF 实现

模块化设计，实现 IModule 接口以增加功能模块

语音播报使用微软 Azure API ，中文采用 晓晓 语音模型，日文采用 Nanami 语音模型，英文采用 Aria 语音模型，不支持单条弹幕多语言混合合成

![./img/screenshot.png](./img/screenshot.png)

![./img/preview.gif](./img/preview.gif)

## 语音播报API配置

配置文件位置: *./config/speech.xml*

配置文件模板

```xml
<?xml version="1.0" encoding="utf-8" ?>
<speech>
    <token_endpoint>https://southeastasia.api.cognitive.microsoft.com/sts/v1.0/issueToken</token_endpoint>
    <tts_endpoint>https://southeastasia.tts.speech.microsoft.com/cognitiveservices/v1</tts_endpoint>
    <api_key>you api key</api_key>
</speech>
```

## Todo

- [ ] 在高并发下，弹幕显示版中，礼物增量计数系统导致的概率性线程死锁问题。

## Directory

|目录|功能|
|----|----|
|Bili|B站直播接口及处理|
|Config|软件配置管理|
|Frame|窗体辅助类|
|Microsoft.Xaml.Behaviors|动效相关|
|Modules|功能模块|
|Speech|语音播报相关|
|UI|弹幕显示UI组件|
|Utils|其他辅助类|
