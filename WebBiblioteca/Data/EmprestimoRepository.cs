using Microsoft.EntityFrameworkCore;
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

        public async Task<bool> CreateAsync(Emprestimo emprestimo)
        {
            if (emprestimo == null || emprestimo.Detalhes == null ||
                emprestimo.Detalhes.Count == 0)
            {
                return false;
            }

            var leitor = await _context.Leitores
                .AsNoTracking()
                .FirstOrDefaultAsync(leitorDaLista =>
                    leitorDaLista.IdLeitor == emprestimo.IdLeitor);

            if (leitor == null)
            {
                return false;
            }

            DateTime agora = DateTime.Now;

            if (leitor.BloqueadoAte.HasValue && leitor.BloqueadoAte.Value > agora)
            {
                return false;
            }

            int diasEmprestimo = ObterDiasEmprestimo(leitor.TipoUtilizador);

            if (diasEmprestimo == 0)
            {
                return false;
            }

            bool temAtraso = await TemEmprestimosEmAtrasoAsync(leitor.IdLeitor);

            if (temAtraso)
            {
                return false;
            }

            bool temMulta = await TemPenalizacoesPorPagarAsync(leitor.IdLeitor);

            if (temMulta)
            {
                return false;
            }

            var idsLivros = new List<int>();

            foreach (var detalhe in emprestimo.Detalhes)
            {
                if (detalhe == null || detalhe.IdLivro <= 0)
                {
                    return false;
                }

                if (idsLivros.Contains(detalhe.IdLivro))
                {
                    return false;
                }

                idsLivros.Add(detalhe.IdLivro);
            }

            int livrosEmprestados = await ContarLivrosEmprestadosAsync(leitor.IdLeitor);

            if (livrosEmprestados + idsLivros.Count > leitor.LimiteEmprestimos)
            {
                return false;
            }

            bool livroJaEmprestado = await _context.EmprestimoDetalhes
                .AnyAsync(detalhe =>
                    detalhe.Emprestimo != null &&
                    detalhe.Emprestimo.IdLeitor == leitor.IdLeitor &&
                    detalhe.DataDevolucao == null &&
                    idsLivros.Contains(detalhe.IdLivro));

            if (livroJaEmprestado)
            {
                return false;
            }

            var livros = await _context.Livros
                .Where(livro => idsLivros.Contains(livro.IdLivro))
                .OrderBy(livro => livro.IdLivro)
                .ToListAsync();

            if (livros.Count != idsLivros.Count ||
                livros.Any(livro => livro.ExemplaresDisponiveis <= 0))
            {
                return false;
            }

            // Validar TODOS os livros antes de descontar stock ou concluir reservas.
            foreach (var livro in livros)
            {
                bool podeEmprestar = await _reservaRepository
                    .PodeEmprestarAsync(leitor.IdLeitor, livro.IdLivro);

                if (!podeEmprestar)
                {
                    return false;
                }
            }

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

            emprestimo.IdEmprestimo = novoEmprestimo.IdEmprestimo;

            return true;
        }

        public async Task<bool> DevolverLivroAsync(int idEmprestimo, int idLivro)
        {
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
                const decimal valorPorDia = 0.50m;

                var penalizacao = new Penalizacao
                {
                    IdEmprestimo = detalhe.IdEmprestimo,
                    IdEmprestimoDetalhe = detalhe.IdEmprestimoDetalhe,
                    DiasAtraso = diasAtraso,
                    Valor = diasAtraso * valorPorDia,
                    Motivo = $"Atraso de {diasAtraso} dia(s) na devolução do livro com ID {detalhe.IdLivro}.",
                    DataPenalizacao = agora,
                    Pago = false,
                    DataPagamento = null
                };

                await _context.Penalizacoes.AddAsync(penalizacao);

                leitor.Atrasos++;

                if (leitor.Atrasos >= 3)
                {
                    DateTime novoBloqueio = agora.AddDays(7);

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

            return true;
        }

        public async Task<bool> AlterarPrazoAsync(int id, DateTime prazoDevolucao)
        {
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

            int diasEmprestimo = ObterDiasEmprestimo(emprestimo.Leitor.TipoUtilizador);
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

            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
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

            return true;
        }

        public async Task<bool> PagarPenalizacaoAsync(int idEmprestimo, int idPenalizacao)
        {
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

            return true;
        }

        private int ObterDiasEmprestimo(string? tipoUtilizador)
        {
            if (tipoUtilizador == "Professor")
            {
                return 30;
            }

            if (tipoUtilizador == "Aluno")
            {
                return 15;
            }

            if (tipoUtilizador == "Público em Geral")
            {
                return 7;
            }

            return 0;
        }
    }
}