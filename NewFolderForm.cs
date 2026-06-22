// Copyright (c) 2026 Glif. Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Drawing;
using System.IO; // Добавлено для работы с File и Path
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Teleport
{
    public partial class NewFolderForm : Form
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
        private static extern int DwmSetWindowAttribute(
            IntPtr hwnd,
            int attr,
            ref int attrValue,
            int attrSize);

        public string FolderName { get; private set; } = "";

        private TextBox txtName;
        private RoundedButton btnOk;
        private RoundedButton btnCancel;

        public NewFolderForm()
        {
            // Настройки стиля окна 
            BackColor = Color.FromArgb(220, 220, 220);
            ForeColor = Color.White;
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterParent;
            
            Width = 320;
            Height = 130;

            // --- БЛОК ЧТЕНИЯ ШРИФТА ИЗ INI ---
            float fontSize = 10f; // Дефолтное значение
            string settingsFile = Path.Combine(Application.StartupPath, "settings.ini");

            if (File.Exists(settingsFile))
            {
                string[] lines = File.ReadAllLines(settingsFile);
                
                foreach (string line in lines)
                {
                    string trimmedLine = line.Trim();
                    
                    if (trimmedLine.StartsWith("FontSize=", StringComparison.OrdinalIgnoreCase))
                    {
                        string valueStr = trimmedLine.Substring(9).Trim();

                        if (float.TryParse(valueStr.Replace(',', '.'), 
                                           System.Globalization.NumberStyles.Any, 
                                           System.Globalization.CultureInfo.InvariantCulture, 
                                           out float parsedSize))
                        {
                            if (parsedSize > 4 && parsedSize < 72)
                            {
                                fontSize = parsedSize;
                            }
                        }
                        break; 
                    }
                }
            }

            // Создаем единый экземпляр шрифта
            Font customFont = new("Segoe UI Variable Small", fontSize);

            // Поле ввода текста
            txtName = new TextBox()
            {
                Font = customFont, // Применяем динамический шрифт
                Left = 22,
                Top = 25, 
                Width = 276, 
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.FromArgb(200, 200, 200), 
                ForeColor = Color.Black 
            };
            txtName.Text = "Новая папка";

            // Кнопка ОК
            btnOk = new RoundedButton()
            {
                Text = "OK",
                Width = 133,
                Height = 34,
                Left = 22,
                Top = 72, 
                ForeColor = Color.FromArgb(255, 0, 0, 0),
                Font = customFont, // Применяем динамический шрифт
                TextAlign = ContentAlignment.MiddleCenter,
                UseCompatibleTextRendering = true
            };
            btnOk.FlatStyle = FlatStyle.Flat;
            btnOk.UseVisualStyleBackColor = false;
            btnOk.Click += BtnOk_Click;

            // Кнопка Отмена
            btnCancel = new RoundedButton()
            {
                Text = "Отмена",
                Width = 133,
                Height = 34,
                Left = 165,
                Top = 72, 
                ForeColor = Color.FromArgb(255, 0, 0, 0),
                Font = customFont, // Применяем динамический шрифт
                TextAlign = ContentAlignment.MiddleCenter,
                UseCompatibleTextRendering = true
            };
            btnCancel.FlatStyle = FlatStyle.Flat;
            btnCancel.UseVisualStyleBackColor = false;
            btnCancel.Click += BtnCancel_Click;

            // Добавляем контролы на форму
            Controls.Add(txtName);
            Controls.Add(btnOk);
            Controls.Add(btnCancel);

            this.AcceptButton = btnOk;
            this.CancelButton = btnCancel;

            // Используем Shown вместо Load — это заставит Windows принудительно
            // активировать текстовое поле и выделить весь текст в момент появления окна!
            this.Shown += (s, e) => 
            {
                txtName.Focus();
                txtName.SelectAll();
            };
        }

        private void BtnOk_Click(object? sender, EventArgs e)
        {
            FolderName = txtName.Text.Trim();
            
            char[] invalidChars = System.IO.Path.GetInvalidFileNameChars();
            if (FolderName.IndexOfAny(invalidChars) >= 0)
            {
                MessageBox.Show("Имя папки содержит запрещенные символы (\\ / : * ? \" < > |)!", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrEmpty(FolderName))
            {
                MessageBox.Show("Имя папки не может быть пустым!", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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

            try
            {
                int corner = (int)DWM_WINDOW_CORNER_PREFERENCE.DWMWCP_ROUND;
                DwmSetWindowAttribute(
                    Handle,
                    DWMWA_WINDOW_CORNER_PREFERENCE,
                    ref corner,
                    sizeof(int));

                int borderColor = 0x00787878; 
                DwmSetWindowAttribute(
                    Handle,
                    34, 
                    ref borderColor,
                    sizeof(uint));    
            }
            catch
            {
            }
        }
    }
}