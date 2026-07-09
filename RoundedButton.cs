// Copyright (c) 2026 Glif. Licensed under the MIT License. See LICENSE file in the project root for full license information.
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Teleport
{
    public class RoundedButton : Control, IButtonControl
    {
        private int radius = 7;
        private bool isHovered = false;
        private DialogResult dialogResult;

        public RoundedButton()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.SupportsTransparentBackColor,
                true);
            UpdateStyles();
        }

        // --- ЗАГЛУШКИ ДЛЯ СОВМЕСТИМОСТИ (чтобы старый код форм не выдавал ошибки) ---
        public ContentAlignment TextAlign { get; set; } = ContentAlignment.MiddleCenter;
        public bool UseCompatibleTextRendering { get; set; } = false;
        public FlatStyle FlatStyle { get; set; } = FlatStyle.Flat;
        public bool UseVisualStyleBackColor { get; set; } = false;
        // --- РЕАЛИЗАЦИЯ ИНТЕРФЕЙСА IBUTTONCONTROL ---
        public DialogResult DialogResult
        {
        get => dialogResult;
        set => dialogResult = value;
        }

        public void NotifyDefault(bool value)
        {
        // Здесь ничего не рисуем, чтобы Windows не добавляла рамки активности
        }

        public void PerformClick()
        {
            if (Enabled)
            {
            OnClick(EventArgs.Empty);
            }
        }

        // --- ОБРАБОТКА МЫШИ ---
        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            isHovered = true;
            Cursor = Cursors.Hand;
            Invalidate(); 
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            isHovered = false;
            Cursor = Cursors.Default;
            Invalidate(); 
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            Focus();
            Invalidate();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            Invalidate();
        }

        // Обрабатываем нажатие пробела или Enter, когда кнопка в фокусе
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter)
            {
            PerformClick();
            e.Handled = true;
            }
        }

        // --- ОТРИСОВКА ---
        protected override void OnPaint(PaintEventArgs e)
        {
            if (this.Region != null)
            {
                this.Region = null;
            }

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            // --- ДИНАМИЧЕСКИЙ РАСЧЕТ ЦВЕТОВ НА ОСНОВЕ ТЕМЫ ---
            Color normalBackground;
            Color hoverBackground;
            Color thinBorderColor;

            if (ThemeManager.CurrentTheme == "Dark")
            {
                normalBackground = Color.FromArgb(50, 50, 50);
                hoverBackground = Color.FromArgb(40, 40, 40);
                thinBorderColor = Color.FromArgb(120, 120, 120);
            }
            else
            {
                normalBackground = Color.FromArgb(230, 230, 230);
                hoverBackground = Color.FromArgb(215, 215, 215);
                thinBorderColor = Color.FromArgb(180, 180, 180);
            }

            // Полностью заливаем фон за кнопкой цветом родительской формы
            Color parentColor = Parent?.BackColor ?? ThemeManager.BackColor;
            using (SolidBrush parentBrush = new SolidBrush(parentColor))
            {
            e.Graphics.FillRectangle(parentBrush, ClientRectangle);
            }

            // Рисуем скругленную кнопку
            using (GraphicsPath path = GetRoundedPath(new Rectangle(0, 0, Width - 1, Height - 1), radius))
            {
                Color currentBgColor = isHovered ? hoverBackground : normalBackground;

                using (SolidBrush brush = new SolidBrush(currentBgColor))
                {
                e.Graphics.FillPath(brush, path);
                }

                using (Pen pen = new Pen(thinBorderColor, 1f))
                {
                e.Graphics.DrawPath(pen, path);
                }
            }

            // --- КАЧЕСТВЕННОЕ ЦЕНТРИРОВАНИЕ ТЕКСТА ЧЕРЕЗ GDI (TextRenderer) ---
            TextFormatFlags flags = TextFormatFlags.HorizontalCenter | 
                                    TextFormatFlags.VerticalCenter | 
                                    TextFormatFlags.SingleLine;
                                    Rectangle textRect = ClientRectangle;
                                              textRect.Y -= 2;

            TextRenderer.DrawText(e.Graphics, Text, Font, textRect, ForeColor, flags);
        }
        private GraphicsPath GetRoundedPath(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int d = radius * 2;

            if (d > rect.Width) d = rect.Width;
            if (d > rect.Height) d = rect.Height;

            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}