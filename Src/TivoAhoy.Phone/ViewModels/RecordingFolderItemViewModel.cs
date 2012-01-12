using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Tivo.Connect.Entities;
using Caliburn.Micro;

namespace TivoAhoy.Phone.ViewModels
{
    public interface IRecordingFolderItemViewModel  
    {
        string Title { get; }
        string Id { get; }
    }

    public abstract class RecordingFolderItemViewModel<T> : PropertyChangedBase, IRecordingFolderItemViewModel where T : RecordingFolderItem
    {
        private T source;

        public RecordingFolderItemViewModel()
        {

        }

        public T Source
        {
            get
            {
                return this.source;
            }
            set
            {
                if (this.source == value)
                    return;

                this.source = value;

                NotifyOfPropertyChange(() => this.Source);
                NotifyOfPropertyChange(() => this.Title);
            }
        }

        public string Title
        {
            get { return this.Source.Title; }
        }

        public string Id
        {
            get { return this.Source.Id; }
        }

        public abstract bool IsSingleShow { get; }
    }
}
