﻿using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Xml.Serialization;
using Torch;
using Torch.Views;
using NLog;

namespace PVEServerPlugin.Modules
{
    [Serializable]
    public class Config : ViewModel
    {
        private static Config _instance;
        private bool _enablePve;
        private bool _enableChallenge;
        private bool _enableNoOwner;
        private MtObservableCollection<Zone> _pvpZones = new MtObservableCollection<Zone>();
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private ObservableCollection<ConflictPairModule> _conflictPairs = new ObservableCollection<ConflictPairModule>();
        private bool _loading;
        private XmlAttributeOverrides _overrides;
        private bool _enableFactionDamage = true;
        private bool _allowLandingGear;

        public static Config Instance => _instance ?? (_instance = new Config());



        public Config()
        {
            _conflictPairs.CollectionChanged += ItemsCollectionChanged;
            PropertyChanged += (sender, args) => Instance.Save();
            _pvpZones.CollectionChanged += ItemsCollectionChanged;
        }

        [Display(Order = 1, Name = "Enable Plugin", Description = "Toggles the state of the plugin")]
        public bool EnablePlugin
        {
            get => _enablePve;
            set
            {
                _enablePve = value;
                OnPropertyChanged();
            }
        }

        [Display(Order = 2, Name = "Enable NoOwnership Death", Description = "Allows death from grids/blocks with no ownership")]
        public bool EnableNoOwner
        {
            get => _enableNoOwner;
            set
            {
                _enableNoOwner = value;
                OnPropertyChanged();
            }
        }

        [Display(Order = 3, Name = "Enable Conflicts", Description = "Toggles the state of the plugin.  Defaults to war state if Nexus plugin is detected")]
        public bool EnableConflict
        {
            get => _enableChallenge;
            set
            {
                _enableChallenge = value;
                OnPropertyChanged();
            }
        }

        [Display(Order = 4, Name = "Allow Faction Member Damage", Description = "When enabled, faction members can kill and destroy each other's grids")]
        public bool EnableFactionDamage
        {
            get => _enableFactionDamage;
            set
            {
                _enableFactionDamage = value;
                OnPropertyChanged();
            }
        }

        [Display(Order = 5, Name = "Allow Landing Gears", Description = "When enabled, players can use landing gear to attach and move grids")]
        public bool AllowLandingGear
        {
            get => _allowLandingGear;
            set
            {
                _allowLandingGear = value;
                OnPropertyChanged();
            }
        }

        [Display(Order = 6, EditorType = typeof(EmbeddedCollectionEditor))]
        public MtObservableCollection<Zone> PvpZones
        {
            get => _pvpZones;
            set
            {
                _pvpZones = value;
                OnPropertyChanged();
            }
        }


        private void ItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged();
        }


        public void Save()
        {
            if (_loading) return;
            try
            {
                lock (this)
                {
                    var fileName = Path.Combine(Core.Instance.StoragePath, "PVEPlugin.cfg");
                    using (var writer = new StreamWriter(fileName))
                    {
                        var x = _overrides != null ? new XmlSerializer(typeof(Config), _overrides) : new XmlSerializer(typeof(Config));
                        x.Serialize(writer, _instance);
                        writer.Close();
                    }
                }
            }
            catch (Exception e)
            {
                lock (this)
                {
                    Log.Error(e, "Unable to set config");
                }
            }
        }

        public void Load()
        {
            _loading = true;
            try
            {
                lock (this)
                {
                    var fileName = Path.Combine(Core.Instance.StoragePath, "PVEPlugin.cfg");
                    if (File.Exists(fileName))
                    {
                        using (var reader = new StreamReader(fileName))
                        {
                            var x = _overrides != null
                                ? new XmlSerializer(typeof(Config), _overrides)
                                : new XmlSerializer(typeof(Config));
                            var settings = (Config) x.Deserialize(reader);

                            reader.Close();
                            if (settings != null) _instance = settings;

                        }
                    }
                    else
                    {
                        Log.Warn("No Settings. Initializing new file at " + fileName);
                        _instance = new Config();
                        using (var writer = new StreamWriter(fileName))
                        {
                            var x = _overrides != null
                                ? new XmlSerializer(typeof(Config), _overrides)
                                : new XmlSerializer(typeof(Config));
                            x.Serialize(writer, _instance);
                            writer.Close();
                        }
                    }
                }
            }
            catch (Exception e)
            {
                lock (this)
                {
                    Log.Error(e, "Failed to load config");
                }
            }
            finally
            {
                _loading = false;
            }
        }
    }
}