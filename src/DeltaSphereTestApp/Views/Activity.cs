using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DeltaSphereTestApp.Views
{
    internal class Activity : INotifyPropertyChanged
    {
        private string progressText;
        private string name;
        private string error;

        public string Name
        {
            get { return name; }
            set
            {
                name = value; 
                OnPropertyChanged();
            }
        }

        public string ProgressText
        {
            get { return progressText; }
            set
            {
                progressText = value; 
                OnPropertyChanged();
            }
        }

        public string Error
        {
            get { return error; }
            set
            {
                error = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}