using SPTarkov.Server.Core.Models.Common;

namespace ItemInfo.Caches.Models;

public record struct AssortSlim(MongoId Id, MongoId TemplateId, string ParentId, int LoyaltyLevel);
