using System.ComponentModel;

namespace Ringkes
{
    public class PDFItem :
        INotifyPropertyChanged
    {
        private double progress;

        private string status;

        public string FilePath { get; set; }

        public string OriginalPath { get; set; }

        public bool IsTemporary { get; set; }

        public string FileName
        {
            get
            {
                return System.IO.Path
                    .GetFileName(FilePath);
            }
        }

        public double Progress
        {
            get { return progress; }

            set
            {
                progress = value;

                OnPropertyChanged("Progress");
            }
        }

        public string Status
        {
            get { return status; }

            set
            {
                status = value;

                OnPropertyChanged("Status");
            }
        }

        public event PropertyChangedEventHandler
            PropertyChanged;

        protected void OnPropertyChanged(
            string name)
        {
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(name));
        }
    }
}