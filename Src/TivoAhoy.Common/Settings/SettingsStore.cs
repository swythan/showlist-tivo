//-----------------------------------------------------------------------
// <copyright file="SettingsStore.cs" company="James Chaldecott">
// Copyright (c) 2012-2013 James Chaldecott. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO.IsolatedStorage;

namespace TivoAhoy.Common.Settings
{
    public static class SettingsStore
    {
        private static readonly object syncLock = new object();

        public static  void AddOrUpdateValue(object value, string key)
        {
            lock (syncLock)
            {
                if (value == null)
                {
                    // Nothing to remove
                    if (!IsolatedStorageSettings.ApplicationSettings.Contains(key))
                        return;

                    IsolatedStorageSettings.ApplicationSettings.Remove(key);
                    Save();
                }

                // If the new value is different, set the new value.
                object oldVal;
                var valueChanged = false;
                if (IsolatedStorageSettings.ApplicationSettings.TryGetValue(key, out oldVal))
                {
                    valueChanged = !Equals(oldVal, value);
                }
                else
                {
                    valueChanged = true; // Key was not found
                }

                if (valueChanged)
                {
                    // Update the value
                    IsolatedStorageSettings.ApplicationSettings[key] = value;
                    Save();
                }
            }
        }

        public static T GetValueOrDefault<T>(T defaultValue, string key)
        {
            lock (syncLock)
            {
                object valObj;
                if (IsolatedStorageSettings.ApplicationSettings.TryGetValue(key, out valObj))
                {
                    return (T)valObj;
                }

                return defaultValue;
            }
        }

        private static void Save()
        {
            try
            {
                IsolatedStorageSettings.ApplicationSettings.Save();
            }
            catch (Exception)
            {
            }
        }
    }
}
