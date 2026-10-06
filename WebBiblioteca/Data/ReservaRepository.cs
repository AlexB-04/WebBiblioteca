using Microsoft.EntityFrameworkCore;
using WebBiblioteca.Models;

namespace WebBiblioteca.Data
{
    public class ReservaRepository : IReservaRepository
    {
        private readonly DataContext _context;

        public ReservaRepository(DataContext context)
        {
            _context = context;
        }

        public IQueryable<Reserva> GetAll()
        {
            return _context.Reservas
                .Include(reserva => reserva.Leitor)
                .Include(reserva => reserva.Livro)
                .AsNoTracking();
        }

        public async Task<Reserva?> GetByIdAsync(int id)
        {
            return await GetAll()
                .FirstOrDefaultAsync(reserva => reserva.IdReserva == id);
        }

        public async Task<bool> ReservaAtivaExisteAsync(int idLeitor, int idLivro)
        {
            return await _context.Reservas.AnyAsync(reserva =>
                reserva.IdLeitor == idLeitor &&
                reserva.IdLivro == idLivro &&
                reserva.Ativa);
        }

        public async Task<bool> AtingiuLimiteReservasAsync(int idLeitor)
        {
            const int limiteReservas = 3;

            int reservasAtivas = await _context.Reservas.CountAsync(reserva =>
                reserva.IdLeitor == idLeitor && reserva.Ativa);

            return reservasAtivas >= limiteReservas;
        }

        public async Task<bool> CreateAsync(Reserva reserva)
        {
            if (reserva == null || reserva.IdLeitor <= 0 || reserva.IdLivro <= 0)
            {
                return false;
            }

            bool leitorExiste = await _context.Leitores
                .AnyAsync(leitor => leitor.IdLeitor == reserva.IdLeitor);

            if (!leitorExiste)
            {
                return false;
            }

            var livro = await _context.Livros
                .AsNoTracking()
                .FirstOrDefaultAsync(livroDaLista => livroDaLista.IdLivro == reserva.IdLivro);

            if (livro == null || livro.ExemplaresDisponiveis > 0)
            {
                return false;
            }

            bool reservaJaExiste = await ReservaAtivaExisteAsync(reserva.IdLeitor, reserva.IdLivro);

            if (reservaJaExiste)
            {
                return false;
            }

            bool atingiuLimite = await AtingiuLimiteReservasAsync(reserva.IdLeitor);

            if (atingiuLimite)
            {
                return false;
            }

            int ordem = await _context.Reservas.CountAsync(reservaDaLista =>
                reservaDaLista.IdLivro == reserva.IdLivro && reservaDaLista.Ativa) + 1;

            // O servidor define os campos da reserva. O pedido indica o leitor e o livro.
            var novaReserva = new Reserva
            {
                IdLeitor = reserva.IdLeitor,
                IdLivro = reserva.IdLivro,
                DataReserva = DateTime.Now,
                Ordem = ordem,
                Ativa = true,
                DataDisponivel = null
            };

            await _context.Reservas.AddAsync(novaReserva);
            await _context.SaveChangesAsync();

            reserva.IdReserva = novaReserva.IdReserva;

            return true;
        }
    }
}