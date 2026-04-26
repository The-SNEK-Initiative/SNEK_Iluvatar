using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace DeveloperTool
{
    public partial class MainWindow : Window
    {
        private ObservableCollection<string> selectedFiles = new();
        private bool enableCompression;
        private bool enableEncryption;
        private bool addStartMenu;
        private bool addToPath;
        private string compressionLevel = "None";
        private string encryptionAlgo = "None";

        private static string logPath = Path.Combine(Path.GetTempPath(), "snek_dev_startup.log");

        public MainWindow()
        {
            File.AppendAllText(logPath, $"[{DateTime.Now}] MainWindow Constructor started\n");
            try
            {
                InitializeComponent();
                File.AppendAllText(logPath, $"[{DateTime.Now}] InitializeComponent finished\n");
                FileList.ItemsSource = selectedFiles;
                LoadUI();
                File.AppendAllText(logPath, $"[{DateTime.Now}] LoadUI finished\n");
            }
            catch (Exception ex)
            {
                File.AppendAllText(logPath, $"[{DateTime.Now}] MAINWINDOW CONSTRUCTOR CRASH: {ex.Message}\n{ex.StackTrace}\n");
                throw;
            }
        }

        private void LoadUI()
        {
            try
            {
                string graphicsPath = Path.Combine(GetProjectRoot(), "graphics", "S.png");
                if (File.Exists(graphicsPath))
                {
                    var bitmap = new BitmapImage(new Uri(graphicsPath, UriKind.Absolute));
                    LogoImage.Source = bitmap;
                }
            }
            catch { }

            CompressionComboBox.Items.Add("None");
            CompressionComboBox.Items.Add("Low");
            CompressionComboBox.Items.Add("Medium");
            CompressionComboBox.Items.Add("High");
            CompressionComboBox.Items.Add("Maximum");
            CompressionComboBox.SelectedIndex = 0;

            EncryptionComboBox.Items.Add("None");
            EncryptionComboBox.Items.Add("AES-128");
            EncryptionComboBox.Items.Add("AES-256");
            EncryptionComboBox.SelectedIndex = 0;

            FileCountText.Text = "0";
            TotalSizeText.Text = "0 KB";
            CompressionText.Text = "None";
            EncryptionText.Text = "None";
        }

        private string GetProjectRoot()
        {
            var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            while (dir != null && !File.Exists(Path.Combine(dir.FullName, "SNEK_Iluvatar.sln")))
            {
                dir = dir.Parent;
            }
            return dir?.FullName ?? AppDomain.CurrentDomain.BaseDirectory;
        }

        private void AddFiles_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new()
            {
                Title = "Select Files to Include",
                Multiselect = true,
                Filter = "All Files|*.*"
            };

            if (dialog.ShowDialog() == true)
            {
                foreach (var file in dialog.FileNames)
                {
                    if (!selectedFiles.Contains(file))
                        selectedFiles.Add(file);
                }
                UpdateSummary();
                UpdateStatus($"Added {dialog.FileNames.Length} file(s) to package");
            }
        }

        private void AddFolder_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Select any file from folder",
                Filter = "All Files|*.*",
                ValidateNames = false,
                CheckFileExists = false
            };

            if (dialog.ShowDialog() == true)
            {
                string folderPath = Path.GetDirectoryName(dialog.FileName);
                var files = Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories);
                int added = 0;
                foreach (var file in files)
                {
                    if (!selectedFiles.Contains(file))
                    {
                        selectedFiles.Add(file);
                        added++;
                    }
                }
                UpdateSummary();
                UpdateStatus($"Added {added} files from folder recursively");
            }
        }

        private void RemoveFile_Click(object sender, RoutedEventArgs e)
        {
            if (FileList.SelectedItem is string selected)
            {
                selectedFiles.Remove(selected);
                UpdateSummary();
                UpdateStatus("File removed from package");
            }
        }

        private void ClearAll_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Clear all files from the package?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                selectedFiles.Clear();
                UpdateSummary();
                UpdateStatus("All files cleared");
            }
        }

        private void Browse_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dialog = new()
            {
                Title = "Save Installer As",
                Filter = "Executable Files (*.exe)|*.exe",
                FileName = "installer.exe"
            };

            if (dialog.ShowDialog() == true)
            {
                OutputPath.Text = dialog.FileName;
                UpdateStatus($"Output path set to {Path.GetFileName(dialog.FileName)}");
            }
        }

        private void CompressionChanged(object sender, SelectionChangedEventArgs e)
        {
            compressionLevel = ((ComboBox)sender).SelectedItem?.ToString() ?? "None";
            enableCompression = compressionLevel != "None";
            if (CompressionText != null) CompressionText.Text = compressionLevel;
            UpdateStatus($"Compression: {compressionLevel}");
        }

        private void EncryptionChanged(object sender, SelectionChangedEventArgs e)
        {
            encryptionAlgo = ((ComboBox)sender).SelectedItem?.ToString() ?? "None";
            enableEncryption = encryptionAlgo != "None";
            if (EncryptionText != null) EncryptionText.Text = encryptionAlgo;
            UpdateStatus($"Encryption: {encryptionAlgo}");
        }

        private void StartMenu_Checked(object sender, RoutedEventArgs e)
        {
            addStartMenu = ((CheckBox)sender).IsChecked ?? false;
            UpdateStatus($"Start Menu: {(addStartMenu ? "Enabled" : "Disabled")}");
        }

        private void Path_Checked(object sender, RoutedEventArgs e)
        {
            addToPath = ((CheckBox)sender).IsChecked ?? false;
            UpdateStatus($"System PATH: {(addToPath ? "Enabled" : "Disabled")}");
        }

        private void UpdateSummary()
        {
            if (FileCountText == null || TotalSizeText == null) return;

            FileCountText.Text = selectedFiles.Count.ToString();
            
            long totalSize = 0;
            foreach (var file in selectedFiles)
            {
                if (File.Exists(file))
                    totalSize += new FileInfo(file).Length;
            }

            if (totalSize < 1024)
                TotalSizeText.Text = $"{totalSize} B";
            else if (totalSize < 1024 * 1024)
                TotalSizeText.Text = $"{totalSize / 1024.0:F1} KB";
            else
                TotalSizeText.Text = $"{totalSize / (1024.0 * 1024):F1} MB";
        }

        private void Build_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(PackageName.Text))
            {
                MessageBox.Show("Please enter a package name", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (string.IsNullOrWhiteSpace(PackageVersion.Text))
            {
                MessageBox.Show("Please enter a package version", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (selectedFiles.Count == 0)
            {
                MessageBox.Show("Please add at least one file", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (string.IsNullOrWhiteSpace(OutputPath.Text))
            {
                MessageBox.Show("Please specify an output path", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            PerformBuild();
        }

        private void PerformBuild()
        {
            try
            {
                ProgressBar.IsIndeterminate = true;
                UpdateStatus("Preparing to build installer...");

                string name = PackageName.Text;
                string version = PackageVersion.Text;
                string author = PackageAuthor.Text ?? "Unknown";
                string outputPath = OutputPath.Text;

                UpdateStatus("Creating package archive...");
                string tempSnekPath = Path.Combine(Path.GetTempPath(), "temp_package.snek");
                
                if (enableCompression || enableEncryption)
                {
                    CreateProcessedPackage(tempSnekPath);
                }
                else
                {
                    CreateBasePackage(tempSnekPath);
                }

                CreateInstallerWithEmbeddedPackage(tempSnekPath, outputPath, name, version, author, addStartMenu, addToPath);

                if (File.Exists(tempSnekPath))
                    File.Delete(tempSnekPath);

                ProgressBar.IsIndeterminate = false;
                UpdateStatus("✓ Installer created successfully!");
                MessageBox.Show($"Installer created at:\n{outputPath}\n\nFile size: {new FileInfo(outputPath).Length / 1024.0 / 1024.0:F2} MB", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                ProgressBar.IsIndeterminate = false;
                UpdateStatus($"✗ Error: {ex.Message}");
                MessageBox.Show($"Error building installer:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CreateBasePackage(string outputPath)
        {
            using (var fs = File.Create(outputPath))
            {
                byte[] magic = Encoding.ASCII.GetBytes("SNEK");
                byte[] versionBytes = Encoding.UTF8.GetBytes("1.0");
                
                fs.Write(magic, 0, 4);
                fs.Write(BitConverter.GetBytes(versionBytes.Length), 0, 4);
                fs.Write(versionBytes, 0, versionBytes.Length);
                
                foreach (var filePath in selectedFiles)
                {
                    if (!File.Exists(filePath)) continue;
                    
                    byte[] nameBytes = Encoding.UTF8.GetBytes(Path.GetFileName(filePath));
                    byte[] fileData = File.ReadAllBytes(filePath);
                    
                    fs.Write(BitConverter.GetBytes(nameBytes.Length), 0, 4);
                    fs.Write(nameBytes, 0, nameBytes.Length);
                    fs.Write(BitConverter.GetBytes((long)fileData.Length), 0, 8);
                    fs.Write(fileData, 0, fileData.Length);
                }
            }
        }

        private void CreateProcessedPackage(string outputPath)
        {
            byte[] baseData = null;
            using (var ms = new MemoryStream())
            {
                byte[] magic = Encoding.ASCII.GetBytes("SNEK");
                byte[] versionBytes = Encoding.UTF8.GetBytes("1.0");
                
                ms.Write(magic, 0, 4);
                ms.Write(BitConverter.GetBytes(versionBytes.Length), 0, 4);
                ms.Write(versionBytes, 0, versionBytes.Length);
                
                foreach (var filePath in selectedFiles)
                {
                    if (!File.Exists(filePath)) continue;
                    
                    byte[] nameBytes = Encoding.UTF8.GetBytes(Path.GetFileName(filePath));
                    byte[] fileData = File.ReadAllBytes(filePath);
                    
                    ms.Write(BitConverter.GetBytes(nameBytes.Length), 0, 4);
                    ms.Write(nameBytes, 0, nameBytes.Length);
                    ms.Write(BitConverter.GetBytes((long)fileData.Length), 0, 8);
                    ms.Write(fileData, 0, fileData.Length);
                }
                baseData = ms.ToArray();
            }

            byte[] processedData = baseData;
            
            if (enableCompression)
            {
                processedData = CompressData(processedData);
            }
            
            if (enableEncryption)
            {
                processedData = EncryptData(processedData);
            }

            File.WriteAllBytes(outputPath, processedData);
        }

        private byte[] CompressData(byte[] data)
        {
            using (var output = new MemoryStream())
            using (var gzip = new GZipStream(output, CompressionMode.Compress))
            {
                gzip.Write(data, 0, data.Length);
                gzip.Flush();
                return output.ToArray();
            }
        }

        private byte[] EncryptData(byte[] data)
        {
            using (var aes = Aes.Create())
            {
                aes.KeySize = encryptionAlgo == "AES-256" ? 256 : 128;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.GenerateKey();
                aes.GenerateIV();

                using (var encryptor = aes.CreateEncryptor())
                using (var ms = new MemoryStream())
                {
                    ms.Write(aes.Key, 0, aes.Key.Length);
                    ms.Write(aes.IV, 0, aes.IV.Length);
                    
                    using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    {
                        cs.Write(data, 0, data.Length);
                        cs.FlushFinalBlock();
                    }
                    return ms.ToArray();
                }
            }
        }

        private void CreateInstallerWithEmbeddedPackage(string snekPath, string outputPath, string name, string version, string author, bool addStartMenu, bool addToPath)
        {
            string templateInstallerPath = FindInstallerTemplate();
            if (!File.Exists(templateInstallerPath))
                throw new Exception("InstallerGUI template not found. Please rebuild the project.");

            UpdateStatus("Copying installer template...");
            File.Copy(templateInstallerPath, outputPath, overwrite: true);
            
            UpdateStatus("Embedding package into executable...");
            EmbedPackageIntoExe(outputPath, snekPath, name, version, author, addStartMenu, addToPath);
            
            UpdateStatus($"Created standalone installer: {Path.GetFileName(outputPath)}");
        }

        private void EmbedPackageIntoExe(string exePath, string snekPath, string name, string version, string author, bool addStartMenu, bool addToPath)
        {
            byte[] snekData = File.ReadAllBytes(snekPath);

            // In .NET 8, appending to a SingleFile EXE often breaks it because the 
            // bundle signature/manifest is at the end. 
            // However, since we switched to standard publishing (PublishSingleFile=false),
            // the EXE is a standard PE and appending is safer.
            
            using (var fs = File.Open(exePath, FileMode.Append, FileAccess.Write))
            {
                // Align to 8 bytes for safety
                long currentPos = fs.Position;
                int padding = (int)(8 - (currentPos % 8)) % 8;
                if (padding > 0) fs.Write(new byte[padding], 0, padding);

                long metadataOffset = fs.Position;

                byte[] nameBytes = Encoding.UTF8.GetBytes(name ?? "");
                byte[] versionBytes = Encoding.UTF8.GetBytes(version ?? "");
                byte[] authorBytes = Encoding.UTF8.GetBytes(author ?? "");

                // 1. Write metadata
                fs.Write(BitConverter.GetBytes(nameBytes.Length), 0, 4);
                fs.Write(BitConverter.GetBytes(versionBytes.Length), 0, 4);
                fs.Write(BitConverter.GetBytes(authorBytes.Length), 0, 4);
                fs.Write(BitConverter.GetBytes((long)snekData.Length), 0, 8);
                
                // Add new options to metadata
                fs.WriteByte((byte)(addStartMenu ? 1 : 0));
                fs.WriteByte((byte)(addToPath ? 1 : 0));

                fs.Write(nameBytes, 0, nameBytes.Length);
                fs.Write(versionBytes, 0, versionBytes.Length);
                fs.Write(authorBytes, 0, authorBytes.Length);
                
                // Restore logo embedding
                string graphicsPath = Path.Combine(GetProjectRoot(), "graphics", "installer.png");
                if (File.Exists(graphicsPath))
                {
                    byte[] logoData = File.ReadAllBytes(graphicsPath);
                    fs.Write(BitConverter.GetBytes((long)logoData.Length), 0, 8);
                    fs.Write(logoData, 0, logoData.Length);
                }
                else
                {
                    fs.Write(BitConverter.GetBytes(0L), 0, 8);
                }

                // 2. Write package data
                fs.Write(snekData, 0, snekData.Length);

                // 3. Write Footer
                // Footer: Metadata Offset (8 bytes) + Magic "SPKG" (4 bytes)
                fs.Flush(); 
                fs.Write(BitConverter.GetBytes(metadataOffset), 0, 8);
                byte[] magic = Encoding.ASCII.GetBytes("SPKG");
                fs.Write(magic, 0, 4);
            }

            // Update icon removed for debugging
            // UpdateExeIcon(exePath);
        }

        private void UpdateExeIcon(string exePath)
        {
            try
            {
                string pngPath = Path.Combine(GetProjectRoot(), "graphics", "installer.png");
                if (!File.Exists(pngPath)) return;

                UpdateStatus("Updating installer icon...");
                
                // We'll use a simple PowerShell script to convert PNG to ICO and update the resource
                // Note: For a true resource update we'd need a tool like ResourceHacker or a library,
                // but we can try to use a temporary C# script or a specialized tool if available.
                // For now, let's at least ensure the logo is embedded correctly for the UI.
            }
            catch (Exception ex)
            {
                UpdateStatus($"Icon update failed: {ex.Message}");
            }
        }

        private string FindInstallerTemplate()
        {
            string[] possiblePaths = new[]
            {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "InstallerGUI.exe"),
                Path.Combine(GetProjectRoot(), "src", "end-user-installer", "gui", "bin", "Release", "publish", "InstallerGUI.exe"),
                Path.Combine(GetProjectRoot(), "src", "end-user-installer", "gui", "bin", "Release", "net8.0-windows", "InstallerGUI.exe"),
                Path.Combine(GetProjectRoot(), "src", "end-user-installer", "gui", "bin", "Debug", "net8.0-windows", "InstallerGUI.exe"),
            };

            foreach (var path in possiblePaths)
            {
                if (File.Exists(path))
                    return Path.GetFullPath(path);
            }

            throw new Exception("InstallerGUI.exe not found at expected locations");
        }

        private void UpdateStatus(string message)
        {
            if (StatusText != null)
                StatusText.Text = $"[{DateTime.Now:HH:mm:ss}] {message}";
        }
    }
}
