namespace CPU_Benchmark
{
    public partial class CPU_Benchmark : Form
    {
        // Создаем приватное поле для нашего поставщика информации.
        // readonly означает, что мы присвоим ему значение только один раз в конструкторе.
        private readonly CpuInfoProvider _cpuInfoProvider;

        public CPU_Benchmark()
        {
            InitializeComponent();

            // Сразу после инициализации компонентов формы, создаем экземпляр нашего класса.
            _cpuInfoProvider = new CpuInfoProvider();

            // Подписываемся на событие загрузки формы.
            // Это лучший момент для первоначального заполнения данных на форме.
            this.Load += OnFormLoad;
        }

        private void OnFormLoad(object? sender, EventArgs e)
        {
            // 1. Выводим информацию о процессоре в текстовое поле.
            txtCpuInfo.Text = _cpuInfoProvider.FullCpuInfoString;

            // 2. Настраиваем NumericUpDown для выбора количества потоков.
            // Максимальное значение - количество логических ядер.
            numericThreads.Maximum = _cpuInfoProvider.LogicalCoreCount;
            // Значение по умолчанию - тоже максимальное.
            numericThreads.Value = _cpuInfoProvider.LogicalCoreCount;
        }
    }
}
