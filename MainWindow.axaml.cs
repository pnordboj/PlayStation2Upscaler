using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PCSX2Upscaler
{
    public partial class MainWindow : Window
    {
        private readonly ConcurrentQueue<string> fileQueue = new ConcurrentQueue<string>();
        private readonly HashSet<string> processedFiles = new HashSet<string>();
        private FileSystemWatcher fileWatcher;
        private CancellationTokenSource cts;
        private bool isUpscaling = false;
        private bool isPaused = false;
        private int totalFiles = 0;
        private int processedFileCount = 0;
        private DispatcherTimer timer;

        private const int TerminalHeight = 200;
        private const int NormalHeight = 600;
        private const int ScaleFactor = 2;
        private const int NoiseLevel = 1;

        public MainWindow()
        {
            InitializeComponent();
            InitializeSettings();
            LoadSettings();
            InitializeTimer();
            ValidatePrerequisites();
            SearchForWaifu2xExecutable();
        }

        private void InitializeTimer()
        {
            timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            timer.Tick += Timer_Tick;
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            UpdateTimeRemaining();
        }

        private async void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFolderDialog();
            var result = await dialog.ShowAsync(this);

            if (!string.IsNullOrEmpty(result))
            {
                FolderPathTextBox.Text = result;
                StartFileWatcher(result);
                SaveSettings();
                ValidatePrerequisites();
            }
        }

        private async void OutputBrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFolderDialog();
            var result = await dialog.ShowAsync(this);

            if (!string.IsNullOrEmpty(result))
            {
                OutputPathTextBox.Text = result;
                SaveSettings();
                ValidatePrerequisites();
            }
        }

        private async void Waifu2xBrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Select waifu2x-caffe executable",
                Filters = { new FileDialogFilter { Name = "Executable", Extensions = { "exe" } } }
            };
            var result = await dialog.ShowAsync(this);

            if (result != null && result.Length > 0)
            {
                Waifu2xPathTextBox.Text = result[0];
                SaveSettings();
                ValidatePrerequisites();
            }
        }

        private void StartFileWatcher(string folderPath)
        {
            fileWatcher?.Dispose();

            fileWatcher = new FileSystemWatcher(folderPath, "*.png")
            {
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite
            };

            fileWatcher.Created += OnNewFileDetected;
            fileWatcher.Changed += OnNewFileDetected;
            fileWatcher.EnableRaisingEvents = true;
        }

        private void OnNewFileDetected(object sender, FileSystemEventArgs e)
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (processedFiles.Contains(e.FullPath) || fileQueue.Contains(e.FullPath)) return;

                fileQueue.Enqueue(e.FullPath);
                totalFiles++;
                UpdateProgressDisplay();
            });
        }

        private async void UpscaleButton_Click(object sender, RoutedEventArgs e)
        {
            if (isUpscaling) return;

            if (!ValidateFolders(out string folderPath, out string outputPath, out string waifu2xPath)) return;

            await EnqueueInitialFiles(folderPath);

            totalFiles = fileQueue.Count;
            processedFileCount = 0;

            StatusTextBlock.Text = "Estimating storage requirements...";

            long estimatedSize = await Task.Run(() => EstimateStorage(fileQueue.ToArray()));
            StorageTextBlock.Text = $"Estimated storage required: {estimatedSize / (1024 * 1024):N2} MB";
            StatusTextBlock.Text = "Upscaling textures...";
            ProgressBar.Value = 0;
            UpdateProgressDisplay();

            cts = new CancellationTokenSource();
            isUpscaling = true;
            SetButtonStates(true);

            timer.Start();
            Stopwatch stopwatch = Stopwatch.StartNew();
            await Task.Run(() => ContinuousUpscaleTexturesWithWaifu2x(outputPath, waifu2xPath, stopwatch, cts.Token));

            stopwatch.Stop();
            timer.Stop();
            isUpscaling = false;
            SetButtonStates(false);

            StatusTextBlock.Text = "Upscaling complete!";
            Process.Start("explorer.exe", outputPath);
        }

        private async Task EnqueueInitialFiles(string folderPath)
        {
            var initialFiles = Directory.GetFiles(folderPath, "*.png", SearchOption.AllDirectories);

            foreach (var file in initialFiles)
            {
                if (!processedFiles.Contains(file))
                {
                    fileQueue.Enqueue(file);
                }
            }
        }

        private bool ValidateFolders(out string folderPath, out string outputPath, out string waifu2xPath)
        {
            folderPath = FolderPathTextBox.Text;
            outputPath = OutputPathTextBox.Text;
            waifu2xPath = Waifu2xPathTextBox.Text;

            if (string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath))
            {
                StatusTextBlock.Text = "Please select a valid folder.";
                return false;
            }

            if (!Directory.Exists(outputPath))
            {
                StatusTextBlock.Text = "Please select a valid output folder.";
                return false;
            }

            if (string.IsNullOrEmpty(waifu2xPath) || !File.Exists(waifu2xPath))
            {
                StatusTextBlock.Text = "Please select a valid waifu2x-caffe executable.";
                return false;
            }

            return true;
        }

        private async void StopButton_Click(object sender, RoutedEventArgs e)
        {
            cts?.Cancel();
            isUpscaling = false;
            StatusTextBlock.Text = "Upscaling stopped.";
            StopButton.IsEnabled = false;
        }

        private long EstimateStorage(string[] files)
        {
            long totalSize = files.Sum(file => new FileInfo(file).Length);
            return (long)(totalSize * 2.5); // More accurate estimate for upscaling with enhancement
        }

        private void ContinuousUpscaleTexturesWithWaifu2x(string outputPath, string waifu2xPath, Stopwatch stopwatch, CancellationToken token)
        {
            while (!token.IsCancellationRequested && !isPaused)
            {
                if (!fileQueue.TryDequeue(out var file)) continue;

                if (processedFiles.Contains(file)) continue; // Skip already processed file

                string outputFile = Path.Combine(outputPath, Path.GetFileName(file));
                if (File.Exists(outputFile))
                {
                    processedFiles.Add(file);
                    processedFileCount++;
                    Dispatcher.UIThread.Post(UpdateProgressDisplay);
                    continue; // Skip already upscaled image
                }

                if (ProcessFileWithWaifu2x(file, outputFile, waifu2xPath))
                {
                    processedFiles.Add(file);
                    processedFileCount++;
                }

                double progress = (processedFileCount / (double)totalFiles) * 100;
                Dispatcher.UIThread.Post(() =>
                {
                    ProgressBar.Value = progress;
                    UpdateProgressDisplay();
                });
            }

            Dispatcher.UIThread.Post(() =>
            {
                StatusTextBlock.Text = isUpscaling ? "Upscaling complete!" : "Upscaling stopped.";
                Process.Start("explorer.exe", outputPath);
            });
        }

        private bool ProcessFileWithWaifu2x(string inputFile, string outputFile, string waifu2xPath)
        {
            string arguments = $"-i \"{inputFile}\" -o \"{outputFile}\" -s {ScaleFactor} -m noise_scale -n {NoiseLevel}";

            var startInfo = new ProcessStartInfo
            {
                FileName = waifu2xPath,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            using var process = new Process { StartInfo = startInfo };
            process.Start();

            Task.WaitAll(
                LogStreamAsync(process.StandardOutput),
                LogStreamAsync(process.StandardError)
            );

            process.WaitForExit();

            if (process.ExitCode == 0)
            {
                Dispatcher.UIThread.Post(() => LogToTerminal($"Successfully processed: {inputFile}"));
                return true;
            }

            Dispatcher.UIThread.Post(() => LogToTerminal($"Error processing: {inputFile}"));
            return false;
        }

        private async Task LogStreamAsync(StreamReader stream)
        {
            string line;
            while ((line = await stream.ReadLineAsync()) != null)
            {
                LogToTerminal(line);
            }
        }

        private void LogToTerminal(string message)
        {
            if (!string.IsNullOrEmpty(message))
            {
                Dispatcher.UIThread.Post(() =>
                {
                    TerminalTextBox.Text += message + Environment.NewLine;
                    TerminalTextBox.CaretIndex = TerminalTextBox.Text.Length;
                });
            }
        }

        private void UpdateProgressDisplay()
        {
            ProgressTextBlock.Text = $"Processing files: {processedFileCount}/{totalFiles} {(processedFileCount / (double)totalFiles) * 100:0.00}%";
        }

        private void UpdateTimeRemaining()
        {
            double elapsedSeconds = timer.Interval.TotalSeconds;
            double estimatedTotalTime = (elapsedSeconds / processedFileCount) * totalFiles;
            double remainingSeconds = estimatedTotalTime - elapsedSeconds;

            TimeSpan remainingTime = TimeSpan.FromSeconds(remainingSeconds);
            TimeTextBlock.Text = $"Estimated time remaining: {remainingTime:hh\\:mm\\:ss}";
        }

        private void ShowTerminalCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            TerminalScrollViewer.IsVisible = true;
            this.Height += TerminalHeight;
        }

        private void ShowTerminalCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            TerminalScrollViewer.IsVisible = false;
            this.Height -= TerminalHeight;
        }

        private void SetButtonStates(bool isUpscaling)
        {
            StopButton.IsEnabled = isUpscaling;
        }

        private void InitializeSettings()
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            if (config.AppSettings.Settings["FolderPath"] == null)
            {
                config.AppSettings.Settings.Add("FolderPath", string.Empty);
            }
            if (config.AppSettings.Settings["OutputPath"] == null)
            {
                config.AppSettings.Settings.Add("OutputPath", string.Empty);
            }
            if (config.AppSettings.Settings["Waifu2xPath"] == null)
            {
                config.AppSettings.Settings.Add("Waifu2xPath", string.Empty);
            }
            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("userSettings");
        }

        private void SaveSettings()
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            config.AppSettings.Settings["FolderPath"].Value = FolderPathTextBox.Text;
            config.AppSettings.Settings["OutputPath"].Value = OutputPathTextBox.Text;
            config.AppSettings.Settings["Waifu2xPath"].Value = Waifu2xPathTextBox.Text;
            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("userSettings");
        }

        private void LoadSettings()
        {
            FolderPathTextBox.Text = ConfigurationManager.AppSettings["FolderPath"];
            OutputPathTextBox.Text = ConfigurationManager.AppSettings["OutputPath"];
            Waifu2xPathTextBox.Text = ConfigurationManager.AppSettings["Waifu2xPath"];
        }

        private void ValidatePrerequisites()
        {
            UpscaleButton.IsEnabled = !string.IsNullOrEmpty(FolderPathTextBox.Text) &&
                                      !string.IsNullOrEmpty(OutputPathTextBox.Text) &&
                                      !string.IsNullOrEmpty(Waifu2xPathTextBox.Text);
        }

        private void SearchForWaifu2xExecutable()
        {
            string[] searchPaths = { @"C:\Program Files\", @"C:\Program Files (x86)\", @"C:\Users\" };
            string waifu2xExecutable = "waifu2x-caffe-cui.exe";

            foreach (var path in searchPaths)
            {
                try
                {
                    var files = Directory.GetFiles(path, waifu2xExecutable, SearchOption.AllDirectories);
                    if (files.Length > 0)
                    {
                        Waifu2xPathTextBox.Text = files[0];
                        SaveSettings();
                        ValidatePrerequisites();
                        break;
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    // Skip directories where access is denied
                }
                catch (Exception ex)
                {
                    // Handle other exceptions (e.g., IO exceptions) as needed
                    Debug.WriteLine($"Error searching for waifu2x executable: {ex.Message}");
                }
            }
        }
    }
}
