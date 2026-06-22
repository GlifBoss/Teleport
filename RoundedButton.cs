// Copyright (c) 2026 Glif. Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

public class RoundedButton : Button
{
    private int radius = 7;
    private bool isHovered = false;

    private Color normalBackground = Color.FromArgb(225, 225, 225);
    private Color hoverBackground = Color.FromArgb(235, 235, 235);
    private Color thinBorderColor = Color.FromArgb(180, 180, 180);

    public RoundedButton()
    {
        this.FlatStyle = FlatStyle.Flat;
        this.UseVisualStyleBackColor = false;
        
        this.FlatAppearance.BorderSize = 0;
        this.FlatAppearance.BorderColor = Color.FromArgb(0, 255, 255, 255);
        this.FlatAppearance.MouseDownBackColor = Color.Transparent;
        this.FlatAppearance.MouseOverBackColor = Color.Transparent;
        
        this.BackColor = normalBackground;

        this.SetStyle(
            ControlStyles.UserPaint |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.SupportsTransparentBackColor,
            true);

        // УБИРАЕМ АРТЕФАКТЫ В УГЛАХ: Сообщаем Windows, что элемент сам полностью 
        // контролирует свои пиксели и ОС не должна пытаться дорисовывать или сдвигать его края.
        this.SetStyle(ControlStyles.Opaque, false);

        this.UpdateStyles();
    }

    protected override bool ShowFocusCues => false;

    public override void NotifyDefault(bool value)
    {
        base.NotifyDefault(false);
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        
        // Оставляем региону небольшой запас по ширине и высоте (+1 пиксель),
        // чтобы правый нижний угол гарантированно не обрезался видеокартой.
        Rectangle rect = new Rectangle(0, 0, Width + 1, Height + 1);
        using (GraphicsPath path = GetRoundedPath(rect, radius))
        {
            this.Region = new Region(path);
        }
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        base.OnMouseEnter(e);
        isHovered = true;
        Invalidate();
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        isHovered = false;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        // Рисуем на чистом прямоугольнике, строго вписываясь в размеры кнопки minus 1 пиксель
        Rectangle rect = new Rectangle(0, 0, Width - 1, Height - 1);
        
        using (GraphicsPath path = GetRoundedPath(rect, radius))
        {
            Color currentBgColor = isHovered ? hoverBackground : BackColor;

            // Заливка тела кнопки
            using (SolidBrush brush = new SolidBrush(currentBgColor))
            {
                e.Graphics.FillPath(brush, path);
            }

            // Рисуем рамку кнопки
            using (Pen pen = new Pen(thinBorderColor, 1f))
            {
                e.Graphics.DrawPath(pen, path);
            }
        }

        // Отрисовка текста
        Rectangle textRect = new Rectangle(0, -2, this.Width, this.Height);

        // Объявляем флаги форматирования ТОЛЬКО ОДИН РАЗ
        TextFormatFlags buttonTextFlags = TextFormatFlags.HorizontalCenter | 
                                          TextFormatFlags.VerticalCenter | 
                                          TextFormatFlags.EndEllipsis;

        // Выводим текст через нативный TextRenderer
        TextRenderer.DrawText(
            e.Graphics, 
            this.Text, 
            this.Font, 
            textRect, 
            this.ForeColor, 
            buttonTextFlags);
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