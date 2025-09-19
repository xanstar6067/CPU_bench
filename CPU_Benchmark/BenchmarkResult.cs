using System.Globalization;

namespace CPU_Benchmark
{
    /// <summary>
    /// Представляет результат выполнения бенчмарка.
    /// </summary>
    public readonly struct BenchmarkResult
    {
        /// <summary>
        /// Затраченное время.
        /// </summary>
        public TimeSpan TimeTaken { get; }

        /// <summary>
        /// Общее количество выполненных операций.
        /// </summary>
        public long TotalOperations { get; }

        /// <summary>
        /// Производительность в операциях в секунду.
        /// </summary>
        public double OperationsPerSecond { get; }

        public BenchmarkResult(TimeSpan timeTaken, long totalOperations)
        {
            TimeTaken = timeTaken;
            TotalOperations = totalOperations;
            OperationsPerSecond = totalOperations / timeTaken.TotalSeconds;
        }

        public override string ToString()
        {
            // Используем NumberFormatInfo для форматирования чисел с разделителями тысяч
            var nfi = new NumberFormatInfo { NumberGroupSeparator = " " };

            string score = OperationsPerSecond.ToString("N0", nfi); // "N0" - числовой формат без дробной части
            string totalOps = TotalOperations.ToString("N0", nfi);

            return $"Результат: {score} оп/сек\r\n" +
                   $"------------\r\n" +
                   $"Всего операций: {totalOps}\r\n" +
                   $"Затрачено времени: {TimeTaken.TotalSeconds:F2} сек.";
        }
    }
}
