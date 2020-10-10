using System.Collections.Specialized;
using Torch;

namespace PVEServerPlugin.Modules
{
    public class ConflictPairs : ViewModel
    {
        private long _id;
        private long _challengingId;
        private bool _pending = true;

        public ConflictPairs()
        {
            CollectionChanged += OnCollectionChanged;
        }

        private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged();
        }

        public long Id
        {
            get => _id;
            set
            {
                _id = value;
                OnPropertyChanged();
            }
        }

        public long ChallengingId
        {
            get => _challengingId;
            set
            {
                _challengingId = value;
                OnPropertyChanged();
            }
        }

        public bool Pending
        {
            get => _pending;
            set
            {
                _pending = value;
                OnPropertyChanged();
            }
        }

    }
}