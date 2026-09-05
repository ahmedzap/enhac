using System;
using System.Drawing;
using System.Windows.Forms;
using EnHac.Models;
using EnHac.Services;
using EnHac.UI;

namespace EnHac.Forms
{
    public partial class SettingsForm : Form
    {
        private ComboBox algorithmCombo, cipherModeCombo, paddingCombo;
        private NumericUpDown keySizeNum, saltSizeNum, iterationsNum, bufferSizeNum;
        private CheckBox compressCheck, verifyCheck, preserveCheck;
        private CheckBox deleteCheck, autoNameCheck, timestampCheck;
        private TextBox extensionText;
        private CheckBox loggingCheck, strengthCheck, clipboardCheck;
        private TextBox logPathText;
        private NumericUpDown cacheTimeoutNum;

        public SettingsForm()
        {
            InitializeForm();
            LoadSettings();
        }

        private void InitializeForm()
        {
            Text = "⚙️ الإعدادات";
            Size = new Size(650, 550);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = ModernUI.BackgroundColor;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            var tabs = new TabControl
            {
                Location = new Point(10, 10),
                Size = new Size(615, 430)
            };
            ModernUI.StyleTabControl(tabs);

            tabs.TabPages.Add(CreateEncryptionTab());
            tabs.TabPages.Add(CreateAppTab());

            var saveBtn = new Button
            {
                Text = "💾 حفظ",
                Location = new Point(200, 455),
                Size = new Size(120, 35)
            };
            ModernUI.StyleButton(saveBtn, ModernUI.PrimaryColor);
            saveBtn.Click += SaveSettings;

            var cancelBtn = new Button
            {
                Text = "إلغاء",
                Location = new Point(330, 455),
                Size = new Size(120, 35)
            };
            ModernUI.StyleButton(cancelBtn, ModernUI.CardColor, true);
            cancelBtn.Click += (s, e) => Close();

            var resetBtn = new Button
            {
                Text = "🔄 افتراضي",
                Location = new Point(460, 455),
                Size = new Size(100, 35)
            };
            ModernUI.StyleButton(resetBtn, ModernUI.WarningColor);
            resetBtn.Click += ResetSettings;

            Controls.AddRange(new Control[] { tabs, saveBtn, cancelBtn, resetBtn });
        }

        private TabPage CreateEncryptionTab()
        {
            var tab = new TabPage("🔐 التشفير") { BackColor = ModernUI.BackgroundColor };

            algorithmCombo = AddComboBox(tab, "الخوارزمية:", 0);
            cipherModeCombo = AddComboBox(tab, "وضع التشفير:", 1);
            paddingCombo = AddComboBox(tab, "الحشو:", 2);
            keySizeNum = AddNumeric(tab, "حجم المفتاح:", 3, 128, 256, 64);
            saltSizeNum = AddNumeric(tab, "حجم Salt:", 4, 128, 512, 64);
            iterationsNum = AddNumeric(tab, "التكرارات:", 5, 10000, 1000000, 10000);
            bufferSizeNum = AddNumeric(tab, "حجم المخزن:", 6, 1024, 65536, 4096);

            extensionText = new TextBox
            {
                Location = new Point(200, 280),
                Size = new Size(100, 25),
                Text = ".encrypted"
            };
            ModernUI.StyleTextBox(extensionText);
            tab.Controls.Add(CreateLabel("الامتداد:", 20, 280));
            tab.Controls.Add(extensionText);

            compressCheck = AddCheckBox(tab, "ضغط الملفات قبل التشفير", 320);
            verifyCheck = AddCheckBox(tab, "التحقق من السلامة", 345);
            preserveCheck = AddCheckBox(tab, "الحفاظ على هيكل المجلدات", 370);
            deleteCheck = AddCheckBox(tab, "⚠️ حذف الملفات الأصلية", 395);

            algorithmCombo.DataSource = Enum.GetValues(typeof(EncryptionAlgorithm));
            cipherModeCombo.DataSource = Enum.GetValues(typeof(CipherModeType));
            paddingCombo.DataSource = Enum.GetValues(typeof(PaddingModeType));

            return tab;
        }

        private TabPage CreateAppTab()
        {
            var tab = new TabPage("⚡ التطبيق") { BackColor = ModernUI.BackgroundColor };

            loggingCheck = AddCheckBox(tab, "تفعيل سجل العمليات", 20);
            strengthCheck = AddCheckBox(tab, "إظهار قوة كلمة المرور", 45);
            clipboardCheck = AddCheckBox(tab, "مسح الحافظة عند الخروج", 70);

            tab.Controls.Add(CreateLabel("مسار ملف السجل:", 20, 100));
            logPathText = new TextBox
            {
                Location = new Point(200, 100),
                Size = new Size(250, 25),
                Text = "encryption_log.txt"
            };
            ModernUI.StyleTextBox(logPathText);
            tab.Controls.Add(logPathText);

            tab.Controls.Add(CreateLabel("مهلة التخزين المؤقت (ثانية):", 20, 140));
            cacheTimeoutNum = new NumericUpDown
            {
                Location = new Point(250, 140),
                Size = new Size(80, 25),
                Minimum = 0,
                Maximum = 3600,
                Value = 300
            };
            tab.Controls.Add(cacheTimeoutNum);

            return tab;
        }

