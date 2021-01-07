using System;
using System.Collections.Specialized;
using System.Windows.Documents;
using Torch;
using Torch.Views;
using Sandbox.Game.Entities;
using Sandbox.Game.EntityComponents;
using VRageMath;
using VRage.Game.Entity;
using VRage.Game.ModAPI.Ingame;


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

        public bool IsWithinZoneRadius(MyEntity entity)
        {
            var sphere = new BoundingSphere(new Vector3(_xValue, _yValue, _zValue), _radius);
            return sphere.Contains(entity.PositionComp.GetPosition()) == ContainmentType.Contains  ;
        }
        
        
    }
}