﻿using System;
using System.Windows;
using System.Linq;
using System.IO;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Windows.Media.Animation;
using System.Diagnostics;
using System.Windows.Threading;
using System.Windows.Media.Imaging;
using log4net;
using System.Reflection;
using System.Net;
using SharpCompress.Readers;
using SharpCompress.Archives.Zip;
using SharpCompress.Archives;
using System.Threading.Tasks;
using System.Management;
using System.Text.RegularExpressions;
using System.Threading;
using System.Text;
using System.Windows.Media;

//Util class for all methods related to the grid, installation and general flow of the layout

namespace Higurashi_Installer_WPF
{
    static class Utils
    {
        public static Regex aria2cValidationRegex = new Regex(@"Checksum.*\s(.*)\/(.*)\((\d*)%\)", RegexOptions.IgnoreCase);
        public static Process process;
        public static DataReceivedEventHandler processEventHandler;

        private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        //Job Management object which ensures batch files are terminated when this program terminates - allowed to be NULL on computers with old .net versions
        private static JobManagement.Job job;

        public static uint HandleDataErrorCount = 0; //This variable is used to rate limit the amount of errors that are recorded from the HandleDataError function

        public static void InitJobManagement()
        {
            job = new JobManagement.Job();
        }

        //Reset the path in case the user changes chapters
        public static void ResetPath(MainWindow window, Boolean ChangedChapter)
        {
            if (ChangedChapter)
            {
                _log.Info("Changed chapter");
                window.PathText.Text = "Insert install folder for the chapter";
            }

            window.TextWarningPath_SetTextInformation($"Please select game folder for '{window.patcher.FriendlyName}'\n" +
                $"Folder should contain '{window.patcher.GetFriendlyExeNamesList()}'");
            window.BtnInstall.IsEnabled = false;
            //   window.BtnUninstall.IsEnabled = true;
        }

        public static void ResetDropBox(PatcherPOCO patcher)
        {
            patcher.IsVoiceOnly = false;
            patcher.IsCustom = false;
            patcher.IsFull = false;
        }

        public static void DelayAction(int millisecond, Action action)
        {
            var timer = new DispatcherTimer();
            timer.Tick += delegate

            {
                action.Invoke();
                timer.Stop();
            };

            timer.Interval = TimeSpan.FromMilliseconds(millisecond);
            timer.Start();
        }

        //Main Util method to resize the window
        public static void ResizeWindow(MainWindow window)
        {

            if (window.ActualWidth < 950)
            {
                _log.Info("Resizing window");
                window.AnimateWindowSize(window.ActualWidth + 500);
                if (window.InstallGrid.Visibility.Equals(Visibility.Collapsed))
                {
                    window.InstallGrid.Visibility = Visibility.Visible;

                }
            }
        }

        /*Checks if there's something informed in the path component before switching grids
        No need to check if the path is valid because the method below already does just that.
        Also disables the icon grid so the user can't change chapters after this point*/
        public static void CheckValidFilePath(MainWindow window, PatcherPOCO patcher)
        {
            if (window.PathText.Text != "Insert install folder for the chapter")
            {
                _log.Info("Confirmation grid");
                window.InstallGrid.Visibility = Visibility.Collapsed;
                window.ConfirmationGrid.Visibility = Visibility.Visible;
                window.LockIconGrid();
                ConstructPatcher(window, patcher);
            }
            else
            {
                window.TextWarningPath_SetTextError("Please select a folder before installing!");
            }
        }

        /// <summary>
        /// Gets the root folder of your steam installation from the registry
        /// the returned path may or may not be valid - make sure to validate it yourself!
        /// </summary>
        /// <param name="steamPath"></param>
        /// <returns></returns>
        private static bool GetSteamappsCommonFolderFromRegistry(out string steamPath)
        {
            steamPath = String.Empty;

            try
            {
                //steamRootPath is like C:\games\Steam
                string steamRootPath = Microsoft.Win32.Registry.GetValue(
                    keyName: @"HKEY_CURRENT_USER\Software\Valve\Steam", 
                    valueName: "SteamPath", 
                    defaultValue:String.Empty) as string;

                //steamPath is like C:\games\Steam\steamapps\common\
                steamPath = Path.Combine(steamRootPath, @"steamapps\common\")
                    .Replace("/", "\\"); //convert any forward slashes to backslashes to fix various issues...
            }
            catch(Exception e)
            {
                _log.Error($"Couldn't retrieve steam path from registry: {e}");
                return false;
            }

            return steamPath != String.Empty;            
        }

        /// <summary>
        /// Attempts to Auto-detect steam game install path. On failure, no action is taken.
        /// On success, still does the regular validation of the path as if path was selcted manually
        /// </summary>
        public static bool AutoDetectGamePathAndValidate(MainWindow window, PatcherPOCO patcher)
        {
            try
            {
                if (GetSteamappsCommonFolderFromRegistry(out string steamPath))
                {
                    foreach (string gameFolder in Directory.EnumerateDirectories(steamPath))
                    {
                        if(CheckValidFileExists(gameFolder, patcher))
                        {
                            ValidateFilePath(window, patcher, gameFolder);
                            return true;
                        }
                    }
                }
            }
            catch(Exception e)
            {
                _log.Info($"Couldn't auto-detect path: {e}");
            }

            return false;
        }

        /// <summary>
        /// Show a FolderPicker Dialog for the user to select the game directory, then validates it is correct
        /// </summary>
        public static void AskFilePathAndValidate(MainWindow window, PatcherPOCO patcher)
        {
            _log.Info("Checking if path is valid");
            var dialog = new CommonOpenFileDialog();
            dialog.IsFolderPicker = true;
            CommonFileDialogResult result = dialog.ShowDialog();
            if (result.ToString() == "Ok")
            {
                ValidateFilePath(window, patcher, dialog.FileName);
            }
        }

        //Validates the path the user informs in the install 
        private static void ValidateFilePath(MainWindow window, PatcherPOCO patcher, string gamePath)
        {
            window.PathText.Text = gamePath;
            if (!CheckValidFileExists(gamePath, patcher))
            {
                _log.Info("Wrong path selected");
                window.TextWarningPath_SetTextError($"Path is INVALID for '{patcher.FriendlyName}'\n" +
                    $"(Couldn't find {patcher.GetFriendlyExeNamesList()})");
                window.BtnInstall.IsEnabled = false;
                //    window.BtnUninstall.IsEnabled = false;
            }
            else
            {
                //installer won't run properly if install location is on a different drive to the installer.
                //Just warn the user to move the install file to the same drive.
                string currentDrive = Path.GetPathRoot(Environment.CurrentDirectory).ToLower();
                string selectedPathDrive = Path.GetPathRoot(gamePath).ToLower();
                if (currentDrive != selectedPathDrive)
                {
                    MessageBox.Show($"Warning!! The installer is on a different drive to the game location!\n\n" +
                        $"Please move the installer to the {selectedPathDrive} drive, and start the installer again!");
                    _log.Info($"Installer on different drive inst: {currentDrive} game: {selectedPathDrive}");
                }

                _log.Info("Correct path selected");
                window.TextWarningPath_SetTextSuccess($"Path is valid for '{patcher.FriendlyName}'\n" +
                    $"(Found {patcher.GetFriendlyExeNamesList()})");
                window.BtnInstall.IsEnabled = true;
                //   window.BtnUninstall.IsEnabled = true;
            }
        }

        public static Boolean CheckValidFileExists(String path, PatcherPOCO patcher)
        {
            foreach (string ExeName in patcher.GetExeNames())
            {
                string file = path + "\\" + ExeName;
                if (File.Exists(file) || Directory.Exists(file))
                    return true;
            }

            return false;
        }

        //Responsible for changing the layout depending on what option is selected in the combo
        public static void InstallComboChoose(MainWindow window, PatcherPOCO patcher)
        {
            switch (window.InstallCombo.SelectedItem.ToString().Split(new string[] { ": " }, StringSplitOptions.None).Last())
            {
                case "Full":
                    ResetDropBox(patcher);
                    patcher.IsFull = true;
                    TreatCheckboxes(window, false);
                    break;
                case "Custom":
                    ResetDropBox(patcher);
                    patcher.IsCustom = true;
                    TreatCheckboxes(window, true);
                    break;
                case "Voice only":
                    ResetDropBox(patcher);
                    patcher.IsVoiceOnly = true;
                    TreatCheckboxes(window, false);
                    break;
            }
        }

        public static void TreatCheckboxes(MainWindow window, Boolean IsCustom)
        {
            if (IsCustom)
            {
                window.ChkPatch.Visibility = Visibility.Visible;
                window.ChkPS3.Visibility = Visibility.Visible;
                window.ChkSteamSprites.Visibility = Visibility.Visible;
                window.ChkUI.Visibility = Visibility.Visible;
                window.ChkVoices.Visibility = Visibility.Visible;
            }
            else
            {
                window.ChkPatch.Visibility = Visibility.Collapsed;
                window.ChkPS3.Visibility = Visibility.Collapsed;
                window.ChkSteamSprites.Visibility = Visibility.Collapsed;
                window.ChkUI.Visibility = Visibility.Collapsed;
                window.ChkVoices.Visibility = Visibility.Collapsed;
            }
        }

        /* Populates the object for installation
          And fills the list in the grid for user confirmation */
        public static void ConstructPatcher(MainWindow window, PatcherPOCO patcher)
        {
            _log.Info("Constructing the patcher");
            string tempFolder = window.PathText.Text + "\\temp";
            Directory.CreateDirectory(tempFolder);
            patcher.InstallPath = tempFolder;
            patcher.IsBackup = (Boolean)window.ChkBackup.IsChecked;
            patcher.InstallUpdate = "Installation";

            window.List1.Content = "Chapter: " + patcher.FriendlyName;
            window.List2.Content = "Path: " + window.PathText.Text;
            window.List3.Content = "Process: Installation";
            window.List5.Content = "Backup: " + (patcher.IsBackup ? "Yes" : "No");

            if (patcher.IsCustom)
            {
                patcher.InstallType = "Custom";
                window.List4.Content = "Installation Type: Custom";
                patcher.BatName = "custom.bat";
            }
            else if (patcher.IsFull)
            {
                patcher.InstallType = "Full";
                window.List4.Content = "Installation Type: Full";
                patcher.BatName = "install.bat";
            }
            else
            {
                patcher.InstallType = "Voice Only";
                window.List4.Content = "Installation Type: Voice Only";
                patcher.BatName = "voice.bat";
            }

            //For the log
            PatcherInfo(patcher);
        }

        public static void PatcherInfo(PatcherPOCO patcher)
        {
            _log.Info("Chapter: " + patcher.ChapterName);
            _log.Info("Install path: " + patcher.InstallPath);

        }

        //Only the installer related methods from here

        /// <summary>
        /// Extract a zip file, overwriting if files already exist. Does not handle multi-part archives.
        /// </summary>
        /// <param name="inputArchivePath">Full path to the archive to be extracted</param>
        /// <param name="outputDirectory">Folder where files should be extracted to</param>
        public static void ExtractZipArchive(string inputArchivePath, string outputDirectory)
        {
            using (var archive = ZipArchive.Open(inputArchivePath))
            {
                foreach (ZipArchiveEntry entry in archive.Entries.Where(entry => !entry.IsDirectory))
                {
                    entry.WriteToDirectory(outputDirectory, new ExtractionOptions()
                    {
                        ExtractFullPath = true,
                        Overwrite = true
                    });
                }
            }
        }

        //downloads and extracts the resources of the temp folder
        public static async Task<bool> DownloadResources(MainWindow window, PatcherPOCO patcher)
        {
            //Note: this function captures the 'window' variable from outer scope.
            void DownloadResourcesProgressCallback(object sender, DownloadProgressChangedEventArgs e, String descriptiveFileName)
            {
                window.Dispatcher.Invoke(() =>
                {
                    InstallerProgressBar(window,
                        $"Downloading {descriptiveFileName}...",
                        $"{e.BytesReceived / 1e6:F2}/{e.TotalBytesToReceive / 1e6:F2}MB",
                        $"{e.ProgressPercentage}%", e.ProgressPercentage);
                });
            }

            void InstallBatCallback(object sender, DownloadProgressChangedEventArgs e) => DownloadResourcesProgressCallback(sender, e, patcher.BatName);
            void ResourcesZipCallback(object sender, DownloadProgressChangedEventArgs e) => DownloadResourcesProgressCallback(sender, e, "resources.zip");

            try
            {
                _log.Info("Downloading install bat and creating temp folder");
                using (var client = new WebClient())
                {
                    _log.Info("Downloading " + patcher.BatName);
                    client.DownloadProgressChanged += InstallBatCallback;
                    await client.DownloadFileTaskAsync("https://raw.githubusercontent.com/07th-mod/resources/master/" + patcher.ChapterName + "/" + patcher.BatName, patcher.InstallPath + "\\" + patcher.BatName);

                    _log.Info("Downloading resources.zip");
                    client.DownloadProgressChanged -= InstallBatCallback;
                    client.DownloadProgressChanged += ResourcesZipCallback;
                    await client.DownloadFileTaskAsync("http://07th-mod.com/dependencies.zip", patcher.InstallPath + "\\resources.zip");
                }
            }
            catch (WebException webException)
            {
                string friendlyMessage = "Couldn't reach 07th mod server to download patch files.\n\n" +
                    "Note that we have blocked Japan from downloading (VPNs are compatible with this installer, however)\n\n" +
                    $"[{webException.Message}]\nClick 'Show Detailed Progress' to view the full error.";
                _log.Error(friendlyMessage + webException);
                MessageBox.Show(friendlyMessage);
                return false;
            }
            catch (Exception error)
            {
                string errormsg = "Couldn't download resources for installer: " + error;
                MessageBox.Show(errormsg);
                _log.Error(errormsg);
                return false;
            }


            //extracting can fail if the files are still in use or if the zip file is corrupted
            _log.Info("Extracting resources");
            try
            {
                ExtractZipArchive(Path.Combine(patcher.InstallPath, "resources.zip"), patcher.InstallPath);
            }
            catch (System.IO.IOException error)
            {
                string errormsg = "Couldn't extract files - probably temp/aria2c.exe or temp/7zip.exe are in use: " + error;
                MessageBox.Show(errormsg);
                _log.Error(errormsg);
                return false;
            }
            catch (Exception error)
            {
                string errormsg = "An unexpected exception occured while extracting the zip file: " + error;
                MessageBox.Show(errormsg);
                _log.Error(errormsg);
                return false;
            }

            _log.Info("Downloaded and extracted resources successfully");
            return true;
        }

        //continously read lines from the stream until it appears empty
        private static void ReadlinesFromStreamUntilEmpty(StreamReader sr, MainWindow window)
        {
            while (true)
            {
                string line = sr.ReadLine();

                if (line == null)
                {
                    return;
                }

                HandleData_LowLevel(line, window);
            }
        }

        //if all lines appear read, wait 2 seconds before trying again
        private static void ReadlinesFromStreamUntilProcessExits(StreamReader sr, MainWindow window)
        {
            bool shouldExit = false;
            //keep trying to read from the stream until process terminates
            while (!shouldExit)
            {
                ReadlinesFromStreamUntilEmpty(sr, window);

                //quit once process exits
                shouldExit = process != null && process.HasExited;

                Thread.Sleep(2000);
            }

            //do one final read incase the process exited but the data wasn't yet written
            //this will occur 2 seconds after the process exits
            ReadlinesFromStreamUntilEmpty(sr, window);

        }

        //When executing the installer in 'shellExecute' mode, this function is used to filter the Aria2c log and populate the main window
        //Poll the file for new lines
        //C#'s stream reader makes this easy - if a new line is not ready/partially written
        //it will return null. Also, since the thread never returns, the position in the file 
        //is maintained automatically.
        private static void TryWatch(string changedFilePath, MainWindow window)
        {
            Thread fileWatcherThread = new Thread(() =>
            {
                bool fileOpened = false;

                //keep trying to open the file (incase it hasn't been created yet)
                while (!fileOpened)
                {
                    try
                    {
                        using (var fs = new FileStream(changedFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        using (var sr = new StreamReader(fs, Encoding.UTF8))
                        {
                            fileOpened = true;

                            ReadlinesFromStreamUntilProcessExits(sr, window);
                        }
                    }
                    catch (Exception e)
                    {
                        _log.Warn($"Trying to open {changedFilePath} in order to observe the bat output...");
                    }

                    Thread.Sleep(5000);
                }
            });

            fileWatcherThread.IsBackground = true; //required so that thread exits if child exits
            fileWatcherThread.Start();
        }

        //Run once the bat has fully finished.
        public static void OnProcessExitedCallback(object sender, System.EventArgs e)
        {
            _log.Info($"Process has Finished with exit code {process.ExitCode}!");
        }

        /*It's dangerous to go alone, take this 
         https://msdn.microsoft.com/en-us/library/system.diagnostics.processstartinfo.redirectstandardoutput.aspx
         https://stackoverflow.com            
         */
        public static void runInstaller(MainWindow window, string bat, string dir)
        {
            Directory.SetCurrentDirectory(dir);

            //Force the batch file to CRLF format before executing it
            if (StandaloneUtils.BatchFileModifier.ForceWindowsLineEndings(bat))
            {
                _log.Warn("Batch file does not have windows line endings! Installation will continue - will try to fix automatically...");
            }

            //Apply ipv4/v6 fix
            if(window.ChkDisableIPV6.IsChecked == true)
            {
                StandaloneUtils.BatchFileModifier.DisableIPV6(bat);
            }

            process = new Process();
            process.EnableRaisingEvents = true; //you must set this true for any events to be raised \ -_- /
            process.Exited += new EventHandler(OnProcessExitedCallback);

            switch (window.patcher.BatchFileExecutionMode)
            {
                case PatcherPOCO.BatchFileExecutionModeEnum.NormalWithLogging:
                    _log.Info("Initializing cmd process");
                    //need to keep a reference to the process so we can terminate it (see KillBatchFile function)
                    process.StartInfo.CreateNoWindow = true;
                    process.StartInfo.FileName = bat;
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.RedirectStandardOutput = true;

                    _log.Info($">> Running [{process.StartInfo.FileName} {process.StartInfo.Arguments}]");
                    //need to keep a reference to the event handler so we can remove it (see KillBatchFile function)
                    processEventHandler = (sender, args) => HandleData(process, args, window);
                    process.OutputDataReceived += processEventHandler;

                    process.Start();
                    process.BeginOutputReadLine();
                    break;

                case PatcherPOCO.BatchFileExecutionModeEnum.ShellExecuteWithLogging:
                    string install_bat_log_filepath = "seventh_mod_batch_file_log.txt";

                    _log.Info("Initializing cmd process");
                    process.StartInfo.FileName = "cmd.exe";
                    process.StartInfo.Arguments = $@"/C {bat} > {install_bat_log_filepath}"; //stdout of batch file redirected to log file
                    process.StartInfo.UseShellExecute = true;
                    process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden; //Hide the cmd window. Can't use "CreateNoWindow" as it is ignored when running with shellExecute.
                    process.StartInfo.WorkingDirectory = dir;
                    _log.Info($">> Running [{process.StartInfo.FileName} {process.StartInfo.Arguments}]");
                    _log.Info($">> Working Directory is [{process.StartInfo.WorkingDirectory}]");
                    _log.Info($">> bat log file will be placed in [{Path.Combine(dir, install_bat_log_filepath)}]");

                    //delete old log file if it already exists
                    try
                    {
                        if (File.Exists(install_bat_log_filepath))
                            File.Delete(install_bat_log_filepath);
                    }
                    catch (Exception e)
                    {
                        _log.Warn("Couldn't delete old log file when using shell execute!" + e.ToString());
                    }

                    process.Start();
                    TryWatch(install_bat_log_filepath, window);
                    break;

                default:
                    _log.Error("Error - unhandled batch file execution mode - installation aborted");
                    return;
            }

            //This ensures that the .batch file process will be killed if this installer closes
            //If the installer unexpectedly closes, aria2C etc. will download in the background and the
            //only way to close it is to find the rogue .batch file and kill it in task manager, along with aria2C.
            if (job != null)
                job.AddProcess(process.Id);
        }

        //When executing the installer in 'normal' mode, this function is used to filter the Aria2c log and populate the main window
        public static void HandleData(Process sendingProcess, DataReceivedEventArgs outLine, MainWindow window)
        {
            HandleData_LowLevel(outLine.Data, window);
        }

        private static void HandleData_LowLevel(string e, MainWindow window)
        {
            //add general try-catch here as we don't want the program to crash just because we couldn't update the progress bar!
            try
            {
                _log.Info(e);

                //Main part with the download speed, ETA, etc
                if (e != null && e.StartsWith("["))
                {
                    if (!e.Contains("0B/0B") && e.Contains("ETA"))
                    {
                        string filesize = e.Split(new string[] { " " }, StringSplitOptions.None).GetValue(1).ToString();
                        string downloadSpeed = e.Split(new string[] { " " }, StringSplitOptions.None).GetValue(3).ToString().Replace("DL:", "");
                        string timeRemaining = e.Split(new string[] { " " }, StringSplitOptions.None).Last().Replace("ETA:", "Time Remaining:").Replace("]", "");

                        string progress = filesize.Split(new string[] { "(" }, StringSplitOptions.None).Last();
                        double progressValue = Convert.ToDouble(progress.Split(new string[] { "%" }, StringSplitOptions.None).First());
                        window.Dispatcher.Invoke(() =>
                        {
                            InstallerProgressBar(window, filesize, downloadSpeed, timeRemaining, progressValue);

                        });

                    }
                    else if (e.Contains("Checksum")) // Attempts to match: "[#6c27a8 1.4GiB/1.4GiB(100%) CN:0] [Checksum:#6c27a8 732MiB/1.4GiB(48%)]"
                    {
                        try
                        {
                            MatchCollection matches = aria2cValidationRegex.Matches(e);
                            if (matches.Count == 1)
                            {
                                GroupCollection groups = matches[0].Groups;
                                if (groups.Count == 4)
                                {
                                    string amountVerified = groups[1].Value;
                                    string totalFileSize = groups[2].Value;
                                    double.TryParse(groups[3].Value, out double percentageComplete);
                                    window.Dispatcher.Invoke(() =>
                                    {
                                        InstallerProgressBar(window, "Verifying...", $"{amountVerified}/{totalFileSize}", $"{percentageComplete}%", percentageComplete);
                                    });
                                }
                            }
                        }
                        catch { } //even if something goes wrong, it's just a status update, so doesn't really matter.
                    }
                    else if (!e.Contains("ETA"))
                    {
                        window.Dispatcher.Invoke(() =>
                        {
                            InstallerProgressMessages(window, "Finishing downloading File...", 100);
                        });
                    }
                }

                //Especific filterings for the other parts of the installer
                if (e != null && e.StartsWith("Downloading"))
                {
                    window.Dispatcher.Invoke(() =>
                    {
                        InstallerPatchMessage(window, e);
                    });
                }

                if (e != null && e.Contains("All done, finishing in three seconds"))
                {
                    window.Dispatcher.Invoke(() =>
                    {
                        InstallerProgressMessages(window, "Install Complete!", 100);
                        InstallerCompleteMessage(window);
                    });
                }

                if (e != null && e.Contains("Extracting files"))
                {
                    window.Dispatcher.Invoke(() =>
                    {
                        InstallerProgressMessages(window, "Extracting and installing files..", 100);
                    });
                }

                if (e != null && e.Contains("Extracting archive:"))
                {
                    window.Dispatcher.Invoke(() =>
                    {
                        ExtractingMessages(window, e);
                    });
                }

                if (e != null && e.Contains("Moving folders"))
                {
                    window.Dispatcher.Invoke(() =>
                    {
                        ExtractingMessages(window, e);
                    });
                }
            }
            catch (Exception exception)
            {
                if (HandleDataErrorCount++ < 10)
                {
                    _log.Error("(HandleData) Error while processing bat output: " + exception.ToString());
                }
                else
                {
                    _log.Error($"(HandleData) Error while processing bat output: ({HandleDataErrorCount} errors)");
                }
            }
        }

        public static void InstallerProgressBar(MainWindow window, String filesize, String speed, String time, double progress)
        {
            window.InstallLabel.Content = filesize + " - " + speed + " - " + time;
            window.InstallBar.Value = progress;
        }

        public static void InstallerProgressMessages(MainWindow window, string message, double progress)
        {
            window.InstallLabel.Content = message;
            window.InstallBar.Value = progress;
            if (message.Contains("Extracting and installing files.."))
            {
                _log.Info("Extracting and installing files..");
                window.InstallLabelPatch3.Content = "Downloading patch... (Done)";
            }

        }

        public static void InstallerPatchMessage(MainWindow window, String message)
        {
            if (message.Contains("Downloading graphics patch"))
            {
                _log.Info("Started downloading graphic patch");
                window.InstallCard1.Visibility = Visibility.Visible;
                window.InstallLabelPatch1.Visibility = Visibility.Visible;
            }
            else if (message.Contains("Downloading voice"))
            {
                _log.Info("Started downloading voice patch");
                window.InstallCard2.Visibility = Visibility.Visible;
                window.InstallLabelPatch2.Visibility = Visibility.Visible;
                window.InstallLabelPatch1.Content = "Downloading graphics patch... (Done)";
            }
            else
            {
                _log.Info("Started downloading patch");
                window.InstallCard3.Visibility = Visibility.Visible;
                window.InstallLabelPatch3.Visibility = Visibility.Visible;
                window.InstallLabelPatch2.Content = "Downloading voice patch... (Done)";
            }
        }

        public static void InstallerCompleteMessage(MainWindow window)
        {
            _log.Info("Finishing installation");
            window.InstallerText.Text = "Installation Complete";
            window.InstallerText.Foreground = Brushes.Lime;
            window.InstallLabel.Content = "";
            window.ExtractLabel.Content = "";
            window.BtnInstallerFinish.Visibility = Visibility.Visible;
        }

        public static void ExtractingMessages(MainWindow window, String message)
        {
            if (message.Contains("Moving folders"))
            {
                _log.Info("Moving folders");
                window.ExtractLabel.Content = "Moving files...   (This may take a while)";
            }
            else
            {
                _log.Info(message);
                window.ExtractLabel.Content = message;
                if (window.InstallBar.Value == 100)
                {
                    window.InstallBar.Value = 0;
                }
                window.InstallBar.Value = window.InstallBar.Value + 20;
            }
        }

        //Reset the window back to default
        public static void FinishInstallation(MainWindow window)
        {
            _log.Info("Reseting window back to original position");
            window.AnimateWindowSize(window.ActualWidth - 500);
            window.UnlockIcongrid();
            window.EpisodeImage.Visibility = Visibility.Collapsed;
            ResetInstallerGrid(window);
        }

        //Resets the installer grid
        public static void ResetInstallerGrid(MainWindow window)
        {
            _log.Info("Reseting installer grid");
            window.InstallerGrid.Visibility = Visibility.Collapsed;
            window.BtnInstallerFinish.Visibility = Visibility.Collapsed;
            window.InstallBar.Value = 0;

            window.InstallCard1.Visibility = Visibility.Collapsed;
            window.InstallLabelPatch1.Visibility = Visibility.Collapsed;

            window.InstallCard2.Visibility = Visibility.Collapsed;
            window.InstallLabelPatch2.Visibility = Visibility.Collapsed;

            window.InstallCard3.Visibility = Visibility.Collapsed;
            window.InstallLabelPatch3.Visibility = Visibility.Collapsed;

            window.InstallerText.Text = "Installation in progress";
            window.InstallLabelPatch1.Content = "Downloading graphics patch...";
            window.InstallLabelPatch2.Content = "Downloading voice patch...";
            window.InstallLabelPatch3.Content = "Downloading patch...";
        }

        //Kills the installer process, otherwise it remains running in the background
        public static void KillBatchFile()
        {
            if (process != null)
            {
                //if the event handler is not removed and the process is killed, an exception occurs.
                if (processEventHandler != null)
                {
                    process.OutputDataReceived -= processEventHandler;
                }

                KillProcessAndChildren(process.Id);
            }
        }

        //Code snippet from https://stackoverflow.com/questions/5901679/kill-process-tree-programmatically-in-c-sharp
        private static void KillProcessAndChildren(int pid)
        {
            // Cannot close 'system idle process'.
            if (pid == 0)
            {
                return;
            }

            ManagementObjectSearcher searcher = new ManagementObjectSearcher("Select * From Win32_Process Where ParentProcessID=" + pid);
            ManagementObjectCollection moc = searcher.Get();

            foreach (ManagementObject mo in moc)
            {
                KillProcessAndChildren(Convert.ToInt32(mo["ProcessID"]));
            }

            try
            {
                Process proc = Process.GetProcessById(pid);
                proc.Kill();
            }
            catch (ArgumentException)
            {
                // Process already exited.
            }
        }
    }

    //This class is responsible for the resize animation
    public static class WindowUtilties
    {
        public static void AnimateWindowSize(this Window target, double newWidth)
        {

            var sb = new Storyboard { Duration = new Duration(new TimeSpan(0, 0, 0, 0, 200)) };

            var aniWidth = new DoubleAnimationUsingKeyFrames();

            aniWidth.Duration = new Duration(new TimeSpan(0, 0, 0, 0, 200));

            aniWidth.KeyFrames.Add(new EasingDoubleKeyFrame(target.ActualWidth, KeyTime.FromTimeSpan(new TimeSpan(0, 0, 0, 0, 00))));
            aniWidth.KeyFrames.Add(new EasingDoubleKeyFrame(newWidth, KeyTime.FromTimeSpan(new TimeSpan(0, 0, 0, 0, 200))));

            Storyboard.SetTarget(aniWidth, target);
            Storyboard.SetTargetProperty(aniWidth, new PropertyPath(Window.WidthProperty));

            sb.Children.Add(aniWidth);

            sb.Begin();

        }
    }
}
