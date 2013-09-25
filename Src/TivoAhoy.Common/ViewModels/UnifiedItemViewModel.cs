//-----------------------------------------------------------------------
// <copyright file="UnifiedItemViewModel.cs" company="James Chaldecott">
// Copyright (c) 2012-2013 James Chaldecott. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using Caliburn.Micro;
using Tivo.Connect.Entities;

namespace TivoAhoy.Common.ViewModels
{
    public interface IUnifiedItemViewModel
    {
        string Title { get; }
    }

    public abstract class UnifiedItemViewModel<T> : PropertyChangedBase, IUnifiedItemViewModel where T : class, IUnifiedItem
    {
        private T source;

        public UnifiedItemViewModel()
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
                NotifyOfPropertyChange(() => this.Subtitle);
            }
        }

        public abstract string Title { get; }
        public abstract string Subtitle { get; }
    }
}
