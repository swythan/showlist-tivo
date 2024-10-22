﻿//-----------------------------------------------------------------------
// <copyright file="PersonItemViewModel.cs" company="James Chaldecott">
// Copyright (c) 2012-2013 James Chaldecott. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Caliburn.Micro;
using Tivo.Connect;
using Tivo.Connect.Entities;
using TivoAhoy.Common.Services;

namespace TivoAhoy.Common.ViewModels
{
    public class PersonItemViewModel : UnifiedItemViewModel<Person>
    {
        private readonly ITivoConnectionService connectionService;
        private readonly INavigationService navigationService;
        private readonly IProgressService progressService;

        public PersonItemViewModel(
            INavigationService navigationService,
            IProgressService progressService,
            ITivoConnectionService connectionService)
        {
            this.connectionService = connectionService;
            this.navigationService = navigationService;
            this.progressService = progressService;
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

        public override string Title
        {
            get
            {
                if (this.Source == null)
                    return null;

                return this.Source.DisplayName;
            }
        }

        public override string Subtitle
        {
            get
            {
                if (this.Source == null)
                    return null;

                if (this.Source.Roles == null)
                {
                    GetRolesForPerson();

                    return null;
                }

                var roles = this.Source.Roles
                    .Select(x => x.SplitCamelCase().UppercaseFirst())
                    .Distinct()
                    .Take(2);

                return string.Join(", ", roles);
            }
        }

        private async void GetRolesForPerson()
        {
            try
            {
                using (this.progressService.Show())
                {
                    var connection = await this.connectionService.GetConnectionAsync();

                    var betterPerson = await connection.GetBasicPersonDetails(this.Source.PersonId);

                    this.Source = betterPerson;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Failed to get better details for person item: {0}", ex);
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
