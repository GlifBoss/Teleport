// Copyright (c) 2026 Glif. Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Drawing;
using System.IO; 
using System.Runtime.InteropServices;
using System.Windows.Forms;

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

    public ConflictForm()
    {
        BackColor = Color.FromArgb(235, 235, 235);
        ForeColor = Color.White;
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterParent;
        Width = 300;
        Height = 220;       

        // --- БЛОК ЧТЕНИЯ ШРИФТА ИЗ INI ---
        float fontSize = 10f; // Дефолтное значение        
        // ТОЧНЫЙ ПУТЬ КАК В ГЛАВНОЙ ФОРМЕ
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

        Font customFont = new("Segoe UI Variable Small", fontSize);

        // --- ИНИЦИАЛИЗАЦИЯ ЭЛЕМЕНТОВ ---

        Label lbl = new()
        {
            Text = "Объекты совпадают!",
            Width = 280,
            Height = 30,
            Left = 10,
            Top = 15,
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = Color.Black,
            Font = customFont
        };

        RoundedButton btnAdd = new()
        {
            Text = "Добавить",
            Width = 150,
            Height = 34,
            Left = 75,
            Top = 67,
            ForeColor = Color.FromArgb(255, 0, 150, 0),
            Font = customFont,
            TextAlign = ContentAlignment.MiddleCenter
        };

        RoundedButton btnReplace = new()
        {
            Text = "Заменить",
            Width = 150,
            Height = 34,
            Left = 75,
            Top = 112,
            ForeColor = Color.FromArgb(255, 225, 0, 0),
            Font = customFont,
            TextAlign = ContentAlignment.MiddleCenter
        };

        RoundedButton btnCancel = new()
        {
            Text = "Отмена",
            Width = 150,
            Height = 34,
            Left = 75,
            Top = 157,
            ForeColor = Color.FromArgb(255, 0, 0, 0),
            Font = customFont,
            TextAlign = ContentAlignment.MiddleCenter
        };

        btnAdd.Click += (_, _) => { Result = ConflictResult.Add; DialogResult = DialogResult.OK; };
        btnReplace.Click += (_, _) => { Result = ConflictResult.Replace; DialogResult = DialogResult.OK; };
        btnCancel.Click += (_, _) => { Result = ConflictResult.Cancel; DialogResult = DialogResult.Cancel; };

        Controls.Add(lbl);
        Controls.Add(btnAdd);
        Controls.Add(btnReplace);
        Controls.Add(btnCancel);

        this.ActiveControl = lbl;
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        try
        {
            int corner = (int)DWM_WINDOW_CORNER_PREFERENCE.DWMWCP_ROUND;
            DwmSetWindowAttribute(Handle, DWMWA_WINDOW_CORNER_PREFERENCE, ref corner, sizeof(int));

            int borderColor = 0x00B4B4B4;
            DwmSetWindowAttribute(Handle, 34, ref borderColor, sizeof(uint));    
        }
        catch { }
    }  
}