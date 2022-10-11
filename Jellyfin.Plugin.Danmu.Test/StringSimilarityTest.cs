
namespace Jellyfin.Plugin.Danmu.Test
{
    [TestClass]
    public class StringSimilarityTest
    {
        [TestMethod]
        public void TestString()
        {
            var str1 = "雄狮少年";
            var str2 = "我是特优声 剧团季";

            var score = Fastenshtein.Levenshtein.Distance(str1, str2);

            str1 = "雄狮少年";
            str2 = "雄狮少年 第二季";

            score = Fastenshtein.Levenshtein.Distance(str2, str1);


            Assert.IsTrue(score > 0);
        }
    }
}
