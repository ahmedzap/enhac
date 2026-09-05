using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace EnHac.UI
{
    public static class ModernUI
    {
        // الألوان الأساسية
        public static Color PrimaryColor { get; set; } = Color.FromArgb(67, 97, 238);
        public static Color SecondaryColor { get; set; } = Color.FromArgb(76, 201, 240);
        public static Color BackgroundColor { get; set; } = Color.FromArgb(18, 18, 26);
        public static Color SurfaceColor { get; set; } = Color.FromArgb(30, 30, 42);
        public static Color CardColor { get; set; } = Color.FromArgb(40, 40, 55);
        public static Color TextColor { get; set; } = Color.FromArgb(230, 230, 240);
        public static Color TextSecondaryColor { get; set; } = Color.FromArgb(160, 160, 175);
        public static Color SuccessColor { get; set; } = Color.FromArgb(46, 204, 113);
        public static Color ErrorColor { get; set; } = Color.FromArgb(231, 76, 60);
        public static Color WarningColor { get; set; } = Color.FromArgb(241, 196, 15);
        public static Color InfoColor { get; set; } = Color.FromArgb(52, 152, 219);

        public static void StyleButton(Button button, Color backColor, bool isOutline = false)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = isOutline ? 2 : 0;
            button.FlatAppearance.BorderColor = backColor;
            button.BackColor = isOutline ? Color.Transparent : backColor;
            button.ForeColor = isOutline ? backColor : Color.White;
            button.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            button.Cursor = Cursors.Hand;
            button.TextAlign = ContentAlignment.MiddleCenter;
            button.UseVisualStyleBackColor = false;

            var originalBack = button.BackColor;
            var originalFore = button.ForeColor;

            button.MouseEnter += (s, e) =>
            {
                button.BackColor = isOutline ? Color.FromArgb(30, backColor) : ControlPaint.Light(backColor, 0.15f);
            };

            button.MouseLeave += (s, e) =>
            {
                button.BackColor = originalBack;
                button.ForeColor = originalFore;
            };
        }

        public static void StyleTextBox(TextBox textBox)
        {
            textBox.BorderStyle = BorderStyle.FixedSingle;
            textBox.BackColor = SurfaceColor;
            textBox.ForeColor = TextColor;
            textBox.Font = new Font("Segoe UI", 10);
        }

        public static void StyleComboBox(ComboBox comboBox)
        {
            comboBox.FlatStyle = FlatStyle.Flat;
            comboBox.BackColor = SurfaceColor;
            comboBox.ForeColor = TextColor;
            comboBox.Font = new Font("Segoe UI", 10);
            comboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        }

        public static void StyleLabel(Label label, bool isTitle = false, bool isSubtitle = false)
        {
            label.ForeColor = isSubtitle ? TextSecondaryColor : TextColor;
            label.Font = isTitle
                ? new Font("Segoe UI", 18, FontStyle.Bold)
                : isSubtitle
                    ? new Font("Segoe UI", 12, FontStyle.Regular)
                    : new Font("Segoe UI", 9);
        }

        public static void StyleProgressBar(ProgressBar progressBar)
        {
            progressBar.Style = ProgressBarStyle.Continuous;
            progressBar.BackColor = SurfaceColor;
            progressBar.ForeColor = PrimaryColor;
            progressBar.Height = 20;
        }

        public static void StyleTabControl(TabControl tabControl)
        {
            tabControl.DrawMode = TabDrawMode.OwnerDrawFixed;
            tabControl.SizeMode = TabSizeMode.Fixed;
            tabControl.ItemSize = new Size(130, 40);
            tabControl.BackColor = BackgroundColor;

            tabControl.DrawItem += (s, e) =>
            {
                var page = tabControl.TabPages[e.Index];
                var textColor = e.State == DrawItemState.Selected ? PrimaryColor : TextSecondaryColor;

                using var brush = new SolidBrush(textColor);
                using var sf = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };

                e.Graphics.FillRectangle(new SolidBrush(BackgroundColor), e.Bounds);
                e.Graphics.DrawString(page.Text, new Font("Segoe UI", 10, FontStyle.Bold), brush, e.Bounds, sf);

                if (e.State == DrawItemState.Selected)
                {
                    using var pen = new Pen(PrimaryColor, 2);
                    e.Graphics.DrawLine(pen, e.Bounds.Left + 10, e.Bounds.Bottom - 2, e.Bounds.Right - 10, e.Bounds.Bottom - 2);
                }
            };
        }

        public static void PaintGradient(object sender, PaintEventArgs e, Color color1, Color color2)
        {
            using var brush = new LinearGradientBrush(
                ((Control)sender).ClientRectangle, color1, color2, LinearGradientMode.Horizontal);
            e.Graphics.FillRectangle(brush, ((Control)sender).ClientRectangle);
        }

        public static void ApplyRoundedCorners(Control control, int radius)
        {
            using var path = new GraphicsPath();
            var rect = control.ClientRectangle;

            path.AddArc(rect.X, rect.Y, radius * 2, radius * 2, 180, 90);
            path.AddArc(rect.Right - radius * 2, rect.Y, radius * 2, radius * 2, 270, 90);
            path.AddArc(rect.Right - radius * 2, rect.Bottom - radius * 2, radius * 2, radius * 2, 0, 90);
            path.AddArc(rect.X, rect.Bottom - radius * 2, radius * 2, radius * 2, 90, 90);
            path.CloseFigure();

            control.Region = new Region(path);
        }

        public static int CalculatePasswordStrength(string password)
        {
            if (string.IsNullOrEmpty(password)) return 0;

            int score = 0;
            if (password.Length >= 8) score++;
            if (password.Length >= 12) score++;
            if (System.Text.RegularExpressions.Regex.IsMatch(password, @"[A-Z]")) score++;
            if (System.Text.RegularExpressions.Regex.IsMatch(password, @"[a-z]")) score++;
            if (System.Text.RegularExpressions.Regex.IsMatch(password, @"[0-9]")) score++;
            if (System.Text.RegularExpressions.Regex.IsMatch(password, @"[!@#$%^&*(),.?""{}|<>]")) score++;

            return score;
        }

        public static Color GetPasswordStrengthColor(int score)
        {
            return score switch
            {
                0 or 1 => ErrorColor,
                2 or 3 => WarningColor,
                4 => InfoColor,
                _ => SuccessColor
            };
        }

        public static string GetPasswordStrengthText(int score)
        {
            return score switch
            {
                0 => "غير آمنة",
                1 => "ضعيفة جداً",
                2 => "ضعيفة",
                3 => "متوسطة",
                4 => "جيدة",
                5 => "قوية",
                _ => "قوية جداً"
            };
        }
    }
}