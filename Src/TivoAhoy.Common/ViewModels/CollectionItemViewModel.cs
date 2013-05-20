using Tivo.Connect.Entities;

namespace TivoAhoy.Common.ViewModels
{
    public class CollectionItemViewModel : UnifiedItemViewModel<Collection>
    {
        public CollectionItemViewModel(Collection collection)
            : base(collection)
        {

        }

        public override string DisplayText
        {
            get
            {
                if (this.Source == null)
                    return null;

                return this.Source.Title;
            }
        }
    }
}
