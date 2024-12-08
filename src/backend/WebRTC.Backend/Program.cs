using WebRTC.Backend.Hubs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR();

var app = builder.Build();

app.MapHub<WebRtcSignalingHub>("/signalhub");

app.MapGet("/", () => "running");

app.Run();