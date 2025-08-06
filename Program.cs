using ElectionApi;
using ElectionApi.Models;
using ElectionApi.QueueModels.Votes;
using ElectionApi.QueueServices;
using ElectionApi.Workers;
using ElectionApi.Services;
using ElectionApi.Settings;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

var mongoConnectionString = builder.Configuration.GetValue<string>("MongoDbSettings:ConnectionString") ?? throw new InvalidOperationException("Section 'MongoDbSettings:ConnectionString' not found.");
var mongoDatabaseName = builder.Configuration.GetValue<string>("MongoDbSettings:DatabaseName") ?? throw new InvalidOperationException("Section 'MongoDbSettings:DatabaseName' not found.");
var mongoDbSettings = builder.Configuration.GetSection("MongoDbSettings") ?? throw new InvalidOperationException("Section 'MongoDbSettings' not found.");
var myAllowSpecificOrigins = "_myAllowSpecificOrigins";
var allowedHost = builder.Configuration.GetValue<string>("AllowedHost") ?? throw new InvalidOperationException("Section 'AllowedHost' not found.");

builder.Services.Configure<MongoDbSettings>(mongoDbSettings);

builder.Services.AddSingleton<IMongoDbSettings, MongoDbSettings>();
builder.Services.AddSingleton<IMongoContext, MongoContext>();
builder.Services.AddSingleton<IDataService, DataService>();
builder.Services.AddSingleton<IBackgroundItemQueue<AddSMSVoteQueueModel>, BackgroundItemQueue<AddSMSVoteQueueModel>>();
builder.Services.AddHostedService<AddSMSVoteWorker>();

builder.Services.AddControllers(options => options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: myAllowSpecificOrigins,
        policy =>
        {
            policy
                .WithOrigins("http://164.68.114.70:8080", "http://localhost:3000", "https://localhost:3000")
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

app.UseAuthorization();

app.MapControllers();

app.Run();
