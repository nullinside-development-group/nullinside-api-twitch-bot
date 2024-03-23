using System.Reflection;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Nullinside.Api.Common;
using Nullinside.Api.Common.AspNetCore.Middleware;
using Nullinside.Api.Model;

const string CORS_KEY = "_customAllowedSpecificOrigins";

var builder = WebApplication.CreateBuilder(args);

// Secrets are mounted into the container.
var server = Environment.GetEnvironmentVariable("MYSQL_SERVER");
var username = Environment.GetEnvironmentVariable("MYSQL_USERNAME");
var password = Environment.GetEnvironmentVariable("MYSQL_PASSWORD");
builder.Services.AddDbContext<NullinsideContext>(optionsBuilder =>
    optionsBuilder.UseMySQL($"server={server};database=nullinside;user={username};password={password};AllowUserVariables=true",
        options => options.CommandTimeout(60 * 5)));
builder.Services.AddScoped<IAuthorizationHandler, BasicAuthorizationHandler>();
builder.Services.AddAuthentication()
    .AddScheme<AuthenticationSchemeOptions, BasicAuthenticationHandler>("Bearer", _ => { });

builder.Services.AddAuthorization(options =>
{
    // Dynamically add all of the user roles that exist in the application.
    foreach (var role in Enum.GetValues(typeof(UserRoles)))
    {
        var roleName = role?.ToString();
        if (null == roleName) continue;

        options.AddPolicy(roleName, policy => policy.Requirements.Add(new BasicAuthorizationRequirement(roleName)));
    }

    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireRole(nameof(UserRoles.User))
        .RequireAuthenticatedUser()
        .Build();
});

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "nullinside-null", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = """
                      JWT Authorization header using the Bearer scheme. \r\n\r\n
                                            Enter 'Bearer' [space] and then your token in the text input below.
                                            \r\n\r\nExample: 'Bearer 12345abcdef'
                      """,
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = ParameterLocation.Header
            },
            new List<string>()
        }
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);
});

// Add services to the container.
builder.Services.AddCors(options =>
{
    options.AddPolicy(CORS_KEY,
        policyBuilder =>
        {
            policyBuilder.WithOrigins("https://www.nullinside.com", "https://nullinside.com", "http://localhost:4200",
                    "http://127.0.0.1:4200")
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
app.UsePathBase("/twitch-bot/v1");
app.UseAuthentication();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors(CORS_KEY);
app.UseAuthorization();

app.MapControllers();

app.Run();