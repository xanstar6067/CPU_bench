using System.Globalization;

namespace CPU_Benchmark
{
    /// <summary>
    /// Представляет неизменяемый результат выполнения бенчмарка.
    /// </summary>
    public readonly struct BenchmarkResult
    {
        /// <summary>
        /// Общее время, затраченное на выполнение бенчмарка.
        /// </summary>
        public TimeSpan TimeTaken { get; }

        /// <summary>
        /// Общее количество выполненных операций за время теста.
        /// </summary>
        public long TotalOperations { get; }

        /// <summary>
        /// Рассчитанная производительность в операциях в секунду (Оп/сек).
        /// </summary>
        public double OperationsPerSecond { get; }

        /// <summary>
        /// Инициализирует новый экземпляр структуры <see cref="BenchmarkResult"/>.
        /// </summary>
        /// <param name="timeTaken">Затраченное время.</param>
        /// <param name="totalOperations">Общее количество выполненных операций.</param>
        public BenchmarkResult(TimeSpan timeTaken, long totalOperations)
        {
            TimeTaken = timeTaken;
            TotalOperations = totalOperations;
            // Рассчитываем производительность, избегая деления на ноль, если время выполнения было слишком мало.
            OperationsPerSecond = timeTaken.TotalSeconds > 0 ? totalOperations / timeTaken.TotalSeconds : 0;
        }

        /// <summary>
        /// Возвращает строковое представление результата бенчмарка, отформатированное для отображения пользователю.
        /// </summary>
        /// <returns>Отформатированная строка с результатами.</returns>
        public override string ToString()
        {
            // Используем NumberFormatInfo для форматирования чисел с пробелом в качестве разделителя тысяч для лучшей читаемости.
            var nfi = new NumberFormatInfo { NumberGroupSeparator = " " };

            string score = OperationsPerSecond.ToString("N0", nfi); // "N0" - числовой формат без дробной части.
            string totalOps = TotalOperations.ToString("N0", nfi);

            return $"Результат: {score} оп/сек\r\n" +
                   $"------------\r\n" +
                   $"Всего операций: {totalOps}\r\n" +
                   $"Затрачено времени: {TimeTaken.TotalSeconds:F2} сек.";
        }
    }
}