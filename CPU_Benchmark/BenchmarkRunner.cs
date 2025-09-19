using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace CPU_Benchmark
{
    /// <summary>
    /// Выполняет выбранный тип теста на заданном количестве потоков.
    /// </summary>
    public class BenchmarkRunner
    {
        /// <summary>
        /// Асинхронно запускает стресс-тест.
        /// </summary>
        /// <param name="type">Тип теста для запуска.</param>
        /// <param name="threadCount">Количество потоков для использования.</param>
        /// <param name="token">Токен для отмены операции.</param>
        public async Task RunStressTestAsync(BenchmarkType type, int threadCount, CancellationToken token)
        {
            // Предварительная проверка. Если UI по какой-то причине позволил выбрать AVX2
            // на неподдерживаемом процессоре, мы должны перехватить это здесь.
            if (type == BenchmarkType.VectorAvx2 && !Avx2.IsSupported)
            {
                throw new NotSupportedException("AVX2 инструкции не поддерживаются данным процессором.");
            }

            // Запускаем параллельную задачу в фоновом потоке, чтобы не блокировать UI.
            await Task.Run(() =>
            {
                var options = new ParallelOptions
                {
                    MaxDegreeOfParallelism = threadCount,
                    CancellationToken = token
                };

                try
                {
                    // Распределяем работу по указанному числу потоков.
                    Parallel.For(0, threadCount, options, (i) =>
                    {
                        // В каждом потоке запускаем бесконечный цикл с вычислениями.
                        // Цикл прервется, когда токен получит сигнал отмены.
                        switch (type)
                        {
                            case BenchmarkType.Integer:
                                IntegerStressLoop(token);
                                break;
                            case BenchmarkType.FloatingPoint:
                                FloatStressLoop(token);
                                break;
                            case BenchmarkType.VectorAvx2:
                                AvxStressLoop(token);
                                break;
                        }
                    });
                }
                catch (OperationCanceledException)
                {
                    // Это ожидаемое исключение при отмене. Просто "глотаем" его,
                    // чтобы TPL не передавал его дальше как ошибку.
                }

            }, token);
        }

        // --- Приватные методы, содержащие саму нагрузку ---

        private void IntegerStressLoop(CancellationToken token)
        {
            long a = 123456789012345;
            long b = 987654321098765;
            while (!token.IsCancellationRequested)
            {
                a ^= b;
                b = (a << 3) | (b >> 61); // Битовые сдвиги и операции
                a = ~a;
            }
        }

        private void FloatStressLoop(CancellationToken token)
        {
            double a = Math.PI;
            while (!token.IsCancellationRequested)
            {
                a += Math.Sin(a) + Math.Cos(a) + Math.Tan(a);
            }
        }

        private void AvxStressLoop(CancellationToken token)
        {
            var vec1 = Vector256.Create(1.1f, 2.2f, 3.3f, 4.4f, 5.5f, 6.6f, 7.7f, 8.8f);
            var vec2 = Vector256.Create(8.8f, 7.7f, 6.6f, 5.5f, 4.4f, 3.3f, 2.2f, 1.1f);

            while (!token.IsCancellationRequested)
            {
                // Выполняем 3 разные AVX инструкции за итерацию.
                // Это эффективно загружает векторные блоки FPU процессора.
                var addResult = Avx.Add(vec1, vec2);
                var mulResult = Avx.Multiply(addResult, vec1);
                vec1 = Avx.Subtract(mulResult, vec2);
            }
        }
    }
}
