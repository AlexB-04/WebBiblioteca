using Microsoft.EntityFrameworkCore;
using WebBiblioteca.Models;

namespace WebBiblioteca.Data
{
    public class LivroRepository : ILivroRepository
    {
        private readonly DataContext _context;
        private readonly IReservaRepository _reservaRepository;

        public LivroRepository(DataContext context, IReservaRepository reservaRepository)
        {
            _context = context;
            _reservaRepository = reservaRepository;
        }

        public IQueryable<Livro> GetAll()
        {
            return _context.Livros
                .Include(livro => livro.CategoriaAtual)
                .AsNoTracking();
        }

        public IQueryable<Livro> PesquisarLivros(string? pesquisa, int? idCategoria, string? estado)
        {
            var livros = GetAll();

            if (!string.IsNullOrWhiteSpace(pesquisa))
            {
                string texto = pesquisa.Trim().ToUpper();

                livros = livros.Where(livro =>
                    (livro.Titulo != null && livro.Titulo.ToUpper().Contains(texto)) ||
                    (livro.Autor != null && livro.Autor.ToUpper().Contains(texto)) ||
                    (livro.CategoriaAtual != null && livro.CategoriaAtual.Nome != null &&
                     livro.CategoriaAtual.Nome.ToUpper().Contains(texto)));
            }

            if (idCategoria.HasValue && idCategoria.Value > 0)
            {
                livros = livros.Where(livro => livro.IdCategoria == idCategoria.Value);
            }

            if (estado == "Disponiveis")
            {
                // As reservas ativas têm prioridade sobre os exemplares na biblioteca.
                livros = livros.Where(livro => livro.ExemplaresDisponiveis >
                    _context.Reservas.Count(reserva => reserva.IdLivro == livro.IdLivro && reserva.Ativa));
            }
            else if (estado == "Emprestados")
            {
                livros = livros.Where(livro => _context.EmprestimoDetalhes.Any(detalhe =>
                    detalhe.IdLivro == livro.IdLivro &&
                    detalhe.DataDevolucao == null &&
                    detalhe.Emprestimo != null && !detalhe.Emprestimo.Eliminado));
            }
            else if (estado == "Reservados")
            {
                livros = livros.Where(livro => _context.Reservas.Any(reserva =>
                    reserva.IdLivro == livro.IdLivro && reserva.Ativa));
            }

            return livros;
        }

        public async Task<Livro?> GetByIdAsync(int id)
        {
            return await _context.Livros
                .Include(livro => livro.CategoriaAtual)
                .AsNoTracking()
                .FirstOrDefaultAsync(livro => livro.IdLivro == id);
        }

        public async Task CreateAsync(Livro livro)
        {
            livro.IdLivro = 0;
            livro.CategoriaAtual = null;

            await _context.Livros.AddAsync(livro);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Livro livro)
        {
            livro.CategoriaAtual = null;

            _context.Livros.Update(livro);

            // Atualizar as reservas quando os exemplares disponíveis mudam.
            // O livro, a fila e o histórico são guardados no mesmo SaveChangesAsync.
            await _reservaRepository.PrepararDisponibilidadeAsync(livro, DateTime.Now);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Livro livro)
        {
            livro.CategoriaAtual = null;

            _context.Livros.Remove(livro);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> CategoriaExisteAsync(int id)
        {
            return await _context.Categorias
                .AnyAsync(categoria => categoria.IdCategoria == id);
        }
    }
}