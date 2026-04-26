using ContentPatcher;
using StardewModdingAPI;

namespace UnlimitedEventExpansion
{

    public partial class ModEntry
    {

        public static void ConfigMenu (IContentPatcherAPI api, IManifest ModManifest, IModHelper Helper)
        {
            

            // get Generic Mod Config Menu's API (if it's installed)
            var configMenu = Helper.ModRegistry.GetApi<UnlimitedEventExpansion.Data.IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (configMenu is null)
                return;

            // register mod
            configMenu.Register(
                mod: ModManifest,
                reset: () => Config = new ModConfig(),
                save: () => Helper.WriteConfig(Config)
            );

            // add OpenAI key text option
            configMenu.AddTextOption(
                mod: ModManifest,
                getValue: () => Config?.OpenAIKey ?? "",
                setValue: value => Config.OpenAIKey = value,
                name: () => "OpenAI key",
                tooltip: () => "If you have an OpenAI key, provide it here. Restart your game after setting the key.\nHaving your own key allows you to bypass the limit, and have much better experiences."
            );

            configMenu.AddTextOption(
                mod: ModManifest,
                name: () => "OpenAI model",
                tooltip: () => "Require OpenAI Key provided.\nSelect the OpenAI model to use. Only effective if AI key is provided.\nNano-level model are much cheaper, but it comes with lower quality.",
                getValue: () => Config.OpenAIModel,
                setValue: value => Config.OpenAIModel = value,
                allowedValues: new string[]
                {
                    ModConfig.OpenAIModel_51,
                    ModConfig.OpenAIModel_5mini,
                    ModConfig.OpenAIModel_5nano,
                    ModConfig.OpenAIModel_54mini,
                    ModConfig.OpenAIModel_54nano
                },
                formatAllowedValue: value => value
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => "Allow early event",
                tooltip: () => "Require OpenAI Key provided. Restart your game after change this setting.\nEnable events at anytime without meeting heartlevel requirements.",
                getValue: () => Config.AllowEarlyEvent,
                setValue: value => Config.AllowEarlyEvent = value
            );

            configMenu.AddTextOption(
                mod: ModManifest,
                name: () => "Event length",
                tooltip: () => "Require OpenAI Key provided.\nSelect the preferred event length. Max of 10, 12, 15 and 20 each level.",
                getValue: () => Config.EventLength,
                setValue: value => Config.EventLength = value,
                allowedValues: new string[]
                {
                    ModConfig.EventLengthShort,
                    ModConfig.EventLengthMedium,
                    ModConfig.EventLengthLong,
                    ModConfig.EventLengthExtraLong
                },
                formatAllowedValue: value => value
            );

            configMenu.AddTextOption(
                mod: ModManifest,
                name: () => "Characteristic mode",
                tooltip: () => "Require OpenAI Key provided.\nSelect the preferred NPC characteristic quality.\nHigher quality provides more detailed characteristics but cost higher usage\n(minimal -100, long +100 extra token per NPC per use).",
                getValue: () => Config.CharacteristicMode,
                setValue: value => Config.CharacteristicMode = value,
                allowedValues: new string[]
                {
                    ModConfig.CharacteristicModeMinimal,
                    ModConfig.CharacteristicModeShort,
                    ModConfig.CharacteristicModeLong
                },
                formatAllowedValue: value => value
            );


        }

    }

}