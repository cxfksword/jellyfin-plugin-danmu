using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using Emby.Plugin.Danmu.Core.Danmaku2Ass;

namespace Danmaku2Ass
{
    public class Bilibili
    {
        private static Bilibili instance;

        private readonly Dictionary<string, bool> config = new Dictionary<string, bool>
        {
            { "top_filter", false },
            { "bottom_filter", false },
            { "scroll_filter", false }
        };

        private readonly Dictionary<int, string> mapping = new Dictionary<int, string>
        {
            { 0, "none" }, // 保留项
            { 1, "scroll" },
            { 2, "scroll" },
            { 3, "scroll" },
            { 4, "bottom" },
            { 5, "top" },
            { 6, "scroll" }, // 逆向滚动弹幕，还是当滚动处理
            { 7, "none" }, // 高级弹幕，暂时不要考虑
            { 8, "none" }, // 代码弹幕，暂时不要考虑
            { 9, "none" }, // BAS弹幕，暂时不要考虑
            { 10, "none" }, // 未知，暂时不要考虑
            { 11, "none" }, // 保留项
            { 12, "none" }, // 保留项
            { 13, "none" }, // 保留项
            { 14, "none" }, // 保留项
            { 15, "none" }, // 保留项
        };

        // 弹幕标准字体大小
        private readonly int normalFontSize = 25;

        /// <summary>
        /// 获取Bilibili实例
        /// </summary>
        /// <returns></returns>
        public static Bilibili GetInstance()
        {
            if (instance == null)
            {
                instance = new Bilibili();
            }

            return instance;
        }

        /// <summary>
        /// 隐藏Bilibili()方法，必须使用单例模式
        /// </summary>
        private Bilibili() { }

        /// <summary>
        /// 是否屏蔽顶部弹幕
        /// </summary>
        /// <param name="isFilter"></param>
        /// <returns></returns>
        public Bilibili SetTopFilter(bool isFilter)
        {
            config["top_filter"] = isFilter;
            return this;
        }

        /// <summary>
        /// 是否屏蔽底部弹幕
        /// </summary>
        /// <param name="isFilter"></param>
        /// <returns></returns>
        public Bilibili SetBottomFilter(bool isFilter)
        {
            config["bottom_filter"] = isFilter;
            return this;
        }

        /// <summary>
        /// 是否屏蔽滚动弹幕
        /// </summary>
        /// <param name="isFilter"></param>
        /// <returns></returns>
        public Bilibili SetScrollFilter(bool isFilter)
        {
            config["scroll_filter"] = isFilter;
            return this;
        }

        public void Create(long avid, long cid, Config subtitleConfig, string assFile)
        {
            //// 弹幕转换
            //var biliDanmakus = DanmakuProtobuf.GetAllDanmakuProto(avid, cid);

            //// 按弹幕出现顺序排序
            //biliDanmakus.Sort((x, y) => { return x.Progress.CompareTo(y.Progress); });

            //var danmakus = new List<Danmaku>();
            //foreach (var biliDanmaku in biliDanmakus)
            //{
            //    var danmaku = new Danmaku
            //    {
            //        // biliDanmaku.Progress单位是毫秒，所以除以1000，单位变为秒
            //        Start = biliDanmaku.Progress / 1000.0f,
            //        Style = mapping[biliDanmaku.Mode],
            //        Color = (int)biliDanmaku.Color,
            //        Commenter = biliDanmaku.MidHash,
            //        Content = biliDanmaku.Content,
            //        SizeRatio = 1.0f * biliDanmaku.Fontsize / normalFontSize
            //    };

            //    danmakus.Add(danmaku);
            //}

            //// 弹幕预处理
            //Producer producer = new Producer(config, danmakus);
            //producer.StartHandle();

            //// 字幕生成
            //var keepedDanmakus = producer.KeepedDanmakus;
            //var studio = new Studio(subtitleConfig, keepedDanmakus);
            //studio.StartHandle();
            //studio.CreateAssFile(assFile);
        }

        public void Create(byte[] xml, Config subtitleConfig, string assFile)
        {
            var danmakus = ParseXml(xml);

            // 弹幕预处理
            Producer producer = new Producer(config, danmakus);
            producer.StartHandle();

            // 字幕生成
            var keepedDanmakus = producer.KeepedDanmakus;
            var studio = new Studio(subtitleConfig, keepedDanmakus);
            studio.StartHandle();
            studio.CreateAssFile(assFile);
        }

        public string ToASS(byte[] xml, Config subtitleConfig)
        {
            var danmakus = ParseXml(xml);

            // 弹幕预处理
            Producer producer = new Producer(config, danmakus);
            producer.StartHandle();

            // 字幕生成
            var keepedDanmakus = producer.KeepedDanmakus;
            var studio = new Studio(subtitleConfig, keepedDanmakus);
            studio.StartHandle();
            return studio.GetText();
        }

        public List<Danmaku> ParseXml(byte[] xml)
        {
            var doc = new XmlDocument();
            using (var stream = new MemoryStream(xml))
            {
                doc.Load(stream);
            }

            var calFontSizeDict = new Dictionary<int, int>();
            var biliDanmakus = new List<BiliDanmaku>();
            var nodes = doc.GetElementsByTagName("d");
            foreach (XmlNode node in nodes)
            {
                // bilibili弹幕格式：
                // <d p="944.95400,5,25,16707842,1657598634,0,ece5c9d1,1094775706690331648,11">今天的风儿甚是喧嚣</d>
                // time, mode, size, color, create, pool, sender, id, weight(屏蔽等级)
                var p = node.Attributes["p"];
                if (p == null)
                {
                    continue;
                }

                var danmaku = new BiliDanmaku();
                var arr = p.Value.Split(',');
                danmaku.Progress = (int)(Convert.ToDouble(arr[0]) * 1000);
                danmaku.Mode = Convert.ToInt32(arr[1]);
                danmaku.Fontsize = Convert.ToInt32(arr[2]);
                danmaku.Color = Convert.ToUInt32(arr[3]);
                danmaku.Ctime = Convert.ToInt64(arr[4]);
                danmaku.Pool = Convert.ToInt32(arr[5]);
                danmaku.MidHash = arr[6];
                danmaku.Id = Convert.ToInt64(arr[7]);
                danmaku.Weight = Convert.ToInt32(arr[8]);
                danmaku.Content = node.InnerText;

                biliDanmakus.Add(danmaku);

                if (calFontSizeDict.ContainsKey(danmaku.Fontsize))
                {
                    calFontSizeDict[danmaku.Fontsize]++;
                }
                else
                {
                    calFontSizeDict[danmaku.Fontsize] = 1;
                }
            }


            // 按弹幕出现顺序排序
            biliDanmakus.Sort((x, y) => { return x.Progress.CompareTo(y.Progress); });

            // 获取使用最多的字体大小
            var mostUsedFontSize = this.normalFontSize;
            if (calFontSizeDict.Count > 0)
            {
                mostUsedFontSize = calFontSizeDict.OrderByDescending(x => x.Value).First().Key;
            }

            var danmakus = new List<Danmaku>();
            foreach (var biliDanmaku in biliDanmakus)
            {
                var danmaku = new Danmaku
                {
                    // biliDanmaku.Progress单位是毫秒，所以除以1000，单位变为秒
                    Start = biliDanmaku.Progress / 1000.0f,
                    Style = mapping[biliDanmaku.Mode],
                    Color = (int)biliDanmaku.Color,
                    Commenter = biliDanmaku.MidHash,
                    Content = biliDanmaku.Content,
                    SizeRatio = 1.0f * biliDanmaku.Fontsize / mostUsedFontSize
                };

                danmakus.Add(danmaku);
            }

            return danmakus;
        }


        public Dictionary<string, int> GetResolution(int quality)
        {
            var resolution = new Dictionary<string, int>
            {
                { "width", 0 },
                { "height", 0 }
            };

            switch (quality)
            {
                // 240P 极速（仅mp4方式）
                case 6:
                    break;
                // 360P 流畅
                case 16:
                    break;
                // 480P 清晰
                case 32:
                    break;
                // 720P 高清（登录）
                case 64:
                    break;
                // 	720P60 高清（大会员）
                case 74:
                    break;
                // 1080P 高清（登录）
                case 80:
                    break;
                // 1080P+ 高清（大会员）
                case 112:
                    break;
                // 1080P60 高清（大会员）
                case 116:
                    break;
                // 4K 超清（大会员）（需要fourk=1）
                case 120:
                    break;
            }
            return resolution;
        }
    }
}