namespace CPU_Benchmark
{
    public partial class CPU_Benchmark : Form
    {
        // ������� ��������� ���� ��� ������ ���������� ����������.
        // readonly ��������, ��� �� �������� ��� �������� ������ ���� ��� � ������������.
        private readonly CpuInfoProvider _cpuInfoProvider;

        public CPU_Benchmark()
        {
            InitializeComponent();

            // ����� ����� ������������� ����������� �����, ������� ��������� ������ ������.
            _cpuInfoProvider = new CpuInfoProvider();

            // ������������� �� ������� �������� �����.
            // ��� ������ ������ ��� ��������������� ���������� ������ �� �����.
            this.Load += OnFormLoad;
        }

        private void OnFormLoad(object? sender, EventArgs e)
        {
            // 1. ������� ���������� � ���������� � ��������� ����.
            txtCpuInfo.Text = _cpuInfoProvider.FullCpuInfoString;

            // 2. ����������� NumericUpDown ��� ������ ���������� �������.
            // ������������ �������� - ���������� ���������� ����.
            numericThreads.Maximum = _cpuInfoProvider.LogicalCoreCount;
            // �������� �� ��������� - ���� ������������.
            numericThreads.Value = _cpuInfoProvider.LogicalCoreCount;
        }
    }
}
