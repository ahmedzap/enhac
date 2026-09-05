using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using EnHac.Models;
using EnHac.Services;
using EnHac.UI;

namespace EnHac.Forms
{
    public partial class MainForm : Form
    {
        private CryptoService _cryptoService;
        private FolderCryptoService _folderCryptoService;

        // Single File Controls
        private TextBox _txtSingleFile;
        private Button _btnSingleBrowse, _btnSingleEncrypt, _btnSingleDecrypt;
        private ProgressBar _pbSingle;
        private Label _lblSingleProgress, _lblSingleStatus;
        private CheckBox _chkDeleteOriginal;

        // Folder Controls
        private TextBox _txtFolderSource, _txtFolderOutput;
        private Button _btnFolderSource, _btnFolderOutput, _btnFolderEncrypt, _btnFolderDecrypt;
        private ProgressBar _pbFolder;
        private Label _lblFolderProgress, _lblFolderStatus;
        private ListBox _lstFolderLog;
        private CheckBox _chkDeleteOriginalFolder;

        // Log Controls
        private ListBox _lstLog;

        // Password Controls
        private TextBox _txtPassword, _txtConfirmPassword;
        private CheckBox _chkShowPassword;
        private Label _lblStrength;
        private ProgressBar _pbStrength;

        private bool _isProcessing;
        private TabControl _tabMain;

        public MainForm()
        {
            BuildUI();
            InitServices();
        }

        #region UI Construction

        private void BuildUI()
        {
            Text = "🔐 EnHac - File Encryptor";
            Size = new Size(920, 700);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(18, 18, 26);
            FormBorderStyle = FormBorderStyle.None;
            MinimumSize = new Size(800, 650);

            var titleBar = CreateTitleBar();
            Controls.Add(titleBar);

            _tabMain = new TabControl { Location = new Point(10, 50), Size = new Size(885, 510) };
            StyleTabControl(_tabMain);
            _tabMain.TabPages.Add(CreateSingleFileTab());
            _tabMain.TabPages.Add(CreateFolderTab());
            _tabMain.TabPages.Add(CreateLogTab());
            Controls.Add(_tabMain);

            var pnlPassword = CreatePasswordPanel();
            pnlPassword.Location = new Point(10, 570);
            pnlPassword.Size = new Size(885, 85);
            Controls.Add(pnlPassword);

            EnableDragging(titleBar);
        }

        private Panel CreateTitleBar()
        {
            var panel = new Panel
            {
                Size = new Size(Width, 40),
                Location = new Point(0, 0),
                BackColor = Color.FromArgb(67, 97, 238)
            };

            var lbl = new Label
            {
                Text = "🔐 EnHac Pro - File Encryptor",
                Location = new Point(15, 8),
                Size = new Size(300, 25),
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Transparent
            };
            panel.Controls.Add(lbl);

            var btnSettings = CreateTitleButton("⚙️", Width - 120);
            btnSettings.Click += (s, e) => new SettingsForm().ShowDialog();
            panel.Controls.Add(btnSettings);

            var btnMin = CreateTitleButton("─", Width - 80);
            btnMin.Click += (s, e) => WindowState = FormWindowState.Minimized;
            panel.Controls.Add(btnMin);

            var btnClose = CreateTitleButton("✕", Width - 40);
            btnClose.Click += (s, e) => Application.Exit();
            panel.Controls.Add(btnClose);

            return panel;
        }

        private Button CreateTitleButton(string text, int x)
        {
            var btn = new Button
            {
                Text = text,
                Location = new Point(x, 5),
                Size = new Size(30, 30),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                Font = new Font("Arial", 12),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        private TabPage CreateSingleFileTab()
        {
            var tab = new TabPage("📄 ملف واحد") { BackColor = Color.FromArgb(18, 18, 26) };
            int y = 20;

            AddLabel(tab, "اختر الملف:", 20, y, 100);
            _txtSingleFile = AddTextBox(tab, 20, y + 30, 650, true);
            _btnSingleBrowse = AddButton(tab, "📁 تصفح", 680, y + 30, 170, Color.FromArgb(76, 201, 240));
            _btnSingleBrowse.Click += (s, e) => BrowseFile();

            y += 40;
            var dragPanel = CreateDragDropPanel(20, y + 30, 830, 60);
            tab.Controls.Add(dragPanel);

            y += 55;
            _chkDeleteOriginal = new CheckBox
            {
                Text = "🗑️ حذف الملف الأصلي بعد التشفير (لا يمكن التراجع!)",
                Location = new Point(20, y + 30),
                Size = new Size(450, 25),
                ForeColor = Color.FromArgb(255, 150, 100),
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            tab.Controls.Add(_chkDeleteOriginal);

            y += 30;
            _pbSingle = AddProgressBar(tab, 20, y + 30, 700);
            _lblSingleProgress = AddLabel(tab, "0%", 730, y + 30, 120, ContentAlignment.MiddleRight);

            y += 30;
            _lblSingleStatus = AddLabel(tab, "جاهز", 20, y + 30, 830);

            y += 30;
            _btnSingleEncrypt = AddButton(tab, "🔒 تشفير الملف", 20, y + 30, 400, Color.FromArgb(46, 204, 113));
            _btnSingleEncrypt.Click += async (s, e) => await EncryptSingleFile();

            _btnSingleDecrypt = AddButton(tab, "🔓 فك تشفير الملف", 440, y + 30, 410, Color.FromArgb(52, 152, 219));
            _btnSingleDecrypt.Click += async (s, e) => await DecryptSingleFile();

            return tab;
        }

        private TabPage CreateFolderTab()
        {
            var tab = new TabPage("📁 مجلد") { BackColor = Color.FromArgb(18, 18, 26) };
            int y = 20;

            AddLabel(tab, "مجلد المصدر:", 20, y, 120);
            _txtFolderSource = AddTextBox(tab, 20, y + 25, 650, true);
            _btnFolderSource = AddButton(tab, "📁 تصفح", 680, y + 25, 170, Color.FromArgb(76, 201, 240));
            _btnFolderSource.Click += (s, e) => BrowseFolder(_txtFolderSource);

            y += 40;
            AddLabel(tab, "مجلد الإخراج:", 20, y + 25, 120);
            _txtFolderOutput = AddTextBox(tab, 20, y + 50, 650, true);
            _btnFolderOutput = AddButton(tab, "📁 تصفح", 680, y + 50, 170, Color.FromArgb(76, 201, 240));
            _btnFolderOutput.Click += (s, e) => BrowseFolder(_txtFolderOutput);

            y += 45;
            _chkDeleteOriginalFolder = new CheckBox
            {
                Text = "🗑️ حذف الملفات الأصلية بعد التشفير (لا يمكن التراجع!)",
                Location = new Point(20, y + 50),
                Size = new Size(500, 25),
                ForeColor = Color.FromArgb(255, 150, 100),
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            tab.Controls.Add(_chkDeleteOriginalFolder);

            y += 30;
            _pbFolder = AddProgressBar(tab, 20, y + 50, 700);
            _lblFolderProgress = AddLabel(tab, "0/0", 730, y + 50, 120, ContentAlignment.MiddleRight);

            y += 30;
            _lblFolderStatus = AddLabel(tab, "جاهز", 20, y + 50, 830);

            y += 30;
            _btnFolderEncrypt = AddButton(tab, "🔒 تشفير المجلد", 20, y + 50, 400, Color.FromArgb(46, 204, 113));
            _btnFolderEncrypt.Click += async (s, e) => await EncryptFolder();

            _btnFolderDecrypt = AddButton(tab, "🔓 فك تشفير المجلد", 440, y + 50, 410, Color.FromArgb(52, 152, 219));
            _btnFolderDecrypt.Click += async (s, e) => await DecryptFolder();

            y += 45;
            _lstFolderLog = new ListBox
            {
                Location = new Point(20, y + 50),
                Size = new Size(830, 110),
                BackColor = Color.FromArgb(30, 30, 42),
                ForeColor = Color.White,
                Font = new Font("Consolas", 9),
                BorderStyle = BorderStyle.FixedSingle,
                HorizontalScrollbar = true
            };
            tab.Controls.Add(_lstFolderLog);

            return tab;
        }

        private TabPage CreateLogTab()
        {
            var tab = new TabPage("📋 السجل") { BackColor = Color.FromArgb(18, 18, 26) };

            _lstLog = new ListBox
            {
                Location = new Point(20, 20),
                Size = new Size(830, 420),
                BackColor = Color.FromArgb(30, 30, 42),
                ForeColor = Color.White,
                Font = new Font("Consolas", 9),
                BorderStyle = BorderStyle.FixedSingle,
                HorizontalScrollbar = true
            };
            tab.Controls.Add(_lstLog);

            var btnClear = AddButton(tab, "🗑️ مسح السجل", 20, 450, 150, Color.FromArgb(231, 76, 60));
            btnClear.Click += (s, e) => _lstLog.Items.Clear();

            return tab;
        }

        private Panel CreatePasswordPanel()
        {
            var panel = new Panel { BackColor = Color.FromArgb(30, 30, 42), BorderStyle = BorderStyle.FixedSingle };

            var lblSection = new Label
            {
                Text = "🔑 كلمة المرور:",
                Location = new Point(15, 10),
                Size = new Size(150, 20),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.FromArgb(67, 97, 238)
            };
            panel.Controls.Add(lblSection);

            AddLabelToPanel(panel, "كلمة المرور:", 15, 35, 100);
            _txtPassword = new TextBox
            {
                Location = new Point(120, 33),
                Size = new Size(250, 28),
                PasswordChar = '●',
                MaxLength = 128,
                BackColor = Color.FromArgb(40, 40, 55),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10)
            };
            panel.Controls.Add(_txtPassword);

            AddLabelToPanel(panel, "تأكيد كلمة المرور:", 390, 35, 120);
            _txtConfirmPassword = new TextBox
            {
                Location = new Point(515, 33),
                Size = new Size(250, 28),
                PasswordChar = '●',
                MaxLength = 128,
                BackColor = Color.FromArgb(40, 40, 55),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10)
            };
            panel.Controls.Add(_txtConfirmPassword);

            _chkShowPassword = new CheckBox
            {
                Text = "👁️ إظهار",
                Location = new Point(775, 35),
                Size = new Size(90, 25),
                ForeColor = Color.FromArgb(200, 200, 210),
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 8)
            };
            _chkShowPassword.CheckedChanged += (s, e) =>
            {
                _txtPassword.PasswordChar = _chkShowPassword.Checked ? '\0' : '●';
                _txtConfirmPassword.PasswordChar = _chkShowPassword.Checked ? '\0' : '●';
            };
            panel.Controls.Add(_chkShowPassword);

            _lblStrength = new Label
            {
                Text = "قوة كلمة المرور: ضعيفة",
                Location = new Point(15, 65),
                Size = new Size(200, 15),
                Font = new Font("Segoe UI", 7),
                ForeColor = Color.FromArgb(231, 76, 60)
            };
            panel.Controls.Add(_lblStrength);

            _pbStrength = new ProgressBar
            {
                Location = new Point(15, 63),
                Size = new Size(850, 3),
                Maximum = 6
            };
            panel.Controls.Add(_pbStrength);

            _txtPassword.TextChanged += (s, e) =>
            {
                if (!SettingsService.Instance.AppSettings.ShowPasswordStrength) return;
                int strength = ModernUI.CalculatePasswordStrength(_txtPassword.Text);
                _pbStrength.Value = Math.Min(strength, _pbStrength.Maximum);
                _pbStrength.ForeColor = ModernUI.GetPasswordStrengthColor(strength);
                _lblStrength.Text = "قوة كلمة المرور: " + ModernUI.GetPasswordStrengthText(strength);
                _lblStrength.ForeColor = ModernUI.GetPasswordStrengthColor(strength);
            };

            return panel;
        }

        private Panel CreateDragDropPanel(int x, int y, int w, int h)
        {
            var panel = new Panel
            {
                Location = new Point(x, y),
                Size = new Size(w, h),
                BackColor = Color.FromArgb(40, 40, 55),
                BorderStyle = BorderStyle.FixedSingle,
                AllowDrop = true
            };

            var lbl = new Label
            {
                Text = "📁 اسحب وأفلت الملف هنا",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 11),
                ForeColor = Color.FromArgb(160, 160, 175)
            };
            panel.Controls.Add(lbl);

            panel.DragEnter += (s, e) =>
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    e.Effect = DragDropEffects.Copy;
                    panel.BackColor = Color.FromArgb(67, 97, 238);
                }
            };
            panel.DragLeave += (s, e) => panel.BackColor = Color.FromArgb(40, 40, 55);
            panel.DragDrop += (s, e) =>
            {
                panel.BackColor = Color.FromArgb(40, 40, 55);
                if (e.Data.GetData(DataFormats.FileDrop) is string[] files && files.Length > 0)
                {
                    _txtSingleFile.Text = files[0];
                }
            };

            return panel;
        }

        #endregion

        #region Helper Controls

        private Label AddLabel(TabPage tab, string text, int x, int y, int w, ContentAlignment align = ContentAlignment.MiddleLeft)
        {
            var lbl = new Label
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(w, 25),
                TextAlign = align,
                ForeColor = Color.FromArgb(200, 200, 210)
            };
            tab.Controls.Add(lbl);
            return lbl;
        }

        private void AddLabelToPanel(Panel panel, string text, int x, int y, int w)
        {
            var lbl = new Label
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(w, 20),
                ForeColor = Color.FromArgb(200, 200, 210),
                Font = new Font("Segoe UI", 9)
            };
            panel.Controls.Add(lbl);
        }

        private TextBox AddTextBox(TabPage tab, int x, int y, int w, bool readOnly = false)
        {
            var txt = new TextBox
            {
                Location = new Point(x, y),
                Size = new Size(w, 28),
                ReadOnly = readOnly,
                BackColor = Color.FromArgb(30, 30, 42),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10)
            };
            tab.Controls.Add(txt);
            return txt;
        }

        private Button AddButton(TabPage tab, string text, int x, int y, int w, Color color)
        {
            var btn = new Button
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(w, 30),
                BackColor = color,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            btn.FlatAppearance.BorderSize = 0;
            tab.Controls.Add(btn);
            return btn;
        }

        private ProgressBar AddProgressBar(TabPage tab, int x, int y, int w)
        {
            var pb = new ProgressBar
            {
                Location = new Point(x, y),
                Size = new Size(w, 25),
                Visible = false,
                Style = ProgressBarStyle.Continuous
            };
            tab.Controls.Add(pb);
            return pb;
        }

        #endregion

        #region TabControl Styling

        private void StyleTabControl(TabControl tabControl)
        {
            tabControl.DrawMode = TabDrawMode.OwnerDrawFixed;
            tabControl.SizeMode = TabSizeMode.Fixed;
            tabControl.ItemSize = new Size(130, 35);
            tabControl.BackColor = Color.FromArgb(18, 18, 26);

            tabControl.DrawItem += (s, e) =>
            {
                var page = tabControl.TabPages[e.Index];
                var textColor = e.State == DrawItemState.Selected
                    ? Color.FromArgb(67, 97, 238)
                    : Color.FromArgb(160, 160, 175);

                using var brush = new SolidBrush(textColor);
                using var sf = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };

                e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(18, 18, 26)), e.Bounds);
                e.Graphics.DrawString(page.Text, new Font("Segoe UI", 10, FontStyle.Bold), brush, e.Bounds, sf);

                if (e.State == DrawItemState.Selected)
                {
                    using var pen = new Pen(Color.FromArgb(67, 97, 238), 2);
                    e.Graphics.DrawLine(pen, e.Bounds.Left + 10, e.Bounds.Bottom - 2, e.Bounds.Right - 10, e.Bounds.Bottom - 2);
                }
            };
        }

        #endregion

        #region Services

        private void InitServices()
        {
            try
            {
                var settings = SettingsService.Instance.EncryptionSettings;
                _cryptoService = new CryptoService(settings);
                _folderCryptoService = new FolderCryptoService(settings);

                _folderCryptoService.FileProcessed += (s, e) =>
                {
                    this.InvokeIfRequired(() =>
                    {
                        string icon = e.Success ? "✅" : e.ErrorMessage?.Contains("⏭️") == true ? "⏭️" : "❌";
                        string msg = e.Success
                            ? $"{icon} [{e.ProcessedFiles}/{e.TotalFiles}] {Path.GetFileName(e.FilePath)}"
                            : $"{icon} {e.ErrorMessage}";
                        _lstFolderLog.Items.Insert(0, msg);
                        _lstFolderLog.TopIndex = 0;
                    });
                };

                _folderCryptoService.ProgressChanged += (s, p) =>
                {
                    this.InvokeIfRequired(() =>
                    {
                        _pbFolder.Value = Math.Min(p, 100);
                        _lblFolderProgress.Text = $"{_folderCryptoService.ProcessedFiles}/{_folderCryptoService.TotalFiles}";
                    });
                };
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تهيئة الخدمات: {ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion

        #region Browse

        private void BrowseFile()
        {
            using var dlg = new OpenFileDialog { Title = "اختر ملف", Filter = "جميع الملفات|*.*" };
            if (dlg.ShowDialog() == DialogResult.OK)
                _txtSingleFile.Text = dlg.FileName;
        }

        private void BrowseFolder(TextBox target)
        {
            using var dlg = new FolderBrowserDialog { Description = "اختر مجلد", ShowNewFolderButton = true };
            if (dlg.ShowDialog() == DialogResult.OK)
                target.Text = dlg.SelectedPath;
        }

        #endregion

        #region Single File Operations

        private async Task EncryptSingleFile()
        {
            if (!ValidateSingleFile()) return;
            await ProcessSingleFile(true);
        }

        private async Task DecryptSingleFile()
        {
            if (!ValidateSingleFile()) return;
            await ProcessSingleFile(false);
        }

        private async Task ProcessSingleFile(bool encrypt)
        {
            if (_isProcessing) return;

            string input = _txtSingleFile.Text;
            string output = GetOutputPath(input, encrypt);

            if (encrypt && _chkDeleteOriginal.Checked)
            {
                var result = MessageBox.Show(
                    "⚠️ تحذير!\n\nسيتم حذف الملف الأصلي بعد التشفير.\nلا يمكن التراجع عن هذا الإجراء!\n\nهل أنت متأكد؟",
                    "تأكيد حذف الملف الأصلي",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning
                );
                if (result != DialogResult.Yes) return;
            }

            SetUIState(false);
            _pbSingle.Visible = true;
            _lblSingleProgress.Visible = true;
            _pbSingle.Value = 0;
            _lblSingleProgress.Text = "0%";
            _lblSingleStatus.Text = encrypt ? "جارٍ التشفير..." : "جارٍ فك التشفير...";
            _lblSingleStatus.ForeColor = Color.FromArgb(52, 152, 219);

            var sw = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                var progress = new Progress<int>(p => this.InvokeIfRequired(() =>
                {
                    _pbSingle.Value = p;
                    _lblSingleProgress.Text = $"{p}%";
                }));

                await Task.Run(() =>
                {
                    if (encrypt)
                        _cryptoService.EncryptFile(input, output, _txtPassword.Text, progress);
                    else
                        _cryptoService.DecryptFile(input, output, _txtPassword.Text, progress);
                });

                sw.Stop();

                // حذف الملف الأصلي إذا كان الخيار مفعلاً
                if (encrypt && _chkDeleteOriginal.Checked && File.Exists(output))
                {
                    try
                    {
                        File.Delete(input);
                        AddLogEntry("حذف", Path.GetFileName(input), true, TimeSpan.Zero);
                    }
                    catch (Exception ex)
                    {
                        AddLogEntry("حذف", Path.GetFileName(input), false, TimeSpan.Zero, ex.Message);
                    }
                }

                _lblSingleStatus.Text = $"✅ نجاح! المدة: {sw.Elapsed.TotalSeconds:F2} ثانية";
                _lblSingleStatus.ForeColor = Color.FromArgb(46, 204, 113);
                AddLogEntry(encrypt ? "تشفير" : "فك تشفير", Path.GetFileName(input), true, sw.Elapsed);

                string msg = encrypt && _chkDeleteOriginal.Checked
                    ? $"تم التشفير وحذف الملف الأصلي بنجاح!\n\nالمخرج: {output}\nالمدة: {sw.Elapsed.TotalSeconds:F2} ثانية"
                    : $"تمت العملية بنجاح!\n\nالمخرج: {output}\nالمدة: {sw.Elapsed.TotalSeconds:F2} ثانية";

                MessageBox.Show(msg, "نجاح");
            }
            catch (Exception ex)
            {
                sw.Stop();
                _lblSingleStatus.Text = $"❌ خطأ: {ex.Message}";
                _lblSingleStatus.ForeColor = Color.FromArgb(231, 76, 60);
                AddLogEntry(encrypt ? "تشفير" : "فك تشفير", Path.GetFileName(input), false, sw.Elapsed, ex.Message);
                MessageBox.Show($"فشلت العملية:\n\n{ex.Message}", "خطأ");
            }
            finally
            {
                SetUIState(true);
                _pbSingle.Visible = false;
                _lblSingleProgress.Visible = false;
            }
        }

        private bool ValidateSingleFile()
        {
            if (string.IsNullOrEmpty(_txtSingleFile.Text))
            { ShowWarning("الرجاء اختيار ملف!"); return false; }
            if (!File.Exists(_txtSingleFile.Text))
            { ShowWarning("الملف غير موجود!"); return false; }
            return ValidatePassword();
        }

        private string GetOutputPath(string input, bool encrypt)
        {
            string dir = Path.GetDirectoryName(input) ?? "";
            string name = Path.GetFileNameWithoutExtension(input);
            string ext = Path.GetExtension(input);

            if (encrypt)
                return Path.Combine(dir, name + ext + ".encrypted");
            else
                return Path.Combine(dir, name + "_decrypted" + Path.GetExtension(name));
        }

        #endregion

        #region Folder Operations

        private async Task EncryptFolder()
        {
            if (!ValidateFolder()) return;
            await ProcessFolder(true);
        }

        private async Task DecryptFolder()
        {
            if (!ValidateFolder()) return;
            await ProcessFolder(false);
        }

        private async Task ProcessFolder(bool encrypt)
        {
            if (_isProcessing) return;

            if (encrypt && _chkDeleteOriginalFolder.Checked)
            {
                var result = MessageBox.Show(
                    "⚠️ تحذير شديد!\n\nسيتم حذف جميع الملفات الأصلية في المجلد بعد التشفير.\nلا يمكن التراجع عن هذا الإجراء!\n\nهل أنت متأكد تماماً؟",
                    "تأكيد حذف الملفات الأصلية",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning
                );
                if (result != DialogResult.Yes) return;
            }

            SetUIState(false);
            _pbFolder.Visible = true;
            _lblFolderProgress.Visible = true;
            _pbFolder.Value = 0;
            _lblFolderProgress.Text = "0/0";
            _lstFolderLog.Items.Clear();
            string op = encrypt ? "تشفير" : "فك تشفير";
            _lblFolderStatus.Text = $"جارٍ {op} المجلد...";
            _lblFolderStatus.ForeColor = Color.FromArgb(52, 152, 219);
            _lstFolderLog.Items.Add($"🚀 بدء {op} المجلد...");

            if (encrypt && _chkDeleteOriginalFolder.Checked)
            {
                _lstFolderLog.Items.Add("⚠️ سيتم حذف الملفات الأصلية بعد التشفير");
            }

            var sw = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                bool preserve = SettingsService.Instance.EncryptionSettings.PreserveFolderStructure;

                if (encrypt)
                {
                    await _folderCryptoService.EncryptFolderAsync(_txtFolderSource.Text, _txtFolderOutput.Text, _txtPassword.Text, preserve);

                    if (_chkDeleteOriginalFolder.Checked)
                    {
                        _lstFolderLog.Items.Insert(0, "🗑️ جارٍ حذف الملفات الأصلية...");
                        await DeleteOriginalFiles(_txtFolderSource.Text);
                        _lstFolderLog.Items.Insert(0, "✅ تم حذف الملفات الأصلية");
                    }
                }
                else
                {
                    await _folderCryptoService.DecryptFolderAsync(_txtFolderSource.Text, _txtFolderOutput.Text, _txtPassword.Text, preserve);
                }

                sw.Stop();
                int total = _folderCryptoService.TotalFiles;
                int processed = _folderCryptoService.ProcessedFiles;
                int failed = _folderCryptoService.FailedFiles.Count;
                int skipped = _folderCryptoService.SkippedFiles.Count;
                int success = processed - failed;

                _lstFolderLog.Items.Insert(0, $"✅ اكتمل {op} في {sw.Elapsed.TotalSeconds:F2} ثانية");
                _lstFolderLog.Items.Insert(0, $"📊 الكلي: {total} | ✅ الناجح: {success} | ❌ الفاشل: {failed} | ⏭️ المتخطى: {skipped}");

                if (skipped > 0)
                {
                    _lstFolderLog.Items.Insert(0, "⏭️ الملفات المتخطاة:");
                    foreach (var f in _folderCryptoService.SkippedFiles)
                        _lstFolderLog.Items.Insert(0, $"  ⏭️ {f}");
                }

                if (failed > 0)
                {
                    _lstFolderLog.Items.Insert(0, "❌ الملفات الفاشلة:");
                    foreach (var f in _folderCryptoService.FailedFiles)
                        _lstFolderLog.Items.Insert(0, $"  ❌ {f}");
                }

                _lblFolderStatus.Text = $"✅ اكتمل! {success}/{total}";
                _lblFolderStatus.ForeColor = failed > 0 ? Color.FromArgb(241, 196, 15) : Color.FromArgb(46, 204, 113);

                string msg = encrypt && _chkDeleteOriginalFolder.Checked
                    ? $"اكتمل {op} وحذف الملفات الأصلية!\n\nالكلي: {total}\nالناجح: {success}\nالفاشل: {failed}\nالمتخطى: {skipped}\nالمدة: {sw.Elapsed.TotalSeconds:F2} ثانية"
                    : $"اكتمل {op} المجلد!\n\nالكلي: {total}\nالناجح: {success}\nالفاشل: {failed}\nالمتخطى: {skipped}\nالمدة: {sw.Elapsed.TotalSeconds:F2} ثانية";

                MessageBox.Show(msg, "اكتمال");
            }
            catch (Exception ex)
            {
                sw.Stop();
                _lstFolderLog.Items.Insert(0, $"❌ خطأ: {ex.Message}");
                _lblFolderStatus.Text = $"❌ خطأ: {ex.Message}";
                _lblFolderStatus.ForeColor = Color.FromArgb(231, 76, 60);
                MessageBox.Show($"فشل {op} المجلد:\n\n{ex.Message}", "خطأ");
            }
            finally
            {
                SetUIState(true);
                _pbFolder.Visible = false;
                _lblFolderProgress.Visible = false;
            }
        }

        private async Task DeleteOriginalFiles(string folderPath)
        {
            await Task.Run(() =>
            {
                try
                {
                    var files = Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories);
                    int deletedCount = 0;
                    int failedCount = 0;

                    foreach (var file in files)
                    {
                        if (!file.EndsWith(".encrypted", StringComparison.OrdinalIgnoreCase))
                        {
                            try
                            {
                                File.Delete(file);
                                deletedCount++;
                            }
                            catch
                            {
                                failedCount++;
                            }
                        }
                    }

                    _lstFolderLog.Items.Insert(0, $"🗑️ تم حذف {deletedCount} ملف أصلي" + (failedCount > 0 ? $" (فشل حذف {failedCount})" : ""));

                    // حذف المجلدات الفارغة
                    try
                    {
                        var dirs = Directory.GetDirectories(folderPath, "*", SearchOption.AllDirectories);
                        foreach (var dir in dirs.Reverse())
                        {
                            if (!Directory.EnumerateFileSystemEntries(dir).Any())
                            {
                                Directory.Delete(dir);
                            }
                        }
                    }
                    catch { }
                }
                catch (Exception ex)
                {
                    _lstFolderLog.Items.Insert(0, $"❌ خطأ أثناء حذف الملفات الأصلية: {ex.Message}");
                }
            });
        }

        private bool ValidateFolder()
        {
            if (string.IsNullOrEmpty(_txtFolderSource.Text))
            { ShowWarning("الرجاء اختيار مجلد المصدر!"); return false; }
            if (!Directory.Exists(_txtFolderSource.Text))
            { ShowWarning("مجلد المصدر غير موجود!"); return false; }
            if (string.IsNullOrEmpty(_txtFolderOutput.Text))
            { ShowWarning("الرجاء اختيار مجلد الإخراج!"); return false; }
            return ValidatePassword();
        }

        #endregion

        #region Validation

        private bool ValidatePassword()
        {
            if (string.IsNullOrEmpty(_txtPassword.Text))
            { ShowWarning("الرجاء إدخال كلمة المرور!"); return false; }
            if (_txtPassword.Text != _txtConfirmPassword.Text)
            { ShowWarning("كلمتا المرور غير متطابقتين!"); return false; }
            if (_txtPassword.Text.Length < 8)
            { ShowWarning("كلمة المرور يجب أن تكون 8 أحرف على الأقل!"); return false; }
            return true;
        }

        private void ShowWarning(string msg)
        {
            MessageBox.Show(msg, "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        #endregion

        #region UI State

        private void SetUIState(bool enabled)
        {
            _isProcessing = !enabled;
            this.InvokeIfRequired(() =>
            {
                _btnSingleBrowse.Enabled = enabled;
                _btnSingleEncrypt.Enabled = enabled;
                _btnSingleDecrypt.Enabled = enabled;
                _btnFolderSource.Enabled = enabled;
                _btnFolderOutput.Enabled = enabled;
                _btnFolderEncrypt.Enabled = enabled;
                _btnFolderDecrypt.Enabled = enabled;
                _chkDeleteOriginal.Enabled = enabled;
                _chkDeleteOriginalFolder.Enabled = enabled;
                _tabMain.Enabled = enabled;
            });
        }

        #endregion

        #region Log

        private void AddLogEntry(string operation, string fileName, bool success, TimeSpan duration, string error = null)
        {
            string icon = success ? "✅" : "❌";
            string entry = $"[{DateTime.Now:HH:mm:ss}] {icon} {operation} - {fileName} - {duration.TotalSeconds:F2}s";
            if (!string.IsNullOrEmpty(error))
                entry += $" - {error}";

            this.InvokeIfRequired(() =>
            {
                _lstLog.Items.Insert(0, entry);
                if (_lstLog.Items.Count > 1000)
                    _lstLog.Items.RemoveAt(_lstLog.Items.Count - 1);
            });
        }

        #endregion

        #region Window Dragging

        private void EnableDragging(Panel titleBar)
        {
            bool dragging = false;
            Point start = Point.Empty;
            titleBar.MouseDown += (s, e) => { dragging = true; start = new Point(e.X, e.Y); };
            titleBar.MouseMove += (s, e) =>
            {
                if (dragging)
                {
                    var p = PointToScreen(e.Location);
                    Location = new Point(p.X - start.X, p.Y - start.Y);
                }
            };
            titleBar.MouseUp += (s, e) => dragging = false;
        }

        #endregion

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (_isProcessing)
            {
                if (MessageBox.Show("هناك عملية جارية. هل تريد الخروج؟", "تأكيد", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.No)
                {
                    e.Cancel = true;
                    return;
                }
            }
            base.OnFormClosing(e);
        }
    }

    public static class ControlExtensions
    {
        public static void InvokeIfRequired(this Control control, Action action)
        {
            if (control.InvokeRequired)
                control.Invoke(action);
            else
                action();
        }
    }
}