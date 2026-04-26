using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StardewValley.Menus;
using StardewValley.Objects;
using System.Collections;

namespace UnlimitedEventExpansion
{
  public partial class ModEntry
  {

    public static bool switched = false;
    public static bool IsMaxedLimit = false;
    public static int totalFailedCheck = 0;
    public static string EventModel = "";
    public static string EventKey = "";
    public static object EventReasoning = new { effort = "minimal" };




    public static async Task<(int, int)> GetOpenAIUsage(string admin_key)
    {
      List<string> premiumModels = new List<string> { "gpt-5.4", "gpt-5.2", "gpt-5.1", "gpt-5.1-codex", "gpt-5", "gpt-5-codex", "gpt-5-chat-latest", "gpt-4.1", "gpt-4o", "o1", "o3" };
      List<string> regularModels = new List<string> { "gpt-5.4-mini", "gpt-5.4-nano", "gpt-5.1-codex-mini", "gpt-5-mini", "gpt-5-nano", "gpt-4.1-mini", "gpt-4.1-nano", "gpt-4o-mini",
                                                            "o1-mini", "o3-mini", "o4-mini", "codex-mini-latest" };

      string usageUrl = "https://api.openai.com/v1/organization/usage/completions";
      using HttpClient client = new HttpClient();

      client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", admin_key);

      DateTime utcNow = DateTime.UtcNow;
      DateTime utcStartOfToday = utcNow.Date;
      long startTime = new DateTimeOffset(utcStartOfToday).ToUnixTimeSeconds();
      long endTime = new DateTimeOffset(utcNow).ToUnixTimeSeconds();

      try
      {
        var perModelTotals = new Dictionary<string, (long Input, long Output)>();
        string? nextPage = null;
        bool hasMore;

        do
        {
          var query = $"start_time={startTime}&end_time={endTime}&group_by=model";
          if (!string.IsNullOrWhiteSpace(nextPage))
            query += $"&page={Uri.EscapeDataString(nextPage)}";

          string requestUrl = $"{usageUrl}?{query}";
          HttpResponseMessage response = await client.GetAsync(requestUrl);

          if (!response.IsSuccessStatusCode)
          {
            string error = await response.Content.ReadAsStringAsync();
            SMonitor.Log($"Error: {response.StatusCode} - {error}", LogLevel.Error);
            return (-1, -1);
          }

          string jsonResponse = await response.Content.ReadAsStringAsync();
          JObject json = JObject.Parse(jsonResponse);

          var data = json["data"] as JArray;
          if (data != null)
          {
            foreach (var bucket in data)
            {
              var results = bucket?["results"] as JArray;
              if (results == null)
                continue;

              foreach (var result in results)
              {
                string? model = result?["model"]?.ToString();
                if (string.IsNullOrWhiteSpace(model))
                  model = "unknown";

                long inputTokens = result?["input_tokens"]?.Value<long>() ?? 0;
                long outputTokens = result?["output_tokens"]?.Value<long>() ?? 0;

                if (perModelTotals.TryGetValue(model, out var totals))
                {
                  perModelTotals[model] = (totals.Input + inputTokens, totals.Output + outputTokens);
                }
                else
                {
                  perModelTotals[model] = (inputTokens, outputTokens);
                }
              }
            }
          }

          hasMore = json["has_more"]?.Value<bool>() ?? false;
          nextPage = json["next_page"]?.ToString();
        }
        while (hasMore && !string.IsNullOrWhiteSpace(nextPage));

        if (perModelTotals.Count == 0)
        {
          return (-1, -1);
        }

        // Aggregate totals for premium and regular model groups
        long premiumInputTotal = 0, premiumOutputTotal = 0;
        long regularInputTotal = 0, regularOutputTotal = 0;

        foreach (var kv in perModelTotals)
        {
          var modelName = kv.Key ?? string.Empty;
          var input = kv.Value.Input;
          var output = kv.Value.Output;

          // Prefer matching regular (specific) names first, then premium.
          bool isRegular = regularModels.Any(rm =>
              modelName.StartsWith(rm, StringComparison.OrdinalIgnoreCase)
              || string.Equals(rm, modelName, StringComparison.OrdinalIgnoreCase)
          );
          bool isPremium = !isRegular && premiumModels.Any(pm =>
              modelName.StartsWith(pm, StringComparison.OrdinalIgnoreCase)
              || string.Equals(pm, modelName, StringComparison.OrdinalIgnoreCase)
          );

          if (isRegular)
          {
            regularInputTotal += input;
            regularOutputTotal += output;
          }
          else if (isPremium)
          {
            premiumInputTotal += input;
            premiumOutputTotal += output;
          }
        }

        SMonitor.Log($"Premium models: {premiumInputTotal} input tokens, {premiumOutputTotal} output tokens.\nRegular models: {regularInputTotal} input tokens, {regularOutputTotal} output tokens.", LogLevel.Error);
        return ((int)(premiumInputTotal + premiumOutputTotal), (int)(regularInputTotal + regularOutputTotal));
      }
      catch (Exception ex)
      {
        SMonitor.Log($"Request failed: {ex.Message}", LogLevel.Error);
        return (-1, -1);
      }
    }



