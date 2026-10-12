using WebBiblioteca.Models;

namespace WebBiblioteca.Data
{
    public interface ILeitorRepository
    {
        IQueryable<Leitor> GetAll();

        Task<Leitor?> GetByIdAsync(int id);

        Task CreateAsync(Leitor leitor);

        Task CreateWithUserAsync(Leitor leitor);

        Task<Leitor?> GetByUserIdAsync(string userId);

        Task<List<Leitor>> GetRegisteredReadersAsync();

        Task UpdateAsync(Leitor leitor);

        Task DeleteAsync(Leitor leitor);
    }
}