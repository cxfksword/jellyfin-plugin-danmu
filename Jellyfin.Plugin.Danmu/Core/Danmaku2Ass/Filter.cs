using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Danmaku2Ass
{
    /// <summary>
    /// 过滤器基类
    /// </summary>
    public class Filter
    {
        public virtual List<Danmaku> DoFilter(List<Danmaku> danmakus)
        {
            throw new NotImplementedException("使用了过滤器的未实现的方法。");
        }
    }

    /// <summary>
    /// 顶部样式过滤器
    /// </summary>
    public class TopFilter : Filter
    {
        public override List<Danmaku> DoFilter(List<Danmaku> danmakus)
        {
            List<Danmaku> keep = new List<Danmaku>();
            foreach (var danmaku in danmakus)
            {
                if (danmaku.Style == "top")
                {
                    continue;
                }
                keep.Add(danmaku);
            }
            return keep;
        }
    }

    /// <summary>
    /// 底部样式过滤器
    /// </summary>
    public class BottomFilter : Filter
    {
        public override List<Danmaku> DoFilter(List<Danmaku> danmakus)
        {
            List<Danmaku> keep = new List<Danmaku>();
            foreach (var danmaku in danmakus)
            {
                if (danmaku.Style == "bottom")
                {
                    continue;
                }
                keep.Add(danmaku);
            }
            return keep;
        }
    }

    /// <summary>
    /// 滚动样式过滤器
    /// </summary>
    public class ScrollFilter : Filter
    {
        public override List<Danmaku> DoFilter(List<Danmaku> danmakus)
        {
            List<Danmaku> keep = new List<Danmaku>();
            foreach (var danmaku in danmakus)
            {
                if (danmaku.Style == "scroll")
                {
                    continue;
                }
                keep.Add(danmaku);
            }
            return keep;
        }
    }

    /// <summary>
    /// 自定义过滤器
    /// </summary>
    public class CustomFilter : Filter
    {
        private Regex regEmoj = new Regex(@"(\u00a9|\u00ae|[\u2000-\u3300]|\ud83c[\ud000-\udfff]|\ud83d[\ud000-\udfff]|\ud83e[\ud000-\udfff])", RegexOptions.Compiled);
        public override List<Danmaku> DoFilter(List<Danmaku> danmakus)
        {   
            // 过滤 emoji 和非法字符
            List<Danmaku> keep = new List<Danmaku>();
            foreach (var danmaku in danmakus)
            {
                danmaku.Content = this.regEmoj.Replace(danmaku.Content, string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(danmaku.Content))
                {
                    continue;
                }
                keep.Add(danmaku);
            }
            return keep;
        }
    }
}
