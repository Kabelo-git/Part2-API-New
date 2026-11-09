using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace RaceDay.Web.Services;

public class ApiResult<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string Error { get; set; } = string.Empty;
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

    // For file uploads (banner image, profile picture)
    public async Task<ApiResult<T>> UploadAsync<T>(string url, IFormFile file, HttpMethod? method = null)
    {
        try
        {
            using var content = new MultipartFormDataContent();
            var stream = file.OpenReadStream();
            var fileContent = new StreamContent(stream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
            content.Add(fileContent, "file", file.FileName);

            var req = new HttpRequestMessage(method ?? HttpMethod.Post, url) { Content = content };
            AttachToken(req);
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
            AttachToken(req);
            var res = await _http.SendAsync(req);
            return await ReadResult<T>(res);
        }
        catch
        {
            return new ApiResult<T> { Error = "We couldn't reach the server. Please try again." };
        }
    }

    private void AttachToken(HttpRequestMessage req)
    {
        var token = _ctx.HttpContext?.Session.GetString(SessionKeys.Token);
        if (!string.IsNullOrEmpty(token))
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    private static async Task<ApiResult<T>> ReadResult<T>(HttpResponseMessage res)
    {
        if (!res.IsSuccessStatusCode)
        {
            var msg = (int)res.StatusCode switch
            {
                400 => "Some of the information provided is invalid.",
                401 => "Please log in to continue.",
                403 => "You don't have permission to do that.",
                404 => "We couldn't find what you were looking for.",
                409 => "That already exists or conflicts with existing data.",
                _ => "Something went wrong. Please try again."
            };
            return new ApiResult<T> { Error = msg };
        }

        var text = await res.Content.ReadAsStringAsync();
        var data = string.IsNullOrWhiteSpace(text) ? default : JsonSerializer.Deserialize<T>(text, JsonOpts);
        return new ApiResult<T> { Success = true, Data = data };
    }
}
