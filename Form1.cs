// Copyright (c) 2026 Glif. Licensed under the MIT License. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Teleport
{
    public partial class Form1 : Form
    {
        public const int WM_SETREDRAW = 11;

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("dwmapi.dll")]
        public static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);
        private TreeNode? previousNode;
        private readonly string[] files;
        private readonly TreeView treeView = new();
        private bool allowExpandCollapse = false;
        private readonly ImageList imageList = new();
        private readonly Panel topLine = new();
        private readonly string settingsFile = Path.Combine(Application.StartupPath, "settings.ini");
        private readonly Panel scrollBarCover = new Panel();
        private readonly Panel customThumb = new Panel();
        private Point dragStartPoint;
        private Panel progressBg = new Panel(); 
        private Panel progressFill = new Panel();
        private Label lblPercent = new Label();
        private ProgressManager _progressManager;
        private ConflictForm.ConflictResult? rememberedResult = null;
        protected override CreateParams CreateParams
        {
          get
          {
          // Получаем стандартные параметры создания окна
          CreateParams cp = base.CreateParams;
        
          // 0x00020000 - это CS_DROPSHADOW (отключает тень, которая часто дает "шлейф")
          // 0x00000002 - это CS_HREDRAW | CS_VREDRAW (принудительная перерисовка)
          cp.ClassStyle |= 0x20002; 
        
          return cp;
          }
        }

        public Form1(string[] args)
        {   
            this.Opacity = 0;
            files = args ?? Array.Empty<string>();
            Text = "Телепорт 1.5";
            Height = 680;
            this.StartPosition = FormStartPosition.Manual;
            this.Visible = false;
            this.FormBorderStyle = FormBorderStyle.Sizable;

            float fontSize = 8f;
            string themeFromIni = "Dark"; 

            if (File.Exists(settingsFile))
            {
                foreach (string line in File.ReadAllLines(settingsFile))
                {
                    string trimmedLine = line.Trim();
                    if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith(";") || trimmedLine.StartsWith("#")) continue;
                    int commentIndex = trimmedLine.IndexOfAny(new char[] { ';', '#' });
                    if (commentIndex != -1) trimmedLine = trimmedLine.Substring(0, commentIndex).Trim();

                    if (trimmedLine.StartsWith("FontSize=", StringComparison.OrdinalIgnoreCase))
                    {
                        if (float.TryParse(trimmedLine.Substring(9).Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out float parsedSize))
                        {
                            if (parsedSize > 4 && parsedSize < 72) fontSize = parsedSize;
                        }
                    }
                    else if (trimmedLine.StartsWith("Theme=", StringComparison.OrdinalIgnoreCase))
                    {
                        themeFromIni = trimmedLine.Substring(6).Trim();
                    }
                }
            }

            ThemeManager.Initialize(themeFromIni);
            ThemeManager.Apply(this);
            this.BackColor = ThemeManager.BackColor;
            
            Font = FontFamily.Families.Any(f => f.Name == "Segoe UI Variable Small") 
            ? new Font("Segoe UI Variable Small", fontSize) 
            : new Font("Segoe UI", fontSize);
            treeView.Font = this.Font;
            AdjustWidthByFontSize();
            
            Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            imageList.ImageSize = new Size(16, 16);
            imageList.ColorDepth = ColorDepth.Depth32Bit;
            LoadFolderIcon();

            topLine.Height = 1;
            topLine.Dock = DockStyle.Top;
            topLine.BackColor = ThemeManager.AccentColor;

            treeView.Dock = DockStyle.Fill;
            treeView.BorderStyle = BorderStyle.None;
            treeView.ShowPlusMinus = true;
            treeView.ShowLines = false;
            treeView.ShowRootLines = true;
            treeView.FullRowSelect = false;
            treeView.HideSelection = false;
            treeView.ImageList = imageList;
            EnableDoubleBuffering(treeView);

            scrollBarCover.Width = 22;
            scrollBarCover.Dock = DockStyle.Right;
            scrollBarCover.BackColor = ThemeManager.BackColor; 
            customThumb.Width = 10;
            customThumb.Height = 40;
            customThumb.BackColor = ThemeManager.CurrentTheme == "Dark" ? Color.FromArgb(80, 80, 80) : Color.FromArgb(180, 180, 180);
            customThumb.Cursor = Cursors.SizeNS;
            customThumb.Location = new Point(6, 5);
            customThumb.Paint += DrawRoundedPanel;
            customThumb.MouseEnter += (s, e) => { customThumb.Tag = "hover"; customThumb.Invalidate(); };
            customThumb.MouseLeave += (s, e) => { customThumb.Tag = ""; customThumb.Invalidate(); };
            customThumb.MouseDown += (s, e) => dragStartPoint = e.Location;
            customThumb.MouseMove += customThumb_MouseMove;
            scrollBarCover.Controls.Add(customThumb);

            Panel footerPanel = new Panel { Height = 31, Dock = DockStyle.Bottom, BackColor = ThemeManager.BackColor };
            this.Controls.Add(footerPanel);
            
            Panel bottomLine = new Panel { Height = 1, Dock = DockStyle.Top, BackColor = ThemeManager.AccentColor };
            footerPanel.Controls.Add(bottomLine);

            Panel footerContent = new Panel { Dock = DockStyle.Fill, Padding = new Padding(5, 0, 5, 0) };
            footerPanel.Controls.Add(footerContent);

            lblPercent.Text = "0%";
            lblPercent.Font = new Font("Segoe UI", 8f);
            lblPercent.AutoSize = true;
            lblPercent.Dock = DockStyle.Right;
            lblPercent.TextAlign = ContentAlignment.MiddleCenter;
            lblPercent.Padding = new Padding(0, 6, 0, 0);
            footerContent.Controls.Add(lblPercent);

            Panel progressWrapper = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10, 13, 50, 12) };
            footerContent.Controls.Add(progressWrapper);

            progressBg.Dock = DockStyle.Fill;
            progressBg.BackColor = ThemeManager.CurrentTheme == "Dark" ? Color.FromArgb(80, 80, 80) : Color.FromArgb(180, 180, 180);
            progressWrapper.Controls.Add(progressBg);

            progressFill.Dock = DockStyle.Left;
            progressFill.BackColor = Color.FromArgb(0, 127, 255);
            progressFill.Width = 0; 
            progressBg.Controls.Add(progressFill);
            
            _progressManager = new ProgressManager(progressBg, progressFill, lblPercent);
            
            bottomLine.BringToFront();
            
            Panel treeContainer = new Panel { Dock = DockStyle.Fill };
            treeContainer.Controls.Add(treeView);
            treeContainer.Controls.Add(scrollBarCover);
            
            this.Controls.Add(treeContainer);
            treeContainer.BringToFront();
            this.Controls.Add(topLine);
            
            treeView.HandleCreated += (s, e) => { treeView.BackColor = ThemeManager.BackColor; treeView.ForeColor = ThemeManager.TextColor; };
            LoadFolders();

            treeView.BeforeExpand += TreeView_BeforeExpand;
            treeView.AfterExpand += (s, e) => { TreeView_AfterExpand(s, e); UpdateScrollbar(); };
            treeView.AfterCollapse += (s, e) => { UpdateScrollbar(); };
            treeView.AfterSelect += TreeView_AfterSelect;
            treeView.NodeMouseDoubleClick += TreeView_NodeMouseDoubleClick;
            treeView.KeyDown += TreeView_KeyDown;
            treeView.MouseDown += TreeView_MouseDown;
            treeView.BeforeCollapse += TreeView_BeforeCollapse;
            treeView.Resize += (s, e) => UpdateScrollbar();
            treeView.MouseWheel += (s, e) => BeginInvoke(new Action(UpdateScrollbar));
            treeView.DrawMode = TreeViewDrawMode.OwnerDrawText;
            treeView.DrawNode += TreeView_DrawNode;
            treeView.Resize += (s, e) => { treeView.Region = new Region(new Rectangle(0, 0, treeView.Width - 25, treeView.Height)); };

            string lastPath = GetLastPath();
            this.BeginInvoke(new Action(() => {
            treeView.BeginUpdate();
            scrollBarCover.Visible = false;
            try { if (!string.IsNullOrEmpty(lastPath) && Directory.Exists(lastPath)) ExpandToPath(lastPath); else treeView.SelectedNode = null; }
            finally { UpdateScrollbar(); treeView.EndUpdate(); }
            }));
            Rectangle screen = Screen.PrimaryScreen.WorkingArea;
            this.Location = new Point(
            (screen.Width - this.Width) / 2,
            (screen.Height - this.Height) / 2
            );
        }

        public void UpdateScrollbar()
        {
            var allVisibleNodes = GetAllVisibleNodes(treeView.Nodes);
            int totalCount = allVisibleNodes.Count;
            int visibleCount = treeView.VisibleCount;

            if (totalCount <= visibleCount || visibleCount == 0)
            {
            scrollBarCover.Visible = false;
            return;
            }
            scrollBarCover.Visible = true;
            float ratio = (float)visibleCount / (float)totalCount;
            customThumb.Height = Math.Max(30, (int)(scrollBarCover.Height * ratio));
            int topIndex = treeView.TopNode != null ? allVisibleNodes.IndexOf(treeView.TopNode) : 0;
            int maxTop = scrollBarCover.Height - customThumb.Height;
            float posRatio = (float)topIndex / (float)Math.Max(1, totalCount - visibleCount);
            customThumb.Top = (int)(posRatio * maxTop);
            customThumb.Invalidate();
        }

        private void EnableDoubleBuffering(Control control)
        {
        typeof(Control).InvokeMember("DoubleBuffered", System.Reflection.BindingFlags.SetProperty | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic, null, control, new object[] { true });
        }

        private void DrawRoundedPanel(object sender, PaintEventArgs e)
        {
            Panel p = (Panel)sender;
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            Color baseColor = p.BackColor;
            Color finalColor = (p.Tag?.ToString() == "hover") 
            ? (ThemeManager.CurrentTheme == "Dark" ? ControlPaint.Light(baseColor, 0.5f) : ControlPaint.Dark(baseColor, 0.1f)) 
            : baseColor;

            using (System.Drawing.Drawing2D.GraphicsPath path = new System.Drawing.Drawing2D.GraphicsPath())
            {
                int radius = 5;
                path.AddArc(0, 0, radius * 2, radius * 2, 180, 90);
                path.AddArc(p.Width - radius * 2 - 1, 0, radius * 2, radius * 2, 270, 90);
                path.AddArc(p.Width - radius * 2 - 1, p.Height - radius * 2 - 1, radius * 2, radius * 2, 0, 90);
                path.AddArc(0, p.Height - radius * 2 - 1, radius * 2, radius * 2, 90, 90);
                path.CloseFigure();
                p.Region = new Region(path);
                using (SolidBrush brush = new SolidBrush(finalColor)) e.Graphics.FillPath(brush, path);
            }
        }

        private void AdjustWidthByFontSize()
        {
            // 1. Таблица фиксированных значений (Шрифт -> Ширина)
            var widthMap = new SortedDictionary<float, int>
            {
              { 8.0f, 340 },
              { 9.0f, 360 },
              { 10.0f, 380 },
              { 11.0f, 405 },
              { 12.0f, 420 },
              { 13.0f, 440 },
              { 14.0f, 465 },
              { 15.0f, 480 },
              { 16.0f, 500 }
            };

            float currentSize = treeView.Font.Size;
            int maxWidth = 600; // Максимальная ширина, если шрифт > 14
            int targetWidth;

            // 2. Ищем ближайшее большее или равное значение
            var foundKey = widthMap.Keys.FirstOrDefault(k => k >= currentSize);

            if (foundKey != 0)
            {
              // Нашли подходящий диапазон
              targetWidth = widthMap[foundKey];
            }
            else
            {
              // Если шрифт больше 14 (самого большого в словаре), 
              // берем сразу maxWidth
              targetWidth = maxWidth;
            }
            // 3. Применяем итоговую ширину
            this.Width = Math.Min(targetWidth, maxWidth);
            this.PerformLayout();
        }
        private List<TreeNode> GetAllVisibleNodes(TreeNodeCollection nodes)
        {
            var list = new List<TreeNode>();
            foreach (TreeNode node in nodes)
            {
            list.Add(node);
            if (node.IsExpanded) list.AddRange(GetAllVisibleNodes(node.Nodes));
            }
            return list;
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            _ = InitializeFormAsync();
        }
        private async Task InitializeFormAsync()
        {
            try
            {
            await Task.Delay(100);
            this.Opacity = 1;
            }
            catch (Exception ex)
            {
            // Логирование ошибки
            Debug.WriteLine("Ошибка инициализации: " + ex.Message);
            }
        }
    }
}