using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace DesktopAiTranslator.Translation;

public class QwenTranslatorProvider : ITranslatorProvider
{
    private readonly HttpClient _httpClient = new();
    private readonly Func<string> _baseUrlProvider;
    private readonly Func<string> _modelProvider;
    private readonly Func<string> _apiKeyProvider;

    public virtual string Name => "Qwen";

    public QwenTranslatorProvider(Func<string> baseUrlProvider, Func<string> modelProvider, Func<string> apiKeyProvider)
    {
        _baseUrlProvider = baseUrlProvider;
        _modelProvider = modelProvider;
        _apiKeyProvider = apiKeyProvider;
    }

    public async Task<TranslationResult> TranslateAsync(TranslationRequest request, CancellationToken cancellationToken)
    {
        var model = _modelProvider();
        var apiKey = _apiKeyProvider();
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return TranslationResult.Fail("请先在设置中配置 API Key", Name, model);
        }

        try
        {
            using var httpRequest = BuildRequest(request, apiKey, model);
            using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            return await ParseResponse(response, model, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return TranslationResult.Fail("翻译请求超时，请稍后重试", Name, model);
        }
        catch (HttpRequestException ex)
        {
            return TranslationResult.Fail($"网络请求失败：{ex.Message}", Name, model);
        }
        catch (Exception ex)
        {
            return TranslationResult.Fail($"翻译请求失败：{ex.Message}", Name, model);
        }
    }

    private HttpRequestMessage BuildRequest(TranslationRequest request, string apiKey, string model)
    {
        var body = new
        {
            model = string.IsNullOrWhiteSpace(request.Model) ? model : request.Model,
            messages = new[]
            {
                new { role = "system", content = PromptBuilder.BuildSystemPrompt(request) },
                new { role = "user", content = request.Text }
            },
            temperature = request.Temperature
        };

        var baseUrl = _baseUrlProvider().TrimEnd('/');
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/chat/completions");
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        httpRequest.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        return httpRequest;
    }

    private async Task<TranslationResult> ParseResponse(HttpResponseMessage response, string model, CancellationToken cancellationToken)
    {
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return TranslationResult.Fail(BuildStatusMessage(response.StatusCode), Name, model);
        }

        try
        {
            using var document = JsonDocument.Parse(content);
            var text = document.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? "";
            if (string.IsNullOrWhiteSpace(text))
            {
                return TranslationResult.Fail("API 返回为空", Name, model);
            }

            return TranslationResult.Ok(text.Trim(), Name, model);
        }
        catch (Exception ex)
        {
            return TranslationResult.Fail($"API 返回解析失败：{ex.Message}", Name, model);
        }
    }

    private static string BuildStatusMessage(HttpStatusCode statusCode)
    {
        return statusCode switch
        {
            HttpStatusCode.Unauthorized => "API Key 无效或未授权（401）",
            HttpStatusCode.Forbidden => "API Key 无权限访问该模型（403）",
            (HttpStatusCode)429 => "API 请求过于频繁或额度不足（429）",
            HttpStatusCode.NotFound => "API 地址或模型不存在（404）",
            HttpStatusCode.BadRequest => "API 请求参数错误，请检查模型名（400）",
            _ => $"API 返回错误：{(int)statusCode}"
        };
    }
}