    private static async Task<string> RequestOpenAiResponseAsync(string instructionPrompt, string userPrompt)
    {
      // SMonitor.Log("\n========================", LogLevel.Error);
      // SMonitor.Log(instructionPrompt, LogLevel.Error);
      // SMonitor.Log("\n========================", LogLevel.Error);
      // SMonitor.Log(userPrompt, LogLevel.Error);

      using var httpClient = new HttpClient();
      httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", EventKey);

      var requestBody = new Dictionary<string, object>
            {
                { "model", EventModel },
                { "input", new object[]
                    {
                        new
                        {
                            role = "developer",
                            content = new object[]
                            {
                                new { type = "input_text", text = instructionPrompt }
                            }
                        },
                        new
                        {
                            role = "user",
                            content = new object[]
                            {
                                new { type = "input_text", text = userPrompt }
                            }
                        }
                    }
                },
                { "text", new { format = new { type = "json_object" }, verbosity = "medium" } },
                { "reasoning", EventReasoning }
            };

      var jsonRequest = JsonConvert.SerializeObject(requestBody);
      var httpContent = new StringContent(jsonRequest, Encoding.UTF8, "application/json");
      var httpResponse = await httpClient.PostAsync("https://api.openai.com/v1/responses", httpContent);
      var jsonResponse = await httpResponse.Content.ReadAsStringAsync();

      if (httpResponse.IsSuccessStatusCode)
      {
        // SMonitor.Log(jsonResponse, LogLevel.Error);

        if (TryExtractResponseMessage(jsonResponse, out var responseMessage))
          return responseMessage;

        return string.Empty;
      }

      LogOpenAiError(httpResponse, jsonResponse);
      return string.Empty;
    }

    private static bool TryExtractResponseMessage(string jsonResponse, out string responseMessage)
    {
      responseMessage = string.Empty;

      try
      {
        var responseJson = JObject.Parse(jsonResponse);
        responseMessage = responseJson.Value<string>("output_text") ?? string.Empty;

        if (string.IsNullOrWhiteSpace(responseMessage))
        {
          responseMessage = string.Join("\n", responseJson
              .SelectTokens("$.output[*].content[*]")
              .OfType<JObject>()
              .Where(content => string.Equals(content.Value<string>("type"), "output_text", StringComparison.OrdinalIgnoreCase))
              .Select(content => content.Value<string>("text"))
              .Where(text => !string.IsNullOrWhiteSpace(text)));
        }

        return !string.IsNullOrWhiteSpace(responseMessage);
      }
      catch (JsonException ex)
      {
        SMonitor.Log($"Error parsing OpenAI response: {ex.Message}", LogLevel.Error);
        return false;
      }
    }

    private static JToken ConvertToJToken(string responseMessage)
    {
      try
      {
        JToken parsed = JToken.Parse(responseMessage);
        return parsed;
      }
      catch (JsonReaderException)
      {
        SMonitor.Log("Error: Assistant returned invalid JSON", LogLevel.Error);
      }
      return null;
    }

