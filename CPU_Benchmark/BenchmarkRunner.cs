using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace CPU_Benchmark
{
    public class BenchmarkRunner
    {
        // --- P/Invoke для получения ID нативного потока. Это нужно для установки привязки. ---
        [DllImport("kernel32.dll")]
        private static extern uint GetCurrentThreadId();

        /// <summary>
        /// Запускает тест с распределением потоков планировщиком ОС (без привязки).
        /// </summary>
        public async Task RunStressTestAsync(BenchmarkType type, int threadCount, CancellationToken token)
        {
            await RunTestInternal(type, threadCount, token, forceAffinity: false);
        }

        /// <summary>
        /// Запускает тест с жесткой привязкой каждого потока к своему логическому ядру.
        /// </summary>
        public async Task RunStressTestWithAffinityAsync(BenchmarkType type, int threadCount, CancellationToken token)
        {
            await RunTestInternal(type, threadCount, token, forceAffinity: true);
        }

        /// <summary>
        /// Внутренний метод, который выполняет основную логику запуска.
        /// </summary>
        private async Task RunTestInternal(BenchmarkType type, int threadCount, CancellationToken token, bool forceAffinity)
        {
            if (type == BenchmarkType.VectorAvx2 && !Avx2.IsSupported)
            {
                throw new NotSupportedException("AVX2 инструкции не поддерживаются данным процессором.");
            }

            // Создаем и запускаем потоки вручную, вместо Parallel.For
            await Task.Run(() =>
            {
                var threads = new List<Thread>();
                for (int i = 0; i < threadCount; i++)
                {
                    // Важно: создаем локальную копию переменной цикла для замыкания
                    int coreIndex = i;

                    var thread = new Thread(() =>
                    {
                        if (forceAffinity)
                        {
                            try
                            {
                                // Устанавливаем привязку к ядру
                                var processThread = GetProcessThreadFromCurrentThread();
                                if (processThread != null)
                                {
                                    // Маска привязки. 1L << coreIndex создает бит в позиции coreIndex.
                                    // Например, для ядра 0 -> 1 (0001b), для ядра 1 -> 2 (0010b), для ядра 2 -> 4 (0100b)
                                    processThread.ProcessorAffinity = (IntPtr)(1L << coreIndex);
                                }
                            }
                            catch (Exception)
                            {
                                // Игнорируем ошибки, если не удалось установить привязку.
                                // Это может случиться из-за прав доступа или особенностей ОС.
                            }
                        }

                        // Запускаем сам тест
                        ExecuteTestLoop(type, token);
                    });
                    threads.Add(thread);
                    thread.Start();
                }

                // Ждем завершения всех потоков
                foreach (var thread in threads)
                {
                    thread.Join();
                }
            }, token);
        }

        /// <summary>
        /// Находит объект ProcessThread, соответствующий текущему управляемому потоку.
        /// </summary>
        private ProcessThread? GetProcessThreadFromCurrentThread()
        {
            uint nativeThreadId = GetCurrentThreadId();
            return Process.GetCurrentProcess().Threads
                          .Cast<ProcessThread>()
                          .FirstOrDefault(pt => pt.Id == nativeThreadId);
        }

        /// <summary>
        /// Выбирает и запускает конкретный цикл нагрузки.
        /// </summary>
        private void ExecuteTestLoop(BenchmarkType type, CancellationToken token)
        {
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
        }

        // --- Сами циклы с нагрузкой (остаются без изменений) ---
        private void IntegerStressLoop(CancellationToken token)
        {
            long a = 123456789012345;
            long b = 987654321098765;
            while (!token.IsCancellationRequested) { a ^= b; b = (a << 3) | (b >> 61); a = ~a; }
        }

        private void FloatStressLoop(CancellationToken token)
        {
            double a = Math.PI;
            while (!token.IsCancellationRequested) { a += Math.Sin(a) + Math.Cos(a) + Math.Tan(a); }
        }

        private void AvxStressLoop(CancellationToken token)
        {
            var vec1 = Vector256.Create(1.1f, 2.2f, 3.3f, 4.4f, 5.5f, 6.6f, 7.7f, 8.8f);
            var vec2 = Vector256.Create(8.8f, 7.7f, 6.6f, 5.5f, 4.4f, 3.3f, 2.2f, 1.1f);
            while (!token.IsCancellationRequested) { var addResult = Avx.Add(vec1, vec2); var mulResult = Avx.Multiply(addResult, vec1); vec1 = Avx.Subtract(mulResult, vec2); }
        }
    }
}
