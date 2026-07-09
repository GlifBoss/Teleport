// Copyright (c) 2026 Glif. Licensed under the MIT License. See LICENSE file in the project root for full license information.
using System;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace Teleport
{
    public static class ThemeManager
    {
        // Выбор пользователя ("Auto", "Dark", "Light")
        public static string UserSelectedTheme { get; private set; } = "Auto";
        // Текущая активная тема (всегда "Dark" или "Light")
        public static string CurrentTheme { get; private set; } = "Dark";
        public static Color BackColor { get; private set; }
        public static Color TextColor { get; private set; }
        public static Color AccentColor { get; private set; }
        public static Color DwmBorderColor { get; private set; }
        public static void Initialize(string theme)
        {
            // 1. Сохраняем выбор пользователя в отдельное свойство
            UserSelectedTheme = theme.Trim();
            // 2. Определяем реальную тему
            string effectiveTheme = UserSelectedTheme;
            if (effectiveTheme.Equals("Auto", StringComparison.OrdinalIgnoreCase))
            {
                effectiveTheme = IsSystemInDarkMode() ? "Dark" : "Light";
            }
            // 3. Устанавливаем текущую тему
            CurrentTheme = effectiveTheme;
            // 4. Настраиваем цвета
            if (CurrentTheme.Equals("Light", StringComparison.OrdinalIgnoreCase))
            {
                BackColor = Color.FromArgb(235, 235, 235);
                TextColor = Color.FromArgb(30, 30, 30);
                AccentColor = Color.FromArgb(180, 180, 180);
                DwmBorderColor = Color.FromArgb(180, 180, 180);
            }
            else
            {
                CurrentTheme = "Dark"; // Гарантируем, что тут всегда "Dark"
                BackColor = Color.FromArgb(30, 30, 30);
                TextColor = Color.FromArgb(250, 250, 250);
                AccentColor = Color.FromArgb(80, 80, 80);
                DwmBorderColor = Color.FromArgb(80, 80, 80);
            }
        }

        private static bool IsSystemInDarkMode()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                {
                var value = key?.GetValue("AppsUseLightTheme");
                return value is int intValue && intValue == 0;
                }
            }
            catch { return false; }
        }

        public static void Apply(Form form)
        {
            form.BackColor = BackColor;
            form.ForeColor = TextColor;
            ApplyToControls(form.Controls);

            try
            {
            int dark = CurrentTheme.Equals("Dark", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
            DwmSetWindowAttribute(form.Handle, 20, ref dark, sizeof(int));
            int border = ColorTranslator.ToWin32(DwmBorderColor);
            DwmSetWindowAttribute(form.Handle, 34, ref border, sizeof(int));
            }
            catch { }
        }

        private static void ApplyToControls(Control.ControlCollection controls)
        {
            foreach (Control ctrl in controls)
            {
                if (ctrl is TreeView tv) { tv.BackColor = BackColor; tv.ForeColor = TextColor; }
                else if (ctrl is Panel p && p.Height == 1) { p.BackColor = AccentColor; }
                else if (ctrl is Button btn) { btn.BackColor = AccentColor; btn.ForeColor = TextColor; }
                else if (ctrl is Label lbl) { lbl.ForeColor = TextColor; }
                else if (ctrl is TextBox txt) 
                { 
                txt.BackColor = CurrentTheme == "Dark" ? Color.FromArgb(45, 45, 45) : Color.White; 
                txt.ForeColor = TextColor; 
                }

                if (ctrl.HasChildren) ApplyToControls(ctrl.Controls);
            }
        }

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);
    }
}