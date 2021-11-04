using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
using DeltaSphereTestApp.Api;
using DeltaSphereTestApp.Entities;
using DeltaSphereTestApp.Helpers;
using DeltaSphereTestApp.Views.Shared;

namespace DeltaSphereTestApp.Views
{
    internal sealed class MainWindowViewModel : INotifyPropertyChanged
    {
        private const string Server = "http://127.0.0.1:8080/v1/";
        private bool loginSuccessful;
        private string errorMessage;
        private string sessionId;

        private IEnumerable<Process> processes;
        private string loginName;
        private IEnumerable<Job> jobs;
        private Process selectedProcess;
        private string inputDirectory;
        private IEnumerable<string> files;
        private string newJobTitle;
        private DeltaSphereCaasApi api;
        private Job selectedJob;
        private string newJobDescription;
        private bool showNewJobScreen;
        private RemoteFolder filesRootFolder;

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
        /// Delete files from s3
        /// </summary>
        public ICommand DeleteFileCommand { get; private set; }

        /// <summary>
        /// Downloads the specified file
        /// </summary>
        public ICommand DownloadFileCommand { get; private set; }

        /// <summary>
        /// Get inputDirectory from folder
        /// </summary>
        public ICommand GetInputFilesFromFolderCommand { get; set; }

        /// <summary>
        /// Function for retrieving file names
        /// </summary>
        public Func<string> GetFolderName { get; set; }

        /// <summary>
        /// Retrieves a save path
        /// </summary>
        public Func<string, string> GetFileName { get; set; }

        /// <summary>
        /// Command for posting job
        /// </summary>
        public ICommand AddJobCommand { get; set; }

        /// <summary>
        /// Deletes the currently selected job
        /// </summary>
        public ICommand DeleteJobCommand { get; set; }

        /// <summary>
        /// Refreshes the jobs for the current process
        /// </summary>
        public ICommand RefreshJobsCommand { get; set; }

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

        public IEnumerable<RemoteFolder> FilesRootFolder
        {
            get { return new[] {filesRootFolder}; }
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

        public Uri BaseUri { get; } = new Uri(Server);

        public Uri LoginUri
        {
            get { return new Uri(BaseUri + "login"); }
        }

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
                var observableCollection = value != null
                    ? new ObservableCollection<Job>(value)
                    : new ObservableCollection<Job>();

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

        public ObservableCollection<Activity> ActivityList { get; } = new ObservableCollection<Activity>();

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

        public bool ShowNewJobScreen
        {
            get { return showNewJobScreen; }
            set
            {
                showNewJobScreen = value; 
                OnPropertyChanged();
            }
        }

        public string NewJobTitle
        {
            get { return newJobTitle; }
            set
            {
                newJobTitle = value;
                OnPropertyChanged();
            }
        }

        public string NewJobDescription
        {
            get { return newJobDescription; }
            set
            {
                newJobDescription = value;
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

            if (string.IsNullOrEmpty(sessionId) || !url.IsEndUrl(BaseUri))
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

            GetFilesOverviewCommand = new RelayCommand(o => UpdateFiles());

            GetInputFilesFromFolderCommand = new RelayCommand(o => InputDirectory = GetFolderName());

            RefreshJobsCommand = new RelayCommand(o => UpdateJobs());

            AddJobCommand = new RelayCommand(o => AddJob());

            DeleteJobCommand = new RelayCommand(
                async o => await api.DeleteJob(SelectedProcess.Id, SelectedJob.JobId),
                o => SelectedJob != null);

            GetMeCommand = new RelayCommand(o => { UpdateLoginName(); });

            DeleteFileCommand = new RelayCommand<IRemotePath>(async o =>
            {
                if (!(o is IRemotePath remotePath)) return;

                var remotePaths = o is RemoteFolder folder
                    ? folder.GetAllSubPathsRecursive().ToArray()
                    : new[] { remotePath };

                foreach (var path in remotePaths)
                {
                    var success = await api.DeleteFile(path.FullPath);
                    if (!success)
                    {
                        ErrorMessage = $"Could not delete {o}";
                    }
                }

                UpdateFiles();
            }, o => o != null);

            DownloadFileCommand = new RelayCommand<IRemotePath>(async remotePath =>
            {
                var fileName = GetFileName?.Invoke(remotePath.Name);
                if (fileName == null) return;

                await api.DownloadFile(remotePath.FullPath, fileName);
            });
        }

        private async void AddJob()
        {
            ShowNewJobScreen = false;
            var jobData = new CreateFmJobData {Title = newJobTitle, Description = newJobDescription, ProcessId = SelectedProcess.Id};

            var addJobActivity = new Activity {Name = $"Adding job {newJobTitle}", ProgressText = "Creating tar file"};

            ActivityList.Add(addJobActivity);

            IProgress<string> progress = new Progress<string>(s => { Dispatcher.CurrentDispatcher.Invoke(() => { addJobActivity.ProgressText = s; }); });

            var tarFilePath = await Task.Run(() => FileHelper.CreateTarGz("InputData", InputDirectory, progress));
            var uploadedFiles = await api.UploadFiles(new[] {tarFilePath}, SelectedProcess.Id, NewJobTitle, progress);

            progress.Report($"Creating job \"{NewJobTitle}\"");
            jobData.RelativeS3Path = uploadedFiles[0].RemotePath.Split(new[] {'/'}, 3)[2];
            await api.CreateJob(jobData);
            progress.Report($"Job \"{NewJobTitle}\" is created");

            ActivityList.Remove(addJobActivity);
            //UpdateJobs();
        }

        private async void UpdateFiles()
        {
            filesRootFolder = new RemoteFolder {Name = "root"};

            var filePaths = await api.GetFiles();

            AddPathsToFolder(filePaths, filesRootFolder);
            OnPropertyChanged(nameof(FilesRootFolder));
        }

        private void AddPathsToFolder(IEnumerable<string> paths, RemoteFolder rootFolder)
        {
            var spitPaths = paths.Where(p => !string.IsNullOrEmpty(p))
                .Select(p => p.Split(new[] {'/'}, 2));

            var pathLookup = spitPaths.GroupBy(p => p[0], p => p.Length > 1 ? p[1]: null);

            foreach (var path in pathLookup)
            {
                var key = path.Key;
                var shortedPaths = path.ToArray();

                if (shortedPaths.Length == 1 && string.IsNullOrEmpty(shortedPaths[0]))
                {
                    rootFolder.Files.Add(new RemotePath
                    {
                        Name = key,
                        ParentFolder = rootFolder
                    });
                    continue;
                }

                var subfolder = rootFolder.SubFolders.FirstOrDefault(f => f.Name == key);
                if (subfolder == null)
                {
                    subfolder = new RemoteFolder { Name = key, ParentFolder = rootFolder };
                    rootFolder.SubFolders.Add(subfolder);
                }

                AddPathsToFolder(shortedPaths, subfolder);
            }
        }
    }
}