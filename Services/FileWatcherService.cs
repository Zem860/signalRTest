using Microsoft.AspNetCore.SignalR;
using Test.Hubs;

namespace Test.Services
{
    public class FileWatcherService : IHostedService
    {

        private FileSystemWatcher? _watcher;
        private readonly MachineDataService _dataService;
        private readonly IHubContext<MachineHub> _hub;
        private readonly string _dataPath;

        public FileWatcherService(MachineDataService dataService, IHubContext<MachineHub> hub, IWebHostEnvironment env)
        {

            //IWebHostEnvironment 
            _dataService = dataService;
            _hub = hub;
            _dataPath = Path.Combine(env.ContentRootPath, "data");
        }

        Task IHostedService.StartAsync(CancellationToken cancellationToken)
        {
            if (!Directory.Exists(_dataPath))
            {
                Directory.CreateDirectory(_dataPath);
            }

            _watcher = new FileSystemWatcher(_dataPath, "*.json")
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName,
                //  LastWrite 檔案內容被修改
                //   FileName 檔案被新增或重新命名
                EnableRaisingEvents = true
                // 開始監控。沒有這行，上面設定了也沒用，等於開了監視器但沒插電。
            };

            //C#通用寫法，註冊等真的改變了再呼叫
            _watcher.Changed += OnFileChanged;
            _watcher.Created += OnFileChanged;
            return Task.CompletedTask;

        }

        Task IHostedService.StopAsync(CancellationToken cancellationToken)
        {
            _watcher?.Dispose();
            return Task.CompletedTask;
        }

        private async void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            await Task.Delay(300);
            _dataService.UpdateFromFile(e.FullPath);
            await _hub.Clients.All.SendAsync("MachineUpdated");
        }
    }
}