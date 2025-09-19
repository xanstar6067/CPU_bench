using System.Runtime.Intrinsics.X86;
using System.Text; // Добавлено для StringBuilder

namespace CPU_Benchmark
{
    public partial class CPU_Benchmark : Form
    {
        #region Fields & Class Members

        // Наши модули с логикой.
        private readonly CpuInfoProvider _cpuInfoProvider;
        private readonly BenchmarkRunner _benchmarkRunner;
        // Модуль для мониторинга температуры
        private readonly TemperatureMonitor _tempMonitor;

        // Словарь для удобного сопоставления enum'а и текста в ComboBox.
        // Содержит все возможные тесты.
        private readonly Dictionary<BenchmarkType, string> _testTypes;

        // Источник токена для отмены теста. Null, когда тест не запущен.
        private CancellationTokenSource? _cancellationTokenSource;

        // Таймер для обновления UI (включая температуру)
        private readonly System.Windows.Forms.Timer _uiUpdateTimer;
        // Хранилище для статической информации о ЦП, чтобы не пересоздавать ее каждую секунду
        private string _staticCpuInfo = string.Empty;

        #endregion

        #region Constructor & Form Initialization

        public CPU_Benchmark()
        {
            InitializeComponent();

            // Инициализируем наши модули.
            _cpuInfoProvider = new CpuInfoProvider();
            _benchmarkRunner = new BenchmarkRunner();
            _tempMonitor = new TemperatureMonitor();

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

            // Настраиваем таймер
            _uiUpdateTimer = new System.Windows.Forms.Timer
            {
                Interval = 1000 // Обновление раз в секунду
            };
            _uiUpdateTimer.Tick += OnUiUpdateTimerTick;


            // Подписываемся на события.
            this.Load += OnFormLoad;
            this.FormClosing += OnFormClosing; // Для корректной очистки ресурсов
            btnStartTest.Click += OnStartTestClick;
            btnStopTest.Click += OnStopTestClick;
        }

        private void OnFormLoad(object? sender, EventArgs e)
        {
            // 1. Инициализируем монитор температуры
            _tempMonitor.Initialize();

            // 2. Сохраняем статическую информацию о ЦП один раз при загрузке.
            _staticCpuInfo = _cpuInfoProvider.FullCpuInfoString;

            // 3. Выполняем первое обновление текстового поля с температурой.
            UpdateCpuInfoText();

            // 4. Запускаем таймер для периодических обновлений.
            _uiUpdateTimer.Start();

            // 5. Настраиваем NumericUpDown для выбора количества потоков.
            numericThreads.Maximum = _cpuInfoProvider.LogicalCoreCount > 0 ? _cpuInfoProvider.LogicalCoreCount : 1;
            numericThreads.Value = _cpuInfoProvider.LogicalCoreCount > 0 ? _cpuInfoProvider.LogicalCoreCount : 1;

            // 6. Динамически заполняем ComboBox только доступными тестами.
            PopulateAvailableTests();
        }

        // Метод для остановки таймера и очистки ресурсов при закрытии формы
        private void OnFormClosing(object? sender, FormClosingEventArgs e)
        {
            _uiUpdateTimer.Stop();
            _tempMonitor.Dispose();
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

        // Метод, который будет вызываться по таймеру для обновления температуры
        private void OnUiUpdateTimerTick(object? sender, EventArgs e)
        {
            UpdateCpuInfoText();
        }

        // Централизованный метод для обновления информации о ЦП
        private void UpdateCpuInfoText()
        {
            float? temperature = _tempMonitor.GetCpuTemperature();
            string tempString = temperature.HasValue
                ? $"{temperature.Value:F1} °C"
                : "N/A";

            var sb = new StringBuilder();
            sb.Append(_staticCpuInfo); // Вставляем всю статическую информацию
            sb.AppendLine();
            sb.AppendLine("Мониторинг");
            sb.AppendLine("-----------------------------------------");
            sb.AppendLine($"Температура ЦП:   \t{tempString}");

            // Чтобы избежать мерцания, обновляем текст только если он изменился
            if (txtCpuInfo.Text != sb.ToString())
            {
                txtCpuInfo.Text = sb.ToString();
            }
        }

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
