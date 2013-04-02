using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using NRegistryInterface;
using NSettings.Properties;

namespace NSettings
{
    public class SettingManager : ISettingProvider, ISettingWriter
    {
        private event EventHandler<SettingChangedEventArgs> SettingChangedEvent;

        private readonly object _settingsLock;

        private Dictionary<String, object> _settings;
        private Dictionary<String, List<EventHandler<SettingChangedEventArgs>>> _settingBindings;
        private Dictionary<String, Type> _settingsTypes;

        private Dictionary<String, Type> SettingsTypes
        {
            get { return _settingsTypes ?? (_settingsTypes = new Dictionary<string, Type>()); }
        }

        private Dictionary<String, object> Settings
        {
            get { return _settings ?? (_settings = new Dictionary<string, object>()); }
        }

        private Dictionary<String, List<EventHandler<SettingChangedEventArgs>>> SettingBindings
        {
            get
            {
                return _settingBindings ??
                       (_settingBindings = new Dictionary<string, List<EventHandler<SettingChangedEventArgs>>>());
            }
        }

        public SettingManager()
        {
            _settingsLock = new object();
        }

        public void AddSetting<T>(String key, T value)
        {
            lock (_settingsLock)
            {
                if (Settings.ContainsKey(key))
                {
                    throw new InvalidOperationException(Resources.EXCEPTION_DUPLICATE_SETTING);
                }
                Settings.Add(key, value);
                SettingsTypes.Add(key, typeof (T));
            }
        }

        public void AddSetting<T>(String key)
        {
            lock (_settingsLock)
            {
                if (Settings.ContainsKey(key))
                {
                    throw new InvalidOperationException(Resources.EXCEPTION_DUPLICATE_SETTING);
                }
                Settings.Add(key, default(T));
                SettingsTypes.Add(key, typeof (T));
            }
        }

        public void RemoveSetting(String key)
        {
            lock (_settingsLock)
            {
                if (!Settings.ContainsKey(key))
                {
                    throw new KeyNotFoundException(Resources.EXCEPTION_KEY_NOT_FOUND);
                }
                UnBind(key);
                Settings.Remove(key);
                SettingsTypes.Remove(key);
            }
        }

        public void ChangeSetting<T>(String key, T value)
        {
            lock (_settingsLock)
            {
                if (!Settings.ContainsKey(key))
                {
                    throw new KeyNotFoundException(Resources.EXCEPTION_KEY_NOT_FOUND);
                }
                if (Settings[key] != null && SettingsTypes[key] != typeof (T))
                {
                    throw new TypeAccessException(Resources.EXCEPTION_TYPE_MISMATCH + ". expected: " +
                                                  typeof (T) + " but found: " + SettingsTypes[key]);
                }
                Settings[key] = value;
                OnSettingChanged(key);
            }
        }

        public T ReadSetting<T>(String key)
        {
            T setting;

            lock (_settingsLock)
            {
                if (!Settings.ContainsKey(key))
                {
                    throw new KeyNotFoundException(Resources.EXCEPTION_KEY_NOT_FOUND);
                }
                if (SettingsTypes[key] != typeof (T))
                {
                    throw new TypeAccessException(Resources.EXCEPTION_TYPE_MISMATCH + ". expected: " +
                                                  typeof (T) + " but found: " + SettingsTypes[key]);
                }
                setting = (T) Settings[key];
            }

            return setting;
        }

        public void Bind(String key, INotifySettingChanged bindingClass, Boolean triggerImmediately = true)
        {
            if (bindingClass == null)
            {
                throw new NullReferenceException(Resources.EXCEPTION_BINDING_CLASS_NULL);
            }

            var handler = new EventHandler<SettingChangedEventArgs>(bindingClass.OnSettingChanged);

            lock (_settingsLock)
            {
                if (!Settings.ContainsKey(key))
                {
                    throw new KeyNotFoundException(Resources.EXCEPTION_KEY_NOT_FOUND);
                }
                if (!SettingBindings.ContainsKey(key))
                {
                    SettingBindings.Add(key, new List<EventHandler<SettingChangedEventArgs>> {handler});

                    if (triggerImmediately)
                    {
                        TriggerHandler(key, handler);
                    }
                }

                else if (SettingBindings[key] != null)
                {
                    if (SettingBindings[key].Contains(handler))
                    {
                        throw new InvalidOperationException(Resources.EXCEPTION_DUPLICATE_HANDLER);
                    }

                    SettingBindings[key].Add(handler);

                    if (triggerImmediately)
                    {
                        TriggerHandler(key, handler);
                    }
                }
                else
                {
                    throw new DivideByZeroException("SHTF!");
                }
            }
        }

        public void UnBind(String key, INotifySettingChanged bindingClass = null)
        {
            if (bindingClass == null)
            {
                throw new NullReferenceException(Resources.EXCEPTION_BINDING_CLASS_NULL);
            }

            EventHandler<SettingChangedEventArgs> handler = bindingClass.OnSettingChanged;

            lock (_settingsLock)
            {
                if (!SettingBindings.ContainsKey(key))
                {
                    throw new KeyNotFoundException(Resources.EXCEPTION_KEY_NOT_FOUND);
                }
                if (SettingBindings[key] == null)
                {
                    throw new DivideByZeroException("SHTF!");
                }
                if (!SettingBindings[key].Contains(handler))
                {
                    throw new InvalidOperationException(Resources.EXCEPTION_BINDING_NOT_FOUND);
                }
                SettingBindings[key].Remove(handler);
            }
        }