    private static void LogOpenAiError(HttpResponseMessage httpResponse, string responseBody)
    {
      var statusCode = (int)httpResponse.StatusCode;
      string errorMessage = "Check for mod update";

      switch (statusCode)
      {
        case 403:
          errorMessage = "Country, region, or territory not supported.";
          break;
        case 429:
          errorMessage = "Please try again in a few minutes. If not work, then total AI usage for all players has passed the limit set by OpenAI. This will be reset the next day in timezone UTC+0";
          break;
        case 500:
          errorMessage = "Server Error: The server had an issue while processing your request. Please try again.";
          break;
        case 503:
          errorMessage = "Server Overload: The server is experiencing high traffic. Please try again later.";
          break;
      }

      SMonitor.Log($"OpenAI error {statusCode}: {responseBody}", LogLevel.Error);
      SMonitor.Log($"Unable to receive AI content. {errorMessage}\n\n", LogLevel.Error);
    }


    private static string GetNpcCharacteristicForPrompt(string npcName, bool minimal = false)
    {
      if (string.IsNullOrWhiteSpace(npcName) || Game1.getCharacterFromName(npcName) is not NPC npc || npc is null)
        return string.Empty;

      string npcAge = npc.Age == 0 ? "adult" : npc.Age == 1 ? "teens" : npc.Age == 2 ? "child" : "adult";
      string npcManner = npc.Manners == 0 ? "a typical neutral manner" : npc.Manners == 1 ? "a polite and courteous manner" : npc.Manners == 2 ? "a distant and reserved manner" : "a typical neutral manner";
      string npcSocial = npc.SocialAnxiety == 0 ? "an outgoing person" : npc.SocialAnxiety == 1 ? "a little shy person" : "neither too outgoing nor shy";

      string npcCharacteristic = $" {npc.Name} is {npcAge}, {npcManner}, and is {npcSocial}";

      // CUSTOM CHARACTERISTIC OVERRIDE
      if (!string.IsNullOrWhiteSpace(Config.OpenAIKey))
      {
        if ( Config.CharacteristicMode == ModConfig.CharacteristicModeLong && NpcCharacteristicsLong.TryGetValue(npc.Name, out string? customCharacteristicLong) && !string.IsNullOrWhiteSpace(customCharacteristicLong))
        {
          npcCharacteristic = customCharacteristicLong;
        }
        else if (Config.CharacteristicMode == ModConfig.CharacteristicModeShort && NpcCharacteristicsShort.TryGetValue(npc.Name, out string? customCharacteristic) && !string.IsNullOrWhiteSpace(customCharacteristic))
        {
          npcCharacteristic = customCharacteristic;
        }
        else if (Config.CharacteristicMode == ModConfig.CharacteristicModeMinimal && NpcCharacteristicsMinimal.TryGetValue(npc.Name, out string? customCharacteristicMinimal) && !string.IsNullOrWhiteSpace(customCharacteristicMinimal))
        {
          npcCharacteristic = customCharacteristicMinimal;
        }
        return npcCharacteristic.Trim();
      }
      // DEFAULT CHARACTERISTIC
      else
      {
        if (NpcCharacteristicsShort.TryGetValue(npc.Name, out string? customCharacteristic) && !string.IsNullOrWhiteSpace(customCharacteristic) && !minimal)
        {
          npcCharacteristic = customCharacteristic;
          if (npcCharacteristic.Length > 800)
            npcCharacteristic = npcCharacteristic.Substring(0, 800);
        }
        else if (NpcCharacteristicsMinimal.TryGetValue(npc.Name, out string? customCharacteristicMinimal) && !string.IsNullOrWhiteSpace(customCharacteristicMinimal) && minimal)
        {
          npcCharacteristic = customCharacteristicMinimal;
          if (npcCharacteristic.Length > 400)
            npcCharacteristic = npcCharacteristic.Substring(0, 400);
        }

        return npcCharacteristic.Trim();
      }
    }


