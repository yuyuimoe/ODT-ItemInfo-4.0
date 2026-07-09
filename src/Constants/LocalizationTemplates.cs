namespace ItemInfo.Constants;

public static class LocalizationTemplates
{
    public const string CurrencyRoubles = "₽";
    public const string CurrencyEuros = "€";
    public const string CurrencyDollars = "$";
    public const string CurrencyGpCoin = "GP";

    public const string TotalBarterFleaPrice = "| Σ ≈ (totalPrice)₽ {Fleaprice}";
    public const string DeltaBarterFleaPrice = "| Δ ≈ (diffPrice)₽ {Fleaprice}";

    public const string BuyBarterItem =
        "{Bought} (parent) {at} (traderName) {lv}(loyaltyLevel) | (extraItems) (totalBarterFleaPrice) (deltaBarterPrice)";
    public const string SellBarterItem =
        "{Traded} (count)x {at} (traderName) {lv}(loyaltyLevel) => (itemName) | (extraItems) (totalBarterFleaPrice) (deltaBarterPrice)";
    public const string QuestRequiredItem =
        "{Found} (count)x (fir) => (questName) {@} (traderName)";

    public const string QuestRewardItem =
        "↺ (questName) (questGiver) {@} {traderName} {lv}(loyaltyLevel) ..> (extraItems)";

    public const string HideoutAreaLevel = "(area) {lv}(requiredLevel)";
    public const string HideoutCraftingCount = "(itemShortName) (count)x + ";
    public const string HideoutResourcePercentage = "(itemShortName) (count)x% + ";

    public const string HideoutCraftingMultipleItems = "{peritem}";

    public const string HideoutCraftingResult =
        "{Crafted} (count)x {@} (area) (quest) <= (component) | Σ (recipe) ≈ (price) ₽";

    public const string HideoutCraftingToolRequirement =
        "{Tool} (count)x => (item) (count)x @ (area) (quest) (extra)";

    public const string HideoutBitcoinInformation =
        "{Crafted} {@} (area) / GPU: 1x (gpu1) | 10x (gpu10) | 25x (gpu25) | 50x (gpu50)";

    public const string HideoutAreaRequirement = "{Need} (count)x => (area) {lv}(level)";
}
