// Copyright (c) 2026 Glif. Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace Teleport
{
    public class Form1 : Form
    {
        // Импорт функции WinAPI для чтения INI-файла
        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filePath);
        private TreeNode? previousNode;
        private readonly string[] files;
        private readonly TreeView treeView = new();
        private bool allowExpandCollapse = false;
        private readonly ImageList imageList = new();
        private readonly Panel topLine = new();
        private readonly string settingsFile =
            Path.Combine(
                Application.StartupPath,
                "settings.ini");

        public Form1(string[] args)
        {         
            files = args ?? Array.Empty<string>();

            Text = "Телепорт 1.4";
            Width = 380;
            Height = 680;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(30, 30, 30);

            // --- БЛОК ЧТЕНИЯ ШРИФТА ИЗ INI ---
            float fontSize = 10f; // Дефолтное значение

if (File.Exists(settingsFile))
{
    // Читаем весь файл построчно, .NET сам разберётся с любой кодировкой UTF-8
    string[] lines = File.ReadAllLines(settingsFile);
    
    foreach (string line in lines)
    {
        string trimmedLine = line.Trim();
        
        // Ищем строку, которая начинается с FontSize=
        if (trimmedLine.StartsWith("FontSize=", StringComparison.OrdinalIgnoreCase))
        {
            // Забираем всё, что идёт после знака "="
            string valueStr = trimmedLine.Substring(9).Trim();

            // Парсим в float (с заменой запятой на точку на всякий случай)
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
            break; // Нашли нужную строку, выходим из цикла
        }
    }
}
           // Применяем считанный или дефолтный шрифт
            Font = new Font("Segoe UI Variable Small", fontSize);

            Icon = Icon.ExtractAssociatedIcon(
                Application.ExecutablePath);

            imageList.ImageSize = new Size(16, 16);
            imageList.ColorDepth = ColorDepth.Depth32Bit;

            LoadFolderIcon();

            topLine.Height = 1;
            topLine.BackColor = Color.FromArgb(200, 200, 200);
            topLine.Anchor =
            AnchorStyles.Top |
            AnchorStyles.Left |
            AnchorStyles.Right;
            treeView.Dock = DockStyle.Fill;
            treeView.ShowPlusMinus = true;
            treeView.ShowLines = false;
            treeView.ShowRootLines = true;
            treeView.FullRowSelect = false;
            treeView.BackColor = Color.FromArgb(235, 235, 235);
            treeView.ForeColor = Color.Black;
            treeView.BorderStyle = BorderStyle.None;
            treeView.HideSelection = false;
            treeView.DrawMode = TreeViewDrawMode.OwnerDrawText;
            treeView.DrawNode += TreeView_DrawNode;
            treeView.ImageList = imageList;

            Controls.Add(treeView);
            Controls.Add(topLine);

LoadFolders();

PositionTeraCopyLines();

topLine.BringToFront();

    treeView.BeforeExpand += TreeView_BeforeExpand;
    treeView.AfterExpand += TreeView_AfterExpand;
    treeView.AfterSelect += TreeView_AfterSelect;
    treeView.NodeMouseDoubleClick += TreeView_NodeMouseDoubleClick;
    treeView.KeyDown += TreeView_KeyDown;
    treeView.MouseDown += TreeView_MouseDown;
    treeView.BeforeCollapse += TreeView_BeforeCollapse;
    treeView.DrawMode = TreeViewDrawMode.OwnerDrawText;
    treeView.DrawNode += TreeView_DrawNode;

    string lastPath = GetLastPath();

    ExpandToPath(lastPath);

    }
private void PositionTeraCopyLines()
{
topLine.SetBounds(
    0,
    0,
    ClientSize.Width,
    1);
}        
    private string ShortName(string text)
    {
        const int maxLen = 22;

        if (text.Length <= maxLen)
            return text;

        return text.Substring(0, maxLen - 3) + "...";
    }

    private string GetLastPath()
    {
        try
        {
            if (!File.Exists(settingsFile))
                return "";

            foreach (string line in File.ReadAllLines(settingsFile))
            {
                if (line.StartsWith("LastPath="))
                    return line.Substring(9);
            }
        }
        catch
        {
        }

        return "";
    }
    
    private void ExpandToPath(string fullPath)
    {
        if (string.IsNullOrEmpty(fullPath))
            return;

        foreach (TreeNode rootNode in treeView.Nodes)
        {
            string nodePath = rootNode.Tag?.ToString() ?? "";
            if (fullPath.StartsWith(nodePath,
                StringComparison.OrdinalIgnoreCase))
            {
                ExpandNodeRecursive(rootNode, fullPath);
                break;
            }
        }
    }    

    private void ExpandNodeRecursive(
        TreeNode node,
        string targetPath)
    {
        string currentPath =
            node.Tag?.ToString() ?? "";
        if (string.Equals(
        currentPath,
        targetPath,
        StringComparison.OrdinalIgnoreCase))
    {
        treeView.SelectedNode = node;
        node.BackColor = Color.FromArgb(70, 70, 70);
        node.ForeColor = Color.White;
        node.EnsureVisible();
        return;
    }

if (!targetPath.StartsWith(
    currentPath,
    StringComparison.OrdinalIgnoreCase))
    return;

allowExpandCollapse = true;
node.Expand();

if (node.Nodes.Count > 0)
{
            TreeNode firstNode = node.Nodes[0];

            if (firstNode.Text == "loading")
            {
                TreeView_BeforeExpand(
                    treeView,
                    new TreeViewCancelEventArgs(
                    node,
                    false,
                    TreeViewAction.Expand));
            }
        }

        foreach (TreeNode child in node.Nodes)
        {
            string childPath =
                child.Tag?.ToString() ?? "";

            if (targetPath.StartsWith(
                childPath,
                StringComparison.OrdinalIgnoreCase))
            {
                ExpandNodeRecursive(
                    child,
                    targetPath);
                return;
            }
        }
    }

private void LoadFolders()
{
    treeView.Nodes.Clear();
    // 1. Пункт TeraCopy
    TreeNode teraNode = new TreeNode("TeraCopy");
    teraNode.Tag = "__TERACOPY__";
    teraNode.ImageIndex = 1;
    teraNode.SelectedImageIndex = 1;
    treeView.Nodes.Add(teraNode);

    // 2. Пункт: В новую папку
    TreeNode newFolderNode = new TreeNode("В новую папку");
    newFolderNode.Tag = "__NEW_FOLDER__";
    newFolderNode.ImageIndex = 2;
    newFolderNode.SelectedImageIndex = 2;
    treeView.Nodes.Add(newFolderNode);

    // 3. Диски
    foreach (DriveInfo drive in DriveInfo.GetDrives())
    {
        try
        {
            if (!drive.IsReady)
                continue;

            TreeNode node = new TreeNode(drive.Name);
            node.Tag = drive.RootDirectory.FullName;
            node.Nodes.Add("loading");
            treeView.Nodes.Add(node);
        }
        catch
        {
        }
    }
}    private void TreeView_BeforeExpand(
        object? sender,
        TreeViewCancelEventArgs e)
    {
        if (!allowExpandCollapse)
        {
            e.Cancel = true;
            return;
        }
        if (e.Node == null)
            return;
        if (e.Node.Nodes.Count != 1)
            return;
        if (e.Node.Nodes[0].Text != "loading")
            return;
        e.Node.Nodes.Clear();
        string path =
            e.Node.Tag?.ToString() ?? "";
        try
        {
            foreach (string dir in Directory.GetDirectories(path))
            {
                FileAttributes attr =
                    File.GetAttributes(dir);
                if ((attr & FileAttributes.Hidden) != 0)
                    continue;
                if ((attr & FileAttributes.System) != 0)
                    continue;
                TreeNode child =
                    new TreeNode(
                        ShortName(
                            Path.GetFileName(dir)));
                child.Tag = dir;
                child.Nodes.Add("loading");
                e.Node.Nodes.Add(child);
            }
        }
        catch
        {
        }
    }
    private void TreeView_BeforeCollapse(
    object? sender,
    TreeViewCancelEventArgs e)
{
    if (!allowExpandCollapse)
        e.Cancel = true;
}
    private void TreeView_MouseDown(
    object? sender,
    MouseEventArgs e)
{
    TreeViewHitTestInfo hit =
        treeView.HitTest(e.Location);
    allowExpandCollapse =
        hit.Location ==
        TreeViewHitTestLocations.PlusMinus;
}
private void TreeView_AfterExpand(
    object? sender,
    TreeViewEventArgs e)
{
    TreeNode? expandedNode = e.Node;
    if (expandedNode == null)
        return;
    TreeNodeCollection nodes;
    if (expandedNode.Parent == null)
        nodes = treeView.Nodes;
    else
        nodes = expandedNode.Parent.Nodes;
    foreach (TreeNode node in nodes)
    {
        if (node != expandedNode)
        {
            allowExpandCollapse = true;
            node.Collapse();
            allowExpandCollapse = false;
        }
    }
}
private void TreeView_NodeMouseDoubleClick(
        object? sender,
        TreeNodeMouseClickEventArgs e)
    {
    if (treeView.SelectedNode == null)
        return;
    string dest =
        treeView.SelectedNode.Tag?.ToString() ?? "";
    if (dest == "__TERACOPY__")
    {
        string listPath =
            Path.Combine(
                Path.GetTempPath(),
                "tc_list.txt");
        File.WriteAllLines(
            listPath,
            files,
            Encoding.GetEncoding(1251));            
            System.Diagnostics.Process.Start(
            @"C:\Program Files\TeraCopy\TeraCopy.exe",
            "AddList *\"" + listPath + "\"");
        Close();
        return;
    }
    // --- ВСТАВКА ДЛЯ НОВОЙ ПАПКИ ---
    if (dest == "__NEW_FOLDER__")
    {
        if (files == null || files.Length == 0) return;
        // Находим папку, в которой лежат исходные файлы
        string firstFilePath = files[0];
        string currentDirectory = Path.GetDirectoryName(firstFilePath) ?? "";
        // Открываем форму ввода имени папки
        using (NewFolderForm inputForm = new NewFolderForm())
        {
            if (inputForm.ShowDialog(this) == DialogResult.OK)
            {
                string newFolderName = inputForm.FolderName;
                // Собираем полный путь к новой папке
                string targetFolderPath = Path.Combine(currentDirectory, newFolderName);
                try
                {
                    // Создаем папку, если её нет
                    if (!Directory.Exists(targetFolderPath))
                    {
                        Directory.CreateDirectory(targetFolderPath);
                    }

                    // Подменяем путь назначения на созданную папку!
                    dest = targetFolderPath; 
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Не удалось создать папку: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return; // прерываем выполнение, если не удалось создать папку
                }
            }
            else
            {
                return; // Если нажали Отмена в окне имени папки — ничего не делаем
            }
        }
    }
    // --- КОНЕЦ ВСТАВКИ ---

    ConflictForm.ConflictResult globalConflictMode =
    ConflictForm.ConflictResult.Cancel;

    bool conflictModeChosen = false;

    try
    {
        File.WriteAllText(
    settingsFile,
    "[Settings]" + Environment.NewLine +
    "LastPath=" + dest + Environment.NewLine +
    "FontSize=" + this.Font.Size.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }
    catch
    {
    }

        foreach (string f in files)
        {
            try
            {
if (Directory.Exists(f))
{
    string targetDir =
        Path.Combine(
            dest,
            new DirectoryInfo(f).Name);

    if (!Directory.Exists(targetDir))
    {
        CopyDirectory(f, targetDir);
        Directory.Delete(f, true);
        continue;
    }

    if (!conflictModeChosen)
    {
        using ConflictForm form = new();

        if (form.ShowDialog(this)
            != DialogResult.OK)
            return;

        globalConflictMode =
            form.Result;

        conflictModeChosen = true;
    }

    if (globalConflictMode ==
        ConflictForm.ConflictResult.Replace)
    {
        Directory.Delete(
            targetDir,
            true);

        CopyDirectory(
            f,
            targetDir);

        Directory.Delete(
            f,
            true);
    }
    else if (globalConflictMode ==
             ConflictForm.ConflictResult.Add)
    {
        MergeDirectory(
            f,
            targetDir);

        Directory.Delete(
            f,
            true);
    }
}
else if (File.Exists(f))
{
    string targetFile =
        Path.Combine(
            dest,
            Path.GetFileName(f));

    if (!File.Exists(targetFile))
    {
        File.Move(f, targetFile);
        continue;
    }

if (!conflictModeChosen)
{
    using ConflictForm form = new();

    if (form.ShowDialog(this)
        != DialogResult.OK)
        return;

    globalConflictMode =
        form.Result;

    conflictModeChosen = true;
}
if (globalConflictMode == ConflictForm.ConflictResult.Replace)
{
    // Проверяем, существует ли уже файл по целевому пути
    if (File.Exists(targetFile))
    {
        File.Delete(targetFile); // Если да, удаляем его, чтобы освободить место
    }

    // Теперь спокойно перемещаем
    File.Move(f, targetFile);
}
else if (globalConflictMode ==
         ConflictForm.ConflictResult.Add)
         {
        string name =
            Path.GetFileNameWithoutExtension(
                targetFile);

        string ext =
            Path.GetExtension(
                targetFile);

        string stamp =
            DateTime.Now.ToString(
                "yyMMddHHmmss");

        string newTarget =
            Path.Combine(
                dest,
                name + "_" +
                stamp +
                ext);

        File.Move(
            f,
            newTarget);
    }
}
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Ошибка",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        Close();
    }
    private void TreeView_KeyDown(
        object? sender,
        KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter)
        {
            if (treeView.SelectedNode != null)
            {
                TreeView_NodeMouseDoubleClick(
                    treeView,
                    new TreeNodeMouseClickEventArgs(
                        treeView.SelectedNode,
                        MouseButtons.Left,
                        2,
                        0,
                        0));
            }

            e.SuppressKeyPress = true;
        }
    }

private void TreeView_AfterSelect(
    object? sender,
    TreeViewEventArgs e)
{
    if (previousNode != null)
    {
        previousNode.BackColor =
            treeView.BackColor;

        previousNode.ForeColor =
            treeView.ForeColor;
    }

    if (e.Node != null)
    {
        e.Node.BackColor =
            Color.FromArgb(60, 60, 60);

        e.Node.ForeColor =
            Color.White;

        previousNode = e.Node;
    }
}

private void TreeView_DrawNode(
    object? sender,
    DrawTreeNodeEventArgs e)
{
    Color backColor;
    Color foreColor = Color.Black;

    if ((e.State & TreeNodeStates.Selected) != 0)
    {
        backColor = Color.FromArgb(255, 255, 255);
        foreColor = Color.FromArgb(0, 0, 255);
    }
    else
    {
        backColor = treeView.BackColor;
        foreColor = Color.Black;
    }

    if (e.Node == null)
        return;

    using (SolidBrush backBrush =
        new SolidBrush(backColor))
    {
        e.Graphics.FillRectangle(
            backBrush,
            e.Bounds);
    }

    TextRenderer.DrawText(
        e.Graphics,
        e.Node?.Text ?? string.Empty,
        treeView.Font,
        e.Bounds,
        foreColor,
        TextFormatFlags.VerticalCenter);
}

    private static void CopyDirectory(
        string sourceDir,
        string destDir)
    {
        Directory.CreateDirectory(destDir);

        foreach (string file in Directory.GetFiles(sourceDir))
        {
            string target =
                Path.Combine(
                    destDir,
                    Path.GetFileName(file));

            File.Copy(file, target, true);
        }

        foreach (string dir in Directory.GetDirectories(sourceDir))
        {
            string target =
                Path.Combine(
                    destDir,
                    Path.GetFileName(dir));

            CopyDirectory(dir, target);
        }
    }

private void MergeDirectory(
    string sourceDir,
    string targetDir)
{
    foreach (string dir in
        Directory.GetDirectories(sourceDir))
    {
        string name =
            Path.GetFileName(dir);

        string targetSubDir =
            Path.Combine(
                targetDir,
                name);

        if (!Directory.Exists(targetSubDir))
        {
            Directory.CreateDirectory(
                targetSubDir);
        }

        MergeDirectory(
            dir,
            targetSubDir);
    }

    foreach (string file in
        Directory.GetFiles(sourceDir))
    {
        string name =
            Path.GetFileName(file);

        string targetFile =
            Path.Combine(
                targetDir,
                name);

        if (!File.Exists(targetFile))
        {
            File.Move(
                file,
                targetFile);

            continue;
        }

        string stamp =
            DateTime.Now.ToString(
                "yyMMddHHmmss");

        string newName =
            Path.GetFileNameWithoutExtension(
                targetFile)
            + "_"
            + stamp
            + Path.GetExtension(
                targetFile);

        string newTarget =
            Path.Combine(
                targetDir,
                newName);

        File.Move(
            file,
            newTarget);
    }
}
private void LoadFolderIcon()
    {
        SHFILEINFO shinfo = new();

        SHGetFileInfo(
            Environment.GetFolderPath(
                Environment.SpecialFolder.Windows),
            0,
            ref shinfo,
            (uint)Marshal.SizeOf(shinfo),
            SHGFI_ICON | SHGFI_SMALLICON);

        if (shinfo.hIcon != IntPtr.Zero)
        {
            // 0. Сначала добавляется системная иконка папки (Индекс 0)
            imageList.Images.Add(
                Icon.FromHandle(shinfo.hIcon));
                
            // 1. Затем загружается TeraCopy (Индекс 1)
            string teraIcon =
                Path.Combine(
                    Application.StartupPath,
                    "TeraCopy.ico");

            if (File.Exists(teraIcon))
            {
                imageList.Images.Add(
                    new Icon(teraIcon));
            }

            // 2. И В САМЫЙ КОНЕЦ добавляем новую иконку (Индекс 2)
            try
            {
                string newFolderIconPath = Path.Combine(Application.StartupPath, "NewFolder.ico");
                if (File.Exists(newFolderIconPath))
                {
                    imageList.Images.Add(new Icon(newFolderIconPath));
                }
            }
            catch { }        

            DestroyIcon(shinfo.hIcon);
        }
    }
    protected override void OnHandleCreated(
        EventArgs e)
    {
        base.OnHandleCreated(e);

        try
        {
            int dark = 0;

            DwmSetWindowAttribute(
                Handle,
                20,
                ref dark,
                sizeof(int));
                int borderColor =
                unchecked((int)0x00787878);

                DwmSetWindowAttribute(
                Handle,
                34,
                ref borderColor,
                sizeof(int));
        }
        catch
        {
        }
    }

    [DllImport("shell32.dll")]
    private static extern IntPtr SHGetFileInfo(
        string pszPath,
        uint dwFileAttributes,
        ref SHFILEINFO psfi,
        uint cbFileInfo,
        uint uFlags);

    [DllImport("user32.dll")]
    private static extern bool DestroyIcon(
        IntPtr hIcon);

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(
        IntPtr hwnd,
        int attr,
        ref int attrValue,
        int attrSize);
    
    private const uint SHGFI_ICON = 0x100;
    private const uint SHGFI_SMALLICON = 0x1;

    [StructLayout(LayoutKind.Sequential)]
    private struct SHFILEINFO
    {
        public IntPtr hIcon;
        public int iIcon;
        public uint dwAttributes;

        [MarshalAs(UnmanagedType.ByValTStr,
            SizeConst = 260)]
        public string szDisplayName;

        [MarshalAs(UnmanagedType.ByValTStr,
            SizeConst = 80)]
        public string szTypeName;
    }
}

}
