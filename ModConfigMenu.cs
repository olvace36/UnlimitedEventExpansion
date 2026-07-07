using ContentPatcher;
using StardewModdingAPI;

namespace UnlimitedEventExpansion
{

    public partial class ModEntry
    {

        public static void ConfigMenu(IContentPatcherAPI api, IManifest modManifest, IModHelper helper)
        {
            // get Generic Mod Config Menu's API (if it's installed)
            var configMenu = helper.ModRegistry.GetApi<UnlimitedEventExpansion.Data.IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (configMenu is null)
                return;

            // register mod
            configMenu.Register(
                mod: modManifest,
                reset: () => Config = new ModConfig(),
                save: () => helper.WriteConfig(Config)
            );

            configMenu.AddSectionTitle(
                            mod: modManifest,
                            text: () => "Language"
                        );

            configMenu.AddTextOption(
                mod: modManifest,
                getValue: () => string.IsNullOrWhiteSpace(Config.Language) ? ModConfig.LanguageEnglish : Config.Language,
                setValue: value => Config.Language = string.IsNullOrWhiteSpace(value) ? ModConfig.LanguageEnglish : value.Trim(),
                name: () => "Language",
                tooltip: () => "Language used for generated dialogue. Use English for default behavior."
            );


            configMenu.AddSectionTitle(
                mod: modManifest,
                text: () => "AI configuration"
            );

            configMenu.AddParagraph(
                mod: modManifest,
                text: () => "All options below require your own API key to be effective. You still can use the mod but will have a limited usage. Return to title screen to see the options."
            );

            configMenu.SetTitleScreenOnlyForNextOptions(mod: modManifest, titleScreenOnly: true);

            configMenu.AddTextOption(
                mod: modManifest,
                getValue: () => Config?.Key ?? "",
                setValue: value => Config.Key = value?.Trim() ?? "",
                name: () => "Key",
                tooltip: () => "OpenAI or Gemini key. See mod page for instructions how to get one.\nOpenAI keys: https://platform.openai.com/account/api-keys\nGemini keys: https://aistudio.google.com/apikey"
            );

            configMenu.AddTextOption(
                mod: modManifest,
                name: () => "Model",
                tooltip: () => "Choose which model to use.\nOf course if you using OpenAI key, choose one of the OpenAI models; and vice versa for Gemini key.",
                getValue: () => Config.Model,
                setValue: value => Config.Model = value,
                allowedValues: ModConfig.AllSupportedModels,
                formatAllowedValue: FormatModel
            );

            configMenu.AddParagraph(
                mod: modManifest,
                text: () => "These options control event quality and pacing."
            );

            configMenu.AddBoolOption(
                mod: modManifest,
                name: () => "Ignore heart-level requirements",
                tooltip: () => "If enabled, you have access to events without the usual heart-level limits.\nRestart the game after changing this option.",
                getValue: () => Config.AllowEarlyEvent,
                setValue: value => Config.AllowEarlyEvent = value
            );

            configMenu.AddTextOption(
                mod: modManifest,
                name: () => "Event length",
                tooltip: () => "Length of the event dialogue.",
                getValue: () => Config.EventLength,
                setValue: value => Config.EventLength = value,
                allowedValues: new string[]
                {
                    ModConfig.EventLengthShort,
                    ModConfig.EventLengthMedium,
                    ModConfig.EventLengthLong,
                    ModConfig.EventLengthExtraLong
                },
                formatAllowedValue: FormatEventLength
            );

            configMenu.AddTextOption(
                mod: modManifest,
                name: () => "NPC detail level",
                tooltip: () => "Higher detail gives richer NPC personality at higher token cost.\nCompared to Standard: Minimal uses about 100 fewer tokens, Detailed uses about 100 extra tokens per NPC.",
                getValue: () => Config.CharacteristicMode,
                setValue: value => Config.CharacteristicMode = value,
                allowedValues: new string[]
                {
                    ModConfig.CharacteristicModeMinimal,
                    ModConfig.CharacteristicModeShort,
                    ModConfig.CharacteristicModeLong
                },
                formatAllowedValue: FormatCharacteristicMode
            );
        }

        private static string FormatModel(string value)
        {
            return value switch
            {
                ModConfig.ModelGpt51 => "GPT-5.1 (best quality, higher cost)",
                ModConfig.ModelGpt5Mini => "GPT-5 Mini (balanced)",
                ModConfig.ModelGpt5Nano => "GPT-5 Nano (lowest cost)",
                ModConfig.ModelGpt54Mini => "GPT-5.4 Mini (balanced, newer)",
                ModConfig.ModelGpt54Nano => "GPT-5.4 Nano (low cost, newer)",
                ModConfig.ModelGemini35Flash => "Gemini 3.5 Flash",
                ModConfig.ModelGemini31FlashLite => "Gemini 3.1 Flash Lite",
                ModConfig.ModelGemini3FlashPreview => "Gemini 3 Flash Preview",
                _ => value
            };
        }

        private static string FormatEventLength(string value)
        {
            return value switch
            {
                ModConfig.EventLengthShort => "Short (up to 10 lines)",
                ModConfig.EventLengthMedium => "Medium (up to 12 lines)",
                ModConfig.EventLengthLong => "Long (up to 15 lines)",
                ModConfig.EventLengthExtraLong => "Extra long (up to 20 lines)",
                _ => value
            };
        }

        private static string FormatCharacteristicMode(string value)
        {
            return value switch
            {
                ModConfig.CharacteristicModeMinimal => "Minimal (lower token use)",
                ModConfig.CharacteristicModeShort => "Standard (recommended)",
                ModConfig.CharacteristicModeLong => "Detailed (higher token use)",
                _ => value
            };
        }

    }

}