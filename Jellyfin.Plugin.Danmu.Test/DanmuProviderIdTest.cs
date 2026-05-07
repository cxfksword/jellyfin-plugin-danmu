using System;
using Jellyfin.Plugin.Danmu.Core;
using Jellyfin.Plugin.Danmu.Scrapers;
using Jellyfin.Plugin.Danmu.Scrapers.Tencent;
using MediaBrowser.Controller.Entities.Movies;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Danmu.Test;

[TestClass]
public class DanmuProviderIdTest
{
	private static readonly ILoggerFactory TestLoggerFactory = LoggerFactory.Create(builder => { });

	[DataTestMethod]
	[DataRow("BilibiliID", "bilibili")]
	[DataRow("DandanID", "dandan")]
	[DataRow("DanmuApiID", "danmuapi")]
	[DataRow("IqiyiID", "iqiyi")]
	[DataRow("MgtvID", "mgtv")]
	[DataRow("TencentID", "tencent")]
	[DataRow("YoukuID", "youku")]
	public void Encode_UsesMappedLowercasePrefix(string scraperProviderId, string expectedPrefix)
	{
		var encodedValue = DanmuProviderId.Encode(scraperProviderId, "provider-value");

		Assert.AreEqual($"{expectedPrefix}:provider-value", encodedValue);
	}

	[DataTestMethod]
	[DataRow("TencentID:g41000zu2yv", "TencentID", "g41000zu2yv")]
	[DataRow("tencent:g41000zu2yv", "TencentID", "g41000zu2yv")]
	public void TryDecode_SupportsLegacyAndMappedPrefix(string rawValue, string expectedScraperProviderId, string expectedProviderValue)
	{
		var success = DanmuProviderId.TryDecode(rawValue, out var scraperProviderId, out var providerValue);

		Assert.IsTrue(success);
		Assert.AreEqual(expectedScraperProviderId, scraperProviderId);
		Assert.AreEqual(expectedProviderValue, providerValue);
	}

	[TestMethod]
	public void Set_WritesMappedUnifiedProviderId()
	{
		var item = new Movie();

		DanmuProviderId.Set(item, Array.Empty<AbstractScraper>(), Tencent.ScraperProviderId, "g41000zu2yv");

		Assert.AreEqual("tencent:g41000zu2yv", item.ProviderIds[DanmuProviderId.UnifiedProviderId]);
	}

	[TestMethod]
	public void TryGet_ReadsMappedUnifiedProviderIdWithLegacyScraperProviderId()
	{
		var item = new Movie();
		item.ProviderIds[DanmuProviderId.UnifiedProviderId] = "tencent:g41000zu2yv";

		var success = DanmuProviderId.TryGet(item, Tencent.ScraperProviderId, out var providerValue);

		Assert.IsTrue(success);
		Assert.AreEqual("g41000zu2yv", providerValue);
	}

	[TestMethod]
	public void TryMigrateToUnified_ConvertsLegacyScraperProviderIdToUnifiedProviderId()
	{
		var item = new Movie();
		item.ProviderIds[Tencent.ScraperProviderId] = "g41000zu2yv";

		var migrated = DanmuProviderId.TryMigrateToUnified(item, CreateScrapers(), Tencent.ScraperProviderId, "g41000zu2yv");

		Assert.IsTrue(migrated);
		Assert.IsFalse(item.ProviderIds.ContainsKey(Tencent.ScraperProviderId));
		Assert.AreEqual("tencent:g41000zu2yv", item.ProviderIds[DanmuProviderId.UnifiedProviderId]);
	}

	[TestMethod]
	public void TryMigrateToUnified_RewritesLegacyUnifiedValueToCanonicalValue()
	{
		var item = new Movie();
		item.ProviderIds[DanmuProviderId.UnifiedProviderId] = "TencentID:g41000zu2yv";

		var migrated = DanmuProviderId.TryMigrateToUnified(item, CreateScrapers(), Tencent.ScraperProviderId, "g41000zu2yv");

		Assert.IsTrue(migrated);
		Assert.AreEqual("tencent:g41000zu2yv", item.ProviderIds[DanmuProviderId.UnifiedProviderId]);
	}

	[TestMethod]
	public void TryMigrateToUnified_SkipsCanonicalUnifiedValue()
	{
		var item = new Movie();
		item.ProviderIds[DanmuProviderId.UnifiedProviderId] = "tencent:g41000zu2yv";

		var migrated = DanmuProviderId.TryMigrateToUnified(item, CreateScrapers(), Tencent.ScraperProviderId, "g41000zu2yv");

		Assert.IsFalse(migrated);
		Assert.AreEqual("tencent:g41000zu2yv", item.ProviderIds[DanmuProviderId.UnifiedProviderId]);
	}

	private static AbstractScraper[] CreateScrapers()
	{
		return new AbstractScraper[] { new Tencent(TestLoggerFactory) };
	}
}
