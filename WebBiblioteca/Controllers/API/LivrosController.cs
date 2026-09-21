using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBiblioteca.Data;
using WebBiblioteca.Models;

namespace WebBiblioteca.Controllers.API
{
    [Route("api/[controller]/{id?}")]
    [ApiController]
    public class LivrosController : Controller
    {
        private readonly ILivroRepository _livroRepository;

        public LivrosController(ILivroRepository livroRepository)
        {
            _livroRepository = livroRepository;
        }

        [HttpGet]
        public async Task<IActionResult> GetLivros(int? id)
        {
            if (id == null)
            {
                return Ok(_livroRepository.GetAll());
            }

            var livro = await _livroRepository.GetByIdAsync(id.Value);

            if (livro == null)
            {
                return NotFound();
            }

            return Ok(livro);
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Livro livro)
        {
            if (livro == null)
            {
                return BadRequest("Dados do livro inválidos.");
            }

            if (string.IsNullOrWhiteSpace(livro.Titulo))
            {
                return BadRequest("O título do livro é obrigatório.");
            }

            if (string.IsNullOrWhiteSpace(livro.Autor))
            {
                return BadRequest("O autor do livro é obrigatório.");
            }

            if (string.IsNullOrWhiteSpace(livro.Editora))
            {
                return BadRequest("A editora do livro é obrigatória.");
            }

            if (string.IsNullOrWhiteSpace(livro.Genero))
            {
                return BadRequest("O género do livro é obrigatório.");
            }

            if (livro.AnoPublicacao <= 0)
            {
                return BadRequest("O ano de publicação deve ser superior a zero.");
            }

            if (livro.ExemplaresDisponiveis < 0)
            {
                return BadRequest("O número de exemplares não pode ser negativo.");
            }

            livro.Titulo = livro.Titulo.Trim();
            livro.Autor = livro.Autor.Trim();
            livro.Editora = livro.Editora.Trim();
            livro.Genero = livro.Genero.Trim();

            bool categoriaExiste = await _livroRepository.CategoriaExisteAsync(livro.IdCategoria);

            if (!categoriaExiste)
            {
                return BadRequest("A categoria indicada não existe.");
            }

            bool livroJaExiste = _livroRepository.GetAll().Any(l => l.Titulo == livro.Titulo && l.Autor == livro.Autor);

            if (livroJaExiste)
            {
                return BadRequest("Já existe um livro com esse título e autor.");
            }

            try
            {
                await _livroRepository.CreateAsync(livro);
            }
            catch (DbUpdateException)
            {
                return BadRequest("Não foi possível guardar o livro. Verifique os dados e a categoria.");
            }

            var livroCriado = await _livroRepository.GetByIdAsync(livro.IdLivro);

            return Ok(livroCriado);
        }

        [HttpPut]
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

            if (string.IsNullOrWhiteSpace(livro.Titulo))
            {
                return BadRequest("O título do livro é obrigatório.");
            }

            if (string.IsNullOrWhiteSpace(livro.Autor))
            {
                return BadRequest("O autor do livro é obrigatório.");
            }

            if (string.IsNullOrWhiteSpace(livro.Editora))
            {
                return BadRequest("A editora do livro é obrigatória.");
            }

            if (string.IsNullOrWhiteSpace(livro.Genero))
            {
                return BadRequest("O género do livro é obrigatório.");
            }

            if (livro.AnoPublicacao <= 0)
            {
                return BadRequest("O ano de publicação deve ser superior a zero.");
            }

            if (livro.ExemplaresDisponiveis < 0)
            {
                return BadRequest("O número de exemplares não pode ser negativo.");
            }

            livro.Titulo = livro.Titulo.Trim();
            livro.Autor = livro.Autor.Trim();
            livro.Editora = livro.Editora.Trim();
            livro.Genero = livro.Genero.Trim();

            bool categoriaExiste = await _livroRepository.CategoriaExisteAsync(livro.IdCategoria);

            if (!categoriaExiste)
            {
                return BadRequest("A categoria indicada não existe.");
            }

            bool livroJaExiste = _livroRepository.GetAll()
                .Any(l => l.Titulo == livro.Titulo &&
                          l.Autor == livro.Autor &&
                          l.IdLivro != id);

            if (livroJaExiste)
            {
                return BadRequest("Já existe outro livro com esse título e autor.");
            }

            livroExistente.Titulo = livro.Titulo;
            livroExistente.Autor = livro.Autor;
            livroExistente.Editora = livro.Editora;
            livroExistente.AnoPublicacao = livro.AnoPublicacao;
            livroExistente.Genero = livro.Genero;
            livroExistente.ExemplaresDisponiveis = livro.ExemplaresDisponiveis;
            livroExistente.IdCategoria = livro.IdCategoria;

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

        [HttpDelete]
        public async Task<IActionResult> Delete(int id)
        {
            var livro = await _livroRepository.GetByIdAsync(id);

            if (livro == null)
            {
                return NotFound();
            }

            try
            {
                await _livroRepository.DeleteAsync(livro);
            }
            catch (DbUpdateException)
            {
                return BadRequest("Não foi possível eliminar o livro. Verifique os registos associados.");
            }

            return Ok();
        }
    }
}