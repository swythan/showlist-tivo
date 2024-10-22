﻿//-----------------------------------------------------------------------
// <copyright file="UnifiedItemCreator.cs" company="James Chaldecott">
// Copyright (c) 2012-2013 James Chaldecott. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using Newtonsoft.Json.Linq;

namespace Tivo.Connect.Entities
{
    internal class UnifiedItemCreator : JsonCreationConverter<IUnifiedItem>
    {
        protected override IUnifiedItem Create(Type objectType, JObject jObject)
        {
            string jsonType = (string) jObject["type"];

            switch (jsonType)
            {
                case "person":
                    return new Person();

                case "collection":
                    return new Collection();
            }

            return null;
        }
    }
}
