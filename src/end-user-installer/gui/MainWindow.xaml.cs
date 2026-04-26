using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Text;
using System.Linq;
using System.IO.Compression;
using System.Security.Cryptography;

namespace InstallerGUI
{
    public partial class MainWindow : Window
    {
        private int currentStep = 0;
        private string packageName = "";
        private string packageVersion = "";
        private string packageAuthor = "";
        private List<string> packageFiles = new();
        private byte[] snekData = null;
        private byte[] logoData = null;
        private bool metadataAddStartMenu = true;
        private bool metadataAddToPath = false;
        private static string logPath = Path.Combine(Path.GetTempPath(), "installer_debug.txt");

        public MainWindow()
        {
            try
            {
                LogMessage("MainWindow.ctor: Starting initialization");
                InitializeComponent();
                LogMessage("MainWindow.ctor: InitializeComponent completed");
                
                ExtractEmbeddedPackage();
                LogMessage("MainWindow.ctor: ExtractEmbeddedPackage completed");
                
                LoadLogo();
                LogMessage("MainWindow.ctor: LoadLogo completed");
                
                LoadPackageMetadata();
                LogMessage("MainWindow.ctor: LoadPackageMetadata completed");
                
                ShowStep(0);
                LogMessage("MainWindow.ctor: ShowStep(0) completed");
            }
            catch (Exception ex)
            {
                LogMessage($"MainWindow.ctor EXCEPTION: {ex}");
                MessageBox.Show($"Initialization error: {ex.Message}\n\nCheck {logPath} for details", "Startup Error");
                throw;
            }
        }

        private static void LogMessage(string message)
        {
            try
            {
                File.AppendAllText(logPath, $"[{DateTime.Now:HH:mm:ss.fff}] {message}\n");
            }
            catch { }
        }

