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

            // Заполняем словарь с названиями тестов.
            _testTypes = new Dictionary<BenchmarkType, string>
            {
                { BenchmarkType.Integer, "Целочисленные операции" },
                { BenchmarkType.FloatingPoint, "Операции с плавающей запятой" },
                { BenchmarkType.VectorAvx2, "Векторные операции (AVX2)" }
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

            // 3. Заполняем ComboBox доступными тестами.
            comboTestType.DataSource = new BindingSource(_testTypes, null);
            comboTestType.DisplayMember = "Value";
            comboTestType.ValueMember = "Key";

            // Удаляем опцию AVX2, если она не поддерживается.
            if (!Avx2.IsSupported)
            {
                // Безопасное удаление из источника данных.
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
            // Получаем выбранные настройки из UI.
            var selectedTest = (BenchmarkType)comboTestType.SelectedValue;
            var threadCount = (int)numericThreads.Value;
            bool useAffinity = chkForceAffinity.Checked; // <-- Проверяем галочку

            _cancellationTokenSource = new CancellationTokenSource();

            // Обновляем UI для состояния "в работе".
            SetUiState(isTestRunning: true);

            try
            {
                // В зависимости от галочки, вызываем нужный метод
                if (useAffinity)
                {
                    txtResults.Text = $"Запуск теста с привязкой к {threadCount} ядрам...";
                    await _benchmarkRunner.RunStressTestWithAffinityAsync(selectedTest, threadCount, _cancellationTokenSource.Token);
                }
                else
                {
                    txtResults.Text = $"Запуск теста на {threadCount} потоках (без привязки)...";
                    await _benchmarkRunner.RunStressTestAsync(selectedTest, threadCount, _cancellationTokenSource.Token);
                }

                // Это выполнится, если тест был отменен пользователем.
                txtResults.Text += Environment.NewLine + "Тест успешно остановлен.";
            }
            catch (Exception ex)
            {
                // ... (остальной код без изменений)
                MessageBox.Show($"Во время теста произошла ошибка: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                txtResults.Text = $"Ошибка: {ex.Message}";
            }
            finally
            {
                // ... (остальной код без изменений)
                SetUiState(isTestRunning: false);
                _cancellationTokenSource.Dispose();
                _cancellationTokenSource = null;
            }
        }


        private void OnStopTestClick(object? sender, EventArgs e)
        {
            // Отправляем сигнал отмены запущенному тесту.
            _cancellationTokenSource?.Cancel();
        }

        #endregion

        #region UI Helper Methods

        /// <summary>
        /// Вспомогательный метод для переключения состояния элементов управления.
        /// </summary>
        private void SetUiState(bool isTestRunning)
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
            btnStartTest.Enabled = !isTestRunning;
            btnStopTest.Enabled = isTestRunning;
        }

        #endregion
    }
}
