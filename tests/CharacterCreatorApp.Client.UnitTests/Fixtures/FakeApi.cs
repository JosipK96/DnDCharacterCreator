using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace CharacterCreatorApp.Client.UnitTests.Fixtures;

/// <summary>
/// The API as a step sees it, faked under the client's real services. The services are concrete
/// classes over <see cref="HttpClient"/>, so Moq cannot stand in for them; this handler can, and it
/// is as strict as a strict mock: a request no test set up throws, and every request's body is kept
/// so a test asserts exactly what a step sent, field names included.
/// </summary>
public sealed class FakeApi : HttpMessageHandler
{
    private readonly Dictionary<string, Func<HttpResponseMessage>> _routes = [];
    private readonly List<(string Route, string? Body)> _requests = [];

    /// <summary>"GET /api/books?edition=2024", "PUT /api/character-drafts/{id}/books", in the order sent.</summary>
    public IReadOnlyList<string> Requests => _requests.ConvertAll(r => r.Route);

    public HttpClient CreateClient() => new(this) { BaseAddress = new Uri("http://localhost/") };

    public void Get(string pathAndQuery, object response) =>
        _routes[$"GET {pathAndQuery}"] = () => new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(response) };

    /// <summary>A file the API sends to be saved, named in its Content-Disposition as ASP.NET Core's File() names it.</summary>
    public void GetFile(string pathAndQuery, string fileName, string contentType, byte[] content) =>
        _routes[$"GET {pathAndQuery}"] = () =>
        {
            var file = new ByteArrayContent(content);
            file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            file.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment") { FileName = fileName, FileNameStar = fileName };
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = file };
        };

    /// <summary>A step's save, which the API answers with no content.</summary>
    public void Put(string path) =>
        _routes[$"PUT {path}"] = () => new HttpResponseMessage(HttpStatusCode.NoContent);

    /// <summary>A save the API refuses with a 400 problem, as its validation and the domain do.</summary>
    public void RefusePut(string path, string detail) =>
        _routes[$"PUT {path}"] = () => new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = JsonContent.Create(new
            {
                status = 400,
                title = "Bad Request",
                errors = new[] { new { code = "Test.Refused", detail } },
            }),
        };

    /// <summary>The body of the last request to the route, read as the API would bind it.</summary>
    public T BodyOf<T>(string route)
    {
        var body = _requests.LastOrDefault(r => r.Route == route).Body
            ?? throw new InvalidOperationException($"Nothing was sent to {route}.");

        return JsonSerializer.Deserialize<T>(body, JsonSerializerOptions.Web)!;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var route = $"{request.Method} {request.RequestUri!.PathAndQuery}";
        var body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
        _requests.Add((route, body));

        return _routes.TryGetValue(route, out var respond)
            ? respond()
            : throw new InvalidOperationException($"The test did not expect {route}.");
    }
}
