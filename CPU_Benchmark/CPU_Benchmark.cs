using System.Runtime.Intrinsics.X86;

namespace CPU_Benchmark
{
    public partial class CPU_Benchmark : Form
    {
        #region Fields & Class Members

        // Наши модули с логикой.
        private readonly CpuInfoProvider _cpuInfoProvider;
        private readonly BenchmarkRunner _benchmarkRunner;

        // Словарь для удобного сопоставления enum'а и текста в ComboBox.
        // Содержит все возможные тесты.
        private readonly Dictionary<BenchmarkType, string> _testTypes;

        // Источник токена для отмены теста. Null, когда тест не запущен.
        private CancellationTokenSource? _cancellationTokenSource;

        #endregion

        #region Constructor & Form Initialization

        public CPU_Benchmark()
        {
            InitializeComponent();

            // Инициализируем наши модули.
            _cpuInfoProvider = new CpuInfoProvider();
            _benchmarkRunner = new BenchmarkRunner();

            // Заполняем словарь со всеми возможными названиями тестов.
            _testTypes = new Dictionary<BenchmarkType, string>
            {
                { BenchmarkType.Integer, "Целочисленные операции" },
                { BenchmarkType.FloatingPoint, "Операции с плавающей запятой" },
                { BenchmarkType.VectorSse2, "Векторные операции (SSE2)" },
                { BenchmarkType.VectorAvx2, "Векторные операции (AVX2)" },
                { BenchmarkType.VectorFma, "Векторные операции (FMA)" },
                { BenchmarkType.CryptoAes, "Криптография (AES)" }
            };

            // Подписываемся на события.
            this.Load += OnFormLoad;
            btnStartTest.Click += OnStartTestClick;
            btnStopTest.Click += OnStopTestClick;
        }

        private void OnFormLoad(object? sender, EventArgs e)
        {
            // 1. Выводим информацию о процессоре.
            txtCpuInfo.Text = _cpuInfoProvider.FullCpuInfoString;

            // 2. Настраиваем NumericUpDown для выбора количества потоков.
            numericThreads.Maximum = _cpuInfoProvider.LogicalCoreCount;
            numericThreads.Value = _cpuInfoProvider.LogicalCoreCount;

            // 3. Динамически заполняем ComboBox только доступными тестами.
            PopulateAvailableTests();
        }

        #endregion

        #region Test Control Event Handlers

        private async void OnStartTestClick(object? sender, EventArgs e)
        {
            // Проверяем, есть ли вообще доступные тесты
            if (comboTestType.SelectedValue == null)
            {
                MessageBox.Show("Нет доступных тестов для вашего процессора.", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Получаем общие настройки из UI
            var selectedTest = (BenchmarkType)comboTestType.SelectedValue;
            var threadCount = (int)numericThreads.Value;
            var useAffinity = chkForceAffinity.Checked;
            var isBenchmark = rbModeBenchmark.Checked;

            // Обновляем UI для состояния "в работе"
            SetUiState(isTestRunning: true, isBenchmarkMode: isBenchmark);

            try
            {
                if (isBenchmark)
                {
                    // --- РЕЖИМ БЕНЧМАРКА ---
                    txtResults.Text = "Выполняется бенчмарк, пожалуйста, подождите...";

                    BenchmarkResult result = useAffinity
                        ? await _benchmarkRunner.RunBenchmarkWithAffinityAsync(selectedTest, threadCount)
                        : await _benchmarkRunner.RunBenchmarkAsync(selectedTest, threadCount);

                    txtResults.Text = result.ToString();
                    lblStatus.Text = "Бенчмарк завершен.";
                }
                else
                {
                    // --- РЕЖИМ СТРЕСС-ТЕСТА ---
                    _cancellationTokenSource = new CancellationTokenSource();

                    txtResults.Text = "Стресс-тест запущен...";
                    if (useAffinity)
                        await _benchmarkRunner.RunStressTestWithAffinityAsync(selectedTest, threadCount, _cancellationTokenSource.Token);
                    else
                        await _benchmarkRunner.RunStressTestAsync(selectedTest, threadCount, _cancellationTokenSource.Token);

                    txtResults.Text += Environment.NewLine + "Тест успешно остановлен.";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Во время теста произошла ошибка: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                txtResults.Text = $"Ошибка: {ex.Message}";
            }
            finally
            {
                SetUiState(isTestRunning: false, isBenchmarkMode: isBenchmark);
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }

        private void OnStopTestClick(object? sender, EventArgs e)
        {
            _cancellationTokenSource?.Cancel();
        }

        #endregion

        #region UI Helper Methods

        /// <summary>
        /// Заполняет ComboBox только теми тестами, которые поддерживаются текущим процессором.
        /// </summary>
        private void PopulateAvailableTests()
        {
            var supportedTests = new List<KeyValuePair<BenchmarkType, string>>();

            foreach (var testEntry in _testTypes)
            {
                if (IsTestTypeSupported(testEntry.Key))
                {
                    supportedTests.Add(testEntry);
                }
            }

            comboTestType.DataSource = new BindingSource(supportedTests, null);
            comboTestType.DisplayMember = "Value";
            comboTestType.ValueMember = "Key";
        }

        /// <summary>
        /// Проверяет, поддерживается ли конкретный тип теста процессором.
        /// </summary>
        private bool IsTestTypeSupported(BenchmarkType type)
        {
            return type switch
            {
                // Базовые тесты поддерживаются всегда
                BenchmarkType.Integer => true,
                BenchmarkType.FloatingPoint => true,

                // Специализированные тесты требуют проверки
                BenchmarkType.VectorSse2 => Sse2.IsSupported,
                BenchmarkType.VectorAvx2 => Avx2.IsSupported,
                BenchmarkType.VectorFma => Fma.IsSupported,
                BenchmarkType.CryptoAes => Aes.IsSupported,

                // На случай, если мы добавим что-то в enum, но забудем здесь
                _ => false
            };
        }

        /// <summary>
        /// Вспомогательный метод для переключения состояния элементов управления.
        /// </summary>
        private void SetUiState(bool isTestRunning, bool isBenchmarkMode)
        {
            if (isTestRunning)
            {
                txtResults.Clear();
                lblStatus.Text = "Выполняется тест...";
                progressBarTest.Style = ProgressBarStyle.Marquee;
            }
            else
            {
                lblStatus.Text = "Готов к работе...";
                progressBarTest.Style = ProgressBarStyle.Blocks;
                progressBarTest.Value = 0;
            }

            groupBoxSettings.Enabled = !isTestRunning;
            groupBoxMode.Enabled = !isTestRunning;
            btnStartTest.Enabled = !isTestRunning;

            // Кнопка СТОП активна, только если запущен тест, и это НЕ бенчмарк
            btnStopTest.Enabled = isTestRunning && !isBenchmarkMode;
        }

        #endregion
    }
}
