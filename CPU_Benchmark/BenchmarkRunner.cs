using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace CPU_Benchmark
{
    public class BenchmarkRunner
    {
        #region Constants for Benchmark

        // Базовое количество операций для одного потока в режиме бенчмарка.
        // Выбрано так, чтобы тест длился несколько секунд на среднем процессоре.
        private const long BASE_OPERATIONS_PER_THREAD = 5_000_000_000L;

        // AVX инструкции выполняются намного быстрее, поэтому для них нужно больше операций,
        // чтобы тест не закончился мгновенно.
        private const long AVX_OPERATIONS_PER_THREAD = 20_000_000_000L;

        #endregion

        #region P/Invoke for Thread Affinity

        [DllImport("kernel32.dll")]
        private static extern uint GetCurrentThreadId();

        #endregion

        #region Public Methods for Stress Test

        /// <summary>
        /// Запускает стресс-тест с распределением потоков планировщиком ОС (без привязки).
        /// </summary>
        public async Task RunStressTestAsync(BenchmarkType type, int threadCount, CancellationToken token)
        {
            await RunTestInternal(type, threadCount, token, forceAffinity: false, isBenchmark: false);
        }

        /// <summary>
        /// Запускает стресс-тест с жесткой привязкой каждого потока к своему логическому ядру.
        /// </summary>
        public async Task RunStressTestWithAffinityAsync(BenchmarkType type, int threadCount, CancellationToken token)
        {
            await RunTestInternal(type, threadCount, token, forceAffinity: true, isBenchmark: false);
        }

        #endregion

        #region Public Methods for Benchmark

        /// <summary>
        /// Запускает бенчмарк с распределением потоков планировщиком ОС (без привязки).
        /// </summary>
        public async Task<BenchmarkResult> RunBenchmarkAsync(BenchmarkType type, int threadCount)
        {
            var result = await RunTestInternal(type, threadCount, CancellationToken.None, forceAffinity: false, isBenchmark: true);
            return result.Value;
        }

        /// <summary>
        /// Запускает бенчмарк с жесткой привязкой каждого потока к своему логическому ядру.
        /// </summary>
        public async Task<BenchmarkResult> RunBenchmarkWithAffinityAsync(BenchmarkType type, int threadCount)
        {
            var result = await RunTestInternal(type, threadCount, CancellationToken.None, forceAffinity: true, isBenchmark: true);
            return result.Value;
        }

        #endregion

        #region Core Test Logic

        /// <summary>
        /// Внутренний метод, который выполняет основную логику запуска для обоих режимов.
        /// </summary>
        private async Task<BenchmarkResult?> RunTestInternal(BenchmarkType type, int threadCount, CancellationToken token, bool forceAffinity, bool isBenchmark)
        {
            if (type == BenchmarkType.VectorAvx2 && !Avx2.IsSupported)
            {
                throw new NotSupportedException("AVX2 инструкции не поддерживаются данным процессором.");
            }

            var stopwatch = new Stopwatch();
            long totalOperations = 0;

            await Task.Run(() =>
            {
                long opsPerThread = (type == BenchmarkType.VectorAvx2) ? AVX_OPERATIONS_PER_THREAD : BASE_OPERATIONS_PER_THREAD;
                if (isBenchmark)
                {
                    totalOperations = opsPerThread * threadCount;
                }

                var threads = new List<Thread>();
                for (int i = 0; i < threadCount; i++)
                {
                    int coreIndex = i;
                    var thread = new Thread(() =>
                    {
                        if (forceAffinity)
                        {
                            SetThreadAffinity(coreIndex);
                        }

                        // Выполняем либо бенчмарк, либо стресс-тест
                        if (isBenchmark)
                            ExecuteBenchmarkLoop(type, opsPerThread);
                        else
                            ExecuteStressLoop(type, token);
                    });
                    threads.Add(thread);
                }

                if (isBenchmark) stopwatch.Start();

                foreach (var thread in threads)
                {
                    thread.Start();
                }

                foreach (var thread in threads)
                {
                    thread.Join();
                }

                if (isBenchmark) stopwatch.Stop();
            });

            return isBenchmark ? new BenchmarkResult(stopwatch.Elapsed, totalOperations) : null;
        }

        #endregion

        #region Loop Implementations & Helpers

        private void SetThreadAffinity(int coreIndex)
        {
            try
            {
                var processThread = GetProcessThreadFromCurrentThread();
                if (processThread != null)
                {
                    processThread.ProcessorAffinity = (IntPtr)(1L << coreIndex);
                }
            }
            catch (Exception)
            {
                // Игнорируем ошибки привязки
            }
        }

        private ProcessThread? GetProcessThreadFromCurrentThread()
        {
            uint nativeThreadId = GetCurrentThreadId();
            return Process.GetCurrentProcess().Threads
                          .Cast<ProcessThread>()
                          .FirstOrDefault(pt => pt.Id == nativeThreadId);
        }

        // --- Методы-диспетчеры для циклов ---

        private void ExecuteStressLoop(BenchmarkType type, CancellationToken token)
        {
            switch (type)
            {
                case BenchmarkType.Integer: IntegerStressLoop(token); break;
                case BenchmarkType.FloatingPoint: FloatStressLoop(token); break;
                case BenchmarkType.VectorAvx2: AvxStressLoop(token); break;
            }
        }

        private void ExecuteBenchmarkLoop(BenchmarkType type, long operations)
        {
            switch (type)
            {
                case BenchmarkType.Integer: IntegerBenchmarkLoop(operations); break;
                case BenchmarkType.FloatingPoint: FloatBenchmarkLoop(operations); break;
                case BenchmarkType.VectorAvx2: AvxBenchmarkLoop(operations); break;
            }
        }

        // --- Циклы для СТРЕСС-ТЕСТА (бесконечные) ---

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
            var vec1 = Vector256.Create(1.1f); var vec2 = Vector256.Create(2.2f);
            while (!token.IsCancellationRequested) { var addResult = Avx.Add(vec1, vec2); var mulResult = Avx.Multiply(addResult, vec1); vec1 = Avx.Subtract(mulResult, vec2); }
        }

        // --- Циклы для БЕНЧМАРКА (фиксированное число итераций) ---

        private void IntegerBenchmarkLoop(long operations)
        {
            long a = 123456789012345;
            long b = 987654321098765;
            for (long i = 0; i < operations; i++) { a ^= b; b = (a << 3) | (b >> 61); a = ~a; }
        }

        private void FloatBenchmarkLoop(long operations)
        {
            double a = Math.PI;
            for (long i = 0; i < operations; i++) { a += Math.Sin(a) + Math.Cos(a) + Math.Tan(a); }
        }

        private void AvxBenchmarkLoop(long operations)
        {
            var vec1 = Vector256.Create(1.1f); var vec2 = Vector256.Create(2.2f);
            for (long i = 0; i < operations; i++) { var addResult = Avx.Add(vec1, vec2); var mulResult = Avx.Multiply(addResult, vec1); vec1 = Avx.Subtract(mulResult, vec2); }
        }

        #endregion
    }
}
