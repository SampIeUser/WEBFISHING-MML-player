namespace SV_WEBFISHING_GuitarMIDI
{
    partial class Form1
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
            button_play = new Button();
            richTextBox_notes1 = new RichTextBox();
            label1 = new Label();
            label2 = new Label();
            button_calibrate_guitar = new Button();
            button_test_calibration = new Button();
            richTextBox_notes2 = new RichTextBox();
            comboBox1 = new ComboBox();
            label3 = new Label();
            button1 = new Button();
            button_debug = new Button();
            comboBox_algorytm = new ComboBox();
            label4 = new Label();
            SuspendLayout();
            // 
            // button_play
            // 
            button_play.Location = new Point(115, 270);
            button_play.Name = "button_play";
            button_play.Size = new Size(302, 23);
            button_play.TabIndex = 0;
            button_play.Text = "Play";
            button_play.UseVisualStyleBackColor = true;
            button_play.Click += button_play_Click;
            // 
            // richTextBox_notes1
            // 
            richTextBox_notes1.Location = new Point(115, 36);
            richTextBox_notes1.Name = "richTextBox_notes1";
            richTextBox_notes1.Size = new Size(142, 218);
            richTextBox_notes1.TabIndex = 1;
            richTextBox_notes1.Text = "";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(159, 7);
            label1.Name = "label1";
            label1.Size = new Size(43, 15);
            label1.TabIndex = 2;
            label1.Text = "Track 1";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(322, 7);
            label2.Name = "label2";
            label2.Size = new Size(43, 15);
            label2.TabIndex = 3;
            label2.Text = "Track 2";
            // 
            // button_calibrate_guitar
            // 
            button_calibrate_guitar.Location = new Point(12, 36);
            button_calibrate_guitar.Name = "button_calibrate_guitar";
            button_calibrate_guitar.Size = new Size(75, 23);
            button_calibrate_guitar.TabIndex = 4;
            button_calibrate_guitar.Text = "Calibrate";
            button_calibrate_guitar.UseVisualStyleBackColor = true;
            button_calibrate_guitar.Click += button_calibrate_guitar_Click;
            // 
            // button_test_calibration
            // 
            button_test_calibration.Location = new Point(12, 65);
            button_test_calibration.Name = "button_test_calibration";
            button_test_calibration.Size = new Size(75, 23);
            button_test_calibration.TabIndex = 5;
            button_test_calibration.Text = "test";
            button_test_calibration.UseVisualStyleBackColor = true;
            button_test_calibration.Click += button_test_calibration_Click;
            // 
            // richTextBox_notes2
            // 
            richTextBox_notes2.Location = new Point(275, 36);
            richTextBox_notes2.Name = "richTextBox_notes2";
            richTextBox_notes2.Size = new Size(142, 218);
            richTextBox_notes2.TabIndex = 6;
            richTextBox_notes2.Text = "";
            // 
            // comboBox1
            // 
            comboBox1.FormattingEnabled = true;
            comboBox1.Items.AddRange(new object[] { "<нет>", "Bad apple", "Coffin Dance", "Crab Rave", "Megaman", "Night of nights", "Titan", "Red march", "Прогноз погоды", "El bimbo", "Sweden", "Mozarella", "futurama", "Und: Nyeh Nyeh Nyeh", "Und: snow", "Remove kebab", "Mario", "stalker campfire", "stalker dirge", "гимн России", "amogus", "Kalinka", "Bad piggies", "running into 90s", "polish cow", "Und: home", "Erika", "Minecraft revenge", "ЛюбЭ опера" });
            comboBox1.Location = new Point(3, 129);
            comboBox1.Name = "comboBox1";
            comboBox1.Size = new Size(97, 23);
            comboBox1.TabIndex = 7;
            comboBox1.SelectedIndexChanged += comboBox1_SelectedIndexChanged;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(12, 111);
            label3.Name = "label3";
            label3.Size = new Size(74, 15);
            label3.TabIndex = 8;
            label3.Text = "Pre recorded";
            // 
            // button1
            // 
            button1.Location = new Point(25, 270);
            button1.Name = "button1";
            button1.Size = new Size(75, 23);
            button1.TabIndex = 9;
            button1.Text = "Regex";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;
            // 
            // button_debug
            // 
            button_debug.Location = new Point(25, 231);
            button_debug.Name = "button_debug";
            button_debug.Size = new Size(75, 23);
            button_debug.TabIndex = 10;
            button_debug.Text = "DEBUG";
            button_debug.UseVisualStyleBackColor = true;
            button_debug.Click += button_debug_Click;
            // 
            // comboBox_algorytm
            // 
            comboBox_algorytm.FormattingEnabled = true;
            comboBox_algorytm.Items.AddRange(new object[] { "6 to 1", "1 to 6" });
            comboBox_algorytm.Location = new Point(3, 192);
            comboBox_algorytm.Name = "comboBox_algorytm";
            comboBox_algorytm.Size = new Size(63, 23);
            comboBox_algorytm.TabIndex = 11;
            comboBox_algorytm.SelectedIndexChanged += comboBox_algorytm_SelectedIndexChanged;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(4, 174);
            label4.Name = "label4";
            label4.Size = new Size(61, 15);
            label4.TabIndex = 12;
            label4.Text = "Algorithm";
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(435, 305);
            Controls.Add(label4);
            Controls.Add(comboBox_algorytm);
            Controls.Add(button_debug);
            Controls.Add(button1);
            Controls.Add(label3);
            Controls.Add(comboBox1);
            Controls.Add(richTextBox_notes2);
            Controls.Add(button_test_calibration);
            Controls.Add(button_calibrate_guitar);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(richTextBox_notes1);
            Controls.Add(button_play);
            FormBorderStyle = FormBorderStyle.FixedToolWindow;
            Name = "Form1";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "WEBFISHING_MML_Guitar_player";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button button_play;
        private RichTextBox richTextBox_notes1;
        private Label label1;
        private Label label2;
        private Button button_calibrate_guitar;
        private Button button_test_calibration;
        private RichTextBox richTextBox_notes2;
        private ComboBox comboBox1;
        private Label label3;
        private Button button1;
        private Button button_debug;
        private ComboBox comboBox_algorytm;
        private Label label4;
    }
}
