using System;
using System.Linq;
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


            // re-route navigating event to view model
            Browser.Navigating += (s, a)=>
            {
                a.Cancel = MainWindowViewModel.BrowserNavigating(a.Uri.AbsoluteUri);
            };
            
            // Start with login page
            Browser.Navigate(new Uri(MainWindowViewModel.BaseUri, "login"));
        }

        private void SelectorOnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            MainWindowViewModel.SelectedJob = JobsDataGrid.SelectedItem as Job ?? e.RemovedItems.OfType<Job>().FirstOrDefault();
        }
    }
}
