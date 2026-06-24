using System.Text.Json;
using Test.Models;

namespace Test.Services
{
    public class MachineDataService
    {
        private readonly List<Machine> _machines = new();
        private readonly object _lock = new();
        private readonly string _dataPath;

        public MachineDataService(IWebHostEnvironment env)
        {
            //建構子呼叫LoadAll刷新Json改變資料
            _dataPath = Path.Combine(env.ContentRootPath, "data");

            LoadAll();
        }

        public List<Machine> GetAll()
        {
            lock (_lock)
                return _machines.ToList();
        }

        public void LoadAll()
        {
            lock (_lock)
            {
                _machines.Clear();
                if (!Directory.Exists(_dataPath)) return;
                foreach (var file in Directory.GetFiles(_dataPath, "*.json"))
                {
                    var machine = LoadFile(file);
                    if (machine != null) _machines.Add(machine);
                }
            }

        }

        public void UpdateFromFile(string filePath)
        {
            var machine = LoadFile(filePath);
            if (machine == null) return;
            lock (_lock)
            {
                var index = _machines.FindIndex(m => m.MachineId == machine.MachineId);
                bool exist = index >= 0;
                if (exist)
                {
                    _machines[index] = machine;
                }
                else
                {
                    _machines.Add(machine);
                }
            }
        }


        private Machine? LoadFile(string filePath)
        {
            try
            {
                var json = File.ReadAllText(filePath);
                return JsonSerializer.Deserialize<Machine>(json);
            }
            catch
            {
                return null;
            }
        }

    }
}