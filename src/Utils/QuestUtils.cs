using System.Collections.Frozen;
using ItemInfo.Services;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Services;

namespace ItemInfo.Utils;

[Injectable]
public class QuestUtils(DatabaseService dbService, LocalizationService localizationService)
{
    public FrozenDictionary<MongoId, List<TraderQuestRewardInfo>> GetAllQuestWithRewards()
    {
        return dbService.GetQuests()
            .Where(q => q.Value.Rewards is not null)
            .SelectMany(q => q.Value.Rewards!
                .SelectMany(rl => rl.Value.Where(r => r is
                    { Type: RewardType.AssortmentUnlock, Target: not null, Items: not null }))
                .Select(r => new TraderQuestRewardInfo(
                    q.Value.TraderId,
                    q.Value.Id,
                    r.Target!,
                    r.Items!.Select(i => i.Template))))
            .GroupBy(x => x.TraderId)
            .ToFrozenDictionary(x => x.Key, v => v.ToList());
    }
}