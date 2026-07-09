using SPTarkov.Server.Core.Models.Common;

namespace ItemInfo;

public record TraderPriceInfo(double Price, MongoId TraderId, string ItemParentId);