    private static string GetPreferedEventLength()
    {
      if (string.IsNullOrWhiteSpace(Config.OpenAIKey))
        return "Keep the total conversation under 8-10 exchanges, with each dialogue under 30 words.";

      switch (Config.EventLength)
      {
        case ModConfig.EventLengthShort:
          return "Keep the total conversation under 8-10 exchanges, with each dialogue under 30 words.";
        case ModConfig.EventLengthMedium:
          return "Keep the total conversation under 10-12 exchanges, with each dialogue under 35 words.";
        case ModConfig.EventLengthLong:
          return "Keep the total conversation under 12-15 exchanges, with each dialogue under 40 words.";
        case ModConfig.EventLengthExtraLong:
          return "Keep the total conversation under 15-20 exchanges, with each dialogue under 40 words.";
        default:
          return "Keep the total conversation under 8-10 exchanges, with each dialogue under 30 words.";
      }
    }


    // -------------------------------------------------------------------------------------------------
    // END OF EVENT GENERATION
    // -------------------------------------------------------------------------------------------------










    // --------
    // Multiple NPC Event
    // --------


    // BIRTHDAY PARTY
    public static async Task<string> GenerateBirthdayEvent(string npcTarget, string guestName, string locationName, List<Item> foodItems)
    {
      string currentTime = Game1.timeOfDay < 1200 ? $"morning" : Game1.timeOfDay < 1800 ? $"afternoon" : $"evening";
      string npcCharacteristic = "";

      List<string> guestNames = $"{npcTarget}, {guestName}"
          .Split(',', StringSplitOptions.RemoveEmptyEntries)
          .Select(npc => npc.Trim())
          .ToList();

      foreach (var name in guestNames.Take(5))
        npcCharacteristic += GetNpcCharacteristicForPrompt(name, true);


      var system = @$"
You are a narrative design assistant specializing in creating dialogue scenes about a birthday party for a NPC in context of game Stardew Valley. The PLAYER and some other NPCs are attending the party. 
Your task is to generate engaging and naturalistic conversation for the host to exchange with the PLAYER and the guests, and some among the guests as well. Not all guests need to speak. Output a structured JSON format. {GetPreferedEventLength()}

Your objectives:
- Create realistic and natural dialogues in the style of Stardew Valley 
- Use a mix of monologue (D) and question for the PLAYER (Q), but mostly should be monologue (D) from the NPC. Question (Q) only be used when the NPC is talking to the PLAYER, not between NPCs.
- If it is a monologue (D) or if it is NPC's reponse to player's choices, you will pick 1 portrait that best fit with the what NPC saying.
- For each question (Q), offer 2 to 4 player response choices, each will come with a corresponding response dialogue from the NPC.
- Ensure that the scene includes setup, emotional build-up, and a wrap-up to signal the end of the interaction.
- Pick a background music code based on the emotional tone and the context of the birthday party.
- Keep the tone consistent with the relationship and NPC characteristics.
- Slice of Life: You can invent small, mundane details happening or happened in context of the game. Be dynamic with the topic and questions. Do not be repetitive.

Formatting Rules:
Respond using only a single JSON object with this structure:
{{
  ""music"": ""background_music_key"",
  ""dialogue"": [
    {{
      ""type"": ""D"",
      ""npc"": ""NPC name"",
      ""dialogue"": ""NPC speaks here."",
      ""portrait"": ""Portrait ID here""
    }},
    {{
      ""type"": ""Q"",
      ""npc"": ""NPC name"",
      ""dialogue"": ""Question for the PLAYER."",
      ""player"": [
        {{
          ""response"": ""Player response option for the question."",
          ""reaction"": ""NPC dialogue to response back."",
          ""portrait"": ""Portrait ID here""
        }}
      ]
    }}
  ]
}}

Background Music Options:
Use one of the following as the ""music"" field:
Emotional tone: ""sweet"", ""breezy"", ""playful"", ""ragtime"", ""50s"", ""SettlingIn""
Seasonal tone: ""spring1"", ""spring2"", ""summer1"", ""summer2"", ""fall1"", ""fall2"", ""winter1"", ""winter2""

Portrait Options:
Use one of the following as the ""portrait"" field. Must use the id only (for example $0), not the whole portrait name:
{string.Join("\n", $"{string.Join(", ", guestNames.Take(5))}".Split(',')
.Select(npc =>
{
  var name = npc.Trim();
  var portrait = NpcPortrait.ContainsKey(name) ? NpcPortrait[name] : "Neutral: $0";
  return $"{name}: {portrait}";
}))}

Some context for you:
Current time is {currentTime}, and party location is {locationName}.
NPCs characteristic: {npcCharacteristic}

Style Guidelines:
 - Dialogue should feel personal, grounded, and affectionate. It should be raw sentence as how NPC will speak, do not add emotional explanations, and must not use character '\'.
 - Questions should open opportunities for meaningful roleplay or emotional responses. For Player response, give them both ways so they can make their choice.
 - Do not include any icons, comments, or extra formatting. Stay within the limit.
";

      var user = @$"{npcTarget} is holding a party at {locationName} to celebrate his/her birthday. Player {Game1.player.Name}, {string.Join(", ", guestNames.Skip(1).Take(4))} and more are attending.
      They are bringing {string.Join(", ", foodItems.ConvertAll(item => item.Name))} to the party, along with many gifts for {npcTarget}. {(string.IsNullOrEmpty(birthdayGiftName) ? "" : $"Player {Game1.player.Name} gift for {npcTarget} this year is {birthdayGiftName}.")}";
      birthdayGiftName = "";

      var responseMessage = await RequestOpenAiResponseAsync(system, user);
      if (!string.IsNullOrWhiteSpace(responseMessage))
        return responseMessage;
      return string.Empty;
    }



