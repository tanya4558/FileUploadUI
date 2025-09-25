using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileUploadTool.Models
{
    public class FileUploadItem : INotifyPropertyChanged
    {
        public string FilePath { get; set; }
        public string FileName => System.IO.Path.GetFileName(FilePath);

        //private bool _isUploaded;
        //public bool IsUploaded
        //{
        //    get => _isUploaded;
        //    set
        //    {
        //        _isUploaded = value;
        //        OnPropertyChanged(nameof(IsUploaded));
        //    }
        //}
        private string _status = "Pending";
        public string Status
        {
            get => _status;
            set
            {
                if (_status != value)
                {
                    _status = value;
                    OnPropertyChanged(nameof(Status));
                }
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
