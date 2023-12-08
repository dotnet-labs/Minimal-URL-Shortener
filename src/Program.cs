using LiteDB;
using Microsoft.AspNetCore.WebUtilities;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<ILiteDatabase, LiteDatabase>(_ => new LiteDatabase("short-links.db"));
await using var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

// Home page: A form for submitting a URL
app.MapGet("/", ctx =>
                {
                    ctx.Response.ContentType = "text/html";
                    return ctx.Response.SendFileAsync("index.html");
                });

// API endpoint for shortening a URL and save it to a local database
app.MapPost("/url", ShortenerDelegate);

// Catch all page: redirecting shortened URL to its original address
app.MapFallback(RedirectDelegate);

await app.RunAsync();
return;

static async Task ShortenerDelegate(HttpContext httpContext)
{
    var request = await httpContext.Request.ReadFromJsonAsync<string>() ;

    if (!Uri.TryCreate(request, UriKind.Absolute, out var inputUri))
    {
        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        await httpContext.Response.WriteAsync("URL is invalid.");
        return;
    }

    var liteDb = httpContext.RequestServices.GetRequiredService<ILiteDatabase>();
    var links = liteDb.GetCollection<ShortUrl>(BsonAutoId.Int32);
    var entry = new ShortUrl(inputUri);
    links.Insert(entry);

    var urlChunk = WebEncoders.Base64UrlEncode(BitConverter.GetBytes(entry.Id));
    var result = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}/{urlChunk}";
    await httpContext.Response.WriteAsJsonAsync(new { url = result });
}

static Task RedirectDelegate(HttpContext httpContext)
{
    var db = httpContext.RequestServices.GetRequiredService<ILiteDatabase>();
    var collection = db.GetCollection<ShortUrl>();

    var path = httpContext.Request.Path.ToUriComponent().Trim('/');
    var id = BitConverter.ToInt32(WebEncoders.Base64UrlDecode(path));
    var entry = collection.Find(p => p.Id == id).FirstOrDefault();

    httpContext.Response.Redirect(entry?.Url ?? "/");

    return Task.CompletedTask;
}

internal class ShortUrl(Uri url)
{
    public int Id { get; protected set; }
    public string Url { get; protected set; } = url.ToString();
}