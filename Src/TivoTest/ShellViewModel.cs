//-----------------------------------------------------------------------
// <copyright file="ShellViewModel.cs" company="James Chaldecott">
// Copyright (c) 2012-2013 James Chaldecott. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace TivoTest {
    using System.ComponentModel.Composition;

    [Export(typeof(IShell))]
    public class ShellViewModel : IShell 
    {
        [ImportingConstructor]
        public ShellViewModel(MainViewModel main)
        {
            this.Main = main;
        }

        public MainViewModel Main { get; private set; }
    }
}
