using ElectionApi;
using ElectionApi.Models;
using ElectionApi.QueueModels.Votes;
using ElectionApi.QueueServices;
using ElectionApi.Workers;
using ElectionApi.Services;
using ElectionApi.Settings;
using ElectionApi.Repositories;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

var mongoConnectionString = builder.Configuration.GetValue<string>("MongoDbSettings:ConnectionString") ?? throw new InvalidOperationException("Section 'MongoDbSettings:ConnectionString' not found.");
var mongoDatabaseName = builder.Configuration.GetValue<string>("MongoDbSettings:DatabaseName") ?? throw new InvalidOperationException("Section 'MongoDbSettings:DatabaseName' not found.");
var mongoDbSettings = builder.Configuration.GetSection("MongoDbSettings") ?? throw new InvalidOperationException("Section 'MongoDbSettings' not found.");
var jwtSettings = builder.Configuration.GetSection("JwtSettings") ?? throw new InvalidOperationException("Section 'JwtSettings' not found.");
var myAllowSpecificOrigins = "_myAllowSpecificOrigins";
var allowedHost = builder.Configuration.GetValue<string>("AllowedHost") ?? throw new InvalidOperationException("Section 'AllowedHost' not found.");

// Configure settings
builder.Services.Configure<ElectionApi.Settings.MongoDbSettings>(mongoDbSettings);
builder.Services.Configure<JwtSettings>(jwtSettings);

// Configure JWT Authentication
var jwtKey = builder.Configuration.GetValue<string>("JwtSettings:Key") ?? throw new InvalidOperationException("JWT Key not found.");
var jwtIssuer = builder.Configuration.GetValue<string>("JwtSettings:Issuer") ?? throw new InvalidOperationException("JWT Issuer not found.");
var jwtAudience = builder.Configuration.GetValue<string>("JwtSettings:Audience") ?? throw new InvalidOperationException("JWT Audience not found.");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
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
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// Register services
builder.Services.AddSingleton<IMongoDbSettings, ElectionApi.Settings.MongoDbSettings>();
builder.Services.AddSingleton<IMongoContext, MongoContext>();
builder.Services.AddSingleton<IDataService, DataService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ITwoFactorService, TwoFactorService>();

// Register repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ICandidateRepository, CandidateRepository>();
builder.Services.AddScoped<ICountryRepository, CountryRepository>();
builder.Services.AddScoped<IOfficeRepository, OfficeRepository>();
builder.Services.AddScoped<IVoteRepository, VoteRepository>();

// Register background services
builder.Services.AddSingleton<IBackgroundItemQueue<AddSMSVoteQueueModel>, BackgroundItemQueue<AddSMSVoteQueueModel>>();
builder.Services.AddHostedService<AddSMSVoteWorker>();

builder.Services.AddControllers(options => options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: myAllowSpecificOrigins,
        policy =>
        {
            policy
                .WithOrigins("http://164.68.114.70:8080", "http://localhost:3000", "https://localhost:3000", "http://192.168.81.78:5173")
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials();
        });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors(myAllowSpecificOrigins);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
