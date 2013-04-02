using System;

namespace NSettings
{
    public class SettingChangedEventArgs : EventArgs
    {
        public ISettingProvider SettingProvider { get; set; }

        public SettingChangedEventArgs(ISettingProvider provider)
        {
            SettingProvider = provider;
        }
    }
}