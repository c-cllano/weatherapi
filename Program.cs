var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();

var app = builder.Build();

// Habilita Swagger/OpenAPI SIEMPRE CELL cambio 17 SIEMPRE, para facilitar el desarrollo y pruebas
app.MapOpenApi();
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

const string apiKey = "f1bea3ec2120c5b99da5f539a978afc2";

app.MapGet("/weatherforecast", async (string? city, IHttpClientFactory httpClientFactory) =>
{
    if (string.IsNullOrWhiteSpace(city))
    {
        return Results.BadRequest("Debe proporcionar el parámetro 'city' en la URL, por ejemplo: /weatherforecast?city=Santiago");
    }
    var client = httpClientFactory.CreateClient();
    var url = $"https://api.openweathermap.org/data/2.5/weather?q={city}&appid={apiKey}&units=metric&lang=es";
    try
    {
        var response = await client.GetAsync(url);
        if (!response.IsSuccessStatusCode)
        {
            return Results.NotFound($"No se encontró información para la ciudad '{city}'.");
        }
        var json = await response.Content.ReadAsStringAsync();
        DateTime fecha = new DateTime();
        fecha = DateTime.Parse(DateTime.Now.ToString("dd/MM/yyyy").ToString());
        using var doc = System.Text.Json.JsonDocument.Parse(json);
        var root = doc.RootElement;
        var tempC = root.GetProperty("main").GetProperty("temp").GetDouble();
        var summary = root.GetProperty("weather")[0].GetProperty("description").GetString();
        var forecast = new WeatherForecast(
            city,
            DateOnly.FromDateTime(fecha),
            (int)tempC,
            summary
        );
        return Results.Ok(new[] { forecast });
    }
    catch (Exception ex)
    {
        return Results.Problem($"Error al consultar el clima: {ex.Message}");
    }
}).WithName("GetWeatherForecast");

// Solo abre el navegador en desarrollo local, no en contenedores
var isRunningInContainer = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";
if (!isRunningInContainer)
{
    try
    {
        var swaggerUrl = "https://localhost:5001/swagger";
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = swaggerUrl,
            UseShellExecute = true
        });
    }
    catch
    {
        // Si falla, no interrumpe la ejecución
    }
}

app.Run();

record WeatherForecast(string City, DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
