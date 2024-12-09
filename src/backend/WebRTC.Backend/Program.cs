using WebRTC.Backend.Hubs;
using WebRTC.Backend.Services;
using WebRTC.Backend.Services.Call;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<ICallManager, InMemoryCallManager>();
builder.Services.AddSingleton<IClientManager, InMemoryClientManager>();
builder.Services.AddSignalR();

var app = builder.Build();

app.MapHub<SignalrHub>("/signalhub");

app.MapGet("/", () => "running");

app.Run();