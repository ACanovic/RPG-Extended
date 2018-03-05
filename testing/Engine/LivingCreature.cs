using System.ComponentModel;

namespace Engine
{
    public class LivingCreature : INotifyPropertyChanged
    {
        private int _currentHitPoints;
        public int CurrentHitPoints
        {
            get { return _currentHitPoints; }
            set
            {
                _currentHitPoints = value;
                OnPropertyChanged("CurrentHitPonts");
            }
        }

        public int MaxHitPoints { get; set; }

        public bool IsDead { get { return CurrentHitPoints <= 0;  } }

        public LivingCreature(int currentHitPoints, int maxHitPoints)
        {
            CurrentHitPoints = currentHitPoints;
            MaxHitPoints = maxHitPoints;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name)
        {
            if(PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }
    }
}
