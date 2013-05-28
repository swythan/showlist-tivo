using Caliburn.Micro;
using Tivo.Connect.Entities;

namespace TivoAhoy.Common.ViewModels
{
    public class CollectionItemViewModel : UnifiedItemViewModel<Collection>
    {
        private readonly INavigationService navigationService;

        public CollectionItemViewModel(INavigationService navigationService)
        {
            this.navigationService = navigationService;
        }


        public CollectionItemViewModel()
        {
            if (Execute.InDesignMode)
                LoadDesignData();
        }

        private void LoadDesignData()
        {
            this.Source =
                new Collection()
                {
                    Title = "The Walking Dead",                   
                };
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

        public void DisplayCollectionDetails()
        {
            this.navigationService
                .UriFor<CollectionDetailsPageViewModel>()
                .WithParam(x => x.CollectionID, this.Source.CollectionId)
                .Navigate();
        }
    }
}
