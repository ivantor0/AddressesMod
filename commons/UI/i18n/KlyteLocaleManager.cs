﻿using ColossalFramework;
using ColossalFramework.Globalization;
using Klyte.Commons.Extensions;
using Klyte.Commons.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Klyte.Commons.i18n
{
    public class KlyteLocaleManager : MonoBehaviour
    {
        internal static readonly string m_translateFilesPath = $"{FileUtils.BASE_FOLDER_PATH}__translations{Path.DirectorySeparatorChar}";

        public static readonly string[] locales = new string[] { "en", "pt", "ko", "de", "cn", "pl", "nl", "fr", "es", "ru", "zh", "ja" };

        public const string m_defaultPrefixInGame = "K45_";
        public const string m_defaultTestKey = "K45_TEST_UP";
        public const string m_defaultTestValue = "OK_V2";
        public const string m_defaultModControllingKey = "K45_MOD_CONTROLLING_LOCALE";

        private const string m_lineSeparator = "\r\n";
        private const string m_kvSeparator = "=";
        private const string m_idxSeparator = ">";
        private const string m_localeKeySeparator = "|";
        private const string m_commentChar = "#";
        private const string m_ignorePrefixChar = "%";
        internal static readonly Func<LocaleManager, Locale> m_localeManagerLocale = ReflectionUtils.GetGetFieldDelegate<LocaleManager, Locale>(typeof(LocaleManager).GetField("m_Locale", RedirectorUtils.allFlags));
        internal static readonly Func<Locale, Dictionary<Locale.Key, string>> m_localeStringsDictionary = ReflectionUtils.GetGetFieldDelegate<Locale, Dictionary<Locale.Key, string>>(typeof(Locale).GetField("m_LocalizedStrings", RedirectorUtils.allFlags));

        internal static SavedString CurrentLanguageId => new SavedString("K45_LoadedLanguage", Settings.gameSettingsFile, "en", true);

        private static string m_language = "";

        public void Awake()
        {
            m_localeStringsDictionary(m_localeManagerLocale(LocaleManager.instance))[new Locale.Key() { m_Identifier = m_defaultTestKey }] = m_defaultTestValue;
            foreach (string lang in locales)
            {
                FileUtils.EnsureFolderCreation($"{m_translateFilesPath}{lang}");
                var di = new DirectoryInfo($"{m_translateFilesPath}{lang}");

                foreach (FileInfo file in di.GetFiles())
                {
                    if (file.Name.StartsWith("1_") || file.Name.StartsWith("0_") || file.Name.StartsWith("9_"))
                    {
                        file.Delete();
                    }
                }
            }


            LogUtils.DoLog($"Set Lang :{ CurrentLanguageId.value}");
            m_language = Array.IndexOf(locales, CurrentLanguageId.value) < 0 ? "" : CurrentLanguageId.value;
            LogUtils.DoLog($"Load Lang { CurrentLanguageId.value}/{m_language}");
            ReloadLanguage();
            LogUtils.DoLog($"Lang Loaded");
        }

        public static void SetLocaleEntry(Locale.Key key, string value) => m_localeStringsDictionary(m_localeManagerLocale(LocaleManager.instance))[key] = value;

        private static bool m_alreadyLoading = false;
        public int LoadedLanguageIdx
        {
            get => GetLoadedLanguage();
            set {
                string newVal = value <= 0 || value > locales.Length ? "" : locales[value - 1];
                if (newVal == m_language)
                {
                    return;
                }

                CurrentLanguageId.value = newVal;
                ReloadLanguage();
            }
        }

        public static void ReloadLanguage() => ReloadLanguage(false);
        public static void ReloadLanguage(bool skipUI)
        {
            if (FindObjectOfType<KlyteLocaleManager>() != null)
            {
                m_localeStringsDictionary(m_localeManagerLocale(LocaleManager.instance))[new Locale.Key() { m_Identifier = m_defaultTestKey }] = m_defaultTestValue;
                m_localeStringsDictionary(m_localeManagerLocale(LocaleManager.instance))[new Locale.Key() { m_Identifier = m_defaultModControllingKey }] = CommonProperties.ModName;
            }

            if (m_alreadyLoading)
            {
                return;
            }

            m_alreadyLoading = true;
            m_language = CurrentLanguageId.value;
            ReadLanguage("en");
            string targetLanguage = m_language == "" ? LocaleManager.instance.language.Substring(0, 2) : m_language;
            if (m_language != "en" && locales.Contains(targetLanguage))
            {
                ReadLanguage(targetLanguage);
            }

            if (!skipUI)
            {
                RedrawUIComponents();
            }

            m_alreadyLoading = false;
        }

        public static void RedrawUIComponents()
        {
            foreach (string eventLocale in new string[] { "eventUIComponentLocaleChanged", "eventLocaleChanged" })
            {
                FieldInfo field = typeof(LocaleManager).GetField(eventLocale, RedirectorUtils.allFlags);
                if (field.GetValue(LocaleManager.instance) != null)
                {
                    foreach (Delegate eventHandler in (field.GetValue(LocaleManager.instance) as MulticastDelegate).GetInvocationList())
                    {
                        try
                        { eventHandler.Method.Invoke(eventHandler.Target, new object[] { }); }
                        catch { }
                    }
                }
            }
        }

        private static void ReadLanguage(string languageCode)
        {
            string folderPath = $"{m_translateFilesPath}{languageCode}{Path.DirectorySeparatorChar}";
            var files = Directory.GetFiles(folderPath, "*.txt").ToList();
            files.Sort();
            LogUtils.DoLog($"{string.Join(",", files.ToArray())}");
            foreach (string file in files)
            {
                FileSplitter(File.ReadAllText(file));
            }
        }


        internal static void FileSplitter(string fileContents)
        {
            foreach (string myString in fileContents.Split(m_lineSeparator.ToCharArray(), StringSplitOptions.RemoveEmptyEntries))
            {
                if (myString.StartsWith(m_commentChar))
                {
                    continue;
                }

                if (!myString.Contains(m_kvSeparator))
                {
                    continue;
                }

                bool noPrefix = myString.StartsWith(m_ignorePrefixChar);
                string[] array = myString.Split(m_kvSeparator.ToCharArray(), 2);
                string value = array[1];
                int idx = 0;
                string localeKey = null;
                if (array[0].Contains(m_idxSeparator))
                {
                    string[] arrayIdx = array[0].Split(m_idxSeparator.ToCharArray());
                    if (!int.TryParse(arrayIdx[1], out idx))
                    {
                        continue;
                    }
                    array[0] = arrayIdx[0];

                }
                if (array[0].Contains(m_localeKeySeparator))
                {
                    array = array[0].Split(m_localeKeySeparator.ToCharArray());
                    localeKey = array[1];
                }
                var k = new Locale.Key()
                {
                    m_Identifier = noPrefix ? array[0].Substring(1) : (m_defaultPrefixInGame + array[0]),
                    m_Key = localeKey,
                    m_Index = idx
                };



                m_localeStringsDictionary(m_localeManagerLocale(LocaleManager.instance))[k] = value.Replace("\\n", "\n").Replace("\\t", "\t");
            }
        }

        internal static void SaveLoadedLanguage(int value)
        {
            string newVal = value <= 0 || value > locales.Length ? "" : locales[value - 1];
            CurrentLanguageId.value = newVal;
        }
        internal static int GetLoadedLanguage() => Array.IndexOf(locales, CurrentLanguageId.value) + 1;
    }
}
