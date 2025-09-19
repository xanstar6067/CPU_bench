using System.Runtime.Intrinsics.X86;

namespace CPU_Benchmark
{
    public partial class CPU_Benchmark : Form
    {
        #region Fields & Class Members

        private readonly CpuInfoProvider _cpuInfoProvider;
        private readonly BenchmarkRunner _benchmarkRunner;
        private readonly Dictionary<BenchmarkType, string> _testTypes;
        private CancellationTokenSource? _cancellationTokenSource;

        #endregion

        #region Constructor & Form Initialization

        public CPU_Benchmark()
        {
            InitializeComponent();

            _cpuInfoProvider = new CpuInfoProvider();
            _benchmarkRunner = new BenchmarkRunner();

            _testTypes = new Dictionary<BenchmarkType, string>
            {
                { BenchmarkType.Integer, "Целочисленные операции" },
                { BenchmarkType.FloatingPoint, "Операции с плавающей запятой" },
                { BenchmarkType.VectorAvx2, "Векторные операции (AVX2)" }
            };

            this.Load += OnFormLoad;
            btnStartTest.Click += OnStartTestClick;
            btnStopTest.Click += OnStopTestClick;
        }

        private void OnFormLoad(object? sender, EventArgs e)
        {
            txtCpuInfo.Text = _cpuInfoProvider.FullCpuInfoString;
            numericThreads.Maximum = _cpuInfoProvider.LogicalCoreCount;
            numericThreads.Value = _cpuInfoProvider.LogicalCoreCount;

            comboTestType.DataSource = new BindingSource(_testTypes, null);
            comboTestType.DisplayMember = "Value";
            comboTestType.ValueMember = "Key";

            if (!Avx2.IsSupported)
            {
                if (comboTestType.Items.Count > 0 && _testTypes.ContainsKey(BenchmarkType.VectorAvx2))
                {
                    ((BindingSource)comboTestType.DataSource).Remove(new KeyValuePair<BenchmarkType, string>(BenchmarkType.VectorAvx2, _testTypes[BenchmarkType.VectorAvx2]));
                }
            }
        }

        #endregion

        #region Test Control Event Handlers

        private async void OnStartTestClick(object? sender, EventArgs e)
        {
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

                    // Асинхронно запускаем бенчмарк и ждем его результат
                    BenchmarkResult result;
                    if (useAffinity)
                    {
                        result = await _benchmarkRunner.RunBenchmarkWithAffinityAsync(selectedTest, threadCount);
                    }
                    else
                    {
                        result = await _benchmarkRunner.RunBenchmarkAsync(selectedTest, threadCount);
                    }

                    // Отображаем отформатированный результат
                    txtResults.Text = result.ToString();
                    lblStatus.Text = "Бенчмарк завершен.";
                }
                else
                {
                    // --- РЕЖИМ СТРЕСС-ТЕСТА ---
                    _cancellationTokenSource = new CancellationTokenSource();

                    txtResults.Text = "Стресс-тест запущен...";
                    if (useAffinity)
                    {
                        await _benchmarkRunner.RunStressTestWithAffinityAsync(selectedTest, threadCount, _cancellationTokenSource.Token);
                    }
                    else
                    {
                        await _benchmarkRunner.RunStressTestAsync(selectedTest, threadCount, _cancellationTokenSource.Token);
                    }

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
                // В любом случае возвращаем UI в исходное состояние
                SetUiState(isTestRunning: false, isBenchmarkMode: isBenchmark);
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }

        private void OnStopTestClick(object? sender, EventArgs e)
        {
            // Отправляем сигнал отмены. Актуально только для стресс-теста.
            _cancellationTokenSource?.Cancel();
        }

        #endregion

        #region UI Helper Methods

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
