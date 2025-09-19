using LibreHardwareMonitor.Hardware;

namespace CPU_Benchmark
{
    /// <summary>
    /// Обертка для работы с LibreHardwareMonitor для получения температуры ЦП.
    /// </summary>
    public class TemperatureMonitor : IDisposable
    {
        private readonly Computer _computer;
        private IHardware? _cpu;

        public TemperatureMonitor()
        {
            _computer = new Computer
            {
                IsCpuEnabled = true
            };
        }

        /// <summary>
        /// Инициализирует монитор. Необходимо вызвать один раз перед использованием.
        /// </summary>
        public void Initialize()
        {
            _computer.Open();
            _cpu = _computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.Cpu);
        }

        /// <summary>
        /// Получает температуру ядра ЦП ("CPU Package").
        /// </summary>
        /// <returns>Температура в градусах Цельсия или null, если сенсор не найден.</returns>
        public float? GetCpuTemperature()
        {
            if (_cpu == null) return null;

            // Необходимо обновлять данные перед каждым чтением
            _cpu.Update();

            // Ищем сенсор температуры "CPU Package", он обычно самый релевантный
            var tempSensor = _cpu.Sensors.FirstOrDefault(s =>
                s.SensorType == SensorType.Temperature && s.Name.Contains("CPU Package"));

            return tempSensor?.Value;
        }

        /// <summary>
        /// Освобождает ресурсы.
        /// </summary>
        public void Dispose()
        {
            _computer.Close();
        }
    }
}