    // CAMPFIRE
    public static async Task<string> GenerateCampfireEvent(string npcs, string locationName)
    {
      string currentTime = Game1.timeOfDay < 1200 ? $"morning" : Game1.timeOfDay < 1800 ? $"afternoon" : $"evening";

      string data = $"\nWorld context: Today weather: {Game1.currentLocation.GetWeather().Weather}; Tomorrow weather: {Game1.weatherForTomorrow}; Current season: {Game1.currentLocation.GetSeason()};";


      List<string> npcNames = npcs.Split(',')
      .Select(npc =>
      {
        return npc.Trim();
      }).ToList();

      string npcCharacteristicMinimal = string.Join(". ", npcNames.Take(5).Select(name => GetNpcCharacteristicForPrompt(name, true)));
      var system = @$"
You are a narrative design assistant specializing in creating dialogue scenes about a campfire event between the PLAYER and a group of close friends in context of game Stardew Valley. 
Your task is to generate dialogues for the NPC to exchange with Player during a campfire night. Output a structured JSON format. {GetPreferedEventLength()}

Your objectives:
- Create realistic and natural dialogues in the style of Stardew Valley 
- Use a mix of monologue (D) and player choice moments (Q), but mostly should be monologue (D) from the NPC. Question (Q) only be used when the NPC is talking to the PLAYER, not between NPCs.
- If it is a monologue (D) or if it is NPC's reponse to player's choices, you will pick 1 portrait that best fit with the what NPC saying.
- For each question (Q), offer 2 to 3 player response choices, each will come with a corresponding response dialogue from the NPC.
- Ensure that the scene includes setup, emotional build-up, and a wrap-up.
- Pick a background music code based on the emotional tone and the context of the campfire event.
- Keep the tone consistent with the relationship, prior interactions, and NPC characteristics.
- Slice of Life: You can invent small, mundane details happening or happened in context of the game. Be dynamic with the topic and questions. Do not be repetitive.

Formatting Rules:
Respond using only a single JSON object with this structure:
{{
  ""music"": ""background_music_key"",
  ""dialogue"": [
    {{
      ""type"": ""D"",
      ""npc"": ""NPC name"",
      ""dialogue"": ""NPC speaks here."",
      ""portrait"": ""Portrait ID here""
    }},
    {{
      ""type"": ""Q"",
      ""npc"": ""NPC name"",
      ""dialogue"": ""Question for the PLAYER."",
      ""player"": [
        {{
          ""response"": ""Player response option for the question."",
          ""reaction"": ""NPC dialogue to response back."",
          ""portrait"": ""Portrait ID here""
        }}
      ]
    }}
  ]
}}

Background Music Options:
Use one of the following as the ""music"" field:
Emotional tone: ""sweet"", ""breezy"", ""playful"", ""ragtime"", ""50s"", ""SettlingIn""
Seasonal tone: ""spring1"", ""spring2"", ""summer1"", ""summer2"", ""fall1"", ""fall2"", ""winter1"", ""winter2""

Portrait Options:
Use one of the following as the ""portrait"" field. Use the portrait id (for example $0), not the portrait name:
{string.Join("\n", npcs.Split(',').Take(5)
.Select(npc =>
{
  var name = npc.Trim();
  var portrait = NpcPortrait.ContainsKey(name) ? NpcPortrait[name] : "Neutral: $0";
  return $"{name}: {portrait}";
}))}

Some context for you:
Current time is {currentTime}, campfire location is {locationName}.
NPC characteristic: {npcCharacteristicMinimal}

Style Guidelines:
 - Dialogue should feel personal, grounded, and affectionate. It should be raw sentence as how NPC will speak, do not give emotional explanations, and must not use character '\'.
 - Questions should open opportunities for meaningful roleplay or emotional responses. For Player response, give them both ways so they can make their choice.
 - Do not include any explanations, comments, or extra formatting. Stay within the limit. Only return the structured JSON.
";


      var user = @$"NPC {string.Join(", ", npcs.Split(',').Take(5).ToArray())} and Player {Game1.player.Name} is having a campfire outing together at {locationName}.
This is some other context you can use: {data}";

      var responseMessage = await RequestOpenAiResponseAsync(system, user);
      if (!string.IsNullOrWhiteSpace(responseMessage))
        return responseMessage;
      return string.Empty;
    }










