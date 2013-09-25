//-----------------------------------------------------------------------
// <copyright file="IProgressService.cs" company="James Chaldecott">
// Copyright (c) 2012-2013 James Chaldecott. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace TivoAhoy.Common.Services
{
    using System;

    public interface IProgressService
    {
        IDisposable Show();
    }
}