        private void LoadLogo()
        {
            try
            {
                if (logoData != null && logoData.Length > 0)
                {
                    LogMessage("LoadLogo: Loading logo from embedded package data");
                    using (var ms = new MemoryStream(logoData))
                    {
                        var bitmap = new System.Windows.Media.Imaging.BitmapImage();
                        bitmap.BeginInit();
                        bitmap.StreamSource = ms;
                        bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                        bitmap.EndInit();
                        LogoImage.Source = bitmap;
                        return;
                    }
                }

                // Fallback to local file if no embedded logo
                string projectRoot = GetProjectRoot();
                string logoFilePath = Path.Combine(projectRoot, "graphics", "S.png");
                if (File.Exists(logoFilePath))
                {
                    var bitmap = new System.Windows.Media.Imaging.BitmapImage(new Uri(logoFilePath, UriKind.Absolute));
                    LogoImage.Source = bitmap;
                }
            }
            catch (Exception ex)
            {
                LogMessage($"LoadLogo EXCEPTION: {ex}");
            }
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

        private void ExtractEmbeddedPackage()
        {
            try
            {
                LogMessage("ExtractEmbeddedPackage: Starting");
                string exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
                LogMessage($"ExtractEmbeddedPackage: exePath = {exePath}");
                if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath))
                    throw new Exception("Cannot find executable path");

                using (var fs = File.OpenRead(exePath))
                {
                    if (fs.Length < 12)
                    {
                        LogMessage("ExtractEmbeddedPackage: File too small for SPKG footer");
                        return;
                    }

                    // 1. Read footer (last 12 bytes)
                    fs.Seek(-12, SeekOrigin.End);
                    byte[] footer = new byte[12];
                    fs.Read(footer, 0, 12);

                    string magic = Encoding.ASCII.GetString(footer, 8, 4);
                    if (magic != "SPKG")
                    {
                        LogMessage("ExtractEmbeddedPackage: No SPKG footer found (template installer)");
                        return;
                    }

                    long metadataOffset = BitConverter.ToInt64(footer, 0);
                    LogMessage($"ExtractEmbeddedPackage: Found SPKG footer, metadataOffset = {metadataOffset}");

                    // 2. Read metadata
                    fs.Seek(metadataOffset, SeekOrigin.Begin);
                    byte[] metaHeader = new byte[20];
                    if (fs.Read(metaHeader, 0, 20) != 20)
                        throw new Exception("Failed to read metadata header");

                    int nameLen = BitConverter.ToInt32(metaHeader, 0);
                    int versionLen = BitConverter.ToInt32(metaHeader, 4);
                    int authorLen = BitConverter.ToInt32(metaHeader, 8);
                    long snekLen = BitConverter.ToInt64(metaHeader, 12);

                    // Read installation options
                    int opt1 = fs.ReadByte();
                    int opt2 = fs.ReadByte();
                    if (opt1 == -1 || opt2 == -1)
                        throw new Exception("Failed to read installation options");
                    
                    metadataAddStartMenu = opt1 == 1;
                    metadataAddToPath = opt2 == 1;

                    LogMessage($"ExtractEmbeddedPackage: nameLen={nameLen}, versionLen={versionLen}, authorLen={authorLen}, snekLen={snekLen}, startMenu={metadataAddStartMenu}, addToPath={metadataAddToPath}");

                    byte[] nameBytes = new byte[nameLen];
                    byte[] versionBytes = new byte[versionLen];
                    byte[] authorBytes = new byte[authorLen];

                    if (fs.Read(nameBytes, 0, nameLen) != nameLen) throw new Exception("Failed to read package name");
                    if (fs.Read(versionBytes, 0, versionLen) != versionLen) throw new Exception("Failed to read package version");
                    if (fs.Read(authorBytes, 0, authorLen) != authorLen) throw new Exception("Failed to read package author");

                    packageName = Encoding.UTF8.GetString(nameBytes);
                    packageVersion = Encoding.UTF8.GetString(versionBytes);
                    packageAuthor = Encoding.UTF8.GetString(authorBytes);

                    // Read logo if present
                    byte[] logoLenBytes = new byte[8];
                    if (fs.Read(logoLenBytes, 0, 8) == 8)
                    {
                        long logoLen = BitConverter.ToInt64(logoLenBytes, 0);
                        if (logoLen > 0 && logoLen < 10 * 1024 * 1024) // 10MB sanity check
                        {
                            logoData = new byte[logoLen];
                            if (fs.Read(logoData, 0, (int)logoLen) == (int)logoLen)
                            {
                                LogMessage($"ExtractEmbeddedPackage: Extracted {logoLen} bytes of logo data");
                            }
                        }
                    }

                    LogMessage($"ExtractEmbeddedPackage: packageName={packageName}, packageVersion={packageVersion}, packageAuthor={packageAuthor}");

                    // 3. Read package data
                    snekData = new byte[snekLen];
                    if (fs.Read(snekData, 0, (int)snekLen) != (int)snekLen)
                        throw new Exception("Failed to read package data");
                    LogMessage($"ExtractEmbeddedPackage: Extracted {snekLen} bytes of package data");
                }
            }
            catch (Exception ex)
            {
                LogMessage($"ExtractEmbeddedPackage EXCEPTION: {ex}");
                MessageBox.Show($"Error extracting package: {ex.Message}", "Error");
            }
        }

