using System.Runtime.Intrinsics.X86;
using System.Text;

namespace CPU_Benchmark
{
    public partial class CPU_Benchmark : Form
    {
        #region Fields & Class Members

        // Наши модули с логикой.
        private readonly CpuInfoProvider _cpuInfoProvider;
        private readonly BenchmarkRunner _benchmarkRunner;
        // Модуль для мониторинга системы (ЦП, накопители и т.д.)
        private readonly SystemMonitor _systemMonitor;

        // Словарь для удобного сопоставления enum'а и текста в ComboBox.
        private readonly Dictionary<BenchmarkType, string> _testTypes;

        // Источник токена для отмены теста.
        private CancellationTokenSource? _cancellationTokenSource;

        // Таймер для обновления UI
        private readonly System.Windows.Forms.Timer _uiUpdateTimer;
        // Хранилище для статической информации о ЦП
        private string _staticCpuInfo = string.Empty;

        #endregion

        #region Constructor & Form Initialization

        public CPU_Benchmark()
        {
            InitializeComponent();

            // Инициализируем наши модули.
            _cpuInfoProvider = new CpuInfoProvider();
            _benchmarkRunner = new BenchmarkRunner();
            _systemMonitor = new SystemMonitor();

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
            this.FormClosing += OnFormClosing;
            btnStartTest.Click += OnStartTestClick;
            btnStopTest.Click += OnStopTestClick;
        }

        private void OnFormLoad(object? sender, EventArgs e)
        {
            // 1. Инициализируем системный монитор
            _systemMonitor.Initialize();

            // 2. Сохраняем статическую информацию о ЦП
            _staticCpuInfo = _cpuInfoProvider.FullCpuInfoString;

            // 3. Выполняем первое обновление текстового поля
            UpdateCpuInfoText();

            // 4. Запускаем таймер для периодических обновлений
            _uiUpdateTimer.Start();

            // 5. Настраиваем NumericUpDown для выбора количества потоков
            numericThreads.Maximum = _cpuInfoProvider.LogicalCoreCount > 0 ? _cpuInfoProvider.LogicalCoreCount : 1;
            numericThreads.Value = _cpuInfoProvider.LogicalCoreCount > 0 ? _cpuInfoProvider.LogicalCoreCount : 1;

            // 6. Динамически заполняем ComboBox
            PopulateAvailableTests();
        }

        private void OnFormClosing(object? sender, FormClosingEventArgs e)
        {
            _uiUpdateTimer.Stop();
            _systemMonitor.Dispose();
        }

        #endregion

        #region Test Control Event Handlers

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

        private void OnStopTestClick(object? sender, EventArgs e)
        {
            _cancellationTokenSource?.Cancel();
        }

        #endregion

        #region UI Helper Methods

        private void OnUiUpdateTimerTick(object? sender, EventArgs e)
        {
            UpdateCpuInfoText();
        }

        private void UpdateCpuInfoText()
        {
            // Получаем все динамические данные от сенсоров
            float? cpuTemp = _systemMonitor.GetCpuTemperature();
            float? cpuLoad = _systemMonitor.GetCpuTotalLoad();
            float? cpuPower = _systemMonitor.GetCpuPackagePower();
            List<StorageInfo> storageInfos = _systemMonitor.GetStorageInfo();

            // Форматируем строки для вывода, обрабатывая null
            string tempString = cpuTemp.HasValue ? $"{cpuTemp.Value:F1} °C" : "N/A";
            string loadString = cpuLoad.HasValue ? $"{cpuLoad.Value:F1} %" : "N/A";
            string powerString = cpuPower.HasValue ? $"{cpuPower.Value:F1} W" : "N/A";

            var sb = new StringBuilder();
            sb.Append(_staticCpuInfo); // Вставляем всю статическую информацию о ЦП
            sb.AppendLine();
            sb.AppendLine("Динамические показатели");
            sb.AppendLine("-----------------------------------------");
            sb.AppendLine($"Температура ЦП:   \t{tempString}");
            sb.AppendLine($"Загрузка ЦП:      \t{loadString}");
            sb.AppendLine($"Потребление ЦП:   \t{powerString}");

            if (storageInfos.Any())
            {
                sb.AppendLine();
                sb.AppendLine("Накопители (HDD/SSD)");
                sb.AppendLine("-----------------------------------------");
                foreach (var storage in storageInfos)
                {
                    string storageTemp = storage.Temperature.HasValue ? $"{storage.Temperature.Value:F0} °C" : "N/A";
                    sb.AppendLine($"{storage.Name}\n\tТемпература: {storageTemp}");
                }
            }

            // Обновляем текст только если он изменился, чтобы избежать мерцания
            if (txtCpuInfo.Text != sb.ToString())
            {
                txtCpuInfo.Text = sb.ToString();
            }
        }

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
            btnStopTest.Enabled = isTestRunning && !isBenchmarkMode;
        }

        #endregion
    }
}