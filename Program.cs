using dotenv.net;
using Microsoft.OpenApi;
using Microsoft.EntityFrameworkCore;
using IntegrationsDemo;

DotEnv.Load();
IDictionary<string, string> env = DotEnv.Read();
var webhookUrl = env["AZURE_LOGIC_APP_URL"];
var connectionString = env["SQL_SERVER_CONNECTION_STRING"];

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c => {
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Dotnet Integrations API", Description = "An Amazing Dotnet Integrations API", Version = "v1" });
});

// connect to azure sql database using entra authentication
builder.Services.AddDbContext<AzureSQLDbContext>(options =>
    options.UseSqlServer(connectionString, sqlOptions =>
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null)));

WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment()) {
    _ = app.UseSwagger();
    _ = app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Dotnet Integrations API v1"));
}

app.UseHttpsRedirection();

app.MapPost("/api/signup", async (AzureSQLDbContext db, Lead lead, ILogger<Program> logger) => {
    // validation
    if (string.IsNullOrEmpty(lead.FirstName) || string.IsNullOrEmpty(lead.LastName) || string.IsNullOrEmpty(lead.Email) || string.IsNullOrEmpty(lead.Phone)) {
        return Results.BadRequest(new { Message = "First Name, Last Name, Email, and Phone are required." });
    }

    // check for valid email
    if (!MyRegex().IsMatch(lead.Email)) {
        return Results.BadRequest(new { Message = "Invalid email format." });
    }

    // add correlation id to track request across services
    lead.CorrelationId = Guid.NewGuid().ToString();

    // save to db
    _ = db.Leads.Add(lead);
    _ = await db.SaveChangesAsync();

    // send webhook to Azure Logic Apps in the background
    JsonWebhook webhook = new() {
        WebhookUrl = webhookUrl,
        CorrelationId = lead.CorrelationId,
        Timeout = TimeSpan.FromSeconds(30),
        Logger = logger
    };
    _ = Task.Run(async () => await webhook.SendAsync(lead));

    return Results.Ok(new { Message = "Signup successful." });
})
.WithName("SignUp");

app.MapGet("/api/leads", async (AzureSQLDbContext db) => await db.Leads.ToListAsync())
.WithName("GetLeads");

app.Run();

internal partial class Program {
    [System.Text.RegularExpressions.GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", System.Text.RegularExpressions.RegexOptions.IgnoreCase, "en-US")]
    private static partial System.Text.RegularExpressions.Regex MyRegex();
}