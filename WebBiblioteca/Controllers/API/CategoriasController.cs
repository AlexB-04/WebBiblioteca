using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBiblioteca.Data;
using WebBiblioteca.Models;

namespace WebBiblioteca.Controllers.API
{
    [Route("api/[controller]/{id?}")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class CategoriasController : Controller
    {
        private readonly ICategoriaRepository _categoriaRepository;

        public CategoriasController(ICategoriaRepository categoriaRepository)
        {
            _categoriaRepository = categoriaRepository;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetCategorias(int? id)
        {
            if (id == null)
            {
                var categorias = await _categoriaRepository.GetAll().ToListAsync();

                return Ok(categorias);
            }

            var categoria = await _categoriaRepository.GetByIdAsync(id.Value);

            if (categoria == null)
            {
                return NotFound();
            }

            return Ok(categoria);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Post([FromBody] Categoria categoria)
        {
            if (categoria == null)
            {
                return BadRequest("Dados da categoria inválidos.");
            }

            string? erroNome = await ValidarNomeCategoriaAsync(categoria);

            if (erroNome != null)
            {
                return BadRequest(erroNome);
            }

            categoria.IdCategoria = 0;

            try
            {
                await _categoriaRepository.CreateAsync(categoria);

                return Ok(new
                {
                    categoria.IdCategoria,
                    categoria.Nome,
                    categoria.Descricao
                });
            }
            catch (DbUpdateException)
            {
                return StatusCode(500, "Não foi possível criar a categoria. Tente novamente.");
            }
        }

        [HttpPut]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Put(int id, [FromBody] Categoria categoria)
        {
            if (categoria == null)
            {
                return BadRequest("Dados da categoria inválidos.");
            }

            var categoriaExistente = await _categoriaRepository.GetByIdAsync(id);

            if (categoriaExistente == null)
            {
                return NotFound();
            }

            string? erroNome = await ValidarNomeCategoriaAsync(categoria, id);

            if (erroNome != null)
            {
                return BadRequest(erroNome);
            }

            categoriaExistente.Nome = categoria.Nome;
            categoriaExistente.Descricao = categoria.Descricao;

            try
            {
                await _categoriaRepository.UpdateAsync(categoriaExistente);

                return Ok(categoriaExistente);
            }
            catch (DbUpdateException)
            {
                return StatusCode(500, "Não foi possível atualizar a categoria. Tente novamente.");
            }
        }

        [HttpDelete]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCategoria(int id)
        {
            var categoria = await _categoriaRepository.GetByIdAsync(id);

            if (categoria == null)
            {
                return NotFound();
            }

            bool possuiLivros = await _categoriaRepository.HasLivrosAsync(id);

            if (possuiLivros)
            {
                return BadRequest("Não é possível eliminar uma categoria com livros associados.");
            }

            try
            {
                await _categoriaRepository.DeleteAsync(categoria);
            }
            catch (DbUpdateException)
            {
                return BadRequest("Não foi possível eliminar a categoria. Verifique os dados associados.");
            }

            return Ok();
        }

        private async Task<string?> ValidarNomeCategoriaAsync(Categoria categoria, int? idCategoriaAtual = null)
        {
            if (string.IsNullOrWhiteSpace(categoria.Nome))
            {
                return "O nome da categoria é obrigatório.";
            }

            categoria.Nome = categoria.Nome.Trim();

            IQueryable<Categoria> categorias = _categoriaRepository.GetAll();

            if (idCategoriaAtual != null)
            {
                int id = idCategoriaAtual.Value;

                categorias = categorias.Where(outraCategoria => outraCategoria.IdCategoria != id);
            }

            bool categoriaJaExiste = await categorias.AnyAsync(
                outraCategoria => outraCategoria.Nome == categoria.Nome);

            if (categoriaJaExiste)
            {
                return "Já existe uma categoria com esse nome.";
            }

            return null;
        }
    }
}