//-----------------------------------------------------------------------
// <copyright file="CreditsViewModel.cs" company="James Chaldecott">
// Copyright (c) 2012-2013 James Chaldecott. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using Caliburn.Micro;
using Tivo.Connect.Entities;

namespace TivoAhoy.Common.ViewModels
{
    public class CreditsViewModel : Screen
    {
        private readonly INavigationService navigationService;
        private List<Credit> credits;

        public CreditsViewModel(INavigationService navigationService)
        {
            this.navigationService = navigationService;
        }

        public CreditsViewModel()
        {
            if (Execute.InDesignMode)
                LoadDesignData();
        }

        private void LoadDesignData()
        {
            Credits = new List<Credit>
                {
                    new Credit
                    {
                        CharacterName= "D.B. Russell",
                        FirstName ="Ted",
                        LastName= "Danson",
                        PersonId= "tivo:pn.61724",
                        OriginalRole = "actor",
                        Images = new List<ImageInfo>
                        {
                            new ImageInfo
                            {
                                Width= 59,
                                Height = 78,
                                ImageId = "tivo:im.458355819",
                                ImageType= "person",
                                OriginalImageUrl= "http://10.185.116.1:8080/images/legacy/small/69/55/695556e67a7b88608fc01a5c72501a24.jpg",
                            },
                        }
                    },
                    new Credit
                    {
                        FirstName ="Quentin",
                        LastName= "Tarantino",
                        PersonId= "tivo:pn.11111",
                        OriginalRole = "director",
                    }

                };
        }

        public List<Credit> Credits 
        {
            get { return this.credits; }

            set
            {
                if (this.credits == value)
                {
                    return;
                }

                this.credits = value;
                this.NotifyOfPropertyChange(() => this.Credits);
            }
        }

        public bool HasCredits
        {
            get
            {
                return
                    this.Credits != null &&
                    this.Credits.Any();
            }
        }

        public void DisplayPersonDetails(Credit credit)
        {
            if (credit == null ||
                credit.PersonId == null)
            {
                return;
            }

            this.navigationService
                .UriFor<PersonDetailsPageViewModel>()
                .WithParam(x => x.PersonID, credit.PersonId)
                .Navigate();
        }

    }
}
