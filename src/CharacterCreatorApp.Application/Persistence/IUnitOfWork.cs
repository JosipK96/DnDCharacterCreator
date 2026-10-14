namespace CharacterCreatorApp.Application.Persistence
{
    public interface IUnitOfWork : IDisposable
    {
        Task SaveChangesAsync(CancellationToken cancellationToken);
    }
}