    // --------
    // Single NPC Event
    // --------



    // DINE OUT
    public static async Task<string> GenerateDineOutEvent(string npcTarget, string locationName, string playerDish, string npcDish)
    {
      NPC npc = Game1.getCharacterFromName(npcTarget);
      string currentTime = Game1.timeOfDay < 1200 ? $"morning" : Game1.timeOfDay < 1800 ? $"afternoon" : $"evening";

      int heartLevel = 0;
      if (Game1.player.friendshipData.ContainsKey(npc.Name)) heartLevel = (int)Game1.player.friendshipData[npc.Name].Points / 250;
      string relation = heartLevel <= 6 ? "friend" : "best friend";
      bool isDating = Game1.player.friendshipData.TryGetValue(npc.Name, out Friendship friendship) && friendship.IsDating();
      bool isRoommate = friendship != null && friendship.IsRoommate();
      bool isMarried = friendship != null && friendship.IsMarried();
      bool isEngaged = friendship != null && friendship.IsEngaged();
      bool isDivorced = friendship != null && friendship.IsDivorced();

      if (isDivorced) relation = "divorced";
      else if (isRoommate) relation = "roommate";
      else if (isMarried) relation = "married";
      else if (isEngaged) relation = "engaged";
      else if (isDating) relation = "dating";

      string summary = "";
      if (npcConversationSummary.ContainsKey(npc.Name))
        summary = $"\nSummary of previous conversation: {npcConversationSummary[npc.Name]}";

      string data = $"\nWorld context: Today weather: {Game1.currentLocation.GetWeather().Weather}; Tomorrow weather: {Game1.weatherForTomorrow}; Current season: {Game1.currentLocation.GetSeason()};";


      string npcCharacteristic = GetNpcCharacteristicForPrompt(npcTarget);

      var system = @$"
You are a narrative design assistant specializing in creating dialogue scenes for a dine out event between the PLAYER and an NPC in context of game Stardew Valley. Your role is to generate engaging and naturalistic dialogue for them to exchange during the meal. Output a structured JSON format. {GetPreferedEventLength()}

Your objectives:
- Create realistic and natural dialogues in the style of Stardew Valley 
- Use a mix of monologue (D) and player choice moments (Q), but mostly should be monologue (D) from the NPC. Question (Q) only be used when the NPC is talking to the PLAYER.
- If it is a monologue (D) or if it is the NPC's response to player's choices, you will pick 1 portrait that best fit with the what the NPC is saying.
- For each question (Q), offer 2 to 4 player response choices, each will come with a corresponding response dialogue from the NPC.
- Ensure that the scene includes setup, emotional build-up, and a wrap-up to signal the end of the interaction.
- Pick a background music code based on the emotional tone and the context of the dine out event.
- Keep the tone consistent with the relationship, prior conversation, and NPC characteristics.
- Slice of Life: You can invent small, mundane details happening or happened in context of the game. Be dynamic with the topic and questions. Do not be repetitive.

Formatting Rules:
Respond using only a single JSON object with this structure:
{{
  ""music"": ""background_music_key"",
  ""dialogue"": [
    {{
      ""type"": ""D"",
      ""dialogue"": ""NPC speaks here."",
      ""portrait"": ""Portrait ID here""
    }},
    {{
      ""type"": ""Q"",
      ""dialogue"": ""NPC asks a question here."",
      ""player"": [
        {{
          ""response"": ""Player response option."",
          ""reaction"": ""NPC dialogue to response back."",
          ""portrait"": ""Portrait ID here""
        }}
      ]
    }}
  ]
}}

Background Music Options:
Use one of the following as the ""music"" field:
Emotional tone: ""sweet"", ""breezy"", ""playful"", ""ragtime"", ""50s"", ""SettlingIn""
Seasonal tone: ""spring1"", ""spring2"", ""summer1"", ""summer2"", ""fall1"", ""fall2"", ""winter1"", ""winter2""

Portrait Options:
Use one of the following as the ""portrait"" field. You must use the id only (for example $0), not the whole portrait name:
{(NpcPortrait.ContainsKey(npcTarget) ? NpcPortrait[npcTarget] : "Neutral: $0")}

Some context for you:
Current time is {currentTime}, party location is {locationName}, and Player {Game1.player.Name} is {relation} to {npcTarget}.
NPC characteristic: {npcCharacteristic}

Style Guidelines:
 - Dialogue should feel personal, grounded, and affectionate. It should be raw sentence as how NPC will speak, do not include emotional explanations, and must not use character '\'.
 - Questions should open opportunities for meaningful roleplay or emotional responses. For Player response, give them both ways so they can make their choice.
 - Do not include any icons, comments, or extra formatting.";

      var user = @$"NPC {npcTarget} and Player {Game1.player.Name} is dining out together at {locationName}. Player {Game1.player.Name} having {playerDish}, and {npcTarget} having {npcDish}.
This is some other context you can use: {summary} {data}";


      var responseMessage = await RequestOpenAiResponseAsync(system, user);
      if (!string.IsNullOrWhiteSpace(responseMessage))
        return responseMessage;

      return string.Empty;
    }




