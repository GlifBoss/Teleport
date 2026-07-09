// Copyright (c) 2026 Glif. Licensed under the MIT License. See LICENSE file in the project root for full license information.
using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Teleport
{
    public partial class NewFolderForm : Form
    {
        public const int WM_SETREDRAW = 11;
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);        private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
        private enum DWM_WINDOW_CORNER_PREFERENCE
        {
            DWMWCP_DEFAULT = 0,
            DWMWCP_DONOTROUND = 1,
            DWMWCP_ROUND = 2,
            DWMWCP_ROUNDSMALL = 3
        }

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);
        public string FolderName { get; private set; } = "";
        private readonly TextBox txtName;
        private readonly RoundedButton btnOk;
        private readonly RoundedButton btnCancel;

        public NewFolderForm()
        {
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterParent;
            Width = 320;
            Height = 130;

            txtName = new TextBox()
            {
                Left = 22,
                Top = 25,
                Width = 276, 
                BorderStyle = BorderStyle.FixedSingle,
                Text = "Новая папка"
            };

            btnOk = new RoundedButton()
            {
                Text = "ОК",
                Width = 133,
                Height = 34,
                Left = 22,
                Top = 72,
                TextAlign = ContentAlignment.MiddleCenter,
                UseCompatibleTextRendering = false
            };

            btnOk.FlatStyle = FlatStyle.Flat;
            btnOk.UseVisualStyleBackColor = false;
            btnOk.Click += BtnOk_Click;

            btnCancel = new RoundedButton()
            {
                Text = "Отмена",
                Width = 133,
                Height = 34,
                Left = 165,
                Top = 72,
                TextAlign = ContentAlignment.MiddleCenter,
                UseCompatibleTextRendering = false
            };

            btnCancel.FlatStyle = FlatStyle.Flat;
            btnCancel.UseVisualStyleBackColor = false;
            btnCancel.Click += BtnCancel_Click;

            Controls.Add(txtName);
            Controls.Add(btnOk);
            Controls.Add(btnCancel);

            AcceptButton = btnOk;
            CancelButton = btnCancel;
            // 1. Сначала применяем глобальную тему ко всему окну
            ThemeManager.Apply(this);
            // 2. И сразу после этого принудительно перекрашиваем текст кнопки ОК в нужный зеленый
            btnOk.ForeColor = ThemeManager.CurrentTheme == "Dark" ? Color.FromArgb(255, 0, 220, 0) : Color.FromArgb(255, 0, 150, 0);
            // Наследование масштаба шрифта из главного окна
            this.Shown += NewFolderForm_Shown;
        }

        private void NewFolderForm_Shown(object? sender, EventArgs e)
        {
            if (Owner != null)
            {
                Font = Owner.Font;
                txtName.Font = Owner.Font;
                btnOk.Font = Owner.Font;
                btnCancel.Font = Owner.Font;
            }

            txtName.Focus();
            txtName.SelectAll();

            // Задаем системные параметры DWM (заголовок) на всякий случай
            try
            {
                int dark = ThemeManager.CurrentTheme == "Dark" ? 1 : 0;
                DwmSetWindowAttribute(Handle, 20, ref dark, sizeof(int));
                int borderColor = ThemeManager.CurrentTheme == "Dark" 
                ? unchecked((int)0x00505050)   
                : unchecked((int)0x00B4B4B4);
                DwmSetWindowAttribute(Handle, 34, ref borderColor, sizeof(int));
            }
            catch { }
            // Обновляем окно для отрисовки кастомной рамки
            this.Invalidate();
        }

        private void BtnOk_Click(object? sender, EventArgs e)
        {
            FolderName = txtName.Text.Trim();
            char[] invalidChars = System.IO.Path.GetInvalidFileNameChars();
            if (FolderName.IndexOfAny(invalidChars) >= 0)
            {
                MessageBox.Show("Имя папки содержит недопустимые символы (\\ / : * ? \" < > |)", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrEmpty(FolderName))
            {
                MessageBox.Show("Имя папки не может быть пустым", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DialogResult = DialogResult.OK;
        }

        private void BtnCancel_Click(object? sender, EventArgs e)
        {
        DialogResult = DialogResult.Cancel;
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            if (this.Handle != IntPtr.Zero)
            try
            {
                int corner = (int)DWM_WINDOW_CORNER_PREFERENCE.DWMWCP_ROUND;
                DwmSetWindowAttribute(Handle, DWMWA_WINDOW_CORNER_PREFERENCE, ref corner, sizeof(int));
                ThemeManager.Apply(this);    
            }
            catch { }
        }

        // Рисуем рамку вручную под размер окна, учитывая текущую тему
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            Color windowBorderColor = ThemeManager.CurrentTheme == "Dark" 
            ? Color.FromArgb(80, 80, 80)     
            : Color.FromArgb(180, 180, 180);  

            using (Pen pen = new Pen(windowBorderColor, 1f))
            {
            e.Graphics.DrawRectangle(pen, 0, 0, this.Width - 1, this.Height - 1);
            }
        }
    }
}