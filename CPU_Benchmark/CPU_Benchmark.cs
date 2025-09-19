using System.Runtime.Intrinsics.X86;
using System.Text;

namespace CPU_Benchmark
{
    public partial class CPU_Benchmark : Form
    {
        #region Fields & Class Members

        private readonly CpuInfoProvider _cpuInfoProvider;
        private readonly BenchmarkRunner _benchmarkRunner;
        private readonly SystemMonitor _systemMonitor;
        private readonly Dictionary<BenchmarkType, string> _testTypes;
        private CancellationTokenSource? _cancellationTokenSource;
        private readonly System.Windows.Forms.Timer _uiUpdateTimer;
        private string _staticCpuInfo = string.Empty;
        // Флаг, предотвращающий одновременный запуск нескольких обновлений
        private bool _isUpdatingInfo = false;

        #endregion

        // Новая простая структура для передачи данных между потоками
        private readonly record struct SystemMetrics(
            string CpuTemp,
            string CpuLoad,
            string CpuPower,
            List<StorageInfo> StorageDevices
        );

        #region Constructor & Form Initialization

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

        private void OnFormLoad(object? sender, EventArgs e)
        {
            _systemMonitor.Initialize();
            _staticCpuInfo = _cpuInfoProvider.FullCpuInfoString;
            // Запускаем первое асинхронное обновление
            OnUiUpdateTimerTick(this, EventArgs.Empty);
            _uiUpdateTimer.Start();
            numericThreads.Maximum = _cpuInfoProvider.LogicalCoreCount > 0 ? _cpuInfoProvider.LogicalCoreCount : 1;
            numericThreads.Value = _cpuInfoProvider.LogicalCoreCount > 0 ? _cpuInfoProvider.LogicalCoreCount : 1;
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
