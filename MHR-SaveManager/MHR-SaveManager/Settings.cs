using System;
using System.IO;
using System.Text.RegularExpressions;

namespace MHR_SaveManager
{
    public static class SettingsHelper
    {
        public static Regex SS_data_slot_bin_Regex = new Regex(@"^SS[0-9]_data[0-9]{3}Slot\.bin$", RegexOptions.Compiled);
        private static Settings _CurrentSettings;
        public static Settings CurrentSettings
        {
            get
            {
                if (_CurrentSettings == null)
                {
                    var settings = new Settings();

                    //Try to read the current ini, if it doesn't exist create a new one, if it does exist read the values
                    var iniFile = new IniFile();

                    //Exists read values
                    if (File.Exists(iniFile.Path))
                    {
                        foreach (var prop in settings.GetType().GetProperties())
                        {
                            var exists = iniFile.KeyExists(prop.Name, "General");

                            if (exists)
                            {
                                var value = iniFile.Read(prop.Name, "General");
                                prop.SetValue(settings, Convert.ChangeType(value, prop.PropertyType), null);
                            }
                            else
                            {
                                var propValue = prop.GetValue(settings, null);
                                iniFile.Write(prop.Name, propValue.ToString(), "General");
                            }
                        }
                    }
                    //Write values to ini file
                    else
                    {
                        foreach (var prop in settings.GetType().GetProperties())
                        {
                            var propValue = prop.GetValue(settings, null);
                            iniFile.Write(prop.Name, propValue.ToString(), "General");
                        }
                    }

                    _CurrentSettings = settings;
                }

                return _CurrentSettings;
            }
            set
            {
                _CurrentSettings = value;
            }
        }
    }

    public class Settings
    {
        private int _MaxSaves = 100;
        private int _BackupInterval = 15;
        private string _AlternativeLaunchExecutable = string.Empty;

        //How many saves to keep
        public int MaxSaves
        {
            get
            {
                return _MaxSaves;
            }
            set
            {
                if (value <= 0)
                {
                    _MaxSaves = 1;
                }
                else
                {
                    _MaxSaves = 1;
                }
            }
        }

        //How many minutes to pass before we backup the saves
        public int BackupInterval
        {
            get
            {
                return _BackupInterval;
            }
            set
            {
                if (value <= 0)
                {
                    _BackupInterval = 1;
                }
                else
                {
                    _BackupInterval = value;
                }
            }
        }

        //Do we want to auto launch the game
        public bool EnableAutoGameLaunch { get; set; } = true;

        //Do we want to use an alternative executable to launch the game e.g. monster hunter rise can use hunterpie
        public bool UseAlternativeLaunchExecutable { get; set; } = false;

        public string AlternativeLaunchExecutable
        {
            get
            {
                return UseAlternativeLaunchExecutable ? _AlternativeLaunchExecutable : string.Empty;
            }
            set { _AlternativeLaunchExecutable = value; }
        }

        //Do we want to ignore any files with SS#_data#slot.bin
        public bool IgnoreSS_data_slot_bin_Files { get; set; } = false;
    }
}