using Danmaku2Ass;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.Danmu.Test
{
    [TestClass]
    public class ToAssTest
    {
        [TestMethod]
        public void TestToAss()
        {
            var xml = @"<?xml version=""1.0"" encoding=""UTF-8""?><i><chatserver>chat.bilibili.com</chatserver><chatid>13113033</chatid><mission>0</mission><maxlimit>3000</maxlimit><state>0</state><real_name>0</real_name><source>k-v</source>
                <d p=""253.09700,1,30,16777215,1665229999,0,e890f28,1158792239837718272,11"">杨笑汝有受到羽海野千花影响啦</d>
                <d p=""225.85500,1,25,16777215,1665229198,0,e890f28,1158785518893433856,11"">风间笑死我了</d>
                <d p=""213.22500,1,25,16777215,1665229172,0,e890f28,1158785301788090624,11"">有搬运车的</d>
                <d p=""253.71600,1,25,16777215,1663566786,0,4cd9e142,1144840193279505664,11"">杨笑汝最喜欢的就是野海羽千花！</d>
                <d p=""634.75000,1,25,16777215,1663517171,0,43c9cec0,1144424000059966464,11"">给女儿买衣服哈哈哈哈哈</d>
                <d p=""1110.38100,1,25,16777215,1662232214,0,e8930b5b,1133644991965657344,11"">kdhr这会才15吧</d>
                <d p=""1167.49900,1,25,16777215,1662112456,0,9589ad2c,1132640394031491584,11"">真田情感好细腻</d>
                <d p=""55.38000,1,25,16777215,1660413677,0,9c28a5a9,1118390004910248704,11"">这个op看得我好迷茫</d></i>
            ";

            var ass = Bilibili.GetInstance().ToASS(xml, new Config());
            Console.WriteLine(ass);
            Assert.IsNotNull(ass);

        }

        [TestMethod]
        public void TestToAssFile()
        {
            var xml = File.ReadAllText(@"F:\ddd\11111.xml");


             Bilibili.GetInstance().Create(xml, new Config(), @"F:\ddd\11111.ass");
 
        }
    }
}
