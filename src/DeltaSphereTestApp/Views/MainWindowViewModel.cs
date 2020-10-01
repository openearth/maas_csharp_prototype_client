using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Windows.Input;
using DeltaSphereTestApp.Api;
using DeltaSphereTestApp.Entities;
using DeltaSphereTestApp.Helpers;
using DeltaSphereTestApp.Views.Shared;

namespace DeltaSphereTestApp.Views
{
    internal sealed class MainWindowViewModel : INotifyPropertyChanged
    {
        private const string Server = "http://127.0.0.1:8000/";
        private bool loginSuccessful;
        private string errorMessage;
        private string data;
        private string sessionId;

        private IEnumerable<Process> processes;
        private string loginName;
        private IEnumerable<Job> jobs;
        private Process selectedProcess;
        private string inputDirectory;
        private IEnumerable<string> files;
        private string newJobName;
        private DeltaSphereCaasApi api;
        private Job selectedJob;

        public MainWindowViewModel()
        {
            CreateCommands();
        }

        /// <summary>
        /// Command for getting all model runs
        /// </summary>
        public ICommand GetProcessesCommand { get; private set; }

        /// <summary>
        /// Command to verify user data
        /// </summary>
        public ICommand GetMeCommand { get; private set; }

        /// <summary>
        /// Command to get (input) files on s3
        /// </summary>
        public ICommand GetFilesOverviewCommand { get; private set; }
        
        /// <summary>
        /// Get inputDirectory from folder
        /// </summary>
        public ICommand GetInputFilesFromFolderCommand { get; set; }

        /// <summary>
        /// Function for retrieving file names
        /// </summary>
        public Func<string> GetFolderName { get; set; }

        /// <summary>
        /// Command for posting job
        /// </summary>
        public ICommand AddJobCommand { get; set; }

        /// <summary>
        /// Deletes the currently selected job
        /// </summary>
        public ICommand DeleteJobCommand { get; set; }

        /// <summary>
        /// Indicator for login successful or not
        /// </summary>
        public bool LoginSuccessful
        {
            get { return loginSuccessful; }
            set
            {
                loginSuccessful = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ShowBrowser));
            }
        }

        /// <summary>
        /// Show browser control (inverse of <see cref="LoginSuccessful"/>
        /// </summary>
        public bool ShowBrowser
        {
            get { return !loginSuccessful; }
        }

        /// <summary>
        /// Received error message
        /// </summary>
        public string ErrorMessage
        {
            get { return errorMessage; }
            set
            {
                errorMessage = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Login name
        /// </summary>
        public string LoginName
        {
            get { return loginName; }
            set
            {
                loginName = value;
                OnPropertyChanged();
            }
        }
        
        /// <summary>
        /// List of files that are on the s3 bucket
        /// </summary>
        public IEnumerable<string> Files
        {
            get { return files; }
            set
            {
                files = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Received data
        /// </summary>
        public string Data
        {
            get { return data; }
            set
            {
                data = value;
                OnPropertyChanged();
            }
        }

        public Uri BaseUri { get; } = new Uri(Server);

        /// <summary>
        /// The currently selected process
        /// </summary>
        public Process SelectedProcess
        {
            get { return selectedProcess; }
            set
            {
                selectedProcess = value;
                
                UpdateJobs();

                OnPropertyChanged();
                OnPropertyChanged(nameof(HasSelectedProcess));
            }
        }

        /// <summary>
        /// The currently selected job
        /// </summary>
        public Job SelectedJob
        {
            get { return selectedJob; }
            set
            {
                selectedJob = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// If a process has been selected
        /// </summary>
        public bool HasSelectedProcess
        {
            get { return selectedProcess != null; }
        }

        /// <summary>
        /// Received processes
        /// </summary>
        public IEnumerable<Process> Processes
        {
            get { return processes; }
            set
            {
                processes = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Received jobs
        /// </summary>
        public IEnumerable<Job> Jobs
        {
            get { return jobs; }
            set
            {
                var observableCollection = new ObservableCollection<Job>(value);
                observableCollection.CollectionChanged += (s, a) =>
                {
                    if (a.Action != NotifyCollectionChangedAction.Remove) return;

                    foreach (var eOldItem in a.OldItems)
                    {
                        DeleteJobCommand.Execute(eOldItem);
                    }
                };

                jobs = observableCollection;
                
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Selected input files to upload
        /// </summary>
        public string InputDirectory
        {
            get { return inputDirectory; }
            set
            {
                inputDirectory = value;
                OnPropertyChanged();
            }
        }

        public string NewJobName
        {
            get { return newJobName; }
            set
            {
                newJobName = value;
                OnPropertyChanged();
            }
        }

        /// <inheritdoc/>
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        internal bool BrowserNavigating(string url)
        {
            // keep session id used for auth endpoint (needed for cookies)
            if (string.IsNullOrEmpty(sessionId))
            {
                sessionId = url.TryGetSession(BaseUri);
            }

            if (string.IsNullOrEmpty(sessionId) || !url.IsEndUrl())
            {
                return false;
            }

            LoginSuccessful = true;
            AfterLogin();

            // cancel navigation to end url
            return true;
        }

        private void AfterLogin()
        {
            api = new DeltaSphereCaasApi(Server, sessionId);
            UpdateProcesses();
            UpdateLoginName();
        }

        private async void UpdateJobs()
        {
            Jobs = await api.GetJobsFor(selectedProcess);
        }

        private async void UpdateLoginName()
        {
            LoginName = await api.GetLoginName();
        }

        private async void UpdateProcesses()
        {
            Processes = await api.GetProcesses();
        }

        private void CreateCommands()
        {
            GetProcessesCommand = new RelayCommand(o => { UpdateProcesses(); });

            GetFilesOverviewCommand = new RelayCommand(async o =>
            {
                Files = await api.GetFiles();
            });
            
            GetInputFilesFromFolderCommand = new RelayCommand(o =>
            {
                InputDirectory = GetFolderName();
            });

            AddJobCommand = new RelayCommand(async o =>
            {
                var tarFilePath = await Task.Run(() => FileHelper.CreateTarGz("InputData", InputDirectory));
                await api.UploadFiles(new []{tarFilePath}, SelectedProcess.Id, NewJobName);
            });

            DeleteJobCommand = new RelayCommand(
                async o => await api.DeleteJob(SelectedProcess.Id, SelectedJob.JobId), 
                o => SelectedJob != null);

            GetMeCommand = new RelayCommand(o => { UpdateLoginName(); });
        }
    }
}