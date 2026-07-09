using System.Diagnostics;
using ItemInfo.Caches;
using ItemInfo.Services;
using ItemInfo.Utils;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Utils;

namespace ItemInfo;

[Injectable(TypePriority = OnLoadOrder.PostSptModLoader + 1)]
public class Mod(
    ISptLogger<Mod> logger,
    TemplateItemUtils templateItemUtils,
    ItemDataCache itemDataCache,
    LocalizationService localizationService
) : IOnLoad
{
    public Task OnLoad()
    {
        var stopWatch = new Stopwatch();
        stopWatch.Start();
        List<KeyValuePair<string, string>> localeChanged = [];
        foreach (var tplId in itemDataCache.TemplateItemSlims.Keys)
        {
            var pricingInfo = templateItemUtils.GetLocalizedPricing(tplId);
            var resultBarterList = templateItemUtils.GetLocalizedBarterResultList(tplId);
            var requiredBarterList = templateItemUtils.GetLocalizedBarterResourceList(tplId);
            var slotRatio = templateItemUtils.GetLocalizedSlotRatio(tplId);
            var hideoutAreaRequirement = templateItemUtils.GetLocalizedHideoutAreaRequirement(
                tplId
            );
            var hideoutStageRequirement =
                templateItemUtils.GetLocalizedHideoutAreaStageImprovementRequirement(tplId);

            localeChanged.Add(
                new KeyValuePair<string, string>(
                    $"{tplId} Description",
                    string.Join(
                        "\n",
                        pricingInfo,
                        resultBarterList,
                        requiredBarterList,
                        slotRatio,
                        hideoutAreaRequirement,
                        hideoutStageRequirement
                    )
                )
            );
        }
        localizationService.UpsertLocaleTransformer(localeChanged);
        stopWatch.Stop();
        logger.Info("loaded in ms" + stopWatch.ElapsedMilliseconds);
        return Task.CompletedTask;
    }
}
