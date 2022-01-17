using System;
using System.Collections.Specialized;
using System.Xml.Serialization;
using Torch;
using Torch.Views;
using VRage.Collections;
using VRageMath;
using VRage.Game.Entity;


namespace PVEServerPlugin.Modules
{
    [Serializable]
    public class Zone : ViewModel
    {
        private bool _enable;
        private double _xValue;
        private double _yValue;
        private double _zValue;
        private int _radius;
        private string _name;
        private string _entryMessage;
        private string _exitMessage;
        private MyConcurrentHashSet<long> _containsEntities = new MyConcurrentHashSet<long>();

        public Zone()
        {
            CollectionChanged += OnCollectionChanged;

        }

        private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged();
            Save();
        }

        public void Save()
        {
            Config.Instance.Save();
        }

        [XmlIgnore]
        [Display(Visible = false)]
        public MyConcurrentHashSet<long> ContainsEntities => _containsEntities;

        [Display(Order = 0, Name = "Name", Description = "Name of the limit. This helps with some of the commands")]
        public string Name
        {
            get => _name;
            set
            {
                _name = string.IsNullOrEmpty(value) ? $"Zone {Config.Instance.PvpZones.Count + 1}":value;
                OnPropertyChanged();
            }
        }
        [Display(Order = 1, Name = "Enable Zone", Description = "Check box to enable this zone")]
        public bool Enable
        {
            get => _enable;
            set
            {
                _enable = value;
                OnPropertyChanged();
            }
        }

        [Display(Order = 2, Name = "Radius", Description = "Zone radius in meters")]
        public int Radius
        {
            get => _radius;
            set
            {
                _radius = value;
                OnPropertyChanged();
            }
        }

        [Display(Order = 3, Name = "X")]
        public double X
        {
            get => _xValue;
            set
            {
                _xValue = value;
                OnPropertyChanged();
            }
        }


        [Display(Order = 4, Name = "Y")]
        public double Y
        {
            get => _yValue;
            set
            {
                _yValue = value;
                OnPropertyChanged();
            }
        }


        [Display(Order = 5, Name = "Z")]
        public double Z
        {
            get => _zValue;
            set
            {
                _zValue = value;
                OnPropertyChanged();
            }
        }

        [Display(Order = 6, Name = "Entry Message")]
        public string EntryMessage
        {
            get => _entryMessage;
            set
            {
                _entryMessage = value;
                OnPropertyChanged();
            }
        }

        [Display(Order = 7, Name = "Exit Message")]
        public string ExitMessage
        {
            get => _exitMessage;
            set
            {
                _exitMessage = value;
                OnPropertyChanged();
            }
        }

        public override string ToString()
        {
            var useName = string.IsNullOrEmpty(Name) ? "UnNamed Zone" : Name;
            return useName;
        }

        public bool IsWithinZoneRadius(MyEntity entity)
        {
            if (!_enable) return false;
            var sphere = new BoundingSphere(new Vector3(_xValue, _yValue, _zValue), _radius);
            return sphere.Contains(entity.PositionComp.GetPosition()) == ContainmentType.Contains  ;
        }
        
        
    }
}