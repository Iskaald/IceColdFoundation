namespace IceCold.Interface
{
    public interface ICoreService
    {
        public void Initialize();
        public void Deinitialize();
        
        public bool IsInitialized { get; }

        public bool OnWillQuit();
    }
}