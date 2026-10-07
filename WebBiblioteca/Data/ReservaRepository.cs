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
                .Include(reserva => reserva.Alteracoes)
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

        public async Task<bool> UpdateAsync(int id, int idLivro)
        {
            if (id <= 0 || idLivro <= 0)
            {
                return false;
            }

            // Tracking: vamos alterar esta reserva e as posições da fila anterior.
            var reserva = await _context.Reservas
                .FirstOrDefaultAsync(reservaDaLista => reservaDaLista.IdReserva == id);

            if (reserva == null || !reserva.Ativa ||
                reserva.DataDisponivel.HasValue || reserva.IdLivro == idLivro)
            {
                return false;
            }

            var livroNovo = await _context.Livros
                .AsNoTracking()
                .FirstOrDefaultAsync(livro => livro.IdLivro == idLivro);

            if (livroNovo == null || livroNovo.ExemplaresDisponiveis > 0)
            {
                return false;
            }

            bool reservaJaExiste = await ReservaAtivaExisteAsync(reserva.IdLeitor, idLivro);

            if (reservaJaExiste)
            {
                return false;
            }

            var livroAnterior = await _context.Livros
                .AsNoTracking()
                .FirstOrDefaultAsync(livro => livro.IdLivro == reserva.IdLivro);

            if (livroAnterior == null)
            {
                return false;
            }

            int idLivroAnterior = reserva.IdLivro;
            int ordemAnterior = reserva.Ordem;
            DateTime agora = DateTime.Now;

            int ordemNova = await _context.Reservas.CountAsync(reservaDaLista =>
                reservaDaLista.IdLivro == idLivro && reservaDaLista.Ativa) + 1;

            // A alteração mantém o leitor e a data de criação; entra no fim da nova fila.
            reserva.IdLivro = idLivro;
            reserva.Ordem = ordemNova;

            await _context.ReservaAlteracoes.AddAsync(new ReservaAlteracao
            {
                IdReserva = reserva.IdReserva,
                DataAlteracao = agora,
                Acao = "AlteracaoLivro",
                IdLivroAnterior = idLivroAnterior,
                LivroAnterior = livroAnterior.Titulo,
                IdLivroNovo = idLivro,
                LivroNovo = livroNovo.Titulo,
                OrdemAnterior = ordemAnterior,
                OrdemNova = ordemNova
            });

            await ReordenarFilaAsync(idLivroAnterior, reserva.IdReserva, livroAnterior.Titulo, agora);

            // A reserva, a fila e o histórico são guardados na mesma operação.
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var reserva = await _context.Reservas
                .FirstOrDefaultAsync(reservaDaLista => reservaDaLista.IdReserva == id);

            if (reserva == null || !reserva.Ativa)
            {
                return false;
            }

            var livro = await _context.Livros
                .AsNoTracking()
                .FirstOrDefaultAsync(livroDaLista => livroDaLista.IdLivro == reserva.IdLivro);

            if (livro == null)
            {
                return false;
            }

            DateTime agora = DateTime.Now;
            bool estavaDisponivel = reserva.DataDisponivel.HasValue;

            // Cancelar conserva a reserva e os seus dados para consulta do histórico.
            reserva.Ativa = false;

            await _context.ReservaAlteracoes.AddAsync(new ReservaAlteracao
            {
                IdReserva = reserva.IdReserva,
                DataAlteracao = agora,
                Acao = "Cancelamento",
                IdLivroAnterior = reserva.IdLivro,
                LivroAnterior = livro.Titulo,
                IdLivroNovo = reserva.IdLivro,
                LivroNovo = livro.Titulo,
                OrdemAnterior = reserva.Ordem,
                OrdemNova = reserva.Ordem
            });

            var reservasRestantes = await ReordenarFilaAsync(
                reserva.IdLivro, reserva.IdReserva, livro.Titulo, agora);

            // Se havia um exemplar à espera deste leitor, passa ao próximo que ainda espera.
            // A atribuição inicial de exemplares será ligada às devoluções na próxima etapa.
            if (estavaDisponivel && livro.ExemplaresDisponiveis > 0)
            {
                var proximaReserva = reservasRestantes
                    .FirstOrDefault(reservaDaLista => !reservaDaLista.DataDisponivel.HasValue);

                if (proximaReserva != null)
                {
                    proximaReserva.DataDisponivel = agora;

                    await _context.ReservaAlteracoes.AddAsync(new ReservaAlteracao
                    {
                        IdReserva = proximaReserva.IdReserva,
                        DataAlteracao = agora,
                        Acao = "Disponibilizacao",
                        IdLivroAnterior = livro.IdLivro,
                        LivroAnterior = livro.Titulo,
                        IdLivroNovo = livro.IdLivro,
                        LivroNovo = livro.Titulo,
                        OrdemAnterior = proximaReserva.Ordem,
                        OrdemNova = proximaReserva.Ordem
                    });
                }
            }

            // Cancelar uma reserva não é devolver um livro: os exemplares não aumentam.
            await _context.SaveChangesAsync();

            return true;
        }

        private async Task<List<Reserva>> ReordenarFilaAsync(
            int idLivro, int idReservaIgnorada, string? tituloLivro, DateTime agora)
        {
            // Excluir explicitamente a reserva alterada/cancelada: ainda não houve SaveChanges.
            var reservas = await _context.Reservas
                .Where(reserva => reserva.IdLivro == idLivro &&
                    reserva.Ativa && reserva.IdReserva != idReservaIgnorada)
                .OrderBy(reserva => reserva.Ordem)
                .ThenBy(reserva => reserva.IdReserva)
                .ToListAsync();

            int ordem = 1;

            foreach (var reserva in reservas)
            {
                if (reserva.Ordem != ordem)
                {
                    int ordemAnterior = reserva.Ordem;
                    reserva.Ordem = ordem;

                    await _context.ReservaAlteracoes.AddAsync(new ReservaAlteracao
                    {
                        IdReserva = reserva.IdReserva,
                        DataAlteracao = agora,
                        Acao = "AtualizacaoFila",
                        IdLivroAnterior = idLivro,
                        LivroAnterior = tituloLivro,
                        IdLivroNovo = idLivro,
                        LivroNovo = tituloLivro,
                        OrdemAnterior = ordemAnterior,
                        OrdemNova = ordem
                    });
                }

                ordem++;
            }

            return reservas;
        }
    }
}