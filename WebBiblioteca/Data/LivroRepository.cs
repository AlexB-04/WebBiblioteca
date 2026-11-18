using Microsoft.EntityFrameworkCore;
using WebBiblioteca.Models;
using WebBiblioteca.Helpers;

namespace WebBiblioteca.Data
{
    public class LivroRepository : ILivroRepository
    {
        private readonly DataContext _context;
        private readonly IReservaRepository _reservaRepository;
        private readonly IMailHelper _mailHelper;

        public LivroRepository(DataContext context, IReservaRepository reservaRepository,
            IMailHelper mailHelper)
        {
            _context = context;
            _reservaRepository = reservaRepository;
            _mailHelper = mailHelper;
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

            DateTime agora = DateTime.Now;

            // Atualizar as reservas quando os exemplares disponíveis mudam.
            await _reservaRepository.PrepararDisponibilidadeAsync(livro, agora);

            // Guardar o livro, a fila e o histórico.
            await _context.SaveChangesAsync();

            // Procurar reservas disponibilizadas nesta operação.
            var reservasDisponiveis = await _context.Reservas
                .Include(reserva => reserva.Leitor)
                .ThenInclude(leitor => leitor!.User)
                .Where(reserva =>
                    reserva.IdLivro == livro.IdLivro &&
                    reserva.Ativa &&
                    reserva.DataDisponivel == agora)
                .ToListAsync();

            // Notificar os leitores.
            foreach (var reservaDisponivel in reservasDisponiveis)
            {
                string? email = reservaDisponivel.Leitor?.User?.Email
                    ?? reservaDisponivel.Leitor?.Email;

                if (!string.IsNullOrWhiteSpace(email))
                {
                    _mailHelper.SendEmail(
                        email,
                        "Reserva disponível para levantamento",
                        "<p>A sua reserva já está disponível para levantamento.</p>" +
                        "<p>Consulte as suas reservas para verificar o prazo.</p>");
                }
            }
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

        public async Task<bool> TemHistoricoEmprestimosAsync(int idLivro)
        {
            // Inclui empréstimos devolvidos: a história deve ser preservada.
            return await _context.EmprestimoDetalhes
                .AnyAsync(detalhe => detalhe.IdLivro == idLivro);
        }

        public async Task<bool> TemReservasAsync(int idLivro)
        {
            // Inclui reservas inativas porque a chave estrangeira impede a eliminação.
            return await _context.Reservas
                .AnyAsync(reserva => reserva.IdLivro == idLivro);
        }
    }
}