using System.Collections.Frozen;
using System.Collections.Immutable;
using ItemInfo.Caches;
using ItemInfo.Constants;
using ItemInfo.Extensions;
using ItemInfo.Models;
using ItemInfo.Services;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Utils;

namespace ItemInfo.Utils;

[Injectable]
public class TemplateItemUtils(
    ISptLogger<TemplateItemUtils> logger,
    ItemDataCache itemDataCache,
    TraderCache traderCache,
    LocaleCache localeCache,
    HideoutCache hideoutCache,
    LocalizationService localizationService
)
{
    private (double price, double profit) CalculateBarterTotalPrice(in TraderBarterInfo barterInfo)
    {
        var barterPrice = 0d;
        if (
            traderCache.TemplateBarterTotalFleaPrice.TryGetValue(
                barterInfo.TemplateId,
                out var barterTotals
            )
        )
        {
            if (!barterTotals.TryGetValue(barterInfo.AssortId, out barterPrice))
            {
                barterPrice = barterTotals.GetValueOrDefault(barterInfo.AssortParentId, 0d);
            }
        }

        if (barterPrice <= 0)
        {
            return (0d, 0d);
        }

        var profit =
            barterPrice - itemDataCache.FleaPrices.GetValueOrDefault(barterInfo.TemplateId, 0d);
        return (barterPrice, profit);
    }

    private string FormatItemRequirements(
        in List<List<BarterScheme>> barters,
        FrozenDictionary<MongoId, string> templateShortNames
    ) =>
        string.Join(
            ", ",
            barters.SelectMany(x =>
                x.Select(y => $"{y.Count ?? 1d}x {templateShortNames[y.Template]}")
            )
        );

    public string GetLocalizedSlotRatio(MongoId templateId)
    {
        return !itemDataCache.InnerOuterSlotRatios.TryGetValue(templateId, out var ratio)
            ? string.Empty
            : $"S.E: x{ratio}";
    }

    public string GetLocalizedPricing(MongoId templateId)
    {
        string bestTrader = "NO TRADER";
        if (itemDataCache.TemplateBestTraderPrice.TryGetValue(templateId, out var offer))
        {
            bestTrader =
                $"{offer.Value.FormatToPrice()} @ {localeCache.TraderNicknames[offer.Key]}";
        }

        string fleaPrice = "BANNED @ FLEA";
        if (itemDataCache.FleaPrices.TryGetValue(templateId, out var price))
        {
            fleaPrice = $"{price.FormatToPrice()} @ FLEA";
        }

        string handbookPrice = "NO HANDBOOK";
        if (itemDataCache.HandbookPrices.TryGetValue(templateId, out var hbPrice))
        {
            handbookPrice = $"Handbook: {hbPrice.FormatToPrice()}";
        }

        return string.Join(" | ", bestTrader, fleaPrice, handbookPrice);
    }

    public string GetLocalizedHeadsetInfo(MongoId templateId)
    {
        if (!itemDataCache.TemplateHeadsetInfo.TryGetValue(templateId, out var headsetInfo))
        {
            return string.Empty;
        }

        var boost =
            headsetInfo.CompressionGain + Math.Min(Math.Abs(headsetInfo.CompressorThreshold), 0);

        return $"Ambient Volume: {headsetInfo.AmbientVolume}dB | Boost: {boost}dB | Distortion: {headsetInfo.Distortion * 100}%";
    }

    public List<string> GetLocalizedHideoutAreaRequirement(MongoId templateId)
    {
        if (!hideoutCache.AreaStageRequirementsByTemplate.TryGetValue(templateId, out var areas))
        {
            return [];
        }

        var hideoutAreaNames = localeCache.HideoutAreaNames["en"];

        return areas
            .Select(a => $"{a.Count}x @ {hideoutAreaNames[a.AreaType]} lv{a.Stage}")
            .ToList();
    }

    public List<string> GetLocalizedHideoutAreaStageImprovementRequirement(MongoId templateId)
    {
        if (
            !hideoutCache.AreaStageImprovementRequirementByTemplate.TryGetValue(
                templateId,
                out var areas
            )
        )
        {
            return [];
        }

        var hideoutAreaNames = localeCache.HideoutAreaNames["en"];

        return areas
            .Select(a =>
                $"{a.Count}x @ {hideoutAreaNames[hideoutCache.AreaTypeById[a.AreaId]]} (STAGE IMPROVEMENT)"
            )
            .ToList();
    }

    public List<string> GetLocalizedBarterResultList(MongoId templateId)
    {
        if (!traderCache.TemplateResultTraderBarter.TryGetValue(templateId, out var barterInfoList))
        {
            return [];
        }

        var templateShortNames = localeCache.TemplateShortNames["en"];
        var traderNames = localeCache.TraderNicknames["en"];

        var returnList = new List<string>();

        foreach (var barterInfo in barterInfoList)
        {
            var requiredItems = FormatItemRequirements(
                barterInfo.BarterScheme!.Barters,
                templateShortNames
            );

            var buyBarterTemplate = new FormatString(
                LocalizationTemplates.BuyBarterItem,
                localizationService
            );
            buyBarterTemplate.AddData("traderName", traderNames[barterInfo.TraderId]);
            buyBarterTemplate.AddData("loyaltyLevel", barterInfo.BarterScheme!.LoyaltyLevel!);
            buyBarterTemplate.AddData("extraItems", requiredItems);

            var parentText = barterInfo.AssortParentId is "" or "hideout"
                ? string.Empty
                : $"∈ {templateShortNames[barterInfo.AssortParentId]}";

            buyBarterTemplate.AddData("parent", parentText);

            var (price, profit) = CalculateBarterTotalPrice(barterInfo);
            if (price > 0d)
            {
                buyBarterTemplate.AddData(
                    "totalBarterFleaPrice",
                    FormatString.QuickFormat(
                        LocalizationTemplates.TotalBarterFleaPrice,
                        "totalPrice",
                        price,
                        localizationService
                    )
                );

                buyBarterTemplate.AddData(
                    "deltaBarterPrice",
                    FormatString.QuickFormat(
                        LocalizationTemplates.DeltaBarterFleaPrice,
                        "diffPrice",
                        profit,
                        localizationService
                    )
                );
            }

            returnList.Add(buyBarterTemplate.ToString());
        }

        return returnList;
    }

    public List<string> GetLocalizedBarterResourceList(MongoId templateId)
    {
        if (
            !traderCache.TemplateResourceTraderBarter.TryGetValue(
                templateId,
                out var barterInfoList
            )
        )
        {
            return [];
        }

        var templateShortNames = localeCache.TemplateShortNames["en"];
        var traderNames = localeCache.TraderNicknames["en"];

        var returnList = new List<string>();

        foreach (var barterInfo in barterInfoList)
        {
            var sellBarterTemplate = new FormatString(
                LocalizationTemplates.SellBarterItem,
                localizationService
            );

            double count = traderCache
                .AssortBarterTemplateCount[barterInfo.AssortId]
                .First(x => x.Key == templateId)
                .Value;

            sellBarterTemplate.AddData("traderName", traderNames[barterInfo.TraderId]);
            sellBarterTemplate.AddData("loyaltyLevel", barterInfo.BarterScheme!.LoyaltyLevel);
            sellBarterTemplate.AddData("itemName", templateShortNames[barterInfo.TemplateId]);
            sellBarterTemplate.AddData("count", count);
            sellBarterTemplate.AddData(
                "extraItems",
                FormatItemRequirements(barterInfo.BarterScheme!.Barters, templateShortNames)
            );

            if (CalculateBarterTotalPrice(barterInfo) is (var price and > 0, var profit))
            {
                sellBarterTemplate.AddData(
                    "totalBarterFleaPrice",
                    FormatString.QuickFormat(
                        LocalizationTemplates.TotalBarterFleaPrice,
                        "totalPrice",
                        price,
                        localizationService
                    )
                );

                sellBarterTemplate.AddData(
                    "deltaBarterPrice",
                    FormatString.QuickFormat(
                        LocalizationTemplates.DeltaBarterFleaPrice,
                        "diffPrice",
                        profit,
                        localizationService
                    )
                );
            }

            returnList.Add(sellBarterTemplate.ToString());
        }

        return returnList;
    }
}
