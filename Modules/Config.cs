using System;
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
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private ObservableCollection<ConflictPairs> _conflictPairs;
        private bool _loading;
        private XmlAttributeOverrides _overrides;
        private bool _enableFactionDamage = true;

        public static Config Instance => _instance ?? (_instance = new Config());


        public Config()
        {
            _conflictPairs = new ObservableCollection<ConflictPairs>();
            _conflictPairs.CollectionChanged += ConflictPairsOnCollectionChanged;
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

        [Display(Order = 2, Name = "Enable Conflicts", Description = "Toggles the state of the plugin")]
        public bool EnableConflict
        {
            get => _enableChallenge;
            set
            {
                _enableChallenge = value;
                OnPropertyChanged();
            }
        }

        [Display(Order = 3, Name = "Allow Faction Member Damage", Description = "Toggles the state of the plugin")]
        public bool EnableFactionDamage
        {
            get => _enableFactionDamage;
            set
            {
                _enableFactionDamage = value;
                OnPropertyChanged();
            }
        }

        [Display(Visible = false)]
        public ObservableCollection<ConflictPairs> ConflictPairs
        {
            get => _conflictPairs;
            set
            {
                _conflictPairs = value;
                OnPropertyChanged();
            }
        }
        private void ConflictPairsOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
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
                        XmlSerializer x;
                        if (_overrides != null)
                            x = new XmlSerializer(typeof(Config), _overrides);
                        else
                        {
                            x = new XmlSerializer(typeof(Config));
                        }
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
                        Instance.ConflictPairs.Add(new ConflictPairs());
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
                    Log.Error(e, "Failed to load config" );
                }
            }
        }
    }
}