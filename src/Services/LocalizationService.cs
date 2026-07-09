using System.Text.Json;
using ItemInfo.Constants;
using ItemInfo.Models;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Enums.Hideout;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Utils;

namespace ItemInfo.Services;

[Injectable(InjectionType.Singleton, TypePriority = OnLoadOrder.Database + 4)]
public class LocalizationService(
    ISptLogger<LocalizationService> logger,
    JsonUtil jsonUtil,
    LocaleService localeService,
    DatabaseService dbService
) : IOnLoad
{
    private static ModTranslation? AllLocales { get; set; }
    private Dictionary<string, string> CachedLocaleDatabase { get; set; }

    public async Task OnLoad()
    {
        CachedLocaleDatabase = localeService.GetLocaleDb();
        var translationFile = await jsonUtil.DeserializeFromFileAsync<ModTranslation>(
            ModPath.TranslationFile
        );
        if (translationFile is null)
        {
            var ex = new NullReferenceException("Could not find translation file");
            logger.Critical(
                "Failed to load translations file. Using translations keys instead",
                ex
            );
            return;
        }
        AllLocales = translationFile;

        foreach (
            var (lang, langdef) in translationFile.RawLanguages.Where(kvp => kvp.Key != "debug")
        )
        {
            Dictionary<string, string> deserializedLangDef =
                langdef.Deserialize<Dictionary<string, string>>() ?? new();
            AllLocales.Language.Add(lang, deserializedLangDef);
        }
    }

    public bool TryGetKey(string key, out string localized)
    {
        if (
            !AllLocales.Language.TryGetValue(ConfigurationService.Config.UserLocale, out var locale)
        )
        {
            logger.Warning(
                $"Tried to fetch localization for key {key}, but the mod is running an unknown translation. Did you change to an invalid locale?"
            );
            localized = key;
            return false;
        }

        if (!locale.TryGetValue(key, out localized))
        {
            localized = key;
            return false;
        }

        return true;
    }

    public string GetItemShortName(MongoId? tpl, string locale) =>
        tpl is null ? string.Empty : GetGameTranslation(tpl + " ShortName", locale);

    public string GetTraderNickname(MongoId? tpl, string locale) =>
        tpl is null ? string.Empty : GetGameTranslation(tpl + " Nickname", locale);

    public string GetQuestName(MongoId tpl, string locale) =>
        GetGameTranslation(tpl + " name", locale);

    public string GetHideoutName(HideoutAreas area, string locale) =>
        area == HideoutAreas.NotSet
            ? "UNKNOWN AREA"
            : GetGameTranslation($"hideout_area_{area}_name", locale);

    public string GetGameTranslation(string key, string lang)
    {
        return CachedLocaleDatabase.GetValueOrDefault(key, key);
    }

    public string GetTranslation(string key) =>
        GetTranslation(key, ConfigurationService.Config.UserLocale);

    public string GetTranslation(string key, string lang)
    {
        /*
         * TODO: Remove this code. SPT has support for custom locales.
         * https://github.com/sp-tarkov/server-mod-examples/blob/main/25AddCustomLocales/AddCustomLocales.cs
         */

        if (AllLocales is null)
        {
            return key;
        }

        string? name;
        if (lang != "en" && AllLocales.Language.ContainsKey(lang))
        {
            if (!AllLocales.Language[lang].TryGetValue(key, out name))
            {
                logger.Debug(
                    $"Failed to find key {key} for locale {lang}. Falling back to english"
                );
                return GetTranslation(key, "en");
            }
        }

        if (!AllLocales.Language["en"].TryGetValue(key, out name))
        {
            logger.Debug(
                $"Failed to find key for locale in English. Falling back to debug information"
            );
            return "GetLocalization() unknown locale " + key;
        }

        return name;
    }

    public void UpsertLocaleTransformer(List<KeyValuePair<string, string>> newLocales) =>
        UpsertLocaleTransformer(newLocales, ConfigurationService.Config.UserLocale);

    public void UpsertLocaleTransformer(
        List<KeyValuePair<string, string>> newLocales,
        string locale
    )
    {
        if (!dbService.GetLocales().Global.TryGetValue(locale, out var locales))
        {
            return;
        }

        locales.AddTransformer(l =>
        {
            foreach (var (k, v) in newLocales)
            {
                if (l.TryGetValue(k, out var existing))
                {
                    l[k] = v + "\n" + existing;
                }
                else
                {
                    l.Add(k, v);
                }
            }
            return l;
        });
    }
}
