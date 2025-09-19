using System.Management;
using System.Runtime.Intrinsics.X86;
using System.Text;

namespace CPU_Benchmark
{
    /// <summary>
    /// Предоставляет статическую информацию о центральном процессоре (ЦП),
    /// такую как имя, количество ядер, тактовая частота и поддерживаемые наборы инструкций.
    /// Данные собираются один раз при создании экземпляра класса.
    /// </summary>
    public class CpuInfoProvider
    {
        /// <summary>
        /// Получает полное наименование процессора.
        /// </summary>
        public string CpuName { get; }

        /// <summary>
        /// Получает производителя процессора (например, "GenuineIntel" или "AuthenticAMD").
        /// </summary>
        public string Manufacturer { get; }

        /// <summary>
        /// Получает количество физических ядер процессора.
        /// </summary>
        public int PhysicalCoreCount { get; }

        /// <summary>
        /// Получает общее количество логических процессоров (потоков).
        /// </summary>
        public int LogicalCoreCount { get; }

        /// <summary>
        /// Получает базовую тактовую частоту процессора в МГц.
        /// </summary>
        public uint BaseClockSpeedMhz { get; }

        /// <summary>
        /// Получает максимальную (турбо) тактовую частоту процессора в МГц.
        /// </summary>
        public uint MaxClockSpeedMhz { get; }

        /// <summary>
        /// Получает общий размер кэша L2 в килобайтах.
        /// </summary>
        public uint L2CacheSizeKb { get; }

        /// <summary>
        /// Получает общий размер кэша L3 в килобайтах.
        /// </summary>
        public uint L3CacheSizeKb { get; }

        /// <summary>
        /// Получает готовую, отформатированную строку со всей информацией о процессоре.
        /// </summary>
        public string FullCpuInfoString { get; }

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="CpuInfoProvider"/>
        /// и выполняет сбор информации о процессоре с помощью WMI.
        /// </summary>
        public CpuInfoProvider()
        {
            // Установка значений по умолчанию на случай, если WMI-запрос завершится неудачей.
            CpuName = "N/A";
            Manufacturer = "N/A";
            PhysicalCoreCount = 0;
            LogicalCoreCount = Environment.ProcessorCount; // Это значение доступно всегда.
            BaseClockSpeedMhz = 0;
            MaxClockSpeedMhz = 0;
            L2CacheSizeKb = 0;
            L3CacheSizeKb = 0;

            try
            {
                var searcher = new ManagementObjectSearcher("select * from Win32_Processor");
                foreach (ManagementObject obj in searcher.Get())
                {
                    CpuName = obj["Name"]?.ToString()?.Trim() ?? "N/A";
                    Manufacturer = obj["Manufacturer"]?.ToString()?.Trim() ?? "N/A";
                    PhysicalCoreCount = Convert.ToInt32(obj["NumberOfCores"]);
                    LogicalCoreCount = Convert.ToInt32(obj["NumberOfLogicalProcessors"]);
                    MaxClockSpeedMhz = Convert.ToUInt32(obj["MaxClockSpeed"]);
                    // Свойство CurrentClockSpeed часто дает более точное значение базовой частоты, 
                    // чем парсинг строки Description или Name.
                    BaseClockSpeedMhz = Convert.ToUInt32(obj["CurrentClockSpeed"]);
                    L2CacheSizeKb = Convert.ToUInt32(obj["L2CacheSize"]);
                    L3CacheSizeKb = Convert.ToUInt32(obj["L3CacheSize"]);
                    break; // Нам нужна информация только о первом (основном) процессоре.
                }
            }
            catch
            {
                // Сбор данных через WMI может завершиться неудачей (например, из-за прав доступа или повреждения службы).
                // В этом случае приложение продолжит работу с данными по умолчанию ("N/A"), не прерывая работу.
            }

            FullCpuInfoString = BuildCpuInfoString();
        }

        /// <summary>
        /// Формирует полную, многострочную строку с информацией о процессоре для отображения в UI.
        /// </summary>
        /// <returns>Отформатированная строка с характеристиками ЦП.</returns>
        private string BuildCpuInfoString()
        {
            var sb = new StringBuilder();

            sb.AppendLine("Процессор (ЦП)");
            sb.AppendLine("-----------------------------------------");
            sb.AppendLine($"Название:         \t{CpuName}");
            sb.AppendLine($"Производитель:    \t{Manufacturer}");
            sb.AppendLine($"Базовая частота:  \t{BaseClockSpeedMhz} MHz");
            sb.AppendLine($"Макс. частота:    \t{MaxClockSpeedMhz} MHz");
            sb.AppendLine($"Кэш L2/L3:        \t{L2CacheSizeKb} KB / {L3CacheSizeKb} KB");
            sb.AppendLine($"Физические ядра:    \t{PhysicalCoreCount}");
            sb.AppendLine($"Логические потоки:\t{LogicalCoreCount}");
            sb.AppendLine();
            sb.AppendLine("Поддерживаемые наборы инструкций (x86/x64)");
            sb.AppendLine("-----------------------------------------");

            sb.AppendLine($"SSE:        \t{Sse.IsSupported}");
            sb.AppendLine($"SSE2:       \t{Sse2.IsSupported}");
            sb.AppendLine($"SSE3:       \t{Sse3.IsSupported}");
            sb.AppendLine($"SSSE3:      \t{Ssse3.IsSupported}");
            sb.AppendLine($"SSE4.1:     \t{Sse41.IsSupported}");
            sb.AppendLine($"SSE4.2:     \t{Sse42.IsSupported}");
            sb.AppendLine($"AVX:        \t{Avx.IsSupported}");
            sb.AppendLine($"AVX2:       \t{Avx2.IsSupported}");
            sb.AppendLine($"FMA:        \t{Fma.IsSupported}");
            sb.AppendLine($"AES:        \t{Aes.IsSupported}");

            return sb.ToString();
        }
    }
}