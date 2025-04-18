﻿using ColossalFramework;
using ICities;
using Klyte.Commons.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace Klyte.Commons.Interfaces
{
    [XmlRoot("ConfigWarehouse")]
    public abstract class ConfigWarehouseBase<T, I> where T : Enum, IConvertible where I : ConfigWarehouseBase<T, I>, new()
    {

        public const string GLOBAL_CONFIG_INDEX = "DEFAULT";
        protected const int TYPE_STRING = 0x100;
        protected const int TYPE_INT = 0x200;
        protected const int TYPE_BOOL = 0x300;
        protected const int TYPE_PART = 0xF00;
        protected const int TYPE_DICTIONARY = 0x500;

        protected static Dictionary<string, I> loadedCities = new Dictionary<string, I>();

        protected string cityId;
        protected string cityName;

        protected bool IsDefaultFile => cityId == GLOBAL_CONFIG_INDEX;

        public static bool IsCityLoaded => Singleton<SimulationManager>.instance.m_metaData != null;
        protected string CurrentCityId => IsCityLoaded ? Singleton<SimulationManager>.instance.m_metaData.m_gameInstanceIdentifier : GLOBAL_CONFIG_INDEX;
        protected string CurrentCityName => IsCityLoaded ? Singleton<SimulationManager>.instance.m_metaData.m_CityName : GLOBAL_CONFIG_INDEX;

        protected string ThisFileName => $"{GetType().Name}_{(cityId ?? GLOBAL_CONFIG_INDEX)}.xml";
        protected string DefaultFileName => $"{GetType().Name}_{(GLOBAL_CONFIG_INDEX)}.xml";
        public string ThisPath => $"{CommonProperties.ModRootFolder }{Path.DirectorySeparatorChar}{ ThisFileName}";
        public string DefaultPath => $"{CommonProperties.ModRootFolder }{Path.DirectorySeparatorChar}{ DefaultFileName}";


        //public static bool GetCurrentConfigBool(T i) => instance.CurrentLoadedCityConfig.GetBool(i);
        //public static void SetCurrentConfigBool(T i, bool? value) => instance.CurrentLoadedCityConfig.SetBool(i, value);
        //public static int GetCurrentConfigInt(T i) => instance.CurrentLoadedCityConfig.GetInt(i);
        //public static void SetCurrentConfigInt(T i, int? value) => instance.CurrentLoadedCityConfig.SetInt(i, value);
        //public static string GetCurrentConfigString(T i) => instance.CurrentLoadedCityConfig.GetString(i);
        //public static void SetCurrentConfigString(T i, string value) => instance.CurrentLoadedCityConfig.SetString(i, value);

        public I CurrentLoadedCityConfig => GetConfig(CurrentCityId, CurrentCityName);




        public I GetConfig2(string cityId, string cityName) => GetConfig(cityId, cityName);
        public I GetConfig2() => GetConfig(null, null);

        public static I GetConfig() => GetConfig(null, null);

        public static I GetConfig(string cityId, string cityName)
        {
            if (cityId == null || cityName == null)
            {
                cityId = GLOBAL_CONFIG_INDEX;
                cityName = GLOBAL_CONFIG_INDEX;
            }
            if (!loadedCities.ContainsKey(cityId))
            {
                loadedCities[cityId] = Construct(cityId, cityName);
            }
            return loadedCities[cityId];
        }

        protected static I Construct(string cityId, string cityName)
        {
            if (string.IsNullOrEmpty(cityId))
            {
                throw new Exception("CITY ID NÃO PODE SER NULO!!!!!");
            }
            var result = new I
            {
                cityId = cityId,
                cityName = cityName
            };

            if (cityId != GLOBAL_CONFIG_INDEX)
            {
                FileUtils.EnsureFolderCreation(CommonProperties.ModRootFolder);
                try
                {
                    I defaultFile = GetConfig(GLOBAL_CONFIG_INDEX, GLOBAL_CONFIG_INDEX);
                    try
                    {
                        foreach (var entry in defaultFile.m_cachedBoolSaved)
                        {
                            result.SetBool(entry.Key, entry.Value);
                        }
                        foreach (var entry in defaultFile.m_cachedIntSaved)
                        {
                            result.SetInt(entry.Key, entry.Value);
                        }
                        foreach (var entry in defaultFile.m_cachedStringSaved)
                        {
                            result.SetString(entry.Key, entry.Value);
                        }
                    }
                    catch (Exception e)
                    {
                        LogUtils.DoErrorLog($"Erro copiando propriedades para o novo arquivo da classe {typeof(I)}: {e.Message}");
                    }
                }
                catch
                {

                }
            }
            else
            {
                if (File.Exists(result.ThisPath))
                {
                    string text = File.ReadAllText(result.ThisPath);
                    try
                    {
                        result = Deserialize(text);
                        if (result == null)
                        {
                            result = new I
                            {
                                cityId = cityId,
                                cityName = cityName
                            };
                            result.FallBackDefaultFile();
                        }
                    }
                    catch (Exception e)
                    {
                        LogUtils.DoErrorLog($"{typeof(I)} CORRUPTED DATA! => Data:\n{text}\nException: {e.Message}\n{e.StackTrace}");
                        result = new I
                        {
                            cityId = cityId,
                            cityName = cityName
                        };
                        result.FallBackDefaultFile();
                    }
                }
                else
                {
                    result?.FallBackDefaultFile();
                }
                result.EventOnPropertyChanged += (a, b, c, d) => result.SaveAsDefault();
            }
            return result;
        }

        protected virtual void FallBackDefaultFile() { }
        public void SaveAsDefault()
        {
            File.WriteAllText(DefaultPath, Serialize((I)this));
            LogUtils.DoErrorLog($"Saved global at {DefaultPath}");
        }
        public void LoadFromDefault()
        {
            if (File.Exists(DefaultPath))
            {
                loadedCities[cityId ?? GLOBAL_CONFIG_INDEX] = Deserialize(File.ReadAllText(DefaultPath));
                LogUtils.DoErrorLog($"Loaded {cityId} from {DefaultPath}");
            }
        }
        public string Export()
        {
            File.WriteAllText(ThisPath, Serialize((I)this));
            LogUtils.DoErrorLog($"Saved Export at {ThisPath}");
            return ThisPath;
        }
        public void ReloadFromDisk()
        {
            if (File.Exists(ThisPath))
            {
                loadedCities[cityId ?? GLOBAL_CONFIG_INDEX] = Deserialize(File.ReadAllText(ThisPath));
                LogUtils.DoErrorLog($"Saved {cityId} from {ThisPath}");
            }
        }
        public virtual string GetString(T i) => GetFromFileString(i) ?? GetDefaultStringValueForProperty(i);
        public virtual bool GetBool(T i) => GetFromFileBool(i) ?? GetDefaultBoolValueForProperty(i);
        public virtual int GetInt(T i) => GetFromFileInt(i) ?? GetDefaultIntValueForProperty(i);
        public virtual void SetString(T i, string value) => SetToFile(i, value);
        public virtual void SetBool(T idx, bool? newVal) => SetToFile(idx, newVal);
        public virtual void SetInt(T idx, int? value) => SetToFile(idx, value);

        [XmlElement("StringData")]
        public SimpleEnumerableList<T, string> m_cachedStringSaved = new SimpleEnumerableList<T, string>();
        [XmlElement("IntData")]
        public SimpleEnumerableList<T, int?> m_cachedIntSaved = new SimpleEnumerableList<T, int?>();
        [XmlElement("BoolData")]
        public SimpleEnumerableList<T, bool?> m_cachedBoolSaved = new SimpleEnumerableList<T, bool?>();


        private string GetSavedString(T i)
        {
            if (!m_cachedStringSaved.ContainsKey(i))
            {
                m_cachedStringSaved[i] = null;
            }
            return m_cachedStringSaved[i];
        }
        private bool? GetSavedBool(T i)
        {
            if (!m_cachedBoolSaved.ContainsKey(i))
            {
                m_cachedBoolSaved[i] = null;
            }
            return m_cachedBoolSaved[i];
        }
        private int? GetSavedInt(T i)
        {
            if (!m_cachedIntSaved.ContainsKey(i))
            {
                m_cachedIntSaved[i] = null;
            }
            return m_cachedIntSaved[i];
        }

        protected string GetFromFileString(T i) => GetSavedString(i);
        protected int? GetFromFileInt(T i) => GetSavedInt(i);
        protected bool? GetFromFileBool(T i) => GetSavedBool(i);

        protected void SetToFile(T i, string value)
        {
            m_cachedStringSaved[i] = value;

            EventOnPropertyChanged?.Invoke(i, null, null, value);
        }
        protected void SetToFile(T i, bool? value)
        {
            m_cachedBoolSaved[i] = value;
            EventOnPropertyChanged?.Invoke(i, value, null, null);
        }

        protected void SetToFile(T i, int? value)
        {
            m_cachedIntSaved[i] = value;

            EventOnPropertyChanged?.Invoke(i, null, value, null);
        }

        public abstract bool GetDefaultBoolValueForProperty(T i);
        public abstract int GetDefaultIntValueForProperty(T i);
        public virtual string GetDefaultStringValueForProperty(T i) => string.Empty;

        #region Serialization
        private static I Deserialize(string data) => XmlUtils.DefaultXmlDeserialize<I>(data) ?? new I();

        private static string Serialize(I data) => XmlUtils.DefaultXmlSerialize(data, false);
        public void OnReleased() { }

        #endregion

        #region Serialization
        protected abstract string ID { get; }
        //[XmlIgnore]
        //public IManagers Managers => SerializableDataManager?.managers;
        //[XmlIgnore]
        //public ISerializableData SerializableDataManager { get; private set; }

        //public void OnCreated(ISerializableData serializableData) => SerializableDataManager = serializableData;
        public I GetLoadData(ISerializableData serializableData)
        {
            if (ID == null || Singleton<ToolManager>.instance.m_properties.m_mode != ItemClass.Availability.Game)
            {
                return null;
            }
            if (!serializableData.EnumerateData().Contains(ID))
            {
                return null;
            }
            using (var memoryStream = new MemoryStream(serializableData.LoadData(ID)))
            {
                byte[] storage = memoryStream.ToArray();
                return Deserialize(System.Text.Encoding.UTF8.GetString(storage));
            }
        }

        // Token: 0x0600003B RID: 59 RVA: 0x00004020 File Offset: 0x00002220
        //public void OnSaveData()
        //{
        //    if (ID == null || Singleton<ToolManager>.instance.m_properties.m_mode != ItemClass.Availability.Game)
        //    {
        //        return;
        //    }

        //    string serialData = Serialize(loadedCities[CurrentCityId]);
        //    LogUtils.DoLog($"serialData: {serialData ?? "<NULL>"}");
        //    if (serialData == null)
        //    {
        //        return;
        //    }

        //    byte[] data = System.Text.Encoding.UTF8.GetBytes(serialData);
        //    SerializableDataManager.SaveData(ID, data);
        //}

        #endregion

        public event OnWarehouseConfigChanged EventOnPropertyChanged;


        public delegate void OnWarehouseConfigChanged(T idx, bool? newValueBool, int? newValueInt, string newValueString);
    }
}
