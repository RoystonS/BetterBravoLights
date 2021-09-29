
using System;

namespace BravoLights.Connections
{
    public interface IConnection
    {
        void AddListener(IVariable variable, EventHandler<ValueChangedEventArgs> handler);
        void RemoveListener(IVariable variable, EventHandler<ValueChangedEventArgs> handler);

        void SendLastValue(IVariable variable, object sender, EventHandler<ValueChangedEventArgs> handler);
    }
}
