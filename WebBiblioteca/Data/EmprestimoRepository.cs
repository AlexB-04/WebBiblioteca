using Microsoft.EntityFrameworkCore;
using WebBiblioteca.Models;

namespace WebBiblioteca.Data
{
    public class EmprestimoRepository : IEmprestimoRepository
    {
        private readonly DataContext _context;

        public EmprestimoRepository(DataContext context)
        {
            _context = context;
        }

        public IQueryable<Emprestimo> GetAll()
        {
            return _context.Emprestimos
                .Include(emprestimo => emprestimo.Leitor)
                .Include(emprestimo => emprestimo.Detalhes)
                .ThenInclude(detalhe => detalhe.Livro)
                .AsNoTracking();
        }

        public async Task<Emprestimo?> GetByIdAsync(int id)
        {
            return await _context.Emprestimos
                .Include(emprestimo => emprestimo.Leitor)
                .Include(emprestimo => emprestimo.Detalhes)
                .ThenInclude(detalhe => detalhe.Livro)
                .AsNoTracking()
                .FirstOrDefaultAsync(emprestimo => emprestimo.IdEmprestimo == id);
        }

        public async Task<bool> EmprestimoExisteAsync(int id)
        {
            return await _context.Emprestimos
                .AnyAsync(emprestimo => emprestimo.IdEmprestimo == id);
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

            var novoEmprestimo = new Emprestimo
            {
                IdLeitor = leitor.IdLeitor,
                DataEmprestimo = agora,
                PrazoDevolucao = agora.Date.AddDays(diasEmprestimo)
            };

            foreach (var livro in livros)
            {
                livro.ExemplaresDisponiveis--;

                novoEmprestimo.Detalhes.Add(new EmprestimoDetalhe
                {
                    IdLivro = livro.IdLivro,
                    DataDevolucao = null
                });
            }

            await _context.Emprestimos.AddAsync(novoEmprestimo);

            await _context.SaveChangesAsync();

            emprestimo.IdEmprestimo = novoEmprestimo.IdEmprestimo;

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