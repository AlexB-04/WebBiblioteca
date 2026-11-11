using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBiblioteca.Data;
using WebBiblioteca.Helpers;
using WebBiblioteca.Models;

namespace WebBiblioteca.Controllers.API
{
    [Route("api/[controller]/{id?}")]
    [ApiController]
    [Authorize(Roles = "Admin,Funcionario")]
    public class LivrosController : Controller
    {
        private readonly ILivroRepository _livroRepository;
        private readonly IImageHelper _imageHelper;
        private readonly ILogger<LivrosController> _logger;

        public LivrosController(ILivroRepository livroRepository, IImageHelper imageHelper,
            ILogger<LivrosController> logger)
        {
            _livroRepository = livroRepository;
            _imageHelper = imageHelper;
            _logger = logger;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetLivros(int? id)
        {
            if (id == null)
            {
                var livros = await _livroRepository.GetAll().ToListAsync();
                return Ok(livros);
            }

            var livro = await _livroRepository.GetByIdAsync(id.Value);

            if (livro == null)
            {
                return NotFound();
            }

            return Ok(livro);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Post([FromBody] Livro livro)
        {
            if (livro == null)
            {
                return BadRequest("Dados do livro inválidos.");
            }

            string? erro = await ValidarLivroAsync(livro);
            if (erro != null)
            {
                return BadRequest(erro);
            }

            // Não aceitar ImageUrl, IdLivro ou CategoriaAtual enviados pelo cliente.
            var novoLivro = new Livro
            {
                Titulo = livro.Titulo,
                Autor = livro.Autor,
                Editora = livro.Editora,
                AnoPublicacao = livro.AnoPublicacao,
                Genero = livro.Genero,
                ExemplaresDisponiveis = livro.ExemplaresDisponiveis,
                IdCategoria = livro.IdCategoria,
                ImageUrl = string.Empty
            };

            try
            {
                await _livroRepository.CreateAsync(novoLivro);
            }
            catch (DbUpdateException)
            {
                return BadRequest("Não foi possível guardar o livro. Verifique os dados e a categoria.");
            }

            var livroCriado = await _livroRepository.GetByIdAsync(novoLivro.IdLivro);
            return Ok(livroCriado);
        }

        [HttpPut]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Put(int id, [FromBody] Livro livro)
        {
            if (livro == null)
            {
                return BadRequest("Dados do livro inválidos.");
            }

            var livroExistente = await _livroRepository.GetByIdAsync(id);

            if (livroExistente == null)
            {
                return NotFound();
            }

            string? erro = await ValidarLivroAsync(livro, id);
            if (erro != null)
            {
                return BadRequest(erro);
            }

            livroExistente.Titulo = livro.Titulo;
            livroExistente.Autor = livro.Autor;
            livroExistente.Editora = livro.Editora;
            livroExistente.AnoPublicacao = livro.AnoPublicacao;
            livroExistente.Genero = livro.Genero;
            livroExistente.ExemplaresDisponiveis = livro.ExemplaresDisponiveis;
            livroExistente.IdCategoria = livro.IdCategoria;
            // A capa atual mantém-se: o API não faz upload de ficheiros.

            try
            {
                await _livroRepository.UpdateAsync(livroExistente);
            }
            catch (DbUpdateException)
            {
                return BadRequest("Não foi possível alterar o livro. Verifique os dados e a categoria.");
            }

            var livroAtualizado = await _livroRepository.GetByIdAsync(id);
            return Ok(livroAtualizado);
        }

        private async Task<string?> ValidarLivroAsync(Livro livro, int? idLivroAtual = null)
        {
            if (string.IsNullOrWhiteSpace(livro.Titulo))
            {
                return "O título do livro é obrigatório.";
            }

            if (string.IsNullOrWhiteSpace(livro.Autor))
            {
                return "O autor do livro é obrigatório.";
            }

            if (string.IsNullOrWhiteSpace(livro.Editora))
            {
                return "A editora do livro é obrigatória.";
            }

            if (string.IsNullOrWhiteSpace(livro.Genero))
            {
                return "O género do livro é obrigatório.";
            }

            if (livro.AnoPublicacao <= 0)
            {
                return "O ano de publicação deve ser superior a zero.";
            }

            if (livro.ExemplaresDisponiveis < 0)
            {
                return "O número de exemplares não pode ser negativo.";
            }

            livro.Titulo = livro.Titulo.Trim();
            livro.Autor = livro.Autor.Trim();
            livro.Editora = livro.Editora.Trim();
            livro.Genero = livro.Genero.Trim();

            if (!await _livroRepository.CategoriaExisteAsync(livro.IdCategoria))
            {
                return "A categoria indicada não existe.";
            }

            var livrosExistentes = _livroRepository.GetAll()
                .Where(outroLivro => outroLivro.Titulo == livro.Titulo && outroLivro.Autor == livro.Autor);

            if (idLivroAtual.HasValue)
            {
                livrosExistentes = livrosExistentes.Where(outroLivro => outroLivro.IdLivro != idLivroAtual.Value);
            }

            if (await livrosExistentes.AnyAsync())
            {
                return idLivroAtual.HasValue
                    ? "Já existe outro livro com esse título e autor."
                    : "Já existe um livro com esse título e autor.";
            }

            return null;
        }

        [HttpDelete]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteLivro(int id)
        {
            var livro = await _livroRepository.GetByIdAsync(id);

            if (livro == null)
            {
                return NotFound();
            }

            if (await _livroRepository.TemHistoricoEmprestimosAsync(id))
            {
                return BadRequest("Não é possível eliminar um livro com histórico de empréstimos.");
            }

            if (await _livroRepository.TemReservasAsync(id))
            {
                return BadRequest("Não é possível eliminar um livro com reservas associadas.");
            }

            try
            {
                await _livroRepository.DeleteAsync(livro);
            }
            catch (DbUpdateException)
            {
                return BadRequest("Não foi possível eliminar o livro. Verifique os registos associados.");
            }

            await TentarEliminarCapaNaoUtilizadaAsync(livro.ImageUrl);
            return Ok();
        }

        private async Task TentarEliminarCapaNaoUtilizadaAsync(string? imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl))
            {
                return;
            }

            try
            {
                bool emUso = await _livroRepository.GetAll()
                    .AnyAsync(livro => livro.ImageUrl == imageUrl);

                if (!emUso)
                {
                    _imageHelper.TryDeleteImage(imageUrl, "livros");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Não foi possível verificar a utilização da capa {ImageUrl}.", imageUrl);
            }
        }
    }
}