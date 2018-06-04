﻿using log4net;
using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace Higurashi_Installer_WPF
{
    public partial class MainWindow : Window
    {
        private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        /* The patcher is the main object that will be used to store
         * all the information necessary for the installer to operate, making it easier to add new chapters
         * since you just need to populate it and the rest will be automatic. */
        PatcherPOCO patcher = new PatcherPOCO();  

        public MainWindow()
        {
            InitializeComponent();
            GameTypeStackPanel.DataContext = new ExpanderListViewModel(); //Required so that only one expander expands at a time.
            Logger.Setup();
            patcher.IsFull = true;
        }
       
        private void BtnOnikakushi_Click(object sender, RoutedEventArgs e)
        {
            _log.Info("clicked onikakushi");
            Utils.ResizeWindow(this);
            Utils.ResetPath(this, true);

            EpisodeImage.Visibility = Visibility.Visible;
            EpisodeImage.Source = new BitmapImage(new Uri("/Resources/header1.jpg", UriKind.Relative));

            patcher.ChapterName = "onikakushi";
            patcher.DataFolder = "HigurashiEp01_Data";
            patcher.SetExeNames("HigurashiEp01.exe");
            patcher.ImagePath = "/Resources/header1.jpg";
        }

        private void BtnWatanagashi_Click(object sender, RoutedEventArgs e)
        {
            _log.Info("Clicked Watanagashi");
            Utils.ResizeWindow(this);
            Utils.ResetPath(this, true);

            EpisodeImage.Visibility = Visibility.Visible;
            EpisodeImage.Source = new BitmapImage(new Uri("/Resources/header2.jpg", UriKind.Relative));

            patcher.ChapterName = "watanagashi";
            patcher.DataFolder = "HigurashiEp02_Data";
            patcher.SetExeNames("HigurashiEp02.exe");
            patcher.ImagePath = "/Resources/header2.jpg";
        }

        private void BtnTatarigoroshi_Click(object sender, RoutedEventArgs e)
        {
            _log.Info("Clicked Tatarigoroshi");
            Utils.ResizeWindow(this);
            Utils.ResetPath(this, true);

            EpisodeImage.Visibility = Visibility.Visible;
            EpisodeImage.Source = new BitmapImage(new Uri("/Resources/header3.jpg", UriKind.Relative));

            patcher.ChapterName = "tatarigoroshi";
            patcher.DataFolder = "HigurashiEp03_Data";
            patcher.SetExeNames("HigurashiEp03.exe");
            patcher.ImagePath = "/Resources/header3.jpg";
        }

        private void BtnHimatsubushi_Click(object sender, RoutedEventArgs e)
        {
            _log.Info("Clicked Himatsubushi");
            Utils.ResizeWindow(this);
            Utils.ResetPath(this, true);

            EpisodeImage.Visibility = Visibility.Visible;
            EpisodeImage.Source = new BitmapImage(new Uri("/Resources/header4.jpg", UriKind.Relative));

            patcher.ChapterName = "himatsubushi";
            patcher.DataFolder = "HigurashiEp04_Data";
            patcher.SetExeNames("HigurashiEp04.exe");
            patcher.ImagePath = "/Resources/header4.jpg";
        }

        private void BtnMeakashi_Click(object sender, RoutedEventArgs e)
        {
            _log.Info("Clicked Meakashi");
            Utils.ResizeWindow(this);
            Utils.ResetPath(this, true);

            EpisodeImage.Visibility = Visibility.Visible;
            EpisodeImage.Source = new BitmapImage(new Uri("/Resources/header5.jpg", UriKind.Relative));

            patcher.ChapterName = "meakashi";
            patcher.DataFolder = "HigurashiEp05_Data";
            patcher.SetExeNames("HigurashiEp05.exe");
            patcher.ImagePath = "/Resources/header5.jpg";
        }

        private void BtnTsumihoroboshi_Click(object sender, RoutedEventArgs e)
        {
            _log.Info("Clicked Tsumihoroboshi");

            Utils.ResizeWindow(this);
            Utils.ResetPath(this, true);

            EpisodeImage.Visibility = Visibility.Visible;
            EpisodeImage.Source = new BitmapImage(new Uri("/Resources/header6.jpg", UriKind.Relative));

            patcher.ChapterName = "tsumihoroboshi";
            patcher.DataFolder = "HigurashiEp06_Data";
            patcher.SetExeNames("HigurashiEp06.exe");
            patcher.ImagePath = "/Resources/header6.jpg";
        }

        private void BtnConsole_Click(object sender, RoutedEventArgs e)
        {
            _log.Info("Clicked Console Arcs");
            Utils.ResizeWindow(this);
            Utils.ResetPath(this, true);

            EpisodeImage.Visibility = Visibility.Visible;
            EpisodeImage.Source = new BitmapImage(new Uri("/Resources/console.png", UriKind.Relative));

            patcher.ChapterName = "console0arcs";
            patcher.DataFolder = "HigurashiEp04_Data";
            patcher.SetExeNames("HigurashiEp04.exe");
            patcher.ImagePath = "/Resources/console.png";
        }

        private void BtnUminekoQuestion_Click(object sender, RoutedEventArgs e)
        {
            _log.Info("Clicked Umineko Question");
            Utils.ResizeWindow(this);
            Utils.ResetPath(this, true);

            EpisodeImage.Visibility = Visibility.Visible;
            EpisodeImage.Source = new BitmapImage(new Uri(@"/Resources/header_umineko_question.jpg", UriKind.Relative));

            patcher.ChapterName = "umineko-question";
            patcher.DataFolder = ".";        //umineko doesn't have a data folder, just put 'temp' in game directory
            patcher.SetExeNames("Umineko1to4.exe", "Umineko1to4", "Umineko1to4.app");
            patcher.ImagePath = "/Resources/header_umineko_question.jpg";
        }

        private void BtnUminekoAnswer_Click(object sender, RoutedEventArgs e)
        {
            _log.Info("Clicked Umineko Answer");
            Utils.ResizeWindow(this);
            Utils.ResetPath(this, true);

            EpisodeImage.Visibility = Visibility.Visible;
            EpisodeImage.Source = new BitmapImage(new Uri(@"/Resources/header_umineko_answer.jpg", UriKind.Relative));

            patcher.ChapterName = "umineko-answer";
            patcher.DataFolder = ".";        //umineko doesn't have a data folder, just put 'temp' in game directory
            patcher.SetExeNames("Umineko5to8.exe", "umineko5to8", "Umineko5to8.app");
            patcher.ImagePath = "/Resources/header_umineko_answer.jpg";
        }

        private void InstallCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Utils.InstallComboChoose(this, patcher);
        }

        private void BtnInstall_Click(object sender, RoutedEventArgs e)
        {
            Utils.CheckValidFilePath(this, patcher);
        }

        private void ConfirmBack_Click(object sender, RoutedEventArgs e)
        {
            ConfirmationGrid.Visibility = Visibility.Collapsed;
            InstallGrid.Visibility = Visibility.Visible;
            IconGrid.IsEnabled = true;
        }

        private void BtnPath_Click(object sender, RoutedEventArgs e)
        {
            Utils.ValidateFilePath(this, patcher);
        }

        private void BtnInstallerFinish_Click(object sender, RoutedEventArgs e)
        {
            MainImage.Source = null;
            Utils.FinishInstallation(this);
        }

        //Main install logic starts here after the confirmation button is clicked
        private async void Confirm_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _log.Info("Starting the installer");
                ConfirmationGrid.Visibility = Visibility.Collapsed;
                InstallerGrid.Visibility = Visibility.Visible;

                //get the latest .bat from github
                await Utils.DownloadResources(patcher);

                // If you don't do this, the InstallerGrid won't be visible
                Utils.DelayAction(5000, new Action(() =>
                {
                    //Initiates installation process
                    if(patcher.ChapterName == "umineko-question" || patcher.ChapterName == "umineko-answer")
                    {
                        //Note: Unlike Higurashi installers, Umineko installer expects to run from the
                        //root directory of the game. From the root, the installer is at temp/install.bat,
                        //and the "../" changes the working directory from "temp" to just the game root.
                        Utils.runInstaller(this, "temp/install.bat", Path.Combine(patcher.InstallPath, "../"));
                    }
                    else
                    {
                        Utils.runInstaller(this, "install.bat", patcher.InstallPath);
                    }

                }));
            }
            catch (Exception error)
            {
                _log.Info(error);
                Environment.Exit(1);
            } 
        }

        //Listeners for mouse
        private void MouseEnterOni(object sender, MouseEventArgs e)
        {
            EpisodeImage.Visibility = Visibility.Visible;
            EpisodeImage.Source = new BitmapImage(new Uri("/Resources/header1.jpg", UriKind.Relative));
        }

        private void MouseEnterWata(object sender, MouseEventArgs e)
        {
            EpisodeImage.Visibility = Visibility.Visible;
            EpisodeImage.Source = new BitmapImage(new Uri("/Resources/header2.jpg", UriKind.Relative));
        }

        private void MouseEnterTata(object sender, MouseEventArgs e)
        {
            EpisodeImage.Visibility = Visibility.Visible;
            EpisodeImage.Source = new BitmapImage(new Uri("/Resources/header3.jpg", UriKind.Relative));
        }

        private void MouseEnterHima(object sender, MouseEventArgs e)
        {
            EpisodeImage.Visibility = Visibility.Visible;
            EpisodeImage.Source = new BitmapImage(new Uri("/Resources/header4.jpg", UriKind.Relative));
        }

        private void MouseEnterMea(object sender, MouseEventArgs e)
        {
            EpisodeImage.Visibility = Visibility.Visible;
            EpisodeImage.Source = new BitmapImage(new Uri("/Resources/header5.jpg", UriKind.Relative));
        }

        private void BtnTsumihoroboshi_MouseEnter(object sender, MouseEventArgs e)
        {
            EpisodeImage.Visibility = Visibility.Visible;
            EpisodeImage.Source = new BitmapImage(new Uri("/Resources/header6.jpg", UriKind.Relative));
        }

        private void MouseEnterConsole(object sender, MouseEventArgs e)
        {
            EpisodeImage.Visibility = Visibility.Visible;
            EpisodeImage.Source = new BitmapImage(new Uri("/Resources/console.png", UriKind.Relative));
        }

        private void UminekoQuestion_MouseEnter(object sender, MouseEventArgs e)
        {
            EpisodeImage.Visibility = Visibility.Visible;
            EpisodeImage.Source = new BitmapImage(new Uri("/Resources/header_umineko_question.jpg", UriKind.Relative));
        }

        private void UminekoAnswer_MouseEnter(object sender, MouseEventArgs e)
        {
            EpisodeImage.Visibility = Visibility.Visible;
            EpisodeImage.Source = new BitmapImage(new Uri("/Resources/header_umineko_answer.jpg", UriKind.Relative));
        }

        private void MouseLeaveEpisode(object sender, MouseEventArgs e)
        {
            if (ActualWidth < 950)
            {
                EpisodeImage.Visibility = Visibility.Collapsed;
            }else
            {
                EpisodeImage.Source = new BitmapImage(new Uri(patcher.ImagePath, UriKind.Relative));
            }
        }

    }

    // The following two classes are required to ensure only one expander is expanded at any time
    // See https://stackoverflow.com/questions/4449000/multiple-expander-have-to-collapse-if-one-is-expanded
    public class ExpanderToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value == parameter);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (System.Convert.ToBoolean(value)) return parameter;
            return null;
        }
    }

    public class ExpanderListViewModel
    {
        public Object SelectedExpander { get; set; }
    }

}
