using System.Collections.Generic;
using System.ComponentModel;

namespace BravoLights.Common
{
    public interface ILightsState : INotifyPropertyChanged
    {
        public IEnumerable<string> LitLights { get; }
    }
}
