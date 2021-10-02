using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace BravoLights.UI
{
    public abstract class ViewModelBase : INotifyPropertyChanged
    {
        protected void SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if ((field == null && value != null) || (field != null && !field.Equals(value)))
            {
                field = value;
                RaisePropertyChanged(propertyName);
            }
        }

        protected void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