    // PICNIC
    public static async Task<string> GeneratePicnicEvent(string npcTarget, string locationName, List<Item> foodItems)
    {
      NPC npc = Game1.getCharacterFromName(npcTarget);
      string currentTime = Game1.timeOfDay < 1200 ? $"morning" : Game1.timeOfDay < 1800 ? $"afternoon" : $"evening";

      int heartLevel = 0;
      if (Game1.player.friendshipData.ContainsKey(npc.Name)) heartLevel = (int)Game1.player.friendshipData[npc.Name].Points / 250;
      string relation = "";

      bool isDating = Game1.player.friendshipData.TryGetValue(npc.Name, out Friendship friendship) && friendship.IsDating();
      bool isRoommate = friendship != null && friendship.IsRoommate();
      bool isMarried = friendship != null && friendship.IsMarried();
      bool isEngaged = friendship != null && friendship.IsEngaged();
      bool isDivorced = friendship != null && friendship.IsDivorced();

      if (isDivorced) relation = "divorced";
      else if (isRoommate) relation = "roommate";
      else if (isMarried) relation = "married";
      else if (isEngaged) relation = "engaged";
      else if (isDating) relation = "dating";
      else relation = heartLevel <= 6 ? "friend" : "best friend";

      string summary = "";
      if (npcConversationSummary.ContainsKey(npc.Name))
        summary = $"\n{(npcConversationSummary.ContainsKey(npc.Name) ? "Summary of previous conversation: " + npcConversationSummary[npc.Name] : "")}";

      string data = $"\nWorld context: Today weather: {Game1.currentLocation.GetWeather().Weather}; Tomorrow weather: {Game1.weatherForTomorrow}; Current season: {Game1.currentLocation.GetSeason()};";

      string npcCharacteristic = GetNpcCharacteristicForPrompt(npcTarget);

      var system = @$"
You are a narrative design assistant specializing in creating dialogue scenes about a picnic event between the PLAYER and an NPC in context of game Stardew Valley. Your role is to generate engaging and naturalistic dialogue for them to exchange during the picnic. Output a structured JSON format. {GetPreferedEventLength()}

Your objectives:
- Create realistic and natural dialogues in the style of Stardew Valley 
- Use a mix of monologue (D) and player choice moments (Q), but mostly should be monologue (D) from the NPC. Question (Q) only be used when the NPC is talking to the PLAYER.
- If it is a monologue (D) or if it is the NPC's response to player's choices, you will pick 1 portrait that best fit with the what the NPC is saying.
- For each question (Q), offer 2 to 4 player response choices, each will come with a corresponding response dialogue from the NPC.
- Ensure that the scene includes setup, emotional build-up, and a wrap-up to signal the end of the interaction.
- Pick a background music code based on the emotional tone and the context of the picnic event.
- Keep the tone consistent with the relationship, prior conversation, and NPC characteristics.
- Slice of Life: You can invent small, mundane details happening or happened in context of the game. Be dynamic with the topic and questions. Do not be repetitive.

Formatting Rules:
Respond using only a single JSON object with this structure:
{{
  ""music"": ""background_music_key"",
  ""dialogue"": [
    {{
      ""type"": ""D"",
      ""dialogue"": ""NPC speaks here."",
      ""portrait"": ""Portrait ID here""
    }},
    {{
      ""type"": ""Q"",
      ""dialogue"": ""NPC asks a question here."",
      ""player"": [
        {{
          ""response"": ""Player response option."",
          ""reaction"": ""NPC dialogue to response back."",
          ""portrait"": ""Portrait ID here""
        }}
      ]
    }}
  ]
}}

Background Music Options:
Use one of the following as the ""music"" field:
Emotional tone: ""sweet"", ""breezy"", ""playful"", ""ragtime"", ""50s"", ""SettlingIn""
Seasonal tone: ""spring1"", ""spring2"", ""summer1"", ""summer2"", ""fall1"", ""fall2"", ""winter1"", ""winter2""

Portrait Options:
Use one of the following as the ""portrait"" field. Use the id for example $0, not the portrait name:
{(NpcPortrait.ContainsKey(npcTarget) ? NpcPortrait[npcTarget] : "Neutral: $0")}

Some context for you:
Current time is {currentTime}, picnic location is {locationName}, and Player {Game1.player.Name} is {relation} to {npcTarget}.
{npcTarget} characteristic: {npcCharacteristic}

Style Guidelines:
 - Dialogue should feel personal, grounded, and affectionate. It should be raw sentence as how NPC will speak, do not include emotional explanations, and must not use character '\'.
 - Questions should open opportunities for meaningful roleplay or emotional responses. For Player response, give them both ways so they can make their choice.
 - Do not include any icons, comments, or extra formatting.

";

      var user = @$"{npcTarget} and Player {Game1.player.Name} is going for a picnic together at {locationName}. They are bringing {string.Join(", ", foodItems.ConvertAll(item => item.Name))} for the trip.
This is some other context you can use: {summary} {data}";

      var responseMessage = await RequestOpenAiResponseAsync(system, user);
      if (!string.IsNullOrWhiteSpace(responseMessage))
        return responseMessage;
      return string.Empty;
    }




  }
}
