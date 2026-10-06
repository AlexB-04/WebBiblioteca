using WebBiblioteca.Models;

namespace WebBiblioteca.Data
{
    public interface IReservaRepository
    {
        IQueryable<Reserva> GetAll();

        Task<Reserva?> GetByIdAsync(int id);

        Task<bool> CreateAsync(Reserva reserva);

        Task<bool> ReservaAtivaExisteAsync(int idLeitor, int idLivro);

        Task<bool> AtingiuLimiteReservasAsync(int idLeitor);
    }
}