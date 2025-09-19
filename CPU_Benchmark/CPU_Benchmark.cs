using System.Runtime.Intrinsics.X86;
using System.Text;

namespace CPU_Benchmark
{
    /// <summary>
    /// Главная форма приложения, отвечающая за отображение информации о системе,
    /// управление настройками тестов и запуск процессов бенчмаркинга и стресс-тестирования.
    /// </summary>
    public partial class CPU_Benchmark : Form
    {
        #region Fields & Class Members

        /// <summary>
        /// Предоставляет статическую информацию о процессоре (имя, ядра, кэш и т.д.).
        /// </summary>
        private readonly CpuInfoProvider _cpuInfoProvider;

        /// <summary>
        /// Выполняет непосредственно вычислительные задачи бенчмарка и стресс-теста.
        /// </summary>
        private readonly BenchmarkRunner _benchmarkRunner;

        /// <summary>
        /// Отвечает за получение динамических данных с сенсоров (температура, загрузка, мощность).
        /// </summary>
        private readonly SystemMonitor _systemMonitor;

        /// <summary>
        /// Словарь для сопоставления типа теста (enum) с его читаемым названием для UI.
        /// </summary>
        private readonly Dictionary<BenchmarkType, string> _testTypes;

        /// <summary>
        /// Источник токенов для отмены асинхронной операции (используется для остановки стресс-теста).
        /// </summary>
        private CancellationTokenSource? _cancellationTokenSource;

        /// <summary>
        /// Таймер для периодического обновления динамической информации (температура, загрузка) в UI.
        /// </summary>
        private readonly System.Windows.Forms.Timer _uiUpdateTimer;

        /// <summary>
        /// Кэшированная строка со статической информацией о процессоре, чтобы не генерировать ее при каждом обновлении.
        /// </summary>
        private string _staticCpuInfo = string.Empty;

        /// <summary>
        /// Флаг, предотвращающий одновременный запуск нескольких асинхронных обновлений информации о системе.
        /// </summary>
        private bool _isUpdatingInfo = false;

        #endregion

        /// <summary>
        /// Структура для передачи собранных метрик системы из фонового потока в основной поток UI.
        /// </summary>
        private readonly record struct SystemMetrics(
            /// <summary>
            /// Температура ЦП в виде отформатированной строки.
            /// </summary>
            string CpuTemp,
            /// <summary>
            /// Общая загрузка ЦП в виде отформатированной строки.
            /// </summary>
            string CpuLoad,
            /// <summary>
            /// Энергопотребление ЦП в виде отформатированной строки.
            /// </summary>
            string CpuPower,
            /// <summary>
            /// Список информации о накопителях.
            /// </summary>
            List<StorageInfo> StorageDevices
        );

        #region Constructor & Form Initialization

        /// <summary>
        /// Инициализирует новый экземпляр главной формы <see cref="CPU_Benchmark"/>.
        /// </summary>
        public CPU_Benchmark()
        {
            InitializeComponent();
            _cpuInfoProvider = new CpuInfoProvider();
            _benchmarkRunner = new BenchmarkRunner();
            _systemMonitor = new SystemMonitor();
            _testTypes = new Dictionary<BenchmarkType, string>
            {
                { BenchmarkType.Integer, "Целочисленные операции" },
                { BenchmarkType.FloatingPoint, "Операции с плавающей запятой" },
                { BenchmarkType.VectorSse2, "Векторные операции (SSE2)" },
                { BenchmarkType.VectorAvx2, "Векторные операции (AVX2)" },
                { BenchmarkType.VectorFma, "Векторные операции (FMA)" },
                { BenchmarkType.CryptoAes, "Криптография (AES)" }
            };
            _uiUpdateTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            _uiUpdateTimer.Tick += OnUiUpdateTimerTick;
            this.Load += OnFormLoad;
            this.FormClosing += OnFormClosing;
            btnStartTest.Click += OnStartTestClick;
            btnStopTest.Click += OnStopTestClick;
        }

        /// <summary>
        /// Обработчик события загрузки формы. Инициализирует системный монитор,
        /// получает статическую информацию о ЦП, настраивает элементы управления и запускает таймер обновления UI.
        /// </summary>
        private void OnFormLoad(object? sender, EventArgs e)
        {
            _systemMonitor.Initialize();
            _staticCpuInfo = _cpuInfoProvider.FullCpuInfoString;
            // Запускаем первое асинхронное обновление сразу, не дожидаясь тика таймера.
            OnUiUpdateTimerTick(this, EventArgs.Empty);
            _uiUpdateTimer.Start();
            numericThreads.Maximum = _cpuInfoProvider.LogicalCoreCount > 0 ? _cpuInfoProvider.LogicalCoreCount : 1;
            numericThreads.Value = _cpuInfoProvider.LogicalCoreCount > 0 ? _cpuInfoProvider.LogicalCoreCount : 1;
            PopulateAvailableTests();
        }

        /// <summary>
        /// Обработчик события закрытия формы. Останавливает таймер и освобождает ресурсы системного монитора.
        /// </summary>
        private void OnFormClosing(object? sender, FormClosingEventArgs e)
        {
            _uiUpdateTimer.Stop();
            _systemMonitor.Dispose();
        }

        #endregion

        #region Test Control Event Handlers

        /// <summary>
        /// Обработчик нажатия на кнопку "Начать тест". Собирает настройки из UI,
        /// переключает состояние интерфейса и запускает соответствующий тест
        /// (бенчмарк или стресс-тест) в зависимости от выбора пользователя.
        /// </summary>
        private async void OnStartTestClick(object? sender, EventArgs e)
        {
            if (comboTestType.SelectedValue == null)
            {
                MessageBox.Show("Нет доступных тестов для вашего процессора.", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            var selectedTest = (BenchmarkType)comboTestType.SelectedValue;
            var threadCount = (int)numericThreads.Value;
            var useAffinity = chkForceAffinity.Checked;
            var isBenchmark = rbModeBenchmark.Checked;
            SetUiState(isTestRunning: true, isBenchmarkMode: isBenchmark);
            try
            {
                if (isBenchmark)
                {
                    txtResults.Text = "Выполняется бенчмарк, пожалуйста, подождите...";
                    BenchmarkResult result = useAffinity
                        ? await _benchmarkRunner.RunBenchmarkWithAffinityAsync(selectedTest, threadCount)
                        : await _benchmarkRunner.RunBenchmarkAsync(selectedTest, threadCount);
                    txtResults.Text = result.ToString();
                    lblStatus.Text = "Бенчмарк завершен.";
                }
                else
                {
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

        /// <summary>
        /// Обработчик нажатия на кнопку "Стоп". Отправляет сигнал отмены для запущенного стресс-теста.
        /// </summary>
        private void OnStopTestClick(object? sender, EventArgs e)
        {
            _cancellationTokenSource?.Cancel();
        }

        #endregion

        #region UI Helper Methods

        /// <summary>
        /// Обработчик тика таймера UI. Асинхронно запрашивает свежие метрики системы
        /// и обновляет текстовое поле с информацией.
        /// </summary>
        private async void OnUiUpdateTimerTick(object? sender, EventArgs e)
        {
            if (_isUpdatingInfo) return;

            try
            {
                _isUpdatingInfo = true;
                SystemMetrics metrics = await FetchSystemMetricsAsync();
                UpdateInfoTextBox(metrics);
            }
            finally
            {
                _isUpdatingInfo = false;
            }
        }

        /// <summary>
        /// Асинхронно собирает динамические метрики системы (температура, загрузка, мощность) в фоновом потоке,
        /// чтобы не блокировать UI.
        /// </summary>
        /// <returns>Задача, результатом которой является структура <see cref="SystemMetrics"/> с актуальными данными.</returns>
        private Task<SystemMetrics> FetchSystemMetricsAsync()
        {
            return Task.Run(() =>
            {
                float? cpuTemp = _systemMonitor.GetCpuTemperature();
                float? cpuLoad = _systemMonitor.GetCpuTotalLoad();
                float? cpuPower = _systemMonitor.GetCpuPackagePower();
                List<StorageInfo> storageInfos = _systemMonitor.GetStorageInfo();

                string tempString = cpuTemp.HasValue ? $"{cpuTemp.Value:F1} °C" : "N/A";
                string loadString = cpuLoad.HasValue ? $"{cpuLoad.Value:F1} %" : "N/A";
                string powerString = cpuPower.HasValue ? $"{cpuPower.Value:F1} W" : "N/A";

                return new SystemMetrics(tempString, loadString, powerString, storageInfos);
            });
        }

        /// <summary>
        /// Обновляет текстовое поле <see cref="txtCpuInfo"/> на основе полученных метрик,
        /// комбинируя статическую и динамическую информацию.
        /// </summary>
        /// <param name="metrics">Собранные метрики системы.</param>
        private void UpdateInfoTextBox(SystemMetrics metrics)
        {
            var sb = new StringBuilder();
            sb.Append(_staticCpuInfo);
            sb.AppendLine();
            sb.AppendLine("Динамические показатели");
            sb.AppendLine("-----------------------------------------");
            sb.AppendLine($"Температура ЦП:   \t{metrics.CpuTemp}");
            sb.AppendLine($"Загрузка ЦП:      \t{metrics.CpuLoad}");
            sb.AppendLine($"Потребление ЦП:   \t{metrics.CpuPower}");

            if (metrics.StorageDevices.Any())
            {
                sb.AppendLine();
                sb.AppendLine("Накопители (HDD/SSD)");
                sb.AppendLine("-----------------------------------------");
                foreach (var storage in metrics.StorageDevices)
                {
                    string storageTemp = storage.Temperature.HasValue ? $"{storage.Temperature.Value:F0} °C" : "N/A";
                    sb.AppendLine($"{storage.Name}\n\tТемпература: {storageTemp}");
                }
            }

            txtCpuInfo.Text = sb.ToString();
        }

        /// <summary>
        /// Заполняет выпадающий список <see cref="comboTestType"/> только теми тестами,
        /// которые поддерживаются текущим процессором.
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
        /// Проверяет, поддерживается ли указанный тип теста текущей аппаратной конфигурацией
        /// (например, наличие набора инструкций AVX2).
        /// </summary>
        /// <param name="type">Тип теста для проверки.</param>
        /// <returns><c>true</c>, если тест поддерживается; иначе <c>false</c>.</returns>
        private bool IsTestTypeSupported(BenchmarkType type)
        {
            return type switch
            {
                BenchmarkType.Integer => true,
                BenchmarkType.FloatingPoint => true,
                BenchmarkType.VectorSse2 => Sse2.IsSupported,
                BenchmarkType.VectorAvx2 => Avx2.IsSupported,
                BenchmarkType.VectorFma => Fma.IsSupported,
                BenchmarkType.CryptoAes => Aes.IsSupported,
                _ => false
            };
        }

        /// <summary>
        /// Управляет состоянием (включено/выключено) элементов управления на форме
        /// в зависимости от того, запущен ли тест.
        /// </summary>
        /// <param name="isTestRunning">Значение <c>true</c>, если тест в данный момент выполняется.</param>
        /// <param name="isBenchmarkMode">Значение <c>true</c>, если запущен бенчмарк (влияет на доступность кнопки "Стоп").</param>
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
            // Кнопка "Стоп" доступна только во время стресс-теста, т.к. бенчмарк нельзя прервать.
            btnStopTest.Enabled = isTestRunning && !isBenchmarkMode;
        }

        #endregion
    }
}