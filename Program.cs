// Copyright (c) 2026 Glif. Licensed under the MIT License. See LICENSE file in the project root for full license information.
using System;
using System.Windows.Forms;

namespace Teleport
{
    internal static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            // 1. Говорим Windows не замыливать окно
            if (Environment.OSVersion.Version.Major >= 6)
            {
            SetProcessDPIAware();
            }

            // 2. Стандартные настройки
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1(args));
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();
    }
}