using SylviaNG.Assets.Infrastructure.Data;

namespace SylviaNG.Assets.SharedKernel.Generic
{
    public interface IUnitOfWork
    {
        Task<int> SaveChangesAsync();

        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();

        ApplicationDBContext Context { get; }
    }
}
