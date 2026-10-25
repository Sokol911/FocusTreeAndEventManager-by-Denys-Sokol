using System.Configuration;
using System.Collections.Generic;
using System.Linq;

namespace FocusTreeManager
{
    public class LanguageSelector
    {
        public string Name { get; set; }

        public string FileName { get; set; }
    }

    public static class Configurator
    {
        
        public static string getLanguage()
        {
            return Properties.Settings.Default.Language;
        } 

        public static void setLanguage(string Language)
        {
            Properties.Settings.Default.Language = Language;
            Properties.Settings.Default.Save();
        }

        public static List<LanguageSelector> returnAllLanguages()
        {
            string Languages = Properties.Resources.Languages;
            return Languages.Split(',').Select(language => new LanguageSelector
            {
                FileName = language.Split(';')[0], Name = language.Split(';')[1]
            }).ToList();
        }

        public static void setGamePath(string path)
        {
            Properties.Settings.Default.Path = path;
            Properties.Settings.Default.Save();
        }

        public static string getGamePath()
        {
            return Properties.Settings.Default.Path;
        }

        public static void setFirstStart()
        {
            Properties.Settings.Default.IsFirstStart = true;
            Properties.Settings.Default.Save();
        }

        public static bool getFirstStart()
        {
            return Properties.Settings.Default.IsFirstStart;
        }

        public static void setScripterPreference(string preference)
        {
            Properties.Settings.Default.ScripterPreference = preference;
            Properties.Settings.Default.Save();
        }

        public static string getScripterPreference()
        {
            return Properties.Settings.Default.ScripterPreference;
        }

        public static void setEditorShowStruct(bool preference)
        {
            Properties.Settings.Default.EditorShowStruct = preference;
            Properties.Settings.Default.Save();
        }

        public static bool getEditorShowStruct()
        {
            return Properties.Settings.Default.EditorShowStruct;
        }

        public static void setEditorShowPlan(bool preference)
        {
            Properties.Settings.Default.EditorShowPlan = preference;
            Properties.Settings.Default.Save();
        }

        public static bool getEditorShowPlan()
        {
            return Properties.Settings.Default.EditorShowPlan;
        }

        public const int AutosaveMinMinutes = 1;

        public const int AutosaveMaxMinutes = 60;

        public static bool getAutosaveEnabled()
        {
            return Properties.Settings.Default.AutosaveEnabled;
        }

        public static void setAutosaveEnabled(bool enabled)
        {
            Properties.Settings.Default.AutosaveEnabled = enabled;
            Properties.Settings.Default.Save();
        }

        public static int getAutosaveMinutes()
        {
            int minutes = Properties.Settings.Default.AutosaveMinutes;
            return minutes < AutosaveMinMinutes ? AutosaveMinMinutes :
                   minutes > AutosaveMaxMinutes ? AutosaveMaxMinutes : minutes;
        }

        public static void setAutosaveMinutes(int minutes)
        {
            minutes = minutes < AutosaveMinMinutes ? AutosaveMinMinutes :
                      minutes > AutosaveMaxMinutes ? AutosaveMaxMinutes : minutes;
            Properties.Settings.Default.AutosaveMinutes = minutes;
            Properties.Settings.Default.Save();
        }
    }
}
