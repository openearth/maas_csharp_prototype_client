using System;
using System.Linq;
using Microsoft.Web.WebView2.Core;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using DeltaSphereTestApp.Entities;

namespace DeltaSphereTestApp.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            MainWindowViewModel.GetFolderName = () =>
            {
                var folderDialog = new FolderBrowserDialog
                {
                    SelectedPath = @"D:\temp\TestModel"
                };

                return folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK
                    ? folderDialog.SelectedPath
                    : "";
            };

            MainWindowViewModel.GetFileName = (s) =>
            {
                var saveDialog = new SaveFileDialog
                {
                    FileName = s
                };

                return saveDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK
                    ? saveDialog.FileName
                    : null;
            };

            // re-route navigating event to view model
            Browser.NavigationStarting += (o, args) =>
            {
                args.Cancel = MainWindowViewModel.BrowserNavigating(args.Uri);
            };

            // Start with login page
            Browser.Source = MainWindowViewModel.LoginUri;
        }

        private void SelectorOnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            MainWindowViewModel.SelectedJob = JobsDataGrid.SelectedItem as Job ?? e.RemovedItems.OfType<Job>().FirstOrDefault();
        }
    }
}
