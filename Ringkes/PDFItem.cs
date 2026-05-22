using System.ComponentModel;

namespace Ringkes
{
    public class PDFItem : INotifyPropertyChanged
    {
        private string status;
        private double progress;

        public string FilePath { get; set; }

        public string FileName
        {
            get
            {
                return System.IO.Path.GetFileName(FilePath);
            }
        }

        public string Status
        {
            get => status;

            set
            {
                status = value;

                OnPropertyChanged(nameof(Status));
            }
        }

        public double Progress
        {
            get => progress;

            set
            {
                progress = value;

                OnPropertyChanged(nameof(Progress));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(name));
        }
    }
}