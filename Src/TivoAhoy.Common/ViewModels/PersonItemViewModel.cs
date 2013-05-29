using Caliburn.Micro;
using Tivo.Connect.Entities;

namespace TivoAhoy.Common.ViewModels
{
    public class PersonItemViewModel : UnifiedItemViewModel<Person>
    {
        private readonly INavigationService navigationService;

        public PersonItemViewModel(INavigationService navigationService)
        {
            this.navigationService = navigationService;
        }

        public PersonItemViewModel()
        {
            if (Execute.InDesignMode)
                LoadDesignData();
        }

        private void LoadDesignData()
        {
            this.Source =
                new Person()
                {
                    FirstName = "James",
                    LastName = "Chaldecott",
                };
        }
        public override string DisplayText
        {
            get
            {
                if (this.Source == null)
                    return null;

                return this.Source.DisplayName;
            }
        }

        public void DisplayPersonDetails()
        {
            this.navigationService
                .UriFor<PersonDetailsPageViewModel>()
                .WithParam(x => x.PersonID, this.Source.PersonId)
                .Navigate();
        }
    }
}
