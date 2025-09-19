using System.Runtime.Intrinsics.X86;
using System.Text;

namespace CPU_Benchmark
{
    /// <summary>
    /// Предоставляет информацию о процессоре: количество ядер и поддерживаемые наборы инструкций.
    /// </summary>
    public class CpuInfoProvider
    {
        /// <summary>
        /// Количество логических процессоров в системе.
        /// </summary>
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
            // Сразу при создании объекта получаем количество ядер
            LogicalCoreCount = Environment.ProcessorCount;

            // И формируем информационную строку
            FullCpuInfoString = BuildInstructionSetString();
        }

        /// <summary>
        /// Собирает информацию о поддерживаемых инструкциях и форматирует её в строку.
        /// </summary>
        /// <returns>Отформатированная строка с информацией о ЦП.</returns>
        private string BuildInstructionSetString()
        {
            var sb = new StringBuilder();

            sb.AppendLine($"Логические процессоры (потоки):\t{LogicalCoreCount}");
            sb.AppendLine("-----------------------------------------");
            sb.AppendLine("Поддерживаемые наборы инструкций (x86/x64)");
            sb.AppendLine("-----------------------------------------");

            // Использование \t (табуляция) поможет выровнять вывод 
            // в TextBox, если у него моноширинный шрифт (я установил Consolas в дизайнере).
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