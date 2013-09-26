//-----------------------------------------------------------------------
// <copyright file="AsyncLazy.cs" company="James Chaldecott">
// Copyright (c) 2012-2013 James Chaldecott. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TivoAhoy.Common.Utils
{
    public class AsyncLazy<T> : Lazy<Task<T>>
    {
        public AsyncLazy(Func<T> valueFactory) :
            base(() => Task.Factory.StartNew(valueFactory))
        {
        }

        public AsyncLazy(Func<Task<T>> taskFactory) :
            base(() => Task.Factory.StartNew(() => taskFactory()).Unwrap())
        {
        }

        public void Start()
        {
            if (this.IsValueCreated)
                return;

            var dummy = this.Value;
        }
    }
}
