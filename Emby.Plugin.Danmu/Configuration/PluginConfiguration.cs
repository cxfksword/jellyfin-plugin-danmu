using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml.Serialization;
using MediaBrowser.Model.Plugins;

namespace Emby.Plugin.Danmu.Configuration
{
    public class PluginConfiguration : BasePluginConfiguration
    {
        // public override string EditorTitle => "弹幕插件设置";
        // public override string EditorDescription => "配置弹幕插件信息";

        /// <summary>
        /// 版本信息
        /// </summary>
        public string Version { get; } = Assembly.GetExecutingAssembly().GetName().Version.ToString();

        public DandanOption Dandan { get; set; } = new DandanOption();

        /// <summary>
        /// 是否同时生成ASS格式弹幕.
        /// </summary>
        public bool ToAss { get; set; } = false;

        /// <summary>
        /// 字体.
        /// </summary>
        public string AssFont { get; set; } = string.Empty;

        /// <summary>
        /// 字体大小.
        /// </summary>
        public string AssFontSize { get; set; } = string.Empty;

        /// <summary>
        /// 限制行数.
        /// </summary>
        public string AssLineCount { get; set; } = string.Empty;

        /// <summary>
        /// 移动速度.
        /// </summary>
        public string AssSpeed { get; set; } = string.Empty;

        /// <summary>
        /// 透明度.
        /// </summary>
        public string AssTextOpacity { get; set; } = string.Empty;
        
        /// <summary>
        /// 开启全部弹幕来源
        /// </summary>
        public bool OpenAllSource { get; set; } = false;


        /// <summary>
        /// 弹幕源.
        /// </summary>
        private List<ScraperConfigItem> _scrapers;
        
        // public ScraperConfigItem2[] ScraperConfigItems => new ScraperConfigItem2[]{};
        [XmlArrayItem(ElementName = "Scraper")]
        public ScraperConfigItem[] Scrapers
        {
            get
            {

                var defaultScrapers = new List<ScraperConfigItem>();
                if (Plugin.Instance?.Scrapers != null)
                {
                    foreach (var scaper in Plugin.Instance.Scrapers)
                    {
                        defaultScrapers.Add(new ScraperConfigItem(scaper.Name, scaper.DefaultEnable));
                    }
                };

                if (_scrapers?.Any() != true)
                {// 没旧配置，返回默认列表
                    return defaultScrapers.ToArray();
                }
                else
                {// 已保存有配置

                    // 删除已废弃的插件配置
                    var allValidScaperNames = defaultScrapers.Select(o => o.Name).ToList();
                    _scrapers.RemoveAll(o => !allValidScaperNames.Contains(o.Name));



                    // 找出新增的插件
                    var oldScrapers = _scrapers.Select(o => o.Name).ToList();
                    defaultScrapers.RemoveAll(o => oldScrapers.Contains(o.Name));

                    // 合并新增的scrapers
                    _scrapers.AddRange(defaultScrapers);
                }
                return _scrapers.ToArray();
            }
            set
            {
                _scrapers = value.ToList();
            }
        }
    }

    /// <summary>
    /// 弹幕源配置
    /// </summary>
    public class ScraperConfigItem
    {
        // [DisplayName("是否启用")]
        public bool Enable { get; set; }

        public string Name { get; set; }

        public ScraperConfigItem()
        {
            this.Name = "";
            this.Enable = false;
        }


        public ScraperConfigItem(string name, bool enable)
        {
            this.Name = name;
            this.Enable = enable;
        }
    }

    /// <summary>
    /// 弹弹play配置
    /// </summary>
    public class DandanOption
    {
        /// <summary>
        /// 同时获取关联的第三方弹幕
        /// </summary>
        public bool WithRelatedDanmu { get; set; } = true;

        /// <summary>
        /// 中文简繁转换。0-不转换，1-转换为简体，2-转换为繁体
        /// </summary>
        public int ChConvert { get; set; } = 0;
    }
}