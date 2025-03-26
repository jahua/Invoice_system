using System.Threading.Tasks;

namespace InvoiceSystem.Domain.Interfaces
{
    public interface IDataSeedingService
    {
        Task SeedAllDataAsync();
        Task ClearAllDataAsync();
    }
} 