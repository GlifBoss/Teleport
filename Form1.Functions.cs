// Copyright (c) 2026 Glif. Licensed under the MIT License. See LICENSE file in the project root for full license information.
using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using System.Threading.Tasks;

namespace Teleport
{
    public partial class Form1
    {
        // Это переменные для хранения общего прогресса
        private long totalBytesToProcess = 0;
        private long totalBytesProcessed = 0;

        private void LoadFolders()
        {
            treeView.Nodes.Clear();
            TreeNode teraNode = new TreeNode("В TeraCopy") { Tag = "__TERACOPY__", ImageIndex = 1, SelectedImageIndex = 1 };
            treeView.Nodes.Add(teraNode);
            TreeNode newFolderNode = new TreeNode("В Новую папку") { Tag = "__NEW_FOLDER__", ImageIndex = 2, SelectedImageIndex = 2 };
            treeView.Nodes.Add(newFolderNode);

            foreach (DriveInfo drive in DriveInfo.GetDrives())
            {
            try { if (!drive.IsReady) continue; TreeNode node = new TreeNode(drive.Name) { Tag = drive.RootDirectory.FullName }; node.Nodes.Add("loading"); treeView.Nodes.Add(node); } catch { }
            }
        }
        
        private void TreeView_BeforeExpand(object? sender, TreeViewCancelEventArgs e)
        {
            if (!allowExpandCollapse) { e.Cancel = true; return; }
            if (e.Node == null || e.Node.Nodes.Count != 1 || e.Node.Nodes[0].Text != "loading") return;
            e.Node.Nodes.Clear();
            string path = e.Node.Tag?.ToString() ?? "";
            try { foreach (string dir in Directory.GetDirectories(path)) { FileAttributes attr = File.GetAttributes(dir); if ((attr & FileAttributes.Hidden) != 0 || (attr & FileAttributes.System) != 0) continue; TreeNode child = new TreeNode(ShortName(Path.GetFileName(dir))) { Tag = dir }; child.Nodes.Add("loading"); e.Node.Nodes.Add(child); } } catch { }
        }

        private async void TreeView_NodeMouseDoubleClick(object? sender, TreeNodeMouseClickEventArgs e)
        {
            if (treeView == null || treeView.SelectedNode == null) return;
            rememberedResult = null;
            string dest = treeView.SelectedNode.Tag?.ToString() ?? "";

            if (dest == "__TERACOPY__")
            {
                string listPath = Path.Combine(Path.GetTempPath(), "tc_list.txt");
                File.WriteAllLines(listPath, files, Encoding.GetEncoding(1251));
                System.Diagnostics.Process.Start(@"C:\Program Files\TeraCopy\TeraCopy.exe", "AddList *\"" + listPath + "\"");
                Close(); return;
            }

            if (dest == "__NEW_FOLDER__")
            {
                if (files == null || files.Length == 0) return;
                string currentDirectory = Path.GetDirectoryName(files[0]) ?? "";
                using (NewFolderForm inputForm = new NewFolderForm())
                {
                    if (inputForm.ShowDialog(this) == DialogResult.OK)
                    {
                        string targetFolderPath = Path.Combine(currentDirectory, inputForm.FolderName);
                        try { if (!Directory.Exists(targetFolderPath)) Directory.CreateDirectory(targetFolderPath); dest = targetFolderPath; }
                        catch (Exception ex) { MessageBox.Show($"Не удалось создать папку: {ex.Message}"); return; }
                    }
                    else return;
                }
            }

            totalBytesToProcess = GetTotalSize(files);
            totalBytesProcessed = 0; // Сбрасываем счетчик перед началом
            var context = new ConflictContext();
            var progressHandler = _progressManager.AsProgress();

            try
            {
                foreach (string f in files)
                {
                if (Directory.Exists(f))
                {
                   string targetPath = Path.Combine(dest, new DirectoryInfo(f).Name);
                   // Проверяем: если мы на одном диске
                   if (IsSameDrive(f, targetPath))
                   {
                   // Проверяем конфликты (вдруг папка с таким именем уже есть)
                   if (Directory.Exists(targetPath))
                   {
                   if (!ResolveConflict(ref targetPath, true)) continue;
                   }
                   // Быстрое перемещение (просто меняем путь в системе)
                   Directory.Move(f, targetPath);
                   // Обновляем прогресс-бар, чтобы он "заполнился" сразу
                   totalBytesProcessed += GetDirectorySize(new DirectoryInfo(targetPath));
                    _progressManager.Update((double)totalBytesProcessed / totalBytesToProcess * 100);
                   }
                   else
                   {
                   // Если диски разные — запускаем медленное копирование
                   await HandleDirectoryMove(f, dest, context);
                   }
                }
                    else if (File.Exists(f))
                    {
                        string targetPath = Path.Combine(dest, Path.GetFileName(f));
                        long fileSize = new FileInfo(f).Length;
                        // Проверяем, один ли это диск
                        if (IsSameDrive(f, targetPath))
                        {
                            // Если файл существует — обрабатываем конфликт
                            if (File.Exists(targetPath))
                            {
                                if (!ResolveConflict(ref targetPath, false)) continue;
                            }
                            // Мгновенное перемещение
                            File.Move(f, targetPath);
                            // Честно добавляем размер к счетчику и обновляем прогресс
                            totalBytesProcessed += fileSize;
                            _progressManager.Update((double)totalBytesProcessed / totalBytesToProcess * 100);
                        }
                        else
                        {
                            // Разные диски — используем ваш метод с FileStream
                            await HandleFileMove(f, targetPath, progressHandler, context, fileSize);
                        }
                    }
                }
                SaveLastPath(dest);
                // пауза перед закрытием окна
                await Task.Delay(300);
            }
            catch (Exception ex) 
            { 
            MessageBox.Show($"Ошибка: {ex.Message}"); 
            }
            finally 
            { 
            Close(); 
            }
        }

        private async Task HandleFileMove(string source, string dest, IProgress<double> progress, ConflictContext context, long currentFileLength)
        {
            long fileLength = new FileInfo(source).Length;
            if (File.Exists(dest))
            {
            if (!ResolveConflict(ref dest, false)) return; // Если false — выходим из метода
            }
            if (File.Exists(source)) 
            {
            // Создаем локальную "обертку" для прогресса этого конкретного файла
            var fileProgress = new Progress<double>(p => {
            // Считаем: сколько уже было сделано + часть текущего файла
            double globalProgress = (totalBytesProcessed + (p / 100.0 * currentFileLength)) / totalBytesToProcess * 100;
            // Обновляем UI
            _progressManager.Update(globalProgress);
            });
            await CopyFileWithProgressAsync(source, dest, fileProgress);
            File.Delete(source);
            // ВАЖНО: когда файл скопирован, добавляем его размер к общему счетчику
            totalBytesProcessed += currentFileLength; 
            } 
        }

        private async Task HandleDirectoryMove(string sourceDir, string destBaseDir, ConflictContext context)
        {
          string targetDir = Path.Combine(destBaseDir, new DirectoryInfo(sourceDir).Name);
          if (Directory.Exists(targetDir))
          {
          if (!ResolveConflict(ref targetDir, true)) return; // Если false — выходим из метода
          }    
          // 1. Создаем папку
          Directory.CreateDirectory(targetDir);
          // 2. Получаем список всех файлов в папке (включая подпапки)
          var files = Directory.GetFiles(sourceDir, "*.*", SearchOption.AllDirectories);
    
          foreach (string file in files)
          {
          FileInfo fi = new FileInfo(file);
          // Вычисляем, куда именно в новой папке пойдет файл
          string relativePath = file.Substring(sourceDir.Length + 1);
          string targetFile = Path.Combine(targetDir, relativePath);
          // Создаем подпапку, если она нужна
          Directory.CreateDirectory(Path.GetDirectoryName(targetFile));
          // Копируем файл и обновляем прогресс
          using (FileStream sourceStream = new FileStream(file, FileMode.Open, FileAccess.Read))
          using (FileStream destStream = new FileStream(targetFile, FileMode.Create, FileAccess.Write))
          {
          byte[] buffer = new byte[8192 * 1024];
          int read;
          while ((read = await sourceStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
          {
          await destStream.WriteAsync(buffer, 0, read);
          totalBytesProcessed += read; // Увеличиваем общий счетчик
          // Обновляем UI
          double percent = (double)totalBytesProcessed / totalBytesToProcess * 100;
          _progressManager.Update(percent);
          }
          }
          }
          // После копирования всех файлов удаляем оригинал
          Directory.Delete(sourceDir, true);
        }

        private async Task CopyFileWithProgressAsync(string sourcePath, string destPath, IProgress<double> progress)
        {
            const int bufferSize = 8192 * 1024;
            using FileStream sourceStream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read);
            using FileStream destStream = new FileStream(destPath, FileMode.Create, FileAccess.Write);
            byte[] buffer = new byte[bufferSize];
            long totalRead = 0; int read;
            while ((read = await sourceStream.ReadAsync(buffer, 0, buffer.Length)) > 0) { await destStream.WriteAsync(buffer, 0, read); totalRead += read; progress.Report((double)totalRead / sourceStream.Length * 100); }
        }

        private void CopyDirectory(string sourceDir, string destDir)
        {
            Directory.CreateDirectory(destDir);
            foreach (string file in Directory.GetFiles(sourceDir)) File.Copy(file, Path.Combine(destDir, Path.GetFileName(file)), true);
            foreach (string dir in Directory.GetDirectories(sourceDir)) CopyDirectory(dir, Path.Combine(destDir, Path.GetFileName(dir)));
        }

        private void MergeDirectory(string sourceDir, string targetDir)
        {
            foreach (string dir in Directory.GetDirectories(sourceDir))
            {
                string targetSubDir = Path.Combine(targetDir, Path.GetFileName(dir));
                if (!Directory.Exists(targetSubDir)) Directory.CreateDirectory(targetSubDir);
                MergeDirectory(dir, targetSubDir);
            }
            foreach (string file in Directory.GetFiles(sourceDir))
            {
                string targetFile = Path.Combine(targetDir, Path.GetFileName(file));
                if (!File.Exists(targetFile)) { File.Move(file, targetFile); continue; }
                File.Move(file, Path.Combine(targetDir, Path.GetFileNameWithoutExtension(targetFile) + "_" + DateTime.Now.ToString("yyMMddHHmmss") + Path.GetExtension(targetFile)));
            }
        }

        private bool IsSameDrive(string path1, string path2)
        {
           try
           {
           string root1 = Path.GetPathRoot(Path.GetFullPath(path1));
           string root2 = Path.GetPathRoot(Path.GetFullPath(path2));
           // Сравниваем корни дисков (например, C:\ и C:\)
           return string.Equals(root1, root2, StringComparison.OrdinalIgnoreCase);
           }
           catch
           {
           return false;
           }
        }
        private void LoadFolderIcon()
        {
            SHFILEINFO shinfo = new();
            SHGetFileInfo(Environment.GetFolderPath(Environment.SpecialFolder.Windows), 0, ref shinfo, (uint)Marshal.SizeOf(shinfo), SHGFI_ICON | SHGFI_SMALLICON);

            if (shinfo.hIcon != IntPtr.Zero)
            {
                imageList.Images.Add(Icon.FromHandle(shinfo.hIcon));
                string teraIcon = Path.Combine(Application.StartupPath, "TeraCopy.ico");
                if (File.Exists(teraIcon))
                {
                imageList.Images.Add(new Icon(teraIcon));
                }
                string newFolderIconPath = Path.Combine(Application.StartupPath, "NewFolder.ico");
                if (File.Exists(newFolderIconPath))
                {
                try { imageList.Images.Add(new Icon(newFolderIconPath)); } catch { }
                }
                DestroyIcon(shinfo.hIcon);
            }
        }
        private string ShortName(string text) => text.Length <= 24 ? text : text.Substring(0, 21) + "...";
        private string GetLastPath() { try { foreach (string line in File.ReadAllLines(settingsFile)) if (line.StartsWith("LastPath=")) return line.Substring(9); } catch { } return ""; }
        private void ExpandToPath(string fullPath) { foreach (TreeNode rootNode in treeView.Nodes) { string nodePath = rootNode.Tag?.ToString() ?? ""; if (fullPath.StartsWith(nodePath, StringComparison.OrdinalIgnoreCase)) { ExpandNodeRecursive(rootNode, fullPath); break; } } }   
        private void ExpandNodeRecursive(TreeNode node, string targetPath)
        {
           // 1. Приводим пути к абсолютному каноническому виду и убираем лишние слеши в конце
           string currentPath = Path.GetFullPath(node.Tag?.ToString() ?? "").TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
           string normalizedTarget = Path.GetFullPath(targetPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
           // 2. Прямое сравнение
           if (string.Equals(currentPath, normalizedTarget, StringComparison.OrdinalIgnoreCase)) 
           { 
           node.Expand(); 
           treeView.SelectedNode = node; 
           node.EnsureVisible(); 
           return; 
           }
           // 3. Если целевой путь длиннее текущего, значит, нужно идти глубже
           // Проверяем, начинается ли целевой путь с текущего (с учетом разделителя)
           if (normalizedTarget.StartsWith(currentPath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) 
           { 
           allowExpandCollapse = true; 
           // Разворачиваем узел
           if (!node.IsExpanded) node.Expand();
           // Если узел "пустой" (заглушка loading), сначала загружаем его содержимое
           if (node.Nodes.Count > 0 && node.Nodes[0].Text == "loading") 
           { 
              TreeView_BeforeExpand(treeView, new TreeViewCancelEventArgs(node, false, TreeViewAction.Expand)); 
           } 
           // Рекурсивно идем по детям
           foreach (TreeNode child in node.Nodes) 
           { 
              ExpandNodeRecursive(child, targetPath); 
           } 
           }
        }
        public class ConflictContext { public ConflictForm.ConflictResult Mode { get; set; } = ConflictForm.ConflictResult.Cancel; public bool IsChosen { get; set; } = false; }

        private void SaveLastPath(string path)
        {
           try
           {
           List<string> lines = new List<string>();
           bool found = false;
           // 1. Читаем существующие настройки
           if (File.Exists(settingsFile))
           {
               foreach (string line in File.ReadAllLines(settingsFile))
               {
                   if (line.StartsWith("LastPath=", StringComparison.OrdinalIgnoreCase))
                   {
                       lines.Add("LastPath=" + path);
                       found = true;
                   }
                   else
                   {
                       lines.Add(line);
                   }
                }
            }
            // 2. Если LastPath не было в файле, добавляем
            if (!found)
            {
               lines.Add("LastPath=" + path);
            }
            // 3. Записываем обновленный список настроек
            File.WriteAllLines(settingsFile, lines);
            }
            catch { /* Игнорируем ошибки записи */ }
        }

        private long GetTotalSize(string[] files)
        {
           long total = 0;
           foreach (string f in files)
           {
           if (File.Exists(f)) total += new FileInfo(f).Length;
           else if (Directory.Exists(f)) total += GetDirectorySize(new DirectoryInfo(f));
           }
           return total;
        }

        private long GetDirectorySize(DirectoryInfo d)
        {
           long size = 0;
           foreach (FileInfo f in d.GetFiles()) size += f.Length;
           foreach (DirectoryInfo dir in d.GetDirectories()) size += GetDirectorySize(dir);
           return size;
        }

        private bool ResolveConflict(ref string targetPath, bool isDirectory)
        {
            // 1. Если выбор еще не был сделан ранее — показываем форму
            if (!rememberedResult.HasValue)
            {
            this.Invoke(new Action(() => {
            using ConflictForm form = new(this.Font);
            form.Owner = this; // Привязываем к главному окну
            if (form.ShowDialog() == DialogResult.OK)
            {
                rememberedResult = form.Result;
            }
            }));
            }
            // 2. Если нажали "Отмена" или просто закрыли окно — прерываем операцию
            if (rememberedResult == ConflictForm.ConflictResult.Cancel || !rememberedResult.HasValue)
            {
               return false; // Сигнал: прервать копирование
            }
             // 3. Применяем логику "Добавить" (переименование)
            if (rememberedResult == ConflictForm.ConflictResult.Add)
            {
            string dir = Path.GetDirectoryName(targetPath)!;
            string name = Path.GetFileNameWithoutExtension(targetPath);
            string ext = Path.GetExtension(targetPath);
            targetPath = Path.Combine(dir, $"{name}_{DateTime.Now:yyMMddHHmmss}{ext}");
            }
            // 4. Логика "Заменить" не требует смены пути, 
            // но требует удаления существующего объекта перед копированием
            else if (rememberedResult == ConflictForm.ConflictResult.Replace)
            {
              if (isDirectory) 
              {
              if (Directory.Exists(targetPath)) Directory.Delete(targetPath, true);
              }
              else 
              {
              if (File.Exists(targetPath)) File.Delete(targetPath);
              }
            }
            return true; // Сигнал: можно продолжать копирование
        }


        [DllImport("shell32.dll")] private static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbFileInfo, uint uFlags);
        [DllImport("user32.dll")] private static extern bool DestroyIcon(IntPtr hIcon);
        private const uint SHGFI_ICON = 0x100; private const uint SHGFI_SMALLICON = 0x1;
        [StructLayout(LayoutKind.Sequential)] private struct SHFILEINFO { public IntPtr hIcon; public int iIcon; public uint dwAttributes; [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)] public string szDisplayName; [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)] public string szTypeName; }
    }
}