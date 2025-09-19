using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace CPU_Benchmark
{
    /// <summary>
    /// Отвечает за выполнение вычислительных циклов для бенчмарков и стресс-тестов.
    /// </summary>
    public class BenchmarkRunner
    {
        #region Constants for Benchmark

        /// <summary>
        /// Базовое количество операций для одного потока для простых тестов (целочисленные, с плавающей запятой).
        /// Выбрано так, чтобы тест длился несколько секунд на среднем процессоре.
        /// </summary>
        private const long BASE_OPERATIONS_PER_THREAD = 5_000_000_000L;

        /// <summary>
        /// Увеличенное количество операций для одного потока для тестов, использующих векторные и специализированные инструкции (SSE, AVX, AES).
        /// Эти инструкции выполняются намного быстрее, поэтому для них нужно больше операций, чтобы тест не закончился мгновенно.
        /// </summary>
        private const long ADVANCED_OPERATIONS_PER_THREAD = 20_000_000_000L;

        #endregion

        #region P/Invoke for Thread Affinity

        /// <summary>
        /// Возвращает идентификатор вызывающего потока.
        /// </summary>
        /// <returns>Идентификатор потока.</returns>
        [DllImport("kernel32.dll")]
        private static extern uint GetCurrentThreadId();

        #endregion

        #region Public Methods for Stress Test

        /// <summary>
        /// Запускает стресс-тест с распределением потоков планировщиком ОС (без привязки к ядрам).
        /// </summary>
        /// <param name="type">Тип выполняемого теста.</param>
        /// <param name="threadCount">Количество потоков для запуска.</param>
        /// <param name="token">Токен для отмены операции.</param>
        /// <returns>Задача, представляющая асинхронную операцию.</returns>
        public async Task RunStressTestAsync(BenchmarkType type, int threadCount, CancellationToken token)
        {
            await RunTestInternal(type, threadCount, token, forceAffinity: false, isBenchmark: false);
        }

        /// <summary>
        /// Запускает стресс-тест с жесткой привязкой каждого потока к своему логическому ядру.
        /// </summary>
        /// <param name="type">Тип выполняемого теста.</param>
        /// <param name="threadCount">Количество потоков для запуска.</param>
        /// <param name="token">Токен для отмены операции.</param>
        /// <returns>Задача, представляющая асинхронную операцию.</returns>
        public async Task RunStressTestWithAffinityAsync(BenchmarkType type, int threadCount, CancellationToken token)
        {
            await RunTestInternal(type, threadCount, token, forceAffinity: true, isBenchmark: false);
        }

        #endregion

        #region Public Methods for Benchmark

        /// <summary>
        /// Запускает бенчмарк с распределением потоков планировщиком ОС (без привязки к ядрам).
        /// </summary>
        /// <param name="type">Тип выполняемого теста.</param>
        /// <param name="threadCount">Количество потоков для запуска.</param>
        /// <returns>Задача, результатом которой является <see cref="BenchmarkResult"/>.</returns>
        public async Task<BenchmarkResult> RunBenchmarkAsync(BenchmarkType type, int threadCount)
        {
            var result = await RunTestInternal(type, threadCount, CancellationToken.None, forceAffinity: false, isBenchmark: true);
            return result.Value;
        }

        /// <summary>
        /// Запускает бенчмарк с жесткой привязкой каждого потока к своему логическому ядру.
        /// </summary>
        /// <param name="type">Тип выполняемого теста.</param>
        /// <param name="threadCount">Количество потоков для запуска.</param>
        /// <returns>Задача, результатом которой является <see cref="BenchmarkResult"/>.</returns>
        public async Task<BenchmarkResult> RunBenchmarkWithAffinityAsync(BenchmarkType type, int threadCount)
        {
            var result = await RunTestInternal(type, threadCount, CancellationToken.None, forceAffinity: true, isBenchmark: true);
            return result.Value;
        }

        #endregion

        #region Core Test Logic

        /// <summary>
        /// Внутренний метод, который выполняет основную логику запуска теста для обоих режимов (бенчмарк и стресс-тест).
        /// </summary>
        /// <param name="type">Тип выполняемого теста.</param>
        /// <param name="threadCount">Количество потоков для запуска.</param>
        /// <param name="token">Токен для отмены (используется только в режиме стресс-теста).</param>
        /// <param name="forceAffinity">Если true, каждый поток будет привязан к своему логическому ядру.</param>
        /// <param name="isBenchmark">Если true, запускается режим бенчмарка с фиксированным числом операций; иначе - стресс-тест.</param>
        /// <returns>
        /// Задача, результатом которой является <see cref="BenchmarkResult"/>, если <paramref name="isBenchmark"/> равен <c>true</c>; в противном случае <c>null</c>.
        /// </returns>
        private async Task<BenchmarkResult?> RunTestInternal(BenchmarkType type, int threadCount, CancellationToken token, bool forceAffinity, bool isBenchmark)
        {
            var stopwatch = new Stopwatch();
            long totalOperations = 0;

            await Task.Run(() =>
            {
                long opsPerThread = GetOperationsForTestType(type);
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

        /// <summary>
        /// Устанавливает привязку текущего потока к указанному логическому ядру.
        /// </summary>
        /// <param name="coreIndex">Индекс логического ядра (от 0).</param>
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
                // Игнорируем ошибки привязки, если что-то пошло не так (например, недостаточно прав).
                // Тест продолжит работу без жесткой привязки потока.
            }
        }

        /// <summary>
        /// Получает объект <see cref="ProcessThread"/> для текущего исполняемого потока.
        /// </summary>
        /// <returns>Объект <see cref="ProcessThread"/> или <c>null</c>, если его не удалось найти.</returns>
        private ProcessThread? GetProcessThreadFromCurrentThread()
        {
            uint nativeThreadId = GetCurrentThreadId();
            return Process.GetCurrentProcess().Threads
                          .Cast<ProcessThread>()
                          .FirstOrDefault(pt => pt.Id == nativeThreadId);
        }

        /// <summary>
        /// Определяет количество операций для указанного типа теста.
        /// </summary>
        /// <param name="type">Тип теста.</param>
        /// <returns>Количество операций для одного потока.</returns>
        private long GetOperationsForTestType(BenchmarkType type)
        {
            switch (type)
            {
                case BenchmarkType.Integer:
                case BenchmarkType.FloatingPoint:
                    return BASE_OPERATIONS_PER_THREAD;

                case BenchmarkType.VectorSse2:
                case BenchmarkType.VectorAvx2:
                case BenchmarkType.VectorFma:
                case BenchmarkType.CryptoAes:
                case BenchmarkType.PowerStressAvxFma: // Наш новый тест тоже "продвинутый"
                default:
                    return ADVANCED_OPERATIONS_PER_THREAD;
            }
        }

        /// <summary>
        /// Выбирает и запускает соответствующий бесконечный цикл для стресс-теста.
        /// </summary>
        /// <param name="type">Тип теста.</param>
        /// <param name="token">Токен для прерывания цикла.</param>
        private void ExecuteStressLoop(BenchmarkType type, CancellationToken token)
        {
            switch (type)
            {
                case BenchmarkType.Integer: IntegerStressLoop(token); break;
                case BenchmarkType.FloatingPoint: FloatStressLoop(token); break;
                case BenchmarkType.VectorSse2: Sse2StressLoop(token); break;
                case BenchmarkType.VectorAvx2: Avx2StressLoop(token); break;
                case BenchmarkType.VectorFma: FmaStressLoop(token); break;
                case BenchmarkType.CryptoAes: AesStressLoop(token); break;
                case BenchmarkType.PowerStressAvxFma: PowerStressLoop(token); break;
            }
        }

        /// <summary>
        /// Выбирает и запускает соответствующий цикл с фиксированным числом итераций для бенчмарка.
        /// </summary>
        /// <param name="type">Тип теста.</param>
        /// <param name="operations">Количество операций для выполнения.</param>
        private void ExecuteBenchmarkLoop(BenchmarkType type, long operations)
        {
            switch (type)
            {
                case BenchmarkType.Integer: IntegerBenchmarkLoop(operations); break;
                case BenchmarkType.FloatingPoint: FloatBenchmarkLoop(operations); break;
                case BenchmarkType.VectorSse2: Sse2BenchmarkLoop(operations); break;
                case BenchmarkType.VectorAvx2: Avx2BenchmarkLoop(operations); break;
                case BenchmarkType.VectorFma: FmaBenchmarkLoop(operations); break;
                case BenchmarkType.CryptoAes: AesBenchmarkLoop(operations); break;
                case BenchmarkType.PowerStressAvxFma: PowerStressBenchmarkLoop(operations); break;
            }
        }

        // --- Циклы для СТРЕСС-ТЕСТА (бесконечные, прерываются по токену) ---

        private void IntegerStressLoop(CancellationToken token) { long a = 1, b = 2; while (!token.IsCancellationRequested) { a ^= b; b = (a << 3) | (b >> 61); a = ~a; } }
        private void FloatStressLoop(CancellationToken token) { double a = Math.PI; while (!token.IsCancellationRequested) { a += Math.Sin(a); } }
        private void Sse2StressLoop(CancellationToken token) { var v1 = Vector128.Create(1.1f); var v2 = Vector128.Create(2.2f); while (!token.IsCancellationRequested) { var r1 = Sse2.Add(v1, v2); var r2 = Sse2.Multiply(r1, v1); v1 = Sse2.Subtract(r2, v2); } }
        private void Avx2StressLoop(CancellationToken token) { var v1 = Vector256.Create(1.1f); var v2 = Vector256.Create(2.2f); while (!token.IsCancellationRequested) { var r1 = Avx2.Add(v1, v2); var r2 = Avx2.Multiply(r1, v1); v1 = Avx2.Subtract(r2, v2); } }
        private void FmaStressLoop(CancellationToken token) { var v1 = Vector256.Create(1.1f); var v2 = Vector256.Create(2.2f); var v3 = Vector256.Create(3.3f); while (!token.IsCancellationRequested) { v1 = Fma.MultiplyAdd(v2, v3, v1); v2 = Fma.MultiplyAdd(v1, v3, v2); } }
        private void AesStressLoop(CancellationToken token) { var data = Vector128.Create((byte)1); var key = Vector128.Create((byte)2); while (!token.IsCancellationRequested) { data = Aes.Encrypt(data, key); } }

        /// <summary>
        /// Выполняет максимально интенсивный стресс-тест с использованием инструкций AVX и FMA
        /// для достижения пикового энергопотребления и тепловыделения процессора.
        /// </summary>
        private void PowerStressLoop(CancellationToken token)
        {
            var v1 = Vector256.Create(1.1f); var v2 = Vector256.Create(2.2f);
            var v3 = Vector256.Create(3.3f); var v4 = Vector256.Create(4.4f);
            var v5 = Vector256.Create(5.5f); var v6 = Vector256.Create(6.6f);
            while (!token.IsCancellationRequested)
            {
                v1 = Fma.MultiplyAdd(v2, v3, v1); v2 = Fma.MultiplyAdd(v4, v5, v2);
                v3 = Fma.MultiplyAdd(v6, v1, v3); v4 = Fma.MultiplyAdd(v1, v2, v4);
                v5 = Fma.MultiplyAdd(v3, v4, v5); v6 = Fma.MultiplyAdd(v5, v1, v6);
                v1 = Avx.Divide(v1, v2); v3 = Avx.Add(v3, v5);
            }
        }

        // --- Циклы для БЕНЧМАРКА (фиксированное число итераций) ---

        private void IntegerBenchmarkLoop(long operations) { long a = 1, b = 2; for (long i = 0; i < operations; i++) { a ^= b; b = (a << 3) | (b >> 61); a = ~a; } }
        private void FloatBenchmarkLoop(long operations) { double a = Math.PI; for (long i = 0; i < operations; i++) { a += Math.Sin(a); } }
        private void Sse2BenchmarkLoop(long operations) { var v1 = Vector128.Create(1.1f); var v2 = Vector128.Create(2.2f); for (long i = 0; i < operations; i++) { var r1 = Sse2.Add(v1, v2); var r2 = Sse2.Multiply(r1, v1); v1 = Sse2.Subtract(r2, v2); } }
        private void Avx2BenchmarkLoop(long operations) { var v1 = Vector256.Create(1.1f); var v2 = Vector256.Create(2.2f); for (long i = 0; i < operations; i++) { var r1 = Avx2.Add(v1, v2); var r2 = Avx2.Multiply(r1, v1); v1 = Avx2.Subtract(r2, v2); } }
        private void FmaBenchmarkLoop(long operations) { var v1 = Vector256.Create(1.1f); var v2 = Vector256.Create(2.2f); var v3 = Vector256.Create(3.3f); for (long i = 0; i < operations; i++) { v1 = Fma.MultiplyAdd(v2, v3, v1); v2 = Fma.MultiplyAdd(v1, v3, v2); } }
        private void AesBenchmarkLoop(long operations) { var data = Vector128.Create((byte)1); var key = Vector128.Create((byte)2); for (long i = 0; i < operations; i++) { data = Aes.Encrypt(data, key); } }

        /// <summary>
        /// Выполняет максимально интенсивный бенчмарк-тест с использованием инструкций AVX и FMA.
        /// </summary>
        private void PowerStressBenchmarkLoop(long operations)
        {
            var v1 = Vector256.Create(1.1f); var v2 = Vector256.Create(2.2f);
            var v3 = Vector256.Create(3.3f); var v4 = Vector256.Create(4.4f);
            var v5 = Vector256.Create(5.5f); var v6 = Vector256.Create(6.6f);
            for (long i = 0; i < operations; i++)
            {
                v1 = Fma.MultiplyAdd(v2, v3, v1); v2 = Fma.MultiplyAdd(v4, v5, v2);
                v3 = Fma.MultiplyAdd(v6, v1, v3); v4 = Fma.MultiplyAdd(v1, v2, v4);
                v5 = Fma.MultiplyAdd(v3, v4, v5); v6 = Fma.MultiplyAdd(v5, v1, v6);
                v1 = Avx.Divide(v1, v2); v3 = Avx.Add(v3, v5);
            }
        }

        #endregion
    }
}
