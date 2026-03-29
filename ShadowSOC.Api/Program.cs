using ShadowSOC.Api.Hubs;
using ShadowSOC.Api.Services;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddSingleton<RabbitMQService>();
builder.Services.AddCors();
builder.Services.AddSignalR();
builder.Services.AddHostedService<AlertBroadcastService>();
builder.Services.AddHttpClient();
var app = builder.Build();

app.MapHub<AlertHub>("/hubs/alerts");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors(policy => policy
    .WithOrigins("http://localhost:4200", "https://shadowsoc-web.fly.dev")
    .AllowCredentials()
    .AllowAnyMethod()
    .AllowAnyHeader()
);

app.UseHttpsRedirection();
app.MapControllers();

app.Run();
