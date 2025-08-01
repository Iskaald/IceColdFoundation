using System.Threading.Tasks;

namespace IceCold.Interface
{
    /// <summary>
    /// Asynchronously prepares the service for quitting.
    /// This is called when the user tries to close the application.
    /// </summary>
    /// <returns>
    /// A Task that resolves to 'true' if the app can quit, 
    /// or 'false' if the quit should be aborted (e.g., save failed or was cancelled by user).
    /// </returns>
    public interface IQuittingService
    {
        public Task<bool> CanQuitAsync();
    }
}