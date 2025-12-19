using dotenv.net; 
using Microsoft.OpenApi;

DotEnv.Load();
var env = DotEnv.Read();
string webhookUrl = env["ZAPIER_WEBHOOK_URL"];

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Dotnet Integrations API", Description = "An Amazing Dotnet Integrations API", Version = "v1" });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Dotnet Integrations API v1"));
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.MapGet("/leads", () =>
{
    // Placeholder for lead retrieval logic
    
    return Results.Ok(new[] { new { Id = 1, Name = "Sample Lead" } });
})
.WithName("GetLeads");

app.MapPost("/leads", async (Lead lead) =>
{
    // validation
    if (string.IsNullOrEmpty(lead.Name) || string.IsNullOrEmpty(lead.Email) || string.IsNullOrEmpty(lead.Phone))
    {
        return Results.BadRequest(new { Message = "Name, Email, and Phone are required." });
    }

    // check for valid email
    if (!System.Text.RegularExpressions.Regex.IsMatch(lead.Email, 
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$", 
        System.Text.RegularExpressions.RegexOptions.IgnoreCase))
    {
        return Results.BadRequest(new { Message = "Invalid email format." });
    }

    // do something with the lead, e.g., save to database
    lead.CreatedAt = DateTime.UtcNow;
    lead.UpdatedAt = DateTime.UtcNow;

    // send new lead to Zapier
    var zapier  = new ZapierWebhook { WebhookUrl = webhookUrl };
    var success = await zapier.Send(lead);

    // send new lead to Hubspot


    return Results.Ok(new { Message = "Lead created successfully" });
})
.WithName("CreateLead");

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
