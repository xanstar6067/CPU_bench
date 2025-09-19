using System.Management;
using System.Runtime.Intrinsics.X86;
using System.Text;

namespace CPU_Benchmark
{
    /// <summary>
    /// Предоставляет информацию о процессоре: имя, количество ядер и поддерживаемые наборы инструкций.
    /// </summary>
    public class CpuInfoProvider
    {
        public string CpuName { get; }
        public int PhysicalCoreCount { get; }
        public int LogicalCoreCount { get; }

        /// <summary>
        /// Полная информация о процессоре в виде отформатированной строки.
        /// </summary>
        public string FullCpuInfoString { get; }

        /// <summary>
        /// Инициализирует новый экземпляр класса, собирая информацию о системе.
        /// </summary>
        public CpuInfoProvider()
        {
            // Устанавливаем значения по умолчанию на случай сбоя WMI
            CpuName = "N/A";
            PhysicalCoreCount = 0;
            LogicalCoreCount = Environment.ProcessorCount; // Старый надежный способ как fallback

            try
            {
                // Используем WMI для получения детальной информации о процессоре
                var searcher = new ManagementObjectSearcher("select * from Win32_Processor");
                foreach (var obj in searcher.Get())
                {
                    CpuName = obj["Name"]?.ToString()?.Trim() ?? "N/A";
                    PhysicalCoreCount = Convert.ToInt32(obj["NumberOfCores"]);
                    LogicalCoreCount = Convert.ToInt32(obj["NumberOfLogicalProcessors"]);
                    break; // В большинстве систем один объект процессора
                }
            }
            catch
            {
                // Если WMI не сработал, останутся значения по умолчанию
            }

            // И формируем информационную строку
            FullCpuInfoString = BuildCpuInfoString();
        }

        /// <summary>
        /// Собирает всю информацию о ЦП и форматирует её в строку.
        /// </summary>
        /// <returns>Отформатированная строка с информацией о ЦП.</returns>
        private string BuildCpuInfoString()
        {
            var sb = new StringBuilder();

            sb.AppendLine("Процессор (ЦП)");
            sb.AppendLine("-----------------------------------------");
            sb.AppendLine($"Название:         \t{CpuName}");
            sb.AppendLine($"Физические ядра:    \t{PhysicalCoreCount}");
            sb.AppendLine($"Логические потоки:\t{LogicalCoreCount}");
            sb.AppendLine();
            sb.AppendLine("Поддерживаемые наборы инструкций (x86/x64)");
            sb.AppendLine("-----------------------------------------");

            // Использование \t (табуляция) поможет выровнять вывод 
            // в TextBox, если у него моноширинный шрифт (у вас Consolas).
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
            sb.AppendLine($"BMI1:       \t{Bmi1.IsSupported}");
            sb.AppendLine($"BMI2:       \t{Bmi2.IsSupported}");
            sb.AppendLine($"PCLMULQDQ:  \t{Pclmulqdq.IsSupported}");

            return sb.ToString();
        }
    }
}
