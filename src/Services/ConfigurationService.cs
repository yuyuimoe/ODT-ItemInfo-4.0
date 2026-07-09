using ItemInfo.Constants;
using ItemInfo.Models;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Utils;

namespace ItemInfo.Services;

[Injectable(InjectionType.Singleton, TypePriority = OnLoadOrder.Database + 2)]
public class ConfigurationService(ISptLogger<ConfigurationService> logger, JsonUtil jsonUtil) : IOnLoad
{
    public static ModConfig Config { get; private set; }

    public async Task OnLoad()
    {
        var configFile = await jsonUtil.DeserializeFromFileAsync<ModConfig>(ModPath.ConfigFile);
        if (configFile is null)
        {
            logger.Critical("Failed to load configuration file", new NullReferenceException("Deserialization of config file returned null"));
            return;
        }

        Config = configFile;
    }
}