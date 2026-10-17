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

        Task UpdateProfileAsync(Leitor leitor);

        Task UpdateDadosAsync(Leitor leitor);

        Task<int> ContarLivrosEmprestadosAsync(int id);

        Task<bool> TemHistoricoAsync(int id);

        Task DeleteAsync(Leitor leitor);
    }
}