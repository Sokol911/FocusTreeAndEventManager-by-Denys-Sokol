using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace FocusTreeManager.Helper
{
    public enum LocalesSource
    {
        Default = 0,
        Tutorial = 1
    }

    public static class LocalizationHelper
    {
        private static readonly string[] TypeToValueAssoc = new string[] { "", "_Tutorial"};

        private const string FALLBACK_LANGUAGE = "En_en";

        private const string LOCALE_FILE_STRING = "/FocusTreeManager;component/Languages/";

        //Parsing a language file for every translated string and for every control that
        //merges it was a large part of the time spent opening tabs. The parsed dictionaries
        //are cached per language file. ResourceDictionary belongs to the thread that created
        //it, so there is one cache per thread.
        [ThreadStatic]
        private static Dictionary<string, ResourceDictionary> localeCache;

        private static ResourceDictionary loadLocale(LocalesSource type)
        {
            string path = LOCALE_FILE_STRING + Configurator.getLanguage() +
                          TypeToValueAssoc[(int)type] + ".xaml";
            if (localeCache == null)
            {
                localeCache = new Dictionary<string, ResourceDictionary>();
            }
            ResourceDictionary dictionary;
            if (!localeCache.TryGetValue(path, out dictionary))
            {
                try
                {
                    dictionary = new ResourceDictionary
                    {
                        Source = new Uri(path, UriKind.Relative)
                    };
                }
                catch (Exception)
                {
                    //This language has no file for this kind of text (the Russian language
                    //has no tutorial texts, for example), use English instead of crashing
                    string fallback = LOCALE_FILE_STRING + FALLBACK_LANGUAGE +
                                      TypeToValueAssoc[(int)type] + ".xaml";
                    if (!localeCache.TryGetValue(fallback, out dictionary))
                    {
                        dictionary = new ResourceDictionary
                        {
                            Source = new Uri(fallback, UriKind.Relative)
                        };
                        localeCache[fallback] = dictionary;
                    }
                }
                localeCache[path] = dictionary;
            }
            return dictionary;
        }

        public static string getValueForKey(string key, LocalesSource type = LocalesSource.Default)
        {
            return loadLocale(type)[key] as string;
        }

        /// <summary>
        /// Text of the current language with String.Format arguments. If the language file
        /// has no such key, the given English text is used instead.
        /// </summary>
        public static string Tr(string key, string fallback, params object[] args)
        {
            string value = null;
            try
            {
                value = loadLocale(LocalesSource.Default)[key] as string;
            }
            catch (Exception)
            {
                //Use the fallback
            }
            if (value == null)
            {
                value = fallback;
            }
            if (args == null || args.Length == 0) return value;
            try
            {
                return string.Format(value, args);
            }
            catch (FormatException)
            {
                return value;
            }
        }

        public static ResourceDictionary getLocale(LocalesSource type = LocalesSource.Default)
        {
            //Every caller still gets its own dictionary (as before), but filled by copying
            //the cached entries (they are all strings) instead of parsing the file again.
            ResourceDictionary cached = loadLocale(type);
            ResourceDictionary copy = new ResourceDictionary();
            foreach (DictionaryEntry entry in cached)
            {
                copy.Add(entry.Key, entry.Value);
            }
            return copy;
        }
    }
}
