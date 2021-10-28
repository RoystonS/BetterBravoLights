namespace BravoLights.Connections
{
    public interface IWASMChannel
    {
        void ClearSubscriptions();
        void Subscribe(short id);
        void Unsubscribe(short id);
    }
}