        public void Save(String registryKey)
        {
            var bf = new BinaryFormatter();

            lock (_settingsLock)
            {
                foreach (String key in Settings.Keys)
                {
                    if (RegistryInterface.RegistryTypeLookup.ContainsKey(SettingsTypes[key]))
                    {
                        //Recognized Type, Serialize and save using RegistryInterface's Parser
                        var enumerable = Settings[key] as IEnumerable<string>;
                        RegistryInterface.WriteRegistry(registryKey, key,
                                                        enumerable != null ? enumerable.ToArray() : Settings[key]);
                    }
                    else
                    {
                        //Unrecognized Type, Serialize to binary and save as binary
                        using (var ms = new MemoryStream())
                        {
                            bf.Serialize(ms, Settings[key]);
                            RegistryInterface.WriteRegistry(registryKey, key, ms.ToArray());
                        }
                    }
                }
            }
        }

        public void Load(String registryKey)
        {
            Boolean entryFound = false;

            lock (_settingsLock)
            {
                foreach (String key in Settings.Keys.ToArray())
                {
                    object retValue;
                    if (RegistryInterface.RegistryTypeLookup.ContainsKey(SettingsTypes[key]))
                    {
                        //Recognized Type, Deserialize and load using RegistryInterface's Parser
                        if (typeof (IEnumerable<String>).IsAssignableFrom(SettingsTypes[key]))
                        {
                            if (SettingsTypes[key] == typeof (String[]))
                            {
                                List<String> retStringList =
                                    RegistryInterface.ReadRegistryCollection<String>(registryKey, key);
                                if (retStringList != null)
                                {
                                    retValue = retStringList.ToArray();
                                    Settings[key] = retValue;
                                    OnSettingChanged(key);
                                }
                            }
                            else
                            {
                                ConstructorInfo cInfo =
                                    SettingsTypes[key].GetConstructor(new[] {typeof (IEnumerable<String>)});
                                if (cInfo != null)
                                {
                                    retValue = cInfo.Invoke(new object[]
                                        {
                                            RegistryInterface.ReadRegistryCollection<String>(registryKey, key)
                                        });
                                    if (retValue != null)
                                    {
                                        Settings[key] = retValue;
                                        OnSettingChanged(key);
                                    }
                                }
                            }
                        }
                        else
                        {
                            MethodInfo method = typeof (RegistryInterface).GetMethod("ReadRegistry")
                                                                          .MakeGenericMethod(new[] {SettingsTypes[key]});

                            var param = new[]
                                {
                                    registryKey, key, entryFound,
                                    SettingsTypes[key].IsValueType ? Activator.CreateInstance(SettingsTypes[key]) : null
                                };

                            retValue = method.Invoke(this, param);
                            if (retValue != null && (Boolean) param[2])
                            {
                                Settings[key] = retValue;
                                OnSettingChanged(key);
                            }
                        }
                    }
                    else
                    {
                        //Unrecognized Type, Deserialize as binary
                        var bf = new BinaryFormatter();
                        var retByteArray = RegistryInterface.ReadRegistry<Byte[]>(registryKey, key, out entryFound);

                        if (retByteArray != null)
                        {
                            using (var ms = new MemoryStream(retByteArray))
                            {
                                retValue = bf.Deserialize(ms);
                                Settings[key] = retValue;
                                OnSettingChanged(key);
                            }
                        }
                    }
                }
            }
        }

        private void OnSettingChanged(String key)
        {
            lock (_settingsLock)
            {
                if (!Settings.ContainsKey(key))
                {
                    throw new KeyNotFoundException(Resources.EXCEPTION_KEY_NOT_FOUND);
                }
                if (SettingBindings.ContainsKey(key) && SettingBindings[key] != null)
                {
                    foreach (EventHandler<SettingChangedEventArgs> handler in SettingBindings[key])
                    {
                        if (handler != null)
                        {
                            TriggerHandler(key, handler);
                        }
                    }
                }
            }
        }

        private void TriggerHandler(String key, EventHandler<SettingChangedEventArgs> handler)
        {
            lock (_settingsLock)
            {
                if (handler == null)
                {
                    throw new NullReferenceException(Resources.EXCEPTION_HANDLER_NULL);
                }
                SettingChangedEvent += handler;
                SettingChangedEvent(key, new SettingChangedEventArgs(this));
                SettingChangedEvent -= handler;
            }
        }
    }

    public interface ISettingProvider
    {
        T ReadSetting<T>(String key);
    }

    public interface ISettingWriter
    {
        void ChangeSetting<T>(String key, T value);
        void Save(String registryKey);
    }

    public interface INotifySettingChanged
    {
        void OnSettingChanged(object sender, SettingChangedEventArgs e);
    }
}