        private ComboBox AddComboBox(TabPage tab, string label, int row)
        {
            int y = 20 + row * 38;
            tab.Controls.Add(CreateLabel(label, 20, y));
            var combo = new ComboBox
            {
                Location = new Point(200, y),
                Size = new Size(200, 25)
            };
            ModernUI.StyleComboBox(combo);
            tab.Controls.Add(combo);
            return combo;
        }

        private NumericUpDown AddNumeric(TabPage tab, string label, int row, int min, int max, int inc)
        {
            int y = 20 + row * 38;
            tab.Controls.Add(CreateLabel(label, 20, y));
            var num = new NumericUpDown
            {
                Location = new Point(200, y),
                Size = new Size(100, 25),
                Minimum = min,
                Maximum = max,
                Increment = inc
            };
            tab.Controls.Add(num);
            return num;
        }

        private CheckBox AddCheckBox(TabPage tab, string text, int y)
        {
            var check = new CheckBox
            {
                Text = text,
                Location = new Point(20, y),
                Size = new Size(300, 20),
                ForeColor = ModernUI.TextColor,
                BackColor = Color.Transparent
            };
            tab.Controls.Add(check);
            return check;
        }

        private Label CreateLabel(string text, int x, int y)
        {
            return new Label
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(170, 25),
                TextAlign = ContentAlignment.MiddleRight,
                ForeColor = ModernUI.TextColor
            };
        }

        private void LoadSettings()
        {
            var es = SettingsService.Instance.EncryptionSettings;
            var app = SettingsService.Instance.AppSettings;

            algorithmCombo.SelectedItem = es.Algorithm;
            cipherModeCombo.SelectedItem = es.CipherMode;
            paddingCombo.SelectedItem = es.Padding;
            keySizeNum.Value = es.KeySize;
            saltSizeNum.Value = es.SaltSize;
            iterationsNum.Value = es.Iterations;
            bufferSizeNum.Value = es.BufferSize;
            extensionText.Text = es.DefaultEncryptionExtension;
            compressCheck.Checked = es.CompressBeforeEncrypt;
            verifyCheck.Checked = es.VerifyAfterEncryption;
            preserveCheck.Checked = es.PreserveFolderStructure;
            deleteCheck.Checked = es.DeleteOriginalFile;

            loggingCheck.Checked = app.EnableLogging;
            strengthCheck.Checked = app.ShowPasswordStrength;
            clipboardCheck.Checked = app.ClearClipboardOnExit;
            logPathText.Text = app.LogFilePath;
            cacheTimeoutNum.Value = app.PasswordCacheTimeout;
        }

        private void SaveSettings(object sender, EventArgs e)
        {
            try
            {
                var es = SettingsService.Instance.EncryptionSettings;
                es.Algorithm = (EncryptionAlgorithm)algorithmCombo.SelectedItem;
                es.CipherMode = (CipherModeType)cipherModeCombo.SelectedItem;
                es.Padding = (PaddingModeType)paddingCombo.SelectedItem;
                es.KeySize = (int)keySizeNum.Value;
                es.SaltSize = (int)saltSizeNum.Value;
                es.Iterations = (int)iterationsNum.Value;
                es.BufferSize = (int)bufferSizeNum.Value;
                es.DefaultEncryptionExtension = extensionText.Text;
                es.CompressBeforeEncrypt = compressCheck.Checked;
                es.VerifyAfterEncryption = verifyCheck.Checked;
                es.PreserveFolderStructure = preserveCheck.Checked;
                es.DeleteOriginalFile = deleteCheck.Checked;

                var app = SettingsService.Instance.AppSettings;
                app.EnableLogging = loggingCheck.Checked;
                app.ShowPasswordStrength = strengthCheck.Checked;
                app.ClearClipboardOnExit = clipboardCheck.Checked;
                app.LogFilePath = logPathText.Text;
                app.PasswordCacheTimeout = (int)cacheTimeoutNum.Value;

                SettingsService.Instance.Save();
                MessageBox.Show("تم حفظ الإعدادات بنجاح!", "نجاح", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في حفظ الإعدادات: {ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void InitializeComponent()
        {

        }

        private void ResetSettings(object sender, EventArgs e)
        {
            if (MessageBox.Show("إعادة تعيين جميع الإعدادات؟", "تأكيد", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                SettingsService.Instance.Reset();
                LoadSettings();
            }
        }
    }
}