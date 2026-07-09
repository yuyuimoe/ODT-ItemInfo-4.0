using SPTarkov.Server.Core.Models.Common;

namespace ItemInfo;

public record TraderQuestRewardInfo(
    MongoId TraderId,
    MongoId QuestId,
    string QuestTarget,
    IEnumerable<MongoId> QuestRewardTemplates);