        private void LoadPackageMetadata()
        {
            PackageName.Text = packageName;
            PackageVersion.Text = $"Version {packageVersion}";
            PackageAuthor.Text = $"By {packageAuthor}";

            // Set default install path based on package name
            if (!string.IsNullOrEmpty(packageName))
            {
                string safeName = string.Join("_", packageName.Split(Path.GetInvalidFileNameChars()));
                InstallPath.Text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), safeName);
            }

            ConfirmName.Text = packageName;
            ConfirmPath.Text = InstallPath.Text;
            
            // Update checkboxes from metadata
            CreateStartMenuShortcut.IsChecked = metadataAddStartMenu;
            AddToPath.IsChecked = metadataAddToPath;

            int fileCount = CountPackageFiles(snekData);
            ConfirmCount.Text = fileCount > 0 ? $"{fileCount} files" : "Package contents will be extracted";
        }

        private int CountPackageFiles(byte[] packageData)
        {
            if (packageData == null || packageData.Length == 0)
                return 0;

            try
            {
                byte[] dataToProcess = packageData;

                // Check if compressed (starts with GZip magic)
                if (packageData.Length > 2 && packageData[0] == 0x1F && packageData[1] == 0x8B)
                {
                    dataToProcess = DecompressData(packageData);
                }

                // Check if encrypted
                if (dataToProcess.Length > 48) // Minimum size for encrypted data
                {
                    try
                    {
                        dataToProcess = DecryptData(dataToProcess);
                    }
                    catch
                    {
                        // Not encrypted, continue
                    }
                }

                int pos = 0;
                int fileCount = 0;

                // Check magic
                if (pos + 4 > dataToProcess.Length || Encoding.ASCII.GetString(dataToProcess, pos, 4) != "SNEK")
                    return 0;
                pos += 4;

                // Read version
                if (pos + 4 > dataToProcess.Length) return 0;
                int versionLen = BitConverter.ToInt32(dataToProcess, pos); pos += 4;
                if (pos + versionLen > dataToProcess.Length) return 0;
                pos += versionLen;

                // Count files
                while (pos < dataToProcess.Length)
                {
                    if (pos + 4 > dataToProcess.Length) break;
                    int nameLen = BitConverter.ToInt32(dataToProcess, pos); pos += 4;
                    if (pos + nameLen > dataToProcess.Length) break;
                    pos += nameLen;

                    if (pos + 8 > dataToProcess.Length) break;
                    long fileLen = BitConverter.ToInt64(dataToProcess, pos); pos += 8;
                    if (pos + fileLen > dataToProcess.Length) break;
                    pos += (int)fileLen;

                    fileCount++;
                }

                return fileCount;
            }
            catch
            {
                return 0;
            }
        }

        private void Window_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
                DragMove();
        }

        private void ShowStep(int step)
        {
            currentStep = step;

            WelcomePage.Visibility = Visibility.Collapsed;
            LocationPage.Visibility = Visibility.Collapsed;
            ConfirmPage.Visibility = Visibility.Collapsed;
            ProgressPage.Visibility = Visibility.Collapsed;

            switch (step)
            {
                case 0:
                    WelcomePage.Visibility = Visibility.Visible;
                    NextButton.Content = "Next";
                    PrevButton.IsEnabled = false;
                    break;

                case 1:
                    LocationPage.Visibility = Visibility.Visible;
                    NextButton.Content = "Next";
                    PrevButton.IsEnabled = true;
                    break;

                case 2:
                    ConfirmPage.Visibility = Visibility.Visible;
                    ConfirmPath.Text = InstallPath.Text;
                    NextButton.Content = "Install";
                    PrevButton.IsEnabled = true;
                    break;

                case 3:
                    ProgressPage.Visibility = Visibility.Visible;
                    NextButton.Visibility = Visibility.Collapsed;
                    PrevButton.Visibility = Visibility.Collapsed;
                    CancelButton.IsEnabled = true;
                    PerformInstallation();
                    break;
            }
        }

        private void Next_Click(object sender, RoutedEventArgs e)
        {
            if (currentStep < 2)
            {
                ShowStep(currentStep + 1);
            }
            else if (currentStep == 2)
            {
                ShowStep(3);
            }
        }

        private void Prev_Click(object sender, RoutedEventArgs e)
        {
            if (currentStep > 0)
            {
                ShowStep(currentStep - 1);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to cancel the installation?", "Confirm", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                Close();
            }
        }

        private void Browse_Click(object sender, RoutedEventArgs e)
        {
            InstallPath.Focus();
            InstallPath.SelectAll();
        }

        private void PerformInstallation()
        {
            try
            {
                LogAdd("Starting installation...");
                
                if (snekData == null || snekData.Length == 0)
                {
                    LogAdd("✓ No package files to install");
                    LogAdd("Installation complete!");
                    InstallProgress.Value = 100;
                    MessageBox.Show("Installation completed successfully!", "Success");
                    Close();
                    return;
                }

                string installDir = InstallPath.Text;
                if (!Directory.Exists(installDir))
                {
                    Directory.CreateDirectory(installDir);
                    LogAdd($"Created directory: {installDir}");
                }

                LogAdd($"Package size: {snekData.Length} bytes");
                LogAdd($"Installation directory: {installDir}");
                LogAdd("Extracting package files...");

                int extractedFiles = ExtractPackageFiles(snekData, installDir);
                LogAdd($"✓ Extracted {extractedFiles} files successfully");
                
                // Create Uninstaller
                CreateUninstaller(installDir);

                if (CreateDesktopShortcut.IsChecked == true)
                {
                    LogAdd("Creating desktop shortcut...");
                    CreateShortcut(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), $"{packageName}.lnk"), installDir);
                    LogAdd("✓ Shortcut created");
                }

                if (CreateStartMenuShortcut.IsChecked == true)
                {
                    LogAdd("Creating start menu shortcut...");
                    string startMenuPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), packageName);
                    if (!Directory.Exists(startMenuPath)) Directory.CreateDirectory(startMenuPath);
                    CreateShortcut(Path.Combine(startMenuPath, $"{packageName}.lnk"), installDir);
                    LogAdd("✓ Start menu shortcut created");
                }

                if (AddToPath.IsChecked == true)
                {
                    LogAdd("Adding to system PATH...");
                    AddDirToPath(installDir);
                    LogAdd("✓ Added to PATH");
                }

                InstallProgress.Value = 100;
                LogAdd("✓ Installation completed successfully!");
                MessageBox.Show("Installation completed successfully!", "Success");
                Close();
            }
            catch (Exception ex)
            {
                LogAdd($"✗ Error: {ex.Message}");
                MessageBox.Show($"Installation error: {ex.Message}", "Error");
            }
        }

        private int ExtractPackageFiles(byte[] packageData, string installDir)
        {
            int fileCount = 0;
            byte[] dataToProcess = packageData;

            // 1. Check if encrypted (Decryption MUST happen first if it was Encrypt(Compress(Data)))
            // We can't easily detect encryption, so we try it if the data doesn't start with "SNEK" or GZip magic
            bool likelyEncrypted = dataToProcess.Length > 4 && 
                                  Encoding.ASCII.GetString(dataToProcess, 0, 4) != "SNEK" &&
                                  !(dataToProcess[0] == 0x1F && dataToProcess[1] == 0x8B);

            if (likelyEncrypted)
            {
                try
                {
                    LogAdd("Attempting to decrypt package...");
                    dataToProcess = DecryptData(dataToProcess);
                    LogAdd("✓ Decryption successful");
                }
                catch
                {
                    LogAdd("Package does not appear to be encrypted or decryption failed");
                }
            }

            // 2. Check if compressed (starts with GZip magic)
            if (dataToProcess.Length > 2 && dataToProcess[0] == 0x1F && dataToProcess[1] == 0x8B)
            {
                LogAdd("Decompressing package...");
                try
                {
                    dataToProcess = DecompressData(dataToProcess);
                    LogAdd("✓ Decompression successful");
                }
                catch (Exception ex)
                {
                    LogAdd($"✗ Decompression failed: {ex.Message}");
                    throw;
                }
            }

            int pos = 0;

            // Check magic
            if (pos + 4 > dataToProcess.Length || Encoding.ASCII.GetString(dataToProcess, pos, 4) != "SNEK")
            {
                LogMessage($"Invalid magic: {BitConverter.ToString(dataToProcess.Take(Math.Min(dataToProcess.Length, 8)).ToArray())}");
                throw new Exception("Invalid package format (missing SNEK magic)");
            }
            pos += 4;

            // Read version
            if (pos + 4 > dataToProcess.Length) throw new Exception("Invalid package format");
            int versionLen = BitConverter.ToInt32(dataToProcess, pos); pos += 4;
            if (pos + versionLen > dataToProcess.Length) throw new Exception("Invalid package format");
            string version = Encoding.UTF8.GetString(dataToProcess, pos, versionLen);
            pos += versionLen;

            LogAdd($"Package version: {version}");

            // Read files
            while (pos < dataToProcess.Length)
            {
                if (pos + 4 > dataToProcess.Length) break;
                int nameLen = BitConverter.ToInt32(dataToProcess, pos); pos += 4;
                if (pos + nameLen > dataToProcess.Length) throw new Exception("Invalid package format");
                string fileName = Encoding.UTF8.GetString(dataToProcess, pos, nameLen);
                pos += nameLen;

                if (pos + 8 > dataToProcess.Length) throw new Exception("Invalid package format");
                long fileLen = BitConverter.ToInt64(dataToProcess, pos); pos += 8;
                if (pos + fileLen > dataToProcess.Length) throw new Exception("Invalid package format");

                byte[] fileData = new byte[fileLen];
                Array.Copy(dataToProcess, pos, fileData, 0, fileLen);
                pos += (int)fileLen;

                string fullPath = Path.Combine(installDir, fileName);
                string dirPath = Path.GetDirectoryName(fullPath);
                if (!string.IsNullOrEmpty(dirPath) && !Directory.Exists(dirPath))
                {
                    Directory.CreateDirectory(dirPath);
                }

                File.WriteAllBytes(fullPath, fileData);
                LogAdd($"  ✓ {fileName} ({fileLen} bytes)");
                fileCount++;
            }

            return fileCount;
        }

        private void CreateShortcut(string shortcutPath, string targetDir)
        {
            try
            {
                // Simple way to create a shortcut via PowerShell to avoid COM references
                string targetExe = Directory.GetFiles(targetDir, "*.exe").FirstOrDefault() ?? targetDir;
                string powershellCommand = $"$s=(New-Object -COM WScript.Shell).CreateShortcut('{shortcutPath}');$s.TargetPath='{targetExe}';$s.WorkingDirectory='{targetDir}';$s.Save()";
                
                var startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{powershellCommand}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                System.Diagnostics.Process.Start(startInfo)?.WaitForExit();
            }
            catch (Exception ex)
            {
                LogAdd($"  ✗ Shortcut failed: {ex.Message}");
            }
        }

        private void AddDirToPath(string dirPath)
        {
            try
            {
                const string name = "PATH";
                string path = Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.User) ?? "";
                if (!path.Split(';').Any(p => p.Trim().Equals(dirPath, StringComparison.OrdinalIgnoreCase)))
                {
                    path = string.IsNullOrEmpty(path) ? dirPath : $"{path};{dirPath}";
                    Environment.SetEnvironmentVariable(name, path, EnvironmentVariableTarget.User);
                }
            }
            catch (Exception ex)
            {
                LogAdd($"  ✗ PATH update failed: {ex.Message}");
            }
        }

        private void CreateUninstaller(string installDir)
        {
            try
            {
                LogAdd("Generating uninstaller binary...");
                
                // 1. Extract the ASM-compiled uninstaller from resources
                string uninstallExe = Path.Combine(installDir, "uninstall.exe");
                var assembly = typeof(MainWindow).Assembly;
                string resourceName = assembly.GetManifestResourceNames().FirstOrDefault(r => r.EndsWith("uninstall.exe"));
                
                if (string.IsNullOrEmpty(resourceName))
                {
                    LogAdd("  ✗ Uninstaller resource not found, falling back to batch");
                    CreateUninstallerFallback(installDir);
                    return;
                }

                using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                using (FileStream fileStream = new FileStream(uninstallExe, FileMode.Create))
                {
                    stream.CopyTo(fileStream);
                }

                // 2. Generate the uninstallation command for the binary to execute
                string desktopShortcut = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), $"{packageName}.lnk");
                string startMenuPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), packageName);
                string pathRemoval = $"$p=[Environment]::GetEnvironmentVariable('PATH','User');$d='{installDir.Replace("\\", "\\\\")}';$newp=($p -split ';').Where({{$_.Trim() -ne $d}}) -join ';';[Environment]::SetEnvironmentVariable('PATH',$newp,'User')";

                // Build a single command line that the binary will pass to cmd.exe
                StringBuilder cmd = new StringBuilder();
                cmd.Append("/c timeout /t 1 & ");
                cmd.Append($"if exist \"{desktopShortcut}\" del /f /q \"{desktopShortcut}\" & ");
                cmd.Append($"if exist \"{startMenuPath}\" rd /s /q \"{startMenuPath}\" & ");
                cmd.Append($"powershell -WindowStyle Hidden -Command \"{pathRemoval}\" & ");
                cmd.Append($"cd .. & rd /s /q \"{installDir}\"");

                // 3. Write the command to uninstall.paths (which the binary reads)
                File.WriteAllText(Path.Combine(installDir, "uninstall.paths"), cmd.ToString());
                
                LogAdd("✓ Uninstaller binary created (ASM minimal size)");
            }
            catch (Exception ex)
            {
                LogAdd($"  ✗ Uninstaller creation failed: {ex.Message}");
                // Fallback to batch if something goes wrong
                CreateUninstallerFallback(installDir);
            }
        }

        private void CreateUninstallerFallback(string installDir)
        {
            try
            {
                string uninstallBatch = Path.Combine(installDir, "uninstall.bat");
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("@echo off");
                sb.AppendLine($"echo Uninstalling {packageName}...");
                sb.AppendLine("timeout /t 2 /nobreak > nul");
                string desktopShortcut = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), $"{packageName}.lnk");
                sb.AppendLine($"if exist \"{desktopShortcut}\" del \"{desktopShortcut}\"");
                string startMenuPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), packageName);
                sb.AppendLine($"if exist \"{startMenuPath}\" rd /s /q \"{startMenuPath}\"");
                sb.AppendLine("powershell -Command \"$p=[Environment]::GetEnvironmentVariable('PATH','User');$d='" + installDir.Replace("\\", "\\\\") + "';$newp=($p -split ';').Where({$_.Trim() -ne $d}) -join ';';[Environment]::SetEnvironmentVariable('PATH',$newp,'User')\"");
                sb.AppendLine("cd ..");
                sb.AppendLine($"rd /s /q \"{installDir}\"");
                File.WriteAllText(uninstallBatch, sb.ToString());
            }
            catch { }
        }

        private byte[] DecompressData(byte[] compressedData)
        {
            using (var input = new MemoryStream(compressedData))
            using (var gzip = new GZipStream(input, CompressionMode.Decompress))
            using (var output = new MemoryStream())
            {
                gzip.CopyTo(output);
                return output.ToArray();
            }
        }

        private byte[] DecryptData(byte[] encryptedData)
        {
            // Try AES-256 first
            try
            {
                return DecryptWithKeySize(encryptedData, 32);
            }
            catch
            {
                // Try AES-128
                try
                {
                    return DecryptWithKeySize(encryptedData, 16);
                }
                catch
                {
                    throw new Exception("Failed to decrypt data with both AES-128 and AES-256");
                }
            }
        }

        private byte[] DecryptWithKeySize(byte[] encryptedData, int keySize)
        {
            using (var aes = Aes.Create())
            {
                byte[] key = new byte[keySize];
                byte[] iv = new byte[16];
                
                Array.Copy(encryptedData, 0, key, 0, keySize);
                Array.Copy(encryptedData, keySize, iv, 0, 16);
                
                aes.Key = key;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (var decryptor = aes.CreateDecryptor())
                using (var ms = new MemoryStream())
                {
                    using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Write))
                    {
                        cs.Write(encryptedData, keySize + 16, encryptedData.Length - keySize - 16);
                        cs.FlushFinalBlock();
                    }
                    return ms.ToArray();
                }
            }
        }

        private void LogAdd(string message)
        {
            Dispatcher.Invoke(() =>
            {
                LogList.Items.Add($"[{DateTime.Now:HH:mm:ss}] {message}");
                LogList.ScrollIntoView(LogList.Items[LogList.Items.Count - 1]);
                ProgressText.Text = message;
                LogMessage(message);
            });
        }
    }
}
