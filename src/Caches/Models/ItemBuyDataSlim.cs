using System.Collections.Frozen;
using SPTarkov.Server.Core.Models.Common;

namespace ItemInfo.Caches.Models;

public record struct ItemBuyDataSlim(FrozenSet<MongoId> Category, FrozenSet<MongoId> IdList);
