// Copyright (c) 2026 Glif. Licensed under the MIT License. See LICENSE file in the project root for full license information.
using System;
using System.Windows.Forms;

namespace Teleport
{
    public class ProgressManager
    {
        private readonly Panel _progressBg;
        private readonly Panel _progressFill;
        private readonly Label _lblPercent;

        public ProgressManager(Panel progressBg, Panel progressFill, Label lblPercent)
        {
            _progressBg = progressBg;
            _progressFill = progressFill;
            _lblPercent = lblPercent;
        }

        public void Update(double percent)
        {
            if (_lblPercent.InvokeRequired)
            {
            _lblPercent.Invoke(new Action(() => Update(percent)));
            return;
            }

            _lblPercent.Text = $"{percent:F0}%";
    
            if (_progressBg != null && _progressFill != null)
            {
            // Вместо Math.Clamp используем ручное ограничение
            double clampedPercent = Math.Max(0, Math.Min(percent, 100));
            int width = (int)((clampedPercent / 100) * _progressBg.Width);
            _progressFill.Width = width;
            }
        }
        public IProgress<double> AsProgress() => new Progress<double>(Update);
    }
}