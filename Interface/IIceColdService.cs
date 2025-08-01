namespace IceCold.Interface
{
    public interface IIceColdService
    {
        public void Initialize();
        public void Deinitialize();
        
        public bool IsInitialized { get; }
        
        public void OnWillQuit();
    }
}