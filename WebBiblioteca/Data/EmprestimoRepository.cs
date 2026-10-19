using Microsoft.EntityFrameworkCore;
using System.Data;
using WebBiblioteca.Models;

namespace WebBiblioteca.Data
{
    public class EmprestimoRepository : IEmprestimoRepository
    {
        private readonly DataContext _context;
        private readonly IReservaRepository _reservaRepository;

        public EmprestimoRepository(DataContext context, IReservaRepository reservaRepository)
        {
            _context = context;
            _reservaRepository = reservaRepository;
        }

        public IQueryable<Emprestimo> GetAll(bool incluirEliminados = false)
        {
            var emprestimos = _context.Emprestimos
                .Include(emprestimo => emprestimo.Leitor)
                .Include(emprestimo => emprestimo.Detalhes)
                .ThenInclude(detalhe => detalhe.Livro)
                .Include(emprestimo => emprestimo.Alteracoes)
                .Include(emprestimo => emprestimo.Penalizacoes)
                .AsNoTracking();

            if (!incluirEliminados)
            {
                emprestimos = emprestimos.Where(emprestimo => !emprestimo.Eliminado);
            }

            return emprestimos;
        }

        public async Task<Emprestimo?> GetByIdAsync(int id, bool incluirEliminados = false)
        {
            return await GetAll(incluirEliminados)
                .FirstOrDefaultAsync(emprestimo => emprestimo.IdEmprestimo == id);
        }

        public async Task<bool> EmprestimoExisteAsync(int id)
        {
            return await _context.Emprestimos
                .AnyAsync(emprestimo => emprestimo.IdEmprestimo == id && !emprestimo.Eliminado);
        }

        public async Task<bool> LeitorExisteAsync(int id)
        {
            return await _context.Leitores.
                AnyAsync(leitor => leitor.IdLeitor == id);
        }

        public async Task<bool> TemEmprestimosEmAtrasoAsync(int idLeitor)
        {
            return await _context.EmprestimoDetalhes.AnyAsync(detalhe =>
                detalhe.Emprestimo != null &&
                detalhe.Emprestimo.IdLeitor == idLeitor &&
                detalhe.DataDevolucao == null &&
                detalhe.Emprestimo.PrazoDevolucao < DateTime.Today);
        }

        public async Task<int> ContarLivrosEmprestadosAsync(int idLeitor)
        {
            return await _context.EmprestimoDetalhes.CountAsync(detalhe =>
                detalhe.Emprestimo != null &&
                detalhe.Emprestimo.IdLeitor == idLeitor &&
                detalhe.DataDevolucao == null);
        }

        public async Task<bool> TemPenalizacoesPorPagarAsync(int idLeitor)
        {
            return await _context.Penalizacoes.AnyAsync(penalizacao =>
                penalizacao.Emprestimo != null &&
                penalizacao.Emprestimo.IdLeitor == idLeitor &&
                !penalizacao.Pago);
        }

        public async Task<bool> TemLivroEmprestadoAsync(int idLeitor, int idLivro)
        {
            return await _context.EmprestimoDetalhes.AnyAsync(detalhe =>
                detalhe.Emprestimo != null &&
                detalhe.Emprestimo.IdLeitor == idLeitor &&
                detalhe.IdLivro == idLivro &&
                detalhe.DataDevolucao == null);
        }

        // Partilhada pelo MVC e pela API. CreateAsync volta a validar dentro da transação.
        public async Task<string?> ObterErroCriacaoAsync(Emprestimo emprestimo)
        {
            if (emprestimo == null || emprestimo.IdLeitor <= 0)
                return "Selecione um leitor válido.";
            if (emprestimo.Detalhes == null || emprestimo.Detalhes.Count == 0)
                return "Selecione pelo menos um livro.";

            var leitor = await _context.Leitores.Include(leitor => leitor.User).AsNoTracking()
                .FirstOrDefaultAsync(leitor => leitor.IdLeitor == emprestimo.IdLeitor);
            if (leitor == null) return "O leitor indicado não existe.";
            // Conservar fichas antigas sem conta. Uma conta associada tem de estar ativa.
            if (leitor.UserId != null && (leitor.User == null || !leitor.User.Aprovado ||
                leitor.User.DeveAlterarPassword || !leitor.User.EmailConfirmed))
                return "A conta do leitor aguarda confirmação do administrador ou definição da palavra-passe.";
            if (leitor.BloqueadoAte.HasValue && leitor.BloqueadoAte.Value > DateTime.Now)
                return "O leitor está temporariamente bloqueado até " + leitor.BloqueadoAte.Value.ToString("dd/MM/yyyy HH:mm") + ".";
            if (EmprestimoRegras.ObterDias(leitor.TipoUtilizador) == 0)
                return "O tipo de utilizador do leitor é inválido.";
            if (await TemEmprestimosEmAtrasoAsync(leitor.IdLeitor))
                return "O leitor possui livros com devolução em atraso.";
            if (await TemPenalizacoesPorPagarAsync(leitor.IdLeitor))
                return "O leitor possui penalizações por pagar.";

            var idsLivros = new List<int>();
            foreach (var detalhe in emprestimo.Detalhes)
            {
                if (detalhe == null || detalhe.IdLivro <= 0)
                    return "Selecione livros válidos.";
                if (idsLivros.Contains(detalhe.IdLivro))
                    return "O mesmo livro não pode aparecer duas vezes no empréstimo.";
                idsLivros.Add(detalhe.IdLivro);
            }
            int emprestados = await ContarLivrosEmprestadosAsync(leitor.IdLeitor);
            if (emprestados + idsLivros.Count > leitor.LimiteEmprestimos)
                return $"O leitor tem {emprestados} livro(s) por devolver e um limite de {leitor.LimiteEmprestimos}. A seleção ultrapassa esse limite.";

            foreach (int idLivro in idsLivros.OrderBy(id => id))
            {
                var livro = await _context.Livros.AsNoTracking().FirstOrDefaultAsync(livro => livro.IdLivro == idLivro);
                if (livro == null) return "Um dos livros selecionados já não existe.";
                if (await TemLivroEmprestadoAsync(leitor.IdLeitor, idLivro))
                    return $"O leitor já tem o livro «{livro.Titulo}» por devolver.";
                if (livro.ExemplaresDisponiveis <= 0)
                    return $"O livro «{livro.Titulo}» não tem exemplares disponíveis.";
                if (!await _reservaRepository.PodeEmprestarAsync(leitor.IdLeitor, idLivro))
                    return $"Os exemplares de «{livro.Titulo}» estão destinados a outros leitores na fila de reservas.";
            }
            return null;
        }

        public async Task<bool> CreateAsync(Emprestimo emprestimo)
        {
            // A leitura das regras e a escrita do stock pertencem à mesma operação.
            // Serializable impede que dois pedidos usem o mesmo último exemplar.
            await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            await _reservaRepository.AtualizarReservasExpiradasAsync();
            if (await ObterErroCriacaoAsync(emprestimo) != null) return false;

            var leitor = await _context.Leitores.AsNoTracking()
                .FirstAsync(leitor => leitor.IdLeitor == emprestimo.IdLeitor);
            DateTime agora = DateTime.Now;
            int diasEmprestimo = EmprestimoRegras.ObterDias(leitor.TipoUtilizador);
            var idsLivros = emprestimo.Detalhes.Select(detalhe => detalhe.IdLivro).ToList();
            var livros = await _context.Livros.Where(livro => idsLivros.Contains(livro.IdLivro))
                .OrderBy(livro => livro.IdLivro).ToListAsync();

            var novoEmprestimo = new Emprestimo
            {
                IdLeitor = leitor.IdLeitor,
                DataEmprestimo = agora,
                PrazoDevolucao = agora.Date.AddDays(diasEmprestimo)
            };

            foreach (var livro in livros)
            {
                livro.ExemplaresDisponiveis--;

                await _reservaRepository.PrepararLevantamentoAsync(leitor.IdLeitor, livro, agora);

                novoEmprestimo.Detalhes.Add(new EmprestimoDetalhe
                {
                    IdLivro = livro.IdLivro,
                    DataDevolucao = null
                });
            }

            await _context.Emprestimos.AddAsync(novoEmprestimo);

            // O mesmo DataContext guarda empréstimo, detalhes, stock, reserva e histórico.
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            emprestimo.IdEmprestimo = novoEmprestimo.IdEmprestimo;

            return true;
        }

        public async Task<bool> DevolverLivroAsync(int idEmprestimo, int idLivro)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            await _reservaRepository.AtualizarReservasExpiradasAsync();

            if (idEmprestimo <= 0 || idLivro <= 0)
            {
                return false;
            }

            var detalhe = await _context.EmprestimoDetalhes
                .Include(detalheDaLista => detalheDaLista.Livro)
                .Include(detalheDaLista => detalheDaLista.Emprestimo)
                .FirstOrDefaultAsync(detalheDaLista =>
                    detalheDaLista.IdEmprestimo == idEmprestimo &&
                    detalheDaLista.IdLivro == idLivro);

            if (detalhe == null || detalhe.DataDevolucao != null || detalhe.Livro == null ||
                detalhe.Emprestimo == null || detalhe.Emprestimo.Eliminado)
            {
                return false;
            }

            var leitor = await _context.Leitores.FirstOrDefaultAsync(leitorDaLista =>
                leitorDaLista.IdLeitor == detalhe.Emprestimo.IdLeitor);

            if (leitor == null)
            {
                return false;
            }

            bool jaTemPenalizacao = await _context.Penalizacoes
                .AnyAsync(penalizacao => penalizacao.IdEmprestimoDetalhe == detalhe.IdEmprestimoDetalhe);

            if (jaTemPenalizacao)
            {
                return false;
            }

            DateTime agora = DateTime.Now;
            int diasAtraso = (agora.Date - detalhe.Emprestimo.PrazoDevolucao.Date).Days;

            if (diasAtraso > 0)
            {
                var penalizacao = new Penalizacao
                {
                    IdEmprestimo = detalhe.IdEmprestimo,
                    IdEmprestimoDetalhe = detalhe.IdEmprestimoDetalhe,
                    DiasAtraso = diasAtraso,
                    Valor = diasAtraso * EmprestimoRegras.ValorPorDia,
                    Motivo = $"Atraso de {diasAtraso} dia(s) na devolução do livro com ID {detalhe.IdLivro}.",
                    DataPenalizacao = agora,
                    Pago = false,
                    DataPagamento = null
                };

                await _context.Penalizacoes.AddAsync(penalizacao);

                leitor.Atrasos++;

                if (leitor.Atrasos >= EmprestimoRegras.LimiteAtrasos)
                {
                    DateTime novoBloqueio = agora.AddDays(EmprestimoRegras.DiasBloqueio);

                    if (!leitor.BloqueadoAte.HasValue || leitor.BloqueadoAte.Value < novoBloqueio)
                    {
                        leitor.BloqueadoAte = novoBloqueio;
                    }
                }
            }

            detalhe.DataDevolucao = agora;
            detalhe.Livro.ExemplaresDisponiveis++;

            await _reservaRepository.PrepararDisponibilidadeAsync(detalhe.Livro, agora);

            // Devolução, exemplar, penalização e disponibilidade da fila são guardados juntos.
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return true;
        }

        public async Task<bool> AlterarPrazoAsync(int id, DateTime prazoDevolucao)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            var emprestimo = await _context.Emprestimos
                .Include(emprestimoDaLista => emprestimoDaLista.Leitor)
                .Include(emprestimoDaLista => emprestimoDaLista.Detalhes)
                .FirstOrDefaultAsync(emprestimoDaLista =>
                    emprestimoDaLista.IdEmprestimo == id && !emprestimoDaLista.Eliminado);

            if (emprestimo == null || emprestimo.Leitor == null || emprestimo.Detalhes.Count == 0)
            {
                return false;
            }

            if (emprestimo.Detalhes.Any(detalhe => detalhe.DataDevolucao != null) ||
                emprestimo.PrazoDevolucao.Date < DateTime.Today)
            {
                return false;
            }

            int diasEmprestimo = EmprestimoRegras.ObterDias(emprestimo.Leitor.TipoUtilizador);
            DateTime novoPrazo = prazoDevolucao.Date;

            if (diasEmprestimo == 0 || novoPrazo < DateTime.Today ||
                novoPrazo < emprestimo.DataEmprestimo.Date ||
                novoPrazo > emprestimo.DataEmprestimo.Date.AddDays(diasEmprestimo))
            {
                return false;
            }

            if (novoPrazo == emprestimo.PrazoDevolucao.Date)
            {
                return true;
            }

            emprestimo.Alteracoes.Add(new EmprestimoAlteracao
            {
                DataAlteracao = DateTime.Now,
                PrazoAnterior = emprestimo.PrazoDevolucao,
                PrazoNovo = novoPrazo
            });

            emprestimo.PrazoDevolucao = novoPrazo;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            var emprestimo = await _context.Emprestimos
                .Include(emprestimoDaLista => emprestimoDaLista.Detalhes)
                .Include(emprestimoDaLista => emprestimoDaLista.Penalizacoes)
                .FirstOrDefaultAsync(emprestimoDaLista =>
                    emprestimoDaLista.IdEmprestimo == id && !emprestimoDaLista.Eliminado);

            if (emprestimo == null)
            {
                return false;
            }

            if (emprestimo.Detalhes.Any(detalhe => detalhe.DataDevolucao == null) ||
                emprestimo.Penalizacoes.Any(penalizacao => !penalizacao.Pago))
            {
                return false;
            }

            // O histórico permanece na base de dados. Não há alteração dos exemplares.
            emprestimo.Eliminado = true;
            emprestimo.DataEliminacao = DateTime.Now;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return true;
        }

        public async Task<bool> PagarPenalizacaoAsync(int idEmprestimo, int idPenalizacao)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            var penalizacao = await _context.Penalizacoes
                .FirstOrDefaultAsync(penalizacaoDaLista =>
                    penalizacaoDaLista.IdPenalizacao == idPenalizacao &&
                    penalizacaoDaLista.IdEmprestimo == idEmprestimo &&
                    penalizacaoDaLista.Emprestimo != null &&
                    !penalizacaoDaLista.Emprestimo.Eliminado);

            if (penalizacao == null || penalizacao.Pago)
            {
                return false;
            }

            penalizacao.Pago = true;
            penalizacao.DataPagamento = DateTime.Now;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return true;
        }

    }
}
