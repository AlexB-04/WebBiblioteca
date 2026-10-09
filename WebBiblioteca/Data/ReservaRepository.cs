using Microsoft.EntityFrameworkCore;
using WebBiblioteca.Models;

namespace WebBiblioteca.Data
{
    public class ReservaRepository : IReservaRepository
    {
        // O enunciado indica X dias na tarefa. Então vou manter os 3 dias da Biblioteca anterior.
        private const int DiasLevantamento = 3;
        private readonly DataContext _context;

        public ReservaRepository(DataContext context)
        {
            _context = context;
        }

        public async Task AtualizarReservasExpiradasAsync()
        {
            DateTime agora = DateTime.Now;
            DateTime limite = agora.AddDays(-DiasLevantamento);

            // Só expira o prazo para levantar um exemplar já disponibilizado.
            // Uma reserva que ainda aguarda uma devolução tem DataDisponivel = null.
            var reservasExpiradas = await _context.Reservas
                .Where(reserva => reserva.Ativa &&
                    reserva.DataDisponivel.HasValue &&
                    reserva.DataDisponivel.Value <= limite)
                .ToListAsync();

            if (reservasExpiradas.Count == 0)
            {
                return;
            }

            var idsLivros = reservasExpiradas
                .Select(reserva => reserva.IdLivro)
                .Distinct()
                .ToList();

            var livros = await _context.Livros
                .Where(livro => idsLivros.Contains(livro.IdLivro))
                .AsNoTracking()
                .ToListAsync();

            foreach (var livro in livros)
            {
                // Retirar todos os prazos vencidos deste livro antes de passar a fila.
                foreach (var reserva in reservasExpiradas
                    .Where(reservaDaLista => reservaDaLista.IdLivro == livro.IdLivro))
                {
                    reserva.Ativa = false;
                    await RegistarAlteracaoAsync(reserva, livro, agora, "Expiracao");
                }

                // Os próximos leitores recebem o seu prazo a partir de agora.
                // A expiração não altera ExemplaresDisponiveis.
                await PrepararDisponibilidadeAsync(livro, agora);
            }

            // Expirações, posições e novas disponibilidades são guardadas em conjunto.
            // Esta operação termina antes de se preparar um empréstimo ou devolução.
            await _context.SaveChangesAsync();
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
            await AtualizarReservasExpiradasAsync();

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

            if (livro == null || await TemExemplaresLivresAsync(reserva.IdLivro))
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
            await AtualizarReservasExpiradasAsync();

            if (id <= 0 || idLivro <= 0)
            {
                return false;
            }

            // Tracking: vou alterar esta reserva e as posições da fila anterior.
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

            if (livroNovo == null || await TemExemplaresLivresAsync(idLivro))
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

            await PrepararDisponibilidadeAsync(livroAnterior, agora, reserva.IdReserva);

            // A reserva, a fila e o histórico são guardados na mesma operação.
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            await AtualizarReservasExpiradasAsync();

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

            // A disponibilidade libertada passa aos primeiros leitores da fila.
            await PrepararDisponibilidadeAsync(livro, agora, reserva.IdReserva);

            // Cancelar uma reserva não é devolver um livro: os exemplares não aumentam.
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> TemExemplaresLivresAsync(int idLivro)
        {
            var livro = await _context.Livros
                .AsNoTracking()
                .FirstOrDefaultAsync(livroDaLista => livroDaLista.IdLivro == idLivro);

            if (livro == null || livro.ExemplaresDisponiveis <= 0)
            {
                return false;
            }

            int reservasAtivas = await _context.Reservas.CountAsync(reserva =>
                reserva.IdLivro == idLivro && reserva.Ativa);

            // Os exemplares na biblioteca atendem primeiro a fila de reservas.
            return livro.ExemplaresDisponiveis > reservasAtivas;
        }

        public async Task<bool> PodeEmprestarAsync(int idLeitor, int idLivro)
        {
            var livro = await _context.Livros
                .AsNoTracking()
                .FirstOrDefaultAsync(livroDaLista => livroDaLista.IdLivro == idLivro);

            if (livro == null || livro.ExemplaresDisponiveis <= 0)
            {
                return false;
            }

            var reservas = await _context.Reservas
                .Where(reserva => reserva.IdLivro == idLivro && reserva.Ativa)
                .OrderBy(reserva => reserva.Ordem)
                .ThenBy(reserva => reserva.IdReserva)
                .AsNoTracking()
                .ToListAsync();

            int posicao = 1;

            foreach (var reserva in reservas)
            {
                if (reserva.IdLeitor == idLeitor)
                {
                    // Dois exemplares podem atender os dois primeiros leitores.
                    return posicao <= livro.ExemplaresDisponiveis;
                }

                posicao++;
            }

            // Quem não está na fila só pode levar um exemplar que sobre.
            return livro.ExemplaresDisponiveis > reservas.Count;
        }

        public async Task PrepararDisponibilidadeAsync(
            Livro livro, DateTime agora, int idReservaIgnorada = 0)
        {
            var reservas = await ReordenarFilaAsync(
                livro.IdLivro, idReservaIgnorada, livro.Titulo, agora);

            int exemplaresPorAtribuir = livro.ExemplaresDisponiveis;

            foreach (var reserva in reservas)
            {
                if (exemplaresPorAtribuir > 0)
                {
                    if (!reserva.DataDisponivel.HasValue)
                    {
                        reserva.DataDisponivel = agora;
                        await RegistarAlteracaoAsync(reserva, livro, agora, "Disponibilizacao");
                    }

                    exemplaresPorAtribuir--;
                }
                else if (reserva.DataDisponivel.HasValue)
                {
                    // Se o stock foi reduzido, não manter uma disponibilidade inexistente.
                    reserva.DataDisponivel = null;
                    await RegistarAlteracaoAsync(reserva, livro, agora, "Indisponibilizacao");
                }
            }

            // Não guardar aqui: a devolução/levantamento guarda também o stock e o histórico.
        }

        public async Task PrepararLevantamentoAsync(int idLeitor, Livro livro, DateTime agora)
        {
            var reserva = await _context.Reservas
                .FirstOrDefaultAsync(reservaDaLista =>
                    reservaDaLista.IdLeitor == idLeitor &&
                    reservaDaLista.IdLivro == livro.IdLivro && reservaDaLista.Ativa);

            int idReservaIgnorada = 0;

            if (reserva != null)
            {
                // O empréstimo já foi validado e o seu exemplar já foi descontado.
                reserva.Ativa = false;
                idReservaIgnorada = reserva.IdReserva;

                await RegistarAlteracaoAsync(reserva, livro, agora, "Levantamento");
            }

            // O stock recebido já é o stock depois do levantamento.
            await PrepararDisponibilidadeAsync(livro, agora, idReservaIgnorada);
        }

        private async Task RegistarAlteracaoAsync(
            Reserva reserva, Livro livro, DateTime agora, string acao)
        {
            await _context.ReservaAlteracoes.AddAsync(new ReservaAlteracao
            {
                IdReserva = reserva.IdReserva,
                DataAlteracao = agora,
                Acao = acao,
                IdLivroAnterior = livro.IdLivro,
                LivroAnterior = livro.Titulo,
                IdLivroNovo = livro.IdLivro,
                LivroNovo = livro.Titulo,
                OrdemAnterior = reserva.Ordem,
                OrdemNova = reserva.Ordem
            });
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

            // A consulta SQL ainda pode devolver reservas que acabámos de expirar.
            // O tracking conserva Ativa = false nos objetos; filtramos antes de ordenar.
            reservas = reservas
                .Where(reserva => reserva.Ativa && reserva.IdLivro == idLivro)
                .ToList();

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