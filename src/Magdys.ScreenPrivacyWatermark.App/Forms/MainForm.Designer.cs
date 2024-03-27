namespace Magdys.ScreenPrivacyWatermark.App.Forms
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            BackgroundWorkerDispatcher = new System.ComponentModel.BackgroundWorker();
            labelInfo = new Label();
            TimerOnlineStatus = new System.Windows.Forms.Timer(components);
            SuspendLayout();
            // 
            // BackgroundWorkerDispatcher
            // 
            BackgroundWorkerDispatcher.DoWork += BackgroundWorkerDispatcher_DoWork;
            // 
            // labelInfo
            // 
            labelInfo.Dock = DockStyle.Fill;
            labelInfo.Font = new Font("Segoe UI", 24F, FontStyle.Regular, GraphicsUnit.Point, 0);
            labelInfo.Location = new Point(0, 0);
            labelInfo.Name = "labelInfo";
            labelInfo.Size = new Size(720, 440);
            labelInfo.TabIndex = 0;
            labelInfo.Text = "Magdy's Screen Privacy Watermark";
            labelInfo.TextAlign = ContentAlignment.MiddleCenter;
            labelInfo.Visible = false;
            // 
            // TimerOnlineStatus
            // 
            TimerOnlineStatus.Interval = 1000;
            TimerOnlineStatus.Tick += TimerOnlineStatus_Tick;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            BackColor = Color.White;
            ClientSize = new Size(720, 440);
            ControlBox = false;
            Controls.Add(labelInfo);
            FormBorderStyle = FormBorderStyle.None;
            Margin = new Padding(1, 1, 1, 1);
            MaximizeBox = false;
            MdiChildrenMinimizedAnchorBottom = false;
            MinimizeBox = false;
            Name = "MainForm";
            ShowIcon = false;
            ShowInTaskbar = false;
            Text = "MainForm";
            TopMost = true;
            TransparencyKey = Color.White;
            FormClosing += MainForm_FormClosing;
            Load += MainForm_Load;
            ResumeLayout(false);
        }

        #endregion

        private System.ComponentModel.BackgroundWorker BackgroundWorkerDispatcher;
        private Label labelInfo;
        private System.Windows.Forms.Timer TimerOnlineStatus;
    }
}