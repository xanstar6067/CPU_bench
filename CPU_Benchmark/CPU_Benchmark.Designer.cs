namespace CPU_Benchmark
{
    partial class CPU_Benchmark
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            tableLayoutPanelMain = new TableLayoutPanel();
            groupBoxSystemInfo = new GroupBox();
            txtCpuInfo = new TextBox();
            panelLeft = new Panel();
            groupBoxResults = new GroupBox();
            txtResults = new TextBox();
            lblStatus = new Label();
            progressBarTest = new ProgressBar();
            groupBoxControls = new GroupBox();
            btnStopTest = new Button();
            btnStartTest = new Button();
            groupBoxSettings = new GroupBox();
            chkForceAffinity = new CheckBox();
            numericThreads = new NumericUpDown();
            lblThreads = new Label();
            comboTestType = new ComboBox();
            lblTestType = new Label();
            tableLayoutPanelMain.SuspendLayout();
            groupBoxSystemInfo.SuspendLayout();
            panelLeft.SuspendLayout();
            groupBoxResults.SuspendLayout();
            groupBoxControls.SuspendLayout();
            groupBoxSettings.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numericThreads).BeginInit();
            SuspendLayout();
            // 
            // tableLayoutPanelMain
            // 
            tableLayoutPanelMain.ColumnCount = 2;
            tableLayoutPanelMain.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 320F));
            tableLayoutPanelMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanelMain.Controls.Add(groupBoxSystemInfo, 1, 0);
            tableLayoutPanelMain.Controls.Add(panelLeft, 0, 0);
            tableLayoutPanelMain.Dock = DockStyle.Fill;
            tableLayoutPanelMain.Location = new Point(0, 0);
            tableLayoutPanelMain.Name = "tableLayoutPanelMain";
            tableLayoutPanelMain.RowCount = 1;
            tableLayoutPanelMain.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanelMain.Size = new Size(884, 561);
            tableLayoutPanelMain.TabIndex = 0;
            // 
            // groupBoxSystemInfo
            // 
            groupBoxSystemInfo.Controls.Add(txtCpuInfo);
            groupBoxSystemInfo.Dock = DockStyle.Fill;
            groupBoxSystemInfo.Location = new Point(323, 3);
            groupBoxSystemInfo.Name = "groupBoxSystemInfo";
            groupBoxSystemInfo.Padding = new Padding(10);
            groupBoxSystemInfo.Size = new Size(558, 555);
            groupBoxSystemInfo.TabIndex = 1;
            groupBoxSystemInfo.TabStop = false;
            groupBoxSystemInfo.Text = "Информация о системе";
            // 
            // txtCpuInfo
            // 
            txtCpuInfo.Dock = DockStyle.Fill;
            txtCpuInfo.Font = new Font("Consolas", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 204);
            txtCpuInfo.Location = new Point(10, 26);
            txtCpuInfo.Multiline = true;
            txtCpuInfo.Name = "txtCpuInfo";
            txtCpuInfo.ReadOnly = true;
            txtCpuInfo.ScrollBars = ScrollBars.Vertical;
            txtCpuInfo.Size = new Size(538, 519);
            txtCpuInfo.TabIndex = 0;
            txtCpuInfo.Text = "Определение параметров процессора...";
            // 
            // panelLeft
            // 
            panelLeft.Controls.Add(groupBoxResults);
            panelLeft.Controls.Add(groupBoxControls);
            panelLeft.Controls.Add(groupBoxSettings);
            panelLeft.Dock = DockStyle.Fill;
            panelLeft.Location = new Point(3, 3);
            panelLeft.Name = "panelLeft";
            panelLeft.Size = new Size(314, 555);
            panelLeft.TabIndex = 2;
            // 
            // groupBoxResults
            // 
            groupBoxResults.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            groupBoxResults.Controls.Add(txtResults);
            groupBoxResults.Controls.Add(lblStatus);
            groupBoxResults.Controls.Add(progressBarTest);
            groupBoxResults.Location = new Point(9, 348);
            groupBoxResults.Name = "groupBoxResults";
            groupBoxResults.Size = new Size(296, 198);
            groupBoxResults.TabIndex = 2;
            groupBoxResults.TabStop = false;
            groupBoxResults.Text = "Выполнение и результат";
            // 
            // txtResults
            // 
            txtResults.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            txtResults.Location = new Point(15, 87);
            txtResults.Multiline = true;
            txtResults.Name = "txtResults";
            txtResults.ReadOnly = true;
            txtResults.ScrollBars = ScrollBars.Vertical;
            txtResults.Size = new Size(266, 96);
            txtResults.TabIndex = 2;
            // 
            // lblStatus
            // 
            lblStatus.AutoSize = true;
            lblStatus.Location = new Point(15, 28);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(95, 15);
            lblStatus.TabIndex = 1;
            lblStatus.Text = "Готов к работе...";
            // 
            // progressBarTest
            // 
            progressBarTest.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            progressBarTest.Location = new Point(15, 52);
            progressBarTest.Name = "progressBarTest";
            progressBarTest.Size = new Size(266, 23);
            progressBarTest.TabIndex = 0;
            // 
            // groupBoxControls
            // 
            groupBoxControls.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            groupBoxControls.Controls.Add(btnStopTest);
            groupBoxControls.Controls.Add(btnStartTest);
            groupBoxControls.Location = new Point(9, 215);
            groupBoxControls.Name = "groupBoxControls";
            groupBoxControls.Size = new Size(296, 127);
            groupBoxControls.TabIndex = 1;
            groupBoxControls.TabStop = false;
            groupBoxControls.Text = "Управление";
            // 
            // btnStopTest
            // 
            btnStopTest.Enabled = false;
            btnStopTest.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            btnStopTest.ForeColor = Color.DarkRed;
            btnStopTest.Location = new Point(15, 75);
            btnStopTest.Name = "btnStopTest";
            btnStopTest.Size = new Size(266, 39);
            btnStopTest.TabIndex = 1;
            btnStopTest.Text = "СТОП";
            btnStopTest.UseVisualStyleBackColor = true;
            // 
            // btnStartTest
            // 
            btnStartTest.Font = new Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point, 204);
            btnStartTest.ForeColor = Color.DarkGreen;
            btnStartTest.Location = new Point(15, 27);
            btnStartTest.Name = "btnStartTest";
            btnStartTest.Size = new Size(266, 39);
            btnStartTest.TabIndex = 0;
            btnStartTest.Text = "НАЧАТЬ ТЕСТ";
            btnStartTest.UseVisualStyleBackColor = true;
            // 
            // groupBoxSettings
            // 
            groupBoxSettings.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            groupBoxSettings.Controls.Add(chkForceAffinity);
            groupBoxSettings.Controls.Add(numericThreads);
            groupBoxSettings.Controls.Add(lblThreads);
            groupBoxSettings.Controls.Add(comboTestType);
            groupBoxSettings.Controls.Add(lblTestType);
            groupBoxSettings.Location = new Point(9, 9);
            groupBoxSettings.Name = "groupBoxSettings";
            groupBoxSettings.Size = new Size(296, 200);
            groupBoxSettings.TabIndex = 0;
            groupBoxSettings.TabStop = false;
            groupBoxSettings.Text = "Настройки теста";
            // 
            // chkForceAffinity
            // 
            chkForceAffinity.AutoSize = true;
            chkForceAffinity.Checked = true;
            chkForceAffinity.CheckState = CheckState.Checked;
            chkForceAffinity.Location = new Point(15, 153);
            chkForceAffinity.Name = "chkForceAffinity";
            chkForceAffinity.Size = new Size(244, 19);
            chkForceAffinity.TabIndex = 4;
            chkForceAffinity.Text = "Привязать потоки к ядрам (1 поток = 1 ядро)";
            chkForceAffinity.UseVisualStyleBackColor = true;
            // 
            // numericThreads
            // 
            numericThreads.Font = new Font("Segoe UI", 11.25F);
            numericThreads.Location = new Point(15, 116);
            numericThreads.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            numericThreads.Name = "numericThreads";
            numericThreads.Size = new Size(266, 27);
            numericThreads.TabIndex = 3;
            numericThreads.Value = new decimal(new int[] { 1, 0, 0, 0 });
            // 
            // lblThreads
            // 
            lblThreads.AutoSize = true;
            lblThreads.Location = new Point(15, 95);
            lblThreads.Name = "lblThreads";
            lblThreads.Size = new Size(118, 15);
            lblThreads.TabIndex = 2;
            lblThreads.Text = "Количество потоков:";
            // 
            // comboTestType
            // 
            comboTestType.DropDownStyle = ComboBoxStyle.DropDownList;
            comboTestType.Font = new Font("Segoe UI", 11.25F);
            comboTestType.FormattingEnabled = true;
            comboTestType.Location = new Point(15, 50);
            comboTestType.Name = "comboTestType";
            comboTestType.Size = new Size(266, 28);
            comboTestType.TabIndex = 1;
            // 
            // lblTestType
            // 
            lblTestType.AutoSize = true;
            lblTestType.Location = new Point(15, 28);
            lblTestType.Name = "lblTestType";
            lblTestType.Size = new Size(62, 15);
            lblTestType.TabIndex = 0;
            lblTestType.Text = "Тип теста:";
            // 
            // CPU_Benchmark
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(884, 561);
            Controls.Add(tableLayoutPanelMain);
            MinimumSize = new Size(750, 550);
            Name = "CPU_Benchmark";
            Text = "CPU Benchmark Tool (.NET 8)";
            tableLayoutPanelMain.ResumeLayout(false);
            groupBoxSystemInfo.ResumeLayout(false);
            groupBoxSystemInfo.PerformLayout();
            panelLeft.ResumeLayout(false);
            groupBoxResults.ResumeLayout(false);
            groupBoxResults.PerformLayout();
            groupBoxControls.ResumeLayout(false);
            groupBoxSettings.ResumeLayout(false);
            groupBoxSettings.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)numericThreads).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private TableLayoutPanel tableLayoutPanelMain;
        private GroupBox groupBoxSystemInfo;
        private TextBox txtCpuInfo;
        private Panel panelLeft;
        private GroupBox groupBoxSettings;
        private GroupBox groupBoxControls;
        private GroupBox groupBoxResults;
        private ComboBox comboTestType;
        private Label lblTestType;
        private NumericUpDown numericThreads;
        private Label lblThreads;
        private Button btnStartTest;
        private Button btnStopTest;
        private ProgressBar progressBarTest;
        private Label lblStatus;
        private TextBox txtResults;
        private CheckBox chkForceAffinity;
    }
}