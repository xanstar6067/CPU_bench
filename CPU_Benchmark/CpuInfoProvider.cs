using System.Management;
using System.Runtime.Intrinsics.X86;
using System.Text;

namespace CPU_Benchmark
{
    /// <summary>
    /// Предоставляет подробную информацию о процессоре.
    /// </summary>
    public class CpuInfoProvider
    {
        public string CpuName { get; }
        public string Manufacturer { get; }
        public int PhysicalCoreCount { get; }
        public int LogicalCoreCount { get; }
        public uint BaseClockSpeedMhz { get; } // Базовая частота в МГц
        public uint MaxClockSpeedMhz { get; }  // Максимальная (турбо) частота в МГц
        public uint L2CacheSizeKb { get; }     // Размер L2 кэша в КБ
        public uint L3CacheSizeKb { get; }     // Размер L3 кэша в КБ

        public string FullCpuInfoString { get; }

        public CpuInfoProvider()
        {
            // Установка значений по умолчанию
            CpuName = "N/A";
            Manufacturer = "N/A";
            PhysicalCoreCount = 0;
            LogicalCoreCount = Environment.ProcessorCount;
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
                    // CurrentClockSpeed может быть более релевантным для базовой частоты, чем Description
                    BaseClockSpeedMhz = Convert.ToUInt32(obj["CurrentClockSpeed"]);
                    L2CacheSizeKb = Convert.ToUInt32(obj["L2CacheSize"]);
                    L3CacheSizeKb = Convert.ToUInt32(obj["L3CacheSize"]);
                    break;
                }
            }
            catch { /* Игнорируем ошибки WMI */ }

            FullCpuInfoString = BuildCpuInfoString();
        }

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

            // ... (остальная часть с инструкциями остается без изменений)
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
