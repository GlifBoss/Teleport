// Copyright (c) 2026 Glif. Licensed under the MIT License. See LICENSE file in the project root for full license information.
using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Teleport
{
    public class ConflictForm : Form
    {
        private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;

        private enum DWM_WINDOW_CORNER_PREFERENCE
        {
            DWMWCP_DEFAULT = 0,
            DWMWCP_DONOTROUND = 1,
            DWMWCP_ROUND = 2,
            DWMWCP_ROUNDSMALL = 3
        }

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);
        
        public enum ConflictResult
        {
            Add,
            Replace,
            Cancel
        }

        public ConflictResult Result { get; private set; } = ConflictResult.Cancel;

        private readonly Label lbl;
        private readonly RoundedButton btnAdd;
        private readonly RoundedButton btnReplace;
        private readonly RoundedButton btnCancel;

        public ConflictForm(Font font)
        {
            this.Font = font;
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterParent;
            Width = 300;
            Height = 220;       
            // ИНИЦИАЛИЗАЦИЯ ЭЛЕМЕНТОВ
            lbl = new()
            {
                Text = "Объекты совпадают",
                Width = 280,
                Height = 30,
                Left = 10,
                Top = 15,
                TextAlign = ContentAlignment.MiddleCenter
            };

            btnAdd = new()
            {
                Text = "Добавить",
                Width = 150,
                Height = 34,
                Left = 75,
                Top = 67,
                TextAlign = ContentAlignment.MiddleCenter
            };
            
            btnReplace = new()
            {
                Text = "Заменить",
                Width = 150,
                Height = 34,
                Left = 75,
                Top = 112,
                TextAlign = ContentAlignment.MiddleCenter
            };
         
            btnCancel = new()
            {
                Text = "Отмена",
                Width = 150,
                Height = 34,
                Left = 75,
                Top = 157,
                TextAlign = ContentAlignment.MiddleCenter
            };

            btnAdd.Click += (_, _) => { Result = ConflictResult.Add; DialogResult = DialogResult.OK; };
            btnReplace.Click += (_, _) => { Result = ConflictResult.Replace; DialogResult = DialogResult.OK; };
            btnCancel.Click += (_, _) => { Result = ConflictResult.Cancel; DialogResult = DialogResult.Cancel; };
            
            Controls.Add(lbl);
            Controls.Add(btnAdd);
            Controls.Add(btnReplace);
            Controls.Add(btnCancel);
            ActiveControl = lbl;

            // 1. Сначала применяем общую тему (это красит форму, лейбл и кнопку "Отмена")
            ThemeManager.Apply(this);
            // 2. ПОСЛЕ этого принудительно задаем цветной текст кнопкам конфликтов
            btnAdd.ForeColor = ThemeManager.CurrentTheme == "Dark" ? Color.FromArgb(255, 0, 220, 0) : Color.FromArgb(255, 0, 150, 0);
            btnReplace.ForeColor = ThemeManager.CurrentTheme == "Dark" ? Color.FromArgb(255, 255, 50, 50) : Color.FromArgb(255, 200, 30, 30);
            // Слушатели событий формы
            this.Shown += ConflictForm_Shown;
        }
        private void ConflictForm_Shown(object? sender, EventArgs e)
        {
          if (Owner != null)
          {
            // 1. Сначала наследуем шрифт
            Font = Owner.Font;
            // 2. Принудительно применяем шрифт ко всем элементам
            lbl.Font = Owner.Font;
            btnAdd.Font = Owner.Font;
            btnReplace.Font = Owner.Font;
            btnCancel.Font = Owner.Font;
          }
            // 3. Теперь обновляем стили темы (цвета), 
            // чтобы они подстроились под новый размер шрифта/контролов
            ThemeManager.Apply(this);
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
          this.Invalidate();
        }
        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            if (this.Handle != IntPtr.Zero)
            try
            {
                // Закругление углов для Windows 11
                int corner = (int)DWM_WINDOW_CORNER_PREFERENCE.DWMWCP_ROUND;
                DwmSetWindowAttribute(Handle, DWMWA_WINDOW_CORNER_PREFERENCE, ref corner, sizeof(int));
                ThemeManager.Apply(this); 
            }
            catch { }
        }
        // Рисуем собственную рамку в 1 пиксель, так как FormBorderStyle = None убирает стандартную
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            Color windowBorderColor = ThemeManager.CurrentTheme == "Dark" 
            ? Color.FromArgb(80, 80, 80) 
            : Color.FromArgb(180, 180, 180); 

            using (Pen pen = new Pen(windowBorderColor, 1f))
            {
            // Рисуем рамку точно по внутреннему краю формы
            e.Graphics.DrawRectangle(pen, 0, 0, this.Width - 1, this.Height - 1);
            }
        }
    }
}