using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Danmaku2Ass
{
    /// <summary>
    /// 字幕工程类
    /// </summary>
    public class Studio
    {
        public Config Config;
        public List<Danmaku> Danmakus;

        public Creater Creater;
        public int KeepedCount;
        public int DropedCount;

        public Studio(Config config, List<Danmaku> danmakus)
        {
            Config = config;
            Danmakus = danmakus;
        }

        public void StartHandle()
        {
            Creater = SetCreater();
            KeepedCount = SetKeepedCount();
            DropedCount = SetDropedCount();
        }

        /// <summary>
        /// ass 创建器
        /// </summary>
        /// <returns></returns>
        protected Creater SetCreater()
        {
            return new Creater(Config, Danmakus);
        }

        /// <summary>
        /// 保留条数
        /// </summary>
        /// <returns></returns>
        protected int SetKeepedCount()
        {
            return Creater.Subtitles.Count();
        }

        /// <summary>
        /// 丢弃条数
        /// </summary>
        /// <returns></returns>
        protected int SetDropedCount()
        {
            return Danmakus.Count - KeepedCount;
        }

        /// <summary>
        /// 创建 ass 字幕
        /// </summary>
        /// <param name="fileName"></param>
        public void CreateAssFile(string fileName)
        {
            CreateFile(fileName, Creater.Text);
        }

        public string GetText()
        {
            return Creater.Text;
        }

        public void CreateFile(string fileName, string text)
        {
            try
            {
                // 文件保存并添加 BOM，解决 ass 的乱码问题
                using (var writer = new StreamWriter(fileName, false, new UTF8Encoding(true)))
                {
                    writer.Write(text);
                }
            }
            catch (Exception)
            { }
        }

        public Dictionary<string, int> Report()
        {
            return new Dictionary<string, int>()
            {
                {"total", Danmakus.Count},
                {"droped", DropedCount},
                {"keeped", KeepedCount},
            };
        }
    }
}
