using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace RaceDay.Web.Services;

public class ApiResult<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string Error { get; set; } = string.Empty;
    public int StatusCode { get; set; }
}

public class ApiClient
{
    private readonly HttpClient _http;
    private readonly IHttpContextAccessor _ctx;
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public ApiClient(HttpClient http, IHttpContextAccessor ctx)
    {
        _http = http;
        _ctx = ctx;
    }

    public Task<ApiResult<T>> GetAsync<T>(string url) => SendAsync<T>(HttpMethod.Get, url);
    public Task<ApiResult<T>> PostAsync<T>(string url, object? body) => SendAsync<T>(HttpMethod.Post, url, body);
    public Task<ApiResult<T>> PutAsync<T>(string url, object? body) => SendAsync<T>(HttpMethod.Put, url, body);
    public Task<ApiResult<T>> DeleteAsync<T>(string url) => SendAsync<T>(HttpMethod.Delete, url);

    // For file uploads (event banner, profile picture)
    public async Task<ApiResult<T>> UploadAsync<T>(string url, IFormFile file, HttpMethod? method = null)
    {
        try
        {
            using var content = new MultipartFormDataContent();
            var fileContent = new StreamContent(file.OpenReadStream());
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
            content.Add(fileContent, "file", file.FileName);

            var req = new HttpRequestMessage(method ?? HttpMethod.Post, url) { Content = content };
            AttachCookie(req);
            var res = await _http.SendAsync(req);
            return await ReadResult<T>(res);
        }
        catch
        {
            return new ApiResult<T> { Error = "We couldn't reach the server. Please try again." };
        }
    }

    private async Task<ApiResult<T>> SendAsync<T>(HttpMethod method, string url, object? body = null)
    {
        try
        {
            var req = new HttpRequestMessage(method, url);
            if (body != null)
                req.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
            AttachCookie(req);
            var res = await _http.SendAsync(req);
            return await ReadResult<T>(res);
        }
        catch
        {
            return new ApiResult<T> { Error = "We couldn't reach the server. Please try again." };
        }
    }

    // Send the API's session cookie (saved in this user's MVC session) with every request
    private void AttachCookie(HttpRequestMessage req)
    {
        var cookie = _ctx.HttpContext?.Session.GetString(SessionKeys.ApiCookie);
        if (!string.IsNullOrEmpty(cookie))
            req.Headers.TryAddWithoutValidation("Cookie", cookie);
    }

    // Save the cookie the API gives us (happens on login)
    private void CaptureCookie(HttpResponseMessage res)
    {
        if (!res.Headers.TryGetValues("Set-Cookie", out var values)) return;
        var pairs = values.Select(v => v.Split(';')[0].Trim()).Where(v => v.Length > 0);
        var joined = string.Join("; ", pairs);
        if (joined.Length > 0)
            _ctx.HttpContext?.Session.SetString(SessionKeys.ApiCookie, joined);
    }

    private async Task<ApiResult<T>> ReadResult<T>(HttpResponseMessage res)
    {
        CaptureCookie(res);
        var text = await res.Content.ReadAsStringAsync();
        var code = (int)res.StatusCode;

        if (!res.IsSuccessStatusCode)
            return new ApiResult<T> { StatusCode = code, Error = FriendlyError(code, text) };

        var data = string.IsNullOrWhiteSpace(text) ? default : JsonSerializer.Deserialize<T>(text, JsonOpts);
        return new ApiResult<T> { Success = true, Data = data, StatusCode = code };
    }

    // Use the API's own friendly "message" if it sent one, otherwise a safe default
    private static string FriendlyError(int code, string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.ValueKind == JsonValueKind.Object &&
                doc.RootElement.TryGetProperty("message", out var m) &&
                m.ValueKind == JsonValueKind.String)
                return m.GetString()!;
        }
        catch { }

        return code switch
        {
            400 => "Some of the information provided is invalid.",
            401 => "Please log in to continue.",
            403 => "You don't have permission to do that.",
            404 => "We couldn't find what you were looking for.",
            409 => "That already exists or conflicts with existing data.",
            _ => "Something went wrong. Please try again."
        };
    }
}
