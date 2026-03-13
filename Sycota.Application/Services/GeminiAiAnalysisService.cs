using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Sycota.Application.Interfaces;
using Sycota.Domain.Classes;

namespace Sycota.Application.Services;

public class GeminiAiAnalysisService : IAiAnalysisService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _modelName;
    private const string BaseUrl = "https://generativelanguage.googleapis.com/v1beta/models";

    public GeminiAiAnalysisService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiKey = configuration["Gemini:ApiKey"] ?? throw new InvalidOperationException("Gemini API key not configured");
        _modelName = configuration["Gemini:ModelName"] ?? "gemini-2.5-flash";
    }

    public async Task<ServiceResult<string>> AnalyzeSessionAsync(string sessionJson, string weaponType, string sessionName, DateTime sessionDate)
    {
        try
        {
            // Parse the session data to extract meaningful statistics
            var sessionSummary = ParseSessionData(sessionJson);
            
            var systemPrompt = GetSystemPrompt();
            var userPrompt = $@"Моля, анализирай тази тренировъчна сесия по стрелба и дай препоръки.
Отговори САМО на български език.

Сесия: {sessionName}
Дата: {sessionDate:dd.MM.yyyy}
Оръжие: {weaponType}

{sessionSummary}

Моля, предостави:
1. Обща оценка на представянето
2. Анализ на групирането на изстрелите (последователност, разпръскване)
3. Забелязани модели (посока на отклонение, групиране)
4. Конкретни области за подобрение
5. Практически съвети за следващата тренировка

Отговорът трябва да е кратък, но полезен, подходящ за състезателен стрелец. Не използвай форматиране (bold, italic и др.).";

            var response = await SendToGeminiAsync(systemPrompt, userPrompt, new List<ChatMessage>());
            return response;
        }
        catch (Exception ex)
        {
            return ServiceResult<string>.Fail($"Неуспешен анализ на сесията: {ex.Message}");
        }
    }

    public async Task<ServiceResult<string>> ChatAsync(string userMessage, string sessionContext, List<ChatMessage> conversationHistory)
    {
        try
        {
            // Parse session context to extract meaningful data
            var parsedContext = ParseSessionContextForChat(sessionContext);
            
            var systemPrompt = GetSystemPrompt() + $@"

Контекст на текущата сесия:
{parsedContext}

Продължаваш разговор за тази тренировъчна сесия. Отговаряй на въпросите на потребителя полезно и давай треньорски съвети когато е уместно. Не използвай форматиране (bold, italic и др.). Отговаряй САМО на български език.";

            var response = await SendToGeminiAsync(systemPrompt, userMessage, conversationHistory);
            return response;
        }
        catch (Exception ex)
        {
            return ServiceResult<string>.Fail($"Неуспешно получаване на AI отговор: {ex.Message}");
        }
    }

    private string ParseSessionData(string sessionJson)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(sessionJson) || sessionJson == "{}")
            {
                return "No shot data available for this session.";
            }

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var data = JsonSerializer.Deserialize<SessionData>(sessionJson, options);
            
            if (data == null)
            {
                return "Unable to parse shot data.";
            }

            var sb = new StringBuilder();
            
            // Warmup shots
            if (data.WarmupShots != null && data.WarmupShots.Count > 0)
            {
                sb.AppendLine($"Warmup Shots ({data.WarmupShots.Count} shots):");
                foreach (var shot in data.WarmupShots)
                {
                    var score = CalculateScore(shot.X, shot.Y);
                    sb.AppendLine($"  - Position: ({shot.X:F1}, {shot.Y:F1}) mm, Score: {score:F1}");
                }
                sb.AppendLine();
            }
            
            // Series groups
            if (data.Groups != null && data.Groups.Count > 0)
            {
                int seriesNum = 0;
                foreach (var group in data.Groups)
                {
                    seriesNum++;
                    var shots = group.Shots ?? new List<ShotData>();
                    var seriesType = group.ValueType ?? "10-shot-series";
                    
                    if (shots.Count == 0)
                    {
                        sb.AppendLine($"Series {seriesNum} ({seriesType}): No shots recorded");
                        continue;
                    }
                    
                    double totalScore = 0;
                    double sumX = 0, sumY = 0;
                    int tens = 0, innerTens = 0;
                    
                    sb.AppendLine($"Series {seriesNum} ({seriesType}, {shots.Count} shots):");
                    
                    foreach (var shot in shots)
                    {
                        var score = CalculateScore(shot.X, shot.Y);
                        totalScore += score;
                        sumX += shot.X;
                        sumY += shot.Y;
                        if (score >= 10.0) tens++;
                        if (score >= 10.5) innerTens++;
                        
                        sb.AppendLine($"  - Position: ({shot.X:F1}, {shot.Y:F1}) mm, Score: {score:F1}");
                    }
                    
                    var avgX = sumX / shots.Count;
                    var avgY = sumY / shots.Count;
                    var avgScore = totalScore / shots.Count;
                    
                    // Calculate group size (max spread)
                    double maxSpread = 0;
                    for (int i = 0; i < shots.Count; i++)
                    {
                        for (int j = i + 1; j < shots.Count; j++)
                        {
                            var dist = Math.Sqrt(Math.Pow(shots[i].X - shots[j].X, 2) + Math.Pow(shots[i].Y - shots[j].Y, 2));
                            maxSpread = Math.Max(maxSpread, dist);
                        }
                    }
                    
                    sb.AppendLine($"  Summary: Total={totalScore:F1}, Avg={avgScore:F2}, 10s={tens}, Inner 10s={innerTens}");
                    sb.AppendLine($"  Group center: ({avgX:F1}, {avgY:F1}) mm, Max spread: {maxSpread:F1} mm");
                    sb.AppendLine();
                }
            }
            
            return sb.ToString();
        }
        catch
        {
            return $"Raw shot data: {sessionJson}";
        }
    }

    private string ParseSessionContextForChat(string sessionContext)
    {
        // Extract the shot data JSON from the context and parse it
        try
        {
            var lines = sessionContext.Split('\n');
            var jsonLine = lines.FirstOrDefault(l => l.StartsWith("Shot Data:"));
            
            if (jsonLine != null)
            {
                var json = jsonLine.Replace("Shot Data:", "").Trim();
                var parsedData = ParseSessionData(json);
                
                // Rebuild context with parsed data
                var contextWithoutJson = string.Join('\n', lines.Where(l => !l.StartsWith("Shot Data:")));
                return contextWithoutJson + "\n\n" + parsedData;
            }
            
            return sessionContext;
        }
        catch
        {
            return sessionContext;
        }
    }

    private double CalculateScore(double x, double y)
    {
        const double PELLET_RADIUS = 2.25;
        const double RING_10_BOUNDARY = 0.25;
        double[] ringRadii = { 2.5, 5.0, 7.5, 10.0, 12.5, 15.0, 17.5, 20.0, 22.5 };

        var dist = Math.Sqrt(x * x + y * y);
        var innerEdge = Math.Max(0, dist - PELLET_RADIUS);

        if (innerEdge <= RING_10_BOUNDARY)
        {
            var maxCenterDistForTen = 2.5;
            var positionRatio = Math.Min(dist / maxCenterDistForTen, 1.0);
            var decimalScore = 10.9 - (positionRatio * 0.9);
            return Math.Round(decimalScore, 1);
        }

        double innerRadius = RING_10_BOUNDARY;
        for (int ring = 9; ring >= 1; ring--)
        {
            var outerRadius = ringRadii[9 - ring];
            if (innerEdge <= outerRadius)
            {
                var positionInRing = (innerEdge - innerRadius) / (outerRadius - innerRadius);
                var decimalScore = (ring + 0.9) - (positionInRing * 0.9);
                return Math.Round(decimalScore, 1);
            }
            innerRadius = outerRadius;
        }

        return 0;
    }

    private static string StripMarkdownFormatting(string text)
    {
        // Remove bold markers: **text** or __text__
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\*\*(.+?)\*\*", "$1");
        text = System.Text.RegularExpressions.Regex.Replace(text, @"__(.+?)__", "$1");
        // Remove italic markers: *text* or _text_ (single, not inside words)
        text = System.Text.RegularExpressions.Regex.Replace(text, @"(?<!\w)\*(.+?)\*(?!\w)", "$1");
        text = System.Text.RegularExpressions.Regex.Replace(text, @"(?<!\w)_(.+?)_(?!\w)", "$1");
        // Remove markdown headers: ## Header
        text = System.Text.RegularExpressions.Regex.Replace(text, @"^#{1,6}\s*", "", System.Text.RegularExpressions.RegexOptions.Multiline);
        // Replace bullet points with asterisks: * item -> - item
        text = System.Text.RegularExpressions.Regex.Replace(text, @"^(\s*)\*\s", "$1- ", System.Text.RegularExpressions.RegexOptions.Multiline);
        return text;
    }

    private string GetSystemPrompt()
    {
        return @"You are an expert ISSF (International Shooting Sport Federation) shooting coach AI assistant. 
You specialize in 10m Air Rifle and Air Pistol Olympic-style precision shooting.

CRITICAL LANGUAGE RULE:
- You MUST respond ONLY in Bulgarian (български език). Every single word of your response must be in Bulgarian.
- Do NOT write in English or any other language under any circumstances.
- Even if the user writes in English, you MUST still respond in Bulgarian.

CRITICAL FORMATTING RULES - YOU MUST FOLLOW THESE:
- Do NOT use any markdown formatting whatsoever.
- Do NOT use asterisks (*) for bold, italic, or bullet points.
- Do NOT use underscores (_) for bold or italic.
- Do NOT use hash symbols (#) for headers.
- Use simple dashes (-) for list items.
- Use plain text only with line breaks for separation.
- Write section titles as plain text on their own line, without any special characters before or after them.

Your knowledge includes:
- ISSF decimal scoring system (10.0 to 10.9 for the inner ring)
- Shot placement analysis and grouping patterns
- Common shooting technique issues and corrections
- Mental game and competition preparation
- Equipment considerations for precision shooting
- Training methodologies for competitive shooters

When analyzing shots:
- X coordinates: positive = right of center, negative = left
- Y coordinates: positive = down from center, negative = up
- Scores are calculated using inward gauging (inner edge of 4.5mm pellet)
- A tight group (small spread) indicates good consistency
- Shot drift patterns can indicate technique issues:
  - Consistent right drift: trigger finger pressure, grip issues
  - Consistent low shots: breathing, follow-through issues
  - Vertical spread: breathing control problems
  - Horizontal spread: trigger control or natural point of aim issues

Be encouraging but honest. Provide specific, actionable feedback.
Use shooting terminology appropriately but explain technical terms when needed.
Keep responses clear and readable without any special formatting.
Remember: ALL responses must be in Bulgarian.";
    }

    private async Task<ServiceResult<string>> SendToGeminiAsync(string systemPrompt, string userMessage, List<ChatMessage> history)
    {
        var url = $"{BaseUrl}/{_modelName}:generateContent?key={_apiKey}";

        var contents = new List<GeminiContent>();

        // Add conversation history
        foreach (var msg in history)
        {
            contents.Add(new GeminiContent
            {
                Role = msg.Role == "assistant" ? "model" : "user",
                Parts = new List<GeminiPart> { new GeminiPart { Text = msg.Content } }
            });
        }

        // Add the current user message with system context for first message
        var currentMessage = history.Count == 0 
            ? $"{systemPrompt}\n\n{userMessage}"
            : userMessage;

        contents.Add(new GeminiContent
        {
            Role = "user",
            Parts = new List<GeminiPart> { new GeminiPart { Text = currentMessage } }
        });

        var request = new GeminiRequest
        {
            Contents = contents,
            GenerationConfig = new GeminiGenerationConfig
            {
                Temperature = 0.7,
                MaxOutputTokens = 16000,
                TopP = 0.95,
                TopK = 40
            },
            SafetySettings = new List<GeminiSafetySetting>
            {
                new() { Category = "HARM_CATEGORY_HARASSMENT", Threshold = "BLOCK_ONLY_HIGH" },
                new() { Category = "HARM_CATEGORY_HATE_SPEECH", Threshold = "BLOCK_ONLY_HIGH" },
                new() { Category = "HARM_CATEGORY_SEXUALLY_EXPLICIT", Threshold = "BLOCK_ONLY_HIGH" },
                new() { Category = "HARM_CATEGORY_DANGEROUS_CONTENT", Threshold = "BLOCK_ONLY_HIGH" }
            }
        };

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        var jsonContent = JsonSerializer.Serialize(request, jsonOptions);
        var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(url, httpContent);
        var responseContent = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            return ServiceResult<string>.Fail($"Грешка в Gemini API: {response.StatusCode} - {responseContent}");
        }

        var geminiResponse = JsonSerializer.Deserialize<GeminiResponse>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        var text = geminiResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;
        
        if (string.IsNullOrEmpty(text))
        {
            return ServiceResult<string>.Fail("Не е получен отговор от AI");
        }

        // Strip any markdown formatting the model may have used despite instructions
        text = StripMarkdownFormatting(text);

        return ServiceResult<string>.Ok(text);
    }
}

#region Gemini API Models

internal class GeminiRequest
{
    public List<GeminiContent> Contents { get; set; } = new();
    public GeminiGenerationConfig? GenerationConfig { get; set; }
    public List<GeminiSafetySetting>? SafetySettings { get; set; }
}

internal class GeminiContent
{
    public string Role { get; set; } = "user";
    public List<GeminiPart> Parts { get; set; } = new();
}

internal class GeminiPart
{
    public string Text { get; set; } = string.Empty;
}

internal class GeminiGenerationConfig
{
    public double Temperature { get; set; } = 0.7;
    public int MaxOutputTokens { get; set; } = 4096;
    public double TopP { get; set; } = 0.95;
    public int TopK { get; set; } = 40;
}

internal class GeminiSafetySetting
{
    public string Category { get; set; } = string.Empty;
    public string Threshold { get; set; } = "BLOCK_MEDIUM_AND_ABOVE";
}

internal class GeminiResponse
{
    public List<GeminiCandidate>? Candidates { get; set; }
}

internal class GeminiCandidate
{
    public GeminiContent? Content { get; set; }
}

// Session data models for parsing
internal class SessionData
{
    public List<ShotData>? WarmupShots { get; set; }
    public List<GroupData>? Groups { get; set; }
}

internal class GroupData
{
    public int GroupId { get; set; }
    public string? ValueType { get; set; }
    public List<ShotData>? Shots { get; set; }
}

internal class ShotData
{
    public double X { get; set; }
    public double Y { get; set; }
}

#endregion
