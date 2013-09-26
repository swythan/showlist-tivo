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
        private static object syncLock = new object();

        public static  void AddOrUpdateValue(object value, string key)
        {
            var valueChanged = false;

            lock (syncLock)
            {
                try
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
                    if (IsolatedStorageSettings.ApplicationSettings[key] != value)
                    {
                        IsolatedStorageSettings.ApplicationSettings[key] = value;
                        valueChanged = true;
                    }
                }
                catch (KeyNotFoundException)
                {
                    IsolatedStorageSettings.ApplicationSettings.Add(key, value);
                    valueChanged = true;
                }
                catch (ArgumentException)
                {
                    IsolatedStorageSettings.ApplicationSettings.Add(key, value);
                    valueChanged = true;
                }

                if (valueChanged)
                {
                    Save();
                }
            }
        }

        public static T GetValueOrDefault<T>(T defaultValue, string key)
        {
            lock (syncLock)
            {
                T value;

                try
                {
                    value = (T)IsolatedStorageSettings.ApplicationSettings[key];
                }
                catch (KeyNotFoundException)
                {
                    value = defaultValue;
                }
                catch (ArgumentException)
                {
                    value = defaultValue;
                }

                return value;
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
                return;
            }
        }
    }
}
