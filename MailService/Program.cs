using MailService.Data;
using MailService.Services;
using MailService.Workers;
using MailService.Data;
using MailService.Services;
using MailService.Workers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// DbContext
builder.Services.AddDbContext<MailServiceDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"] ?? "MailServiceSecretKeyMinimum32CharactersLong12345";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "https://localhost";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "mail-service";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        NameClaimType = "client_id",
        RoleClaimType = "scope",
        ClockSkew = TimeSpan.Zero
    };
});

// Authorization Policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("MailSend", policy =>
        policy.RequireAssertion(context => HasScope(context.User, "mail.send")));

    options.AddPolicy("MailBulkSend", policy =>
        policy.RequireAssertion(context => HasScope(context.User, "mail.bulk.send")));

    options.AddPolicy("MailTemplateRead", policy =>
        policy.RequireAssertion(context => HasScope(context.User, "mail.template.read")));

    options.AddPolicy("MailTemplateWrite", policy =>
        policy.RequireAssertion(context => HasScope(context.User, "mail.template.write")));

    options.AddPolicy("MailAdmin", policy =>
        policy.RequireAssertion(context => HasScope(context.User, "mail.admin")));
});

// Configure Options
builder.Services.Configure<RabbitMqSettings>(builder.Configuration.GetSection("RabbitMq"));
builder.Services.Configure<KafkaSettings>(builder.Configuration.GetSection("Kafka"));
builder.Services.Configure<SendGridSettings>(builder.Configuration.GetSection("SendGrid"));

// Queue Provider Selection (from appsettings)
var queueType = builder.Configuration["Queue:Type"] ?? "RabbitMQ";

if (queueType.Equals("Kafka", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddScoped<IMailQueueService, KafkaMailQueueService>();
    builder.Services.AddHostedService<KafkaMailWorker>();
}
else
{
    builder.Services.AddScoped<IMailQueueService, RabbitMqMailQueueService>();
    builder.Services.AddHostedService<MailWorker>();
}

// Common Services
builder.Services.AddScoped<ITemplateService, TemplateService>();
builder.Services.AddScoped<IMailSender, SendGridMailSender>();
builder.Services.AddScoped<IRateLimiterService, RateLimiterService>();

var app = builder.Build();

// Initialize database and seed OAuth clients
//using (var scope = app.Services.CreateScope())
//{
//    var db = scope.ServiceProvider.GetRequiredService<MailServiceDbContext>();
//    // db.Database.EnsureCreated(); // Use migrations instead
//    // db.Database.Migrate();
//}

// Initialize Kafka topic if using Kafka
var queueTypeKafka = builder.Configuration["Queue:Type"]?.Equals("Kafka", StringComparison.OrdinalIgnoreCase) ?? false;
if (queueTypeKafka)
{
    var bootstrapServers = builder.Configuration["Kafka:BootstrapServers"] ?? "localhost:9092";
    var topic = builder.Configuration["Kafka:Topic"] ?? "mail_queue";

    using var scope = app.Services.CreateScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        var adminConfig = new Confluent.Kafka.AdminClientConfig
        {
            BootstrapServers = bootstrapServers
        };

        using var adminClient = new Confluent.Kafka.AdminClientBuilder(adminConfig).Build();

        // Check if topic exists
        var metadata = adminClient.GetMetadata(TimeSpan.FromSeconds(10));
        var topicExists = metadata.Topics.Any(t => t.Topic == topic);

        if (!topicExists)
        {
            logger.LogInformation("Creating Kafka topic: {Topic}", topic);

            var topicSpecification = new Confluent.Kafka.Admin.TopicSpecification
            {
                Name = topic,
                NumPartitions = 1,
                ReplicationFactor = 1
            };

            adminClient.CreateTopicsAsync(new[] { topicSpecification }).Wait();
            logger.LogInformation("Kafka topic created: {Topic}", topic);
        }
        else
        {
            logger.LogInformation("Kafka topic already exists: {Topic}", topic);
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to initialize Kafka topic: {Topic}", topic);
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

static bool HasScope(ClaimsPrincipal user, string scope)
{
    var scopeClaims = user.FindAll("scope");
    foreach (var claim in scopeClaims)
    {
        var scopes = claim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (scopes.Contains(scope))
            return true;
    }
    return false;
}
