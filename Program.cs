using Test.Hubs;
using Test.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddSingleton<MachineDataService>();      // 被動：等人注入才建立
builder.Services.AddHostedService<FileWatcherService>();  // 主動：App 啟動就自動執行


var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();
app.MapControllers(); // 把所有 Controller 的路由都開放
app.MapHub<MachineHub>("/machineHub");// 把 SignalR 的連線點開放在 /machineHub

// app.MapGet("/", () => "Hello World!");

app.Run();
