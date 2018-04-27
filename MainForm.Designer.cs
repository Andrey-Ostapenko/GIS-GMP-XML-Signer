namespace gisgmp_signer
{
    partial class MainForm
    {
        /// <summary>
        /// Обязательная переменная конструктора.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Освободить все используемые ресурсы.
        /// </summary>
        /// <param name="disposing">истинно, если управляемый ресурс должен быть удален; иначе ложно.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Код, автоматически созданный конструктором форм Windows

        /// <summary>
        /// Требуемый метод для поддержки конструктора — не изменяйте 
        /// содержимое этого метода с помощью редактора кода.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.bSign = new System.Windows.Forms.Button();
            this.bSettings = new System.Windows.Forms.Button();
            this.bStartService = new System.Windows.Forms.Button();
            this.cmsServiceMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.miStartService = new System.Windows.Forms.ToolStripMenuItem();
            this.miStopService = new System.Windows.Forms.ToolStripMenuItem();
            this.miStripSeparator = new System.Windows.Forms.ToolStripSeparator();
            this.miSettingsService = new System.Windows.Forms.ToolStripMenuItem();
            this.tbLog = new System.Windows.Forms.TextBox();
            this.cmsServiceMenu.SuspendLayout();
            this.SuspendLayout();
            // 
            // bSign
            // 
            this.bSign.Location = new System.Drawing.Point(12, 362);
            this.bSign.Name = "bSign";
            this.bSign.Size = new System.Drawing.Size(88, 23);
            this.bSign.TabIndex = 0;
            this.bSign.Text = "Подписать";
            this.bSign.UseVisualStyleBackColor = true;
            this.bSign.Click += new System.EventHandler(this.bSign_Click);
            // 
            // bSettings
            // 
            this.bSettings.Location = new System.Drawing.Point(106, 362);
            this.bSettings.Name = "bSettings";
            this.bSettings.Size = new System.Drawing.Size(75, 23);
            this.bSettings.TabIndex = 3;
            this.bSettings.Text = "Настройки";
            this.bSettings.UseVisualStyleBackColor = true;
            this.bSettings.Click += new System.EventHandler(this.bSettings_Click);
            // 
            // bStartService
            // 
            this.bStartService.Location = new System.Drawing.Point(435, 362);
            this.bStartService.Name = "bStartService";
            this.bStartService.Size = new System.Drawing.Size(137, 23);
            this.bStartService.TabIndex = 4;
            this.bStartService.Text = "Управление службой";
            this.bStartService.UseVisualStyleBackColor = true;
            this.bStartService.Click += new System.EventHandler(this.bStartService_Click);
            // 
            // cmsServiceMenu
            // 
            this.cmsServiceMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.miStartService,
            this.miStopService,
            this.miStripSeparator,
            this.miSettingsService});
            this.cmsServiceMenu.Name = "contextMenuStrip1";
            this.cmsServiceMenu.ShowImageMargin = false;
            this.cmsServiceMenu.Size = new System.Drawing.Size(128, 98);
            // 
            // miStartService
            // 
            this.miStartService.Name = "miStartService";
            this.miStartService.Size = new System.Drawing.Size(113, 22);
            this.miStartService.Text = "Запустить";
            this.miStartService.Click += new System.EventHandler(this.miStartService_Click);
            // 
            // miStopService
            // 
            this.miStopService.Name = "miStopService";
            this.miStopService.Size = new System.Drawing.Size(113, 22);
            this.miStopService.Text = "Остановить";
            this.miStopService.TextDirection = System.Windows.Forms.ToolStripTextDirection.Horizontal;
            this.miStopService.Click += new System.EventHandler(this.miStopService_Click);
            // 
            // miStripSeparator
            // 
            this.miStripSeparator.Name = "miStripSeparator";
            this.miStripSeparator.Size = new System.Drawing.Size(110, 6);
            // 
            // miSettingsService
            // 
            this.miSettingsService.Name = "miSettingsService";
            this.miSettingsService.Size = new System.Drawing.Size(127, 22);
            this.miSettingsService.Text = "Настройка";
            this.miSettingsService.Click += new System.EventHandler(this.miSettingsService_Click);
            // 
            // tbLog
            // 
            this.tbLog.Location = new System.Drawing.Point(12, 12);
            this.tbLog.Multiline = true;
            this.tbLog.Name = "tbLog";
            this.tbLog.ReadOnly = true;
            this.tbLog.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.tbLog.Size = new System.Drawing.Size(560, 344);
            this.tbLog.TabIndex = 5;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(584, 395);
            this.Controls.Add(this.tbLog);
            this.Controls.Add(this.bStartService);
            this.Controls.Add(this.bSettings);
            this.Controls.Add(this.bSign);
            this.Name = "MainForm";
            this.Text = "Подписание XML пакетов";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            this.cmsServiceMenu.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button bSign;
        private System.Windows.Forms.Button bSettings;
        private System.Windows.Forms.Button bStartService;
        private System.Windows.Forms.ContextMenuStrip cmsServiceMenu;
        public System.Windows.Forms.TextBox tbLog;
        private System.Windows.Forms.ToolStripMenuItem miStartService;
        private System.Windows.Forms.ToolStripMenuItem miStopService;
        private System.Windows.Forms.ToolStripMenuItem miSettingsService;
        private System.Windows.Forms.ToolStripSeparator miStripSeparator;
    }
}

