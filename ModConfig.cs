using StardewModdingAPI;
using StardewValley;
using System.Collections.Generic;

namespace UnlimitedEventExpansion
{
    public class ModConfig
    {
        /// <summary>
        /// If you have an OpenAI key, provide it here.
        /// </summary>
        public const string OpenAIModel_51 = "gpt-5.1";
        public const string OpenAIModel_5mini = "gpt-5-mini";
        public const string OpenAIModel_5nano = "gpt-5-nano";
        public const string OpenAIModel_54mini = "gpt-5.4-mini";
        public const string OpenAIModel_54nano = "gpt-5.4-nano";

        public const string EventLengthShort = "short";
        public const string EventLengthMedium = "medium";
        public const string EventLengthLong = "long";
        public const string EventLengthExtraLong = "extra_long";

        public const string CharacteristicModeMinimal = "minimal";
        public const string CharacteristicModeShort = "short";
        public const string CharacteristicModeLong = "long";

        public string OpenAIKey { get; set; } = "";
        public string OpenAIModel { get; set; } = OpenAIModel_51;
        public bool AllowEarlyEvent { get; set; } = false;

        public string EventLength { get; set; } = EventLengthShort;
        public string CharacteristicMode { get; set; } = CharacteristicModeShort;

    }
}
