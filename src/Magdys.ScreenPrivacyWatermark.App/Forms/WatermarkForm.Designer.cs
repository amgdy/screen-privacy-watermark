namespace Magdys.ScreenPrivacyWatermark.App.Forms
{
    partial class WatermarkForm
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
            labelInfo = new Label();
            TimeProcessPolicyCheck = new System.Windows.Forms.Timer(components);
            SuspendLayout();
            // 
            // labelInfo
            // 
            labelInfo.Dock = DockStyle.Fill;
            labelInfo.Font = new Font("Segoe UI", 24F, FontStyle.Regular, GraphicsUnit.Point, 0);
            labelInfo.Location = new Point(0, 0);
            labelInfo.Name = "labelInfo";
            labelInfo.Size = new Size(800, 450);
            labelInfo.TabIndex = 1;
            labelInfo.Text = "Magdy's Screen Privacy Watermark";
            labelInfo.TextAlign = ContentAlignment.MiddleCenter;
            labelInfo.Visible = false;
            // 
            // TimeProcessPolicyCheck
            // 
            TimeProcessPolicyCheck.Tick += TimeProcessPolicyCheck_Tick;
            // 
            // WatermarkForm
            // 
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            BackColor = Color.White;
            ClientSize = new Size(800, 450);
            ControlBox = false;
            Controls.Add(labelInfo);
            FormBorderStyle = FormBorderStyle.None;
            MaximizeBox = false;
            MdiChildrenMinimizedAnchorBottom = false;
            MinimizeBox = false;
            Name = "WatermarkForm";
            ShowIcon = false;
            ShowInTaskbar = false;
            Text = "WatermarkForm";
            TopMost = true;
            TransparencyKey = Color.White;
            FormClosing += WatermarkForm_FormClosing;
            Load += WatermarkForm_Load;
            Paint += WatermarkForm_Paint;
            ResumeLayout(false);
        }

        #endregion

        private Label labelInfo;
        private System.Windows.Forms.Timer TimeProcessPolicyCheck;
    }
}