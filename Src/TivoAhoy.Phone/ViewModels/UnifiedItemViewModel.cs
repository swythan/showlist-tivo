using Caliburn.Micro;
using Tivo.Connect.Entities;

namespace TivoAhoy.Phone.ViewModels
{
    public interface IUnifiedItemViewModel
    {
        string DisplayText { get; }
    }

    public abstract class UnifiedItemViewModel<T> : PropertyChangedBase, IUnifiedItemViewModel where T : class, IUnifiedItem
    {
        private T source;

        public UnifiedItemViewModel(T source)
        {
            this.source = source;
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
                NotifyOfPropertyChange(() => this.DisplayText);
            }
        }

        public abstract string DisplayText { get; }
    }
}
