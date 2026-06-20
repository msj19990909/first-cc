using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Media;
using System.Windows.Forms;

namespace PomodoroTimer
{
    public partial class MainForm : Form
    {
        private enum TimerState { Idle, Working, Break, Paused }
        private TimerState _state = TimerState.Idle;

        private int _workMinutes = 25;
        private int _breakMinutes = 5;
        private int _pomodoroCount = 0;
        private int _timeRemaining;

        private Label _lblTimer;
        private Label _lblStatus;
        private Button _btnStartPause;
        private Button _btnReset;
        private Button _btnSettings;
        private ListBox _taskList;
        private TextBox _txtTask;
        private Button _btnAddTask;
        private Button _btnRemoveTask;
        private NumericUpDown _numWork;
        private NumericUpDown _numBreak;
        private Panel _settingsPanel;
        private Label _lblPomodoroCount;
        private Timer _timer;
        private Panel _circlePanel;

        private int _progressAngle = 0;
        private Color _primaryColor = Color.FromArgb(214, 69, 69);
        private Color _breakColor = Color.FromArgb(46, 168, 184);
        private Image _backgroundImage;

        public MainForm()
        {
            LoadBackgroundImage();
            InitializeComponent();
            ResetTimer();
        }

        private void LoadBackgroundImage()
        {
            string exeDir = Path.GetDirectoryName(Application.ExecutablePath);
            string bgPath = Path.Combine(exeDir, "background.jpg");
            if (File.Exists(bgPath))
            {
                try
                {
                    _backgroundImage = Image.FromFile(bgPath);
                }
                catch { }
            }

            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.DoubleBuffer, true);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            if (_backgroundImage != null)
            {
                // 绘制背景图片，拉伸填满窗口
                e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                e.Graphics.DrawImage(_backgroundImage, 0, 0, this.Width, this.Height);

                // 半透明暗色遮罩，提高文字可读性
                using (var brush = new SolidBrush(Color.FromArgb(180, 12, 12, 16)))
                {
                    e.Graphics.FillRectangle(brush, 0, 0, this.Width, this.Height);
                }
            }
            else
            {
                // 无背景图时用纯色背景
                using (var brush = new SolidBrush(Color.FromArgb(26, 26, 30)))
                {
                    e.Graphics.FillRectangle(brush, 0, 0, this.Width, this.Height);
                }
            }
        }

        private void InitializeComponent()
        {
            this.Text = "🍅 番茄钟";
            this.Size = new Size(420, 640);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.BackColor = Color.FromArgb(26, 26, 30);
            this.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);

            _timer = new Timer();
            _timer.Interval = 1000;
            _timer.Tick += Timer_Tick;

            // 标题
            var lblTitle = new Label();
            lblTitle.Text = "🍅 番茄钟";
            lblTitle.Font = new Font("Segoe UI", 20F, FontStyle.Bold);
            lblTitle.ForeColor = Color.White;
            lblTitle.TextAlign = ContentAlignment.MiddleCenter;
            lblTitle.Size = new Size(400, 50);
            lblTitle.Location = new Point(10, 15);
            lblTitle.BackColor = Color.Transparent;

            // 圆形进度画板
            _circlePanel = new Panel();
            _circlePanel.Size = new Size(260, 260);
            _circlePanel.Location = new Point(80, 65);
            _circlePanel.BackColor = Color.Transparent;
            _circlePanel.Paint += CirclePanel_Paint;

            // 计时器文字
            _lblTimer = new Label();
            _lblTimer.Text = "25:00";
            _lblTimer.Font = new Font("Segoe UI", 52F, FontStyle.Bold);
            _lblTimer.ForeColor = Color.White;
            _lblTimer.TextAlign = ContentAlignment.MiddleCenter;
            _lblTimer.Size = new Size(260, 80);
            _lblTimer.Location = new Point(0, 75);
            _lblTimer.BackColor = Color.Transparent;
            _circlePanel.Controls.Add(_lblTimer);

            // 状态文字
            _lblStatus = new Label();
            _lblStatus.Text = "准备好了，开始专注吧 💪";
            _lblStatus.Font = new Font("Segoe UI", 11F, FontStyle.Regular);
            _lblStatus.ForeColor = Color.FromArgb(150, 150, 155);
            _lblStatus.TextAlign = ContentAlignment.MiddleCenter;
            _lblStatus.Size = new Size(260, 30);
            _lblStatus.Location = new Point(0, 155);
            _lblStatus.BackColor = Color.Transparent;
            _circlePanel.Controls.Add(_lblStatus);

            // 番茄计数
            _lblPomodoroCount = new Label();
            _lblPomodoroCount.Text = "今日已完成番茄：0 个 🍅";
            _lblPomodoroCount.Font = new Font("Segoe UI", 10F, FontStyle.Regular);
            _lblPomodoroCount.ForeColor = Color.FromArgb(180, 180, 185);
            _lblPomodoroCount.TextAlign = ContentAlignment.MiddleCenter;
            _lblPomodoroCount.Size = new Size(400, 25);
            _lblPomodoroCount.Location = new Point(10, 335);
            _lblPomodoroCount.BackColor = Color.Transparent;

            // 按钮：开始/暂停
            _btnStartPause = CreateButton("▶ 开始", 85, 370, 160, 40, Color.FromArgb(214, 69, 69));
            _btnStartPause.Click += BtnStartPause_Click;

            // 按钮：重置
            _btnReset = CreateButton("⟳ 重置", 255, 370, 80, 40, Color.FromArgb(85, 85, 88));
            _btnReset.Click += BtnReset_Click;

            // 按钮：设置
            _btnSettings = CreateButton("⚙", 350, 370, 40, 40, Color.FromArgb(65, 65, 68));
            _btnSettings.Font = new Font("Segoe UI", 14F, FontStyle.Regular);
            _btnSettings.Click += BtnSettings_Click;

            // 设置面板
            _settingsPanel = new Panel();
            _settingsPanel.Size = new Size(380, 80);
            _settingsPanel.Location = new Point(20, 420);
            _settingsPanel.BackColor = Color.FromArgb(36, 36, 40);
            _settingsPanel.Visible = false;

            var lblWork = new Label();
            lblWork.Text = "工作时长：";
            lblWork.ForeColor = Color.White;
            lblWork.Font = new Font("Segoe UI", 9F);
            lblWork.Location = new Point(15, 15);
            lblWork.Size = new Size(80, 25);

            _numWork = new NumericUpDown();
            _numWork.Location = new Point(95, 15);
            _numWork.Size = new Size(55, 25);
            _numWork.Minimum = 1;
            _numWork.Maximum = 120;
            _numWork.Value = 25;
            _numWork.BackColor = Color.FromArgb(50, 50, 54);
            _numWork.ForeColor = Color.FromArgb(220, 220, 220);
            _numWork.BorderStyle = BorderStyle.FixedSingle;

            var lblWorkMin = new Label();
            lblWorkMin.Text = "分钟";
            lblWorkMin.ForeColor = Color.FromArgb(130, 130, 135);
            lblWorkMin.Font = new Font("Segoe UI", 9F);
            lblWorkMin.Location = new Point(155, 17);
            lblWorkMin.Size = new Size(40, 20);

            var lblBreak = new Label();
            lblBreak.Text = "休息时长：";
            lblBreak.ForeColor = Color.FromArgb(200, 200, 205);
            lblBreak.Font = new Font("Segoe UI", 9F);
            lblBreak.Location = new Point(15, 45);
            lblBreak.Size = new Size(80, 25);

            _numBreak = new NumericUpDown();
            _numBreak.Location = new Point(95, 45);
            _numBreak.Size = new Size(55, 25);
            _numBreak.Minimum = 1;
            _numBreak.Maximum = 60;
            _numBreak.Value = 5;
            _numBreak.BackColor = Color.FromArgb(50, 50, 54);
            _numBreak.ForeColor = Color.FromArgb(220, 220, 220);
            _numBreak.BorderStyle = BorderStyle.FixedSingle;

            var lblBreakMin = new Label();
            lblBreakMin.Text = "分钟";
            lblBreakMin.ForeColor = Color.FromArgb(130, 130, 135);
            lblBreakMin.Font = new Font("Segoe UI", 9F);
            lblBreakMin.Location = new Point(155, 47);
            lblBreakMin.Size = new Size(40, 20);

            var btnSaveSettings = new Button();
            btnSaveSettings.Text = "保存";
            btnSaveSettings.FlatStyle = FlatStyle.Flat;
            btnSaveSettings.BackColor = Color.FromArgb(61, 166, 98);
            btnSaveSettings.ForeColor = Color.FromArgb(230, 230, 230);
            btnSaveSettings.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnSaveSettings.Location = new Point(215, 20);
            btnSaveSettings.Size = new Size(70, 40);

            var savePath = new System.Drawing.Drawing2D.GraphicsPath();
            savePath.AddArc(0, 0, 10, 10, 180, 90);
            savePath.AddArc(btnSaveSettings.Width - 10, 0, 10, 10, 270, 90);
            savePath.AddArc(btnSaveSettings.Width - 10, btnSaveSettings.Height - 10, 10, 10, 0, 90);
            savePath.AddArc(0, btnSaveSettings.Height - 10, 10, 10, 90, 90);
            savePath.CloseFigure();
            btnSaveSettings.Region = new Region(savePath);
            btnSaveSettings.Click += BtnSaveSettings_Click;

            _settingsPanel.Controls.Add(lblWork);
            _settingsPanel.Controls.Add(_numWork);
            _settingsPanel.Controls.Add(lblWorkMin);
            _settingsPanel.Controls.Add(lblBreak);
            _settingsPanel.Controls.Add(_numBreak);
            _settingsPanel.Controls.Add(lblBreakMin);
            _settingsPanel.Controls.Add(btnSaveSettings);

            // 任务列表标题
            var lblTaskTitle = new Label();
            lblTaskTitle.Text = "📋 任务列表";
            lblTaskTitle.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            lblTaskTitle.ForeColor = Color.White;
            lblTaskTitle.Location = new Point(25, 425);
            lblTaskTitle.Size = new Size(120, 25);

            // 任务输入框
            _txtTask = new TextBox();
            _txtTask.Location = new Point(25, 455);
            _txtTask.Size = new Size(270, 25);
            _txtTask.BackColor = Color.FromArgb(36, 36, 40);
            _txtTask.ForeColor = Color.FromArgb(220, 220, 220);
            _txtTask.BorderStyle = BorderStyle.FixedSingle;
            _txtTask.KeyDown += TxtTask_KeyDown;

            // 添加任务按钮
            _btnAddTask = CreateButton("添加", 300, 453, 70, 30, Color.FromArgb(61, 166, 98));
            _btnAddTask.Font = new Font("Segoe UI", 9F);
            _btnAddTask.Click += BtnAddTask_Click;

            // 任务列表
            _taskList = new ListBox();
            _taskList.Location = new Point(25, 490);
            _taskList.Size = new Size(345, 95);
            _taskList.BackColor = Color.FromArgb(36, 36, 40);
            _taskList.ForeColor = Color.FromArgb(220, 220, 220);
            _taskList.BorderStyle = BorderStyle.FixedSingle;
            _taskList.Font = new Font("Segoe UI", 10F);

            // 删除任务按钮
            _btnRemoveTask = CreateButton("删除任务", 300, 590, 80, 30, Color.FromArgb(180, 65, 65));
            _btnRemoveTask.Font = new Font("Segoe UI", 9F);
            _btnRemoveTask.Click += BtnRemoveTask_Click;

            // 底部提示
            var lblFooter = new Label();
            lblFooter.Text = "提示：一个番茄 = 25分钟专注 + 5分钟休息";
            lblFooter.ForeColor = Color.FromArgb(100, 100, 105);
            lblFooter.Font = new Font("Segoe UI", 8F);
            lblFooter.TextAlign = ContentAlignment.MiddleCenter;
            lblFooter.Size = new Size(400, 20);
            lblFooter.Location = new Point(10, 578);
            lblFooter.BackColor = Color.Transparent;

            this.Controls.Add(lblTitle);
            this.Controls.Add(_circlePanel);
            this.Controls.Add(_lblPomodoroCount);
            this.Controls.Add(_btnStartPause);
            this.Controls.Add(_btnReset);
            this.Controls.Add(_btnSettings);
            this.Controls.Add(_settingsPanel);
            this.Controls.Add(lblTaskTitle);
            this.Controls.Add(_txtTask);
            this.Controls.Add(_btnAddTask);
            this.Controls.Add(_taskList);
            this.Controls.Add(_btnRemoveTask);
            this.Controls.Add(lblFooter);
        }

        private Button CreateButton(string text, int x, int y, int w, int h, Color color)
        {
            var btn = new Button();
            btn.Text = text;
            btn.FlatStyle = FlatStyle.Flat;
            btn.BackColor = color;
            btn.ForeColor = Color.White;
            btn.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btn.Location = new Point(x, y);
            btn.Size = new Size(w, h);
            btn.Cursor = Cursors.Hand;
            btn.FlatAppearance.BorderSize = 0;

            var path = new GraphicsPath();
            path.AddArc(0, 0, 10, 10, 180, 90);
            path.AddArc(w - 10, 0, 10, 10, 270, 90);
            path.AddArc(w - 10, h - 10, 10, 10, 0, 90);
            path.AddArc(0, h - 10, 10, 10, 90, 90);
            path.CloseFigure();
            btn.Region = new Region(path);
            return btn;
        }

        private void CirclePanel_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            int size = 240;
            int x = (_circlePanel.Width - size) / 2;
            int y = (_circlePanel.Height - size) / 2;
            int penWidth = 8;
            int innerSize = size - penWidth * 2;

            using (var pen = new Pen(Color.FromArgb(45, 45, 50), penWidth))
            {
                e.Graphics.DrawArc(pen, x + penWidth, y + penWidth, innerSize, innerSize, 0, 360);
            }

            if (_progressAngle > 0)
            {
                Color progressColor = _state == TimerState.Break ? _breakColor : _primaryColor;
                using (var pen = new Pen(progressColor, penWidth))
                {
                    pen.StartCap = LineCap.Round;
                    pen.EndCap = LineCap.Round;
                    e.Graphics.DrawArc(pen, x + penWidth, y + penWidth, innerSize, innerSize, -90, _progressAngle);
                }
            }
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (_state == TimerState.Paused) return;

            _timeRemaining--;
            if (_timeRemaining <= 0)
            {
                TimerComplete();
                return;
            }
            UpdateDisplay();
        }

        private void TimerComplete()
        {
            _timer.Stop();

            if (_state == TimerState.Working)
            {
                _pomodoroCount++;
                _lblPomodoroCount.Text = string.Format(
                    "今日已完成番茄：{0} 个 🍅", _pomodoroCount);

                SystemSounds.Exclamation.Play();
                MessageBox.Show(
                    "🍅 番茄时间到！该休息一下啦 ☕",
                    "番茄钟",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                _state = TimerState.Break;
                _timeRemaining = _breakMinutes * 60;
                _lblStatus.Text = "☕ 休息时间，放松一下吧";
                _btnStartPause.Text = "▶ 开始休息";
                _primaryColor = _breakColor;
                UpdateDisplay();
            }
            else if (_state == TimerState.Break)
            {
                SystemSounds.Exclamation.Play();
                MessageBox.Show(
                    "☕ 休息结束！开始新的番茄吧 🍅",
                    "番茄钟",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                ResetTimer();
            }
        }

        private void BtnStartPause_Click(object sender, EventArgs e)
        {
            if (_state == TimerState.Idle)
            {
                _state = TimerState.Working;
                _timeRemaining = _workMinutes * 60;
                _lblStatus.Text = "🎯 专注工作中...";
                _primaryColor = Color.FromArgb(214, 69, 69);
                _timer.Start();
                _btnStartPause.Text = "⏸ 暂停";
                _btnStartPause.BackColor = Color.FromArgb(212, 135, 10);
            }
            else if (_state == TimerState.Break)
            {
                _timer.Start();
                _btnStartPause.Text = "⏸ 暂停";
                _btnStartPause.BackColor = Color.FromArgb(212, 135, 10);
            }
            else if (_state == TimerState.Working || _state == TimerState.Break)
            {
                _state = TimerState.Paused;
                _timer.Stop();
                _btnStartPause.Text = "▶ 继续";
                _btnStartPause.BackColor = Color.FromArgb(214, 69, 69);
            }
            else if (_state == TimerState.Paused)
            {
                _state = (_lblStatus.Text.Contains("休息")) ? TimerState.Break : TimerState.Working;
                _timer.Start();
                _btnStartPause.Text = "⏸ 暂停";
                _btnStartPause.BackColor = Color.FromArgb(212, 135, 10);
            }

            UpdateDisplay();
        }

        private void BtnReset_Click(object sender, EventArgs e)
        {
            _timer.Stop();
            ResetTimer();
        }

        private void ResetTimer()
        {
            _state = TimerState.Idle;
            _timeRemaining = _workMinutes * 60;
            _primaryColor = Color.FromArgb(214, 69, 69);
            _btnStartPause.Text = "▶ 开始";
            _btnStartPause.BackColor = Color.FromArgb(214, 69, 69);
            _lblStatus.Text = "准备好了，开始专注吧 💪";
            UpdateDisplay();
        }

        private void UpdateDisplay()
        {
            int mins = _timeRemaining / 60;
            int secs = _timeRemaining % 60;
            _lblTimer.Text = string.Format("{0:D2}:{1:D2}", mins, secs);

            int totalSeconds = 0;
            switch (_state)
            {
                case TimerState.Working:
                case TimerState.Break:
                    totalSeconds = (_state == TimerState.Working) ? _workMinutes * 60 : _breakMinutes * 60;
                    break;
                case TimerState.Paused:
                    totalSeconds = (_lblStatus.Text.Contains("休息")) ? _breakMinutes * 60 : _workMinutes * 60;
                    break;
                default:
                    _progressAngle = 0;
                    _circlePanel.Invalidate();
                    return;
            }

            if (totalSeconds > 0)
                _progressAngle = (int)((double)(totalSeconds - _timeRemaining) / totalSeconds * 360);
            else
                _progressAngle = 0;

            _circlePanel.Invalidate();

            this.Text = (_state == TimerState.Idle)
                ? "🍅 番茄钟"
                : string.Format("{0} - 🍅 番茄钟", _lblTimer.Text);
        }

        private void BtnSettings_Click(object sender, EventArgs e)
        {
            _settingsPanel.Visible = !_settingsPanel.Visible;
            _numWork.Value = _workMinutes;
            _numBreak.Value = _breakMinutes;
        }

        private void BtnSaveSettings_Click(object sender, EventArgs e)
        {
            _workMinutes = (int)_numWork.Value;
            _breakMinutes = (int)_numBreak.Value;

            if (_state == TimerState.Idle)
            {
                _timeRemaining = _workMinutes * 60;
                UpdateDisplay();
            }

            _settingsPanel.Visible = false;
        }

        private void BtnAddTask_Click(object sender, EventArgs e)
        {
            AddTask();
        }

        private void TxtTask_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                AddTask();
                e.SuppressKeyPress = true;
            }
        }

        private void AddTask()
        {
            string task = _txtTask.Text.Trim();
            if (string.IsNullOrEmpty(task)) return;

            _taskList.Items.Add(task);
            _txtTask.Clear();
            _txtTask.Focus();
        }

        private void BtnRemoveTask_Click(object sender, EventArgs e)
        {
            if (_taskList.SelectedIndex >= 0)
            {
                _taskList.Items.RemoveAt(_taskList.SelectedIndex);
            }
            else if (_taskList.Items.Count > 0)
            {
                _taskList.Items.RemoveAt(_taskList.Items.Count - 1);
            }
        }

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
