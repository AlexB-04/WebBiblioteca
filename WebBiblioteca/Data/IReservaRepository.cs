using WebBiblioteca.Models;

namespace WebBiblioteca.Data
{
    public interface IReservaRepository
    {
        // Processar os prazos antes de consultar ou alterar a fila.
        // Chamar antes de preparar outras alterações no DataContext.
        Task AtualizarReservasExpiradasAsync();

        IQueryable<Reserva> GetAll();

        Task<Reserva?> GetByIdAsync(int id);

        Task<bool> CreateAsync(Reserva reserva);

        Task<bool> UpdateAsync(int id, int idLivro);

        Task<bool> DeleteAsync(int id);

        Task<bool> ReservaAtivaExisteAsync(int idLeitor, int idLivro);

        Task<bool> AtingiuLimiteReservasAsync(int idLeitor);

        Task<bool> TemExemplaresLivresAsync(int idLivro);

        Task<bool> PodeEmprestarAsync(int idLeitor, int idLivro);

        // Estes dois métodos preparam alterações no mesmo DataContext.
        // O método que os chama guarda tudo no seu SaveChangesAsync.
        Task PrepararDisponibilidadeAsync(Livro livro, DateTime agora, int idReservaIgnorada = 0);

        Task PrepararLevantamentoAsync(int idLeitor, Livro livro, DateTime agora);
    }
}