using LibreHardwareMonitor.Hardware;
using System.Collections.Generic;
using System.Linq;

namespace CPU_Benchmark
{
    // Структура для удобного хранения информации о накопителе
    public readonly struct StorageInfo
    {
        public string Name { get; }
        public float? Temperature { get; }

        public StorageInfo(string name, float? temp)
        {
            Name = name;
            Temperature = temp;
        }
    }

    /// <summary>
    /// Обертка для работы с LibreHardwareMonitor для получения данных с сенсоров.
    /// </summary>
    public class SystemMonitor : IDisposable
    {
        private readonly Computer _computer;
        private IHardware? _cpu;
        private readonly List<IHardware> _storageDevices = new();

        public SystemMonitor()
        {
            _computer = new Computer
            {
                IsCpuEnabled = true,
                IsStorageEnabled = true // Включаем мониторинг накопителей
            };
        }

        public void Initialize()
        {
            try
            {
                _computer.Open();
                _cpu = _computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.Cpu);
                // Находим все устройства типа Storage
                _storageDevices.AddRange(_computer.Hardware.Where(h => h.HardwareType == HardwareType.Storage));
            }
            catch { /* Игнорируем ошибки инициализации */ }
        }

        // Этот метод обновляет данные всех сенсоров на устройстве
        private void UpdateHardware(IHardware hardware)
        {
            hardware?.Update();
        }

        public float? GetCpuTemperature()
        {
            if (_cpu == null) return null;
            UpdateHardware(_cpu);
            var sensor = _cpu.Sensors.FirstOrDefault(s =>
                s.SensorType == SensorType.Temperature && s.Name.Contains("CPU Package"));
            return sensor?.Value;
        }

        public float? GetCpuTotalLoad()
        {
            if (_cpu == null) return null;
            UpdateHardware(_cpu);
            var sensor = _cpu.Sensors.FirstOrDefault(s =>
                s.SensorType == SensorType.Load && s.Name.Contains("CPU Total"));
            return sensor?.Value;
        }

        public float? GetCpuPackagePower()
        {
            if (_cpu == null) return null;
            UpdateHardware(_cpu);
            var sensor = _cpu.Sensors.FirstOrDefault(s =>
                s.SensorType == SensorType.Power && s.Name.Contains("CPU Package"));
            return sensor?.Value;
        }

        public List<StorageInfo> GetStorageInfo()
        {
            var result = new List<StorageInfo>();
            foreach (var device in _storageDevices)
            {
                UpdateHardware(device);
                // Имя устройства - это и есть его модель
                string name = device.Name;
                // Ищем сенсор температуры
                var tempSensor = device.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Temperature);
                result.Add(new StorageInfo(name, tempSensor?.Value));
            }
            return result;
        }

        public void Dispose()
        {
            try { _computer.Close(); } catch { }
        }
    }
}
