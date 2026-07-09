using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics;
using ItemInfo.Caches.Models;
using ItemInfo.Extensions;
using ItemInfo.Services;
using ItemInfo.Utils;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Utils;

namespace ItemInfo.Caches;

[Injectable(InjectionType.Singleton, TypePriority = OnLoadOrder.RagfairCallbacks + 65536)]
public sealed class ItemDataCache(
    ISptLogger<ItemDataCache> logger,
    DatabaseService db,
    ItemBaseClassService itemBaseClassService,
    RandomUtil randomUtil,
    LocalizationService localizationService,
    RagfairUtils ragfairUtils,
    TraderCache traderCache
) : IOnLoad
{
    public FrozenDictionary<MongoId, TemplateItemSlim> TemplateItemSlims { get; private set; }

    /// <summary>
    /// Dictionary of item's highest sell price
    /// Key = Item Template MongoID
    /// Value = KeyValuePair of Trader MongoID and price
    /// </summary>
    public FrozenDictionary<MongoId, KeyValuePair<MongoId, double>> TemplateBestTraderPrice
    {
        get;
        private set;
    }

    public FrozenDictionary<MongoId, double> FleaPrices { get; private set; }

    public FrozenDictionary<MongoId, double> HandbookPrices { get; private set; }

    public FrozenSet<MongoId> ItemsWithHandbookEntry { get; private set; }

    public FrozenDictionary<MongoId, double> InnerOuterSlotRatios { get; private set; }

    public FrozenDictionary<MongoId, FrozenSet<MongoId>> TemplateBaseClasses { get; private set; }

    public Task OnLoad()
    {
        var watch = new Stopwatch();
        watch.Start();

        var items = db.GetItems();
        var locales = db.GetLocales().Languages;
        var handbook = db.GetHandbook().Items;

        TemplateItemSlims = BuildTemplateItemSlims(items);
        TemplateBaseClasses = BuildTemplateBaseClasses();
        FleaPrices = BuildFleaPrices();
        ItemsWithHandbookEntry = BuildItemsWithHandbook(handbook);
        HandbookPrices = BuildHandbookPrices(handbook);
        InnerOuterSlotRatios = BuildSlotRatio();
        TemplateBestTraderPrice = BuildBestTraderPrice();

        watch.Stop();
        logger.Info($"Item Cache loaded in {watch.ElapsedMilliseconds}ms");
        return Task.CompletedTask;
    }

    private FrozenDictionary<MongoId, TemplateItemSlim> BuildTemplateItemSlims(
        Dictionary<MongoId, TemplateItem> items
    ) =>
        items
            .Select(i => new TemplateItemSlim(
                TemplateId: i.Key,
                Width: i.Value.Properties?.Width ?? 1,
                Height: i.Value.Properties?.Height ?? 1,
                StackMaxSize: i.Value.Properties?.StackMaxSize ?? 1,
                Grids: i.Value.Properties?.Grids?.ToImmutableList() ?? []
            ))
            .ToFrozenDictionary(k => k.TemplateId);

    private FrozenDictionary<MongoId, FrozenSet<MongoId>> BuildTemplateBaseClasses() =>
        TemplateItemSlims.ToFrozenDictionary(
            i => i.Key,
            i => itemBaseClassService.GetItemBaseClasses(i.Key).ToFrozenSet()
        );

    private FrozenDictionary<MongoId, double> BuildFleaPrices() =>
        TemplateItemSlims.ToFrozenDictionary(
            k => k.Key,
            v => ragfairUtils.GetItemPrice(v.Value.TemplateId)
        );

    private FrozenSet<MongoId> BuildItemsWithHandbook(List<HandbookItem> handbookItems) =>
        handbookItems.Select(hi => hi.Id).ToFrozenSet();

    private FrozenDictionary<MongoId, double> BuildHandbookPrices(
        List<HandbookItem> handbookItems
    ) => handbookItems.ToFrozenDictionary(k => k.Id, v => v.Price ?? 0);

    private FrozenDictionary<MongoId, double> BuildSlotRatio() =>
        TemplateItemSlims
            .Where(i => !i.Value.Grids.IsEmpty)
            .ToFrozenDictionary(
                k => k.Key,
                v =>
                {
                    var isc = v.Value.Grids.Sum(x =>
                        x.Properties?.CellsH * x.Properties?.CellsV ?? 0
                    );
                    return isc == 0 ? 0 : (double)(v.Value.Height * v.Value.Width) / isc;
                }
            );

    private FrozenDictionary<MongoId, KeyValuePair<MongoId, double>> BuildBestTraderPrice() =>
        TemplateItemSlims
            .Keys.Where(tplId =>
                ItemsWithHandbookEntry.Contains(tplId)
                && traderCache.TemplateTraderAssorts.ContainsKey(tplId)
            )
            .Select(tplId =>
            {
                var baseClass = TemplateBaseClasses[tplId];

                var offers = traderCache
                    .TemplateTraderAssorts[tplId]
                    .Keys.Where(traderId =>
                    {
                        var buyData = traderCache.TraderBuyData[traderId];
                        var blacklist = traderCache.TraderBuyBlacklists[traderId];
                        return buyData.IdList.Contains(tplId)
                            && buyData.Category.Overlaps(baseClass)
                            && !blacklist.IdList.Contains(tplId);
                    })
                    .Select(traderId => new KeyValuePair<MongoId, double>(
                        traderId,
                        randomUtil.GetPercentOfValue(
                            traderCache.TraderBuyPriceCoefficient[traderId],
                            HandbookPrices[tplId],
                            0
                        )
                    ))
                    .DefaultIfEmpty(new KeyValuePair<MongoId, double>(MongoId.Empty(), 0))
                    .MaxBy(x => x.Value);

                return (Key: tplId, Best: offers);
            })
            .Where(x => x.Best.Key != default)
            .ToFrozenDictionary(x => x.Key, x => x.Best);
}
