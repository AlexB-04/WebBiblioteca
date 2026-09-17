using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBiblioteca.Data;
using WebBiblioteca.Models;

namespace WebBiblioteca.Controllers.API
{
    [Route("api/[controller]/{id?}")]
    [ApiController]
    public class CategoriasController : Controller
    {
        private ICategoriaRepository _categoriaRepository;

        public CategoriasController(ICategoriaRepository categoriaRepository)
        {
            _categoriaRepository = categoriaRepository;
        }

        [HttpGet]
        public async Task<IActionResult> GetCategorias(int? id)
        {
            if (id == null)
            {
                return Ok(_categoriaRepository.GetAll());
            }

            var categoria = await _categoriaRepository.GetByIdAsync(id.Value);

            if (categoria == null)
            {
                return NotFound();
            }

            return Ok(categoria);
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Categoria categoria)
        {
            if (categoria == null)
            {
                return BadRequest("Dados da categoria inválidos.");
            }

            if (string.IsNullOrWhiteSpace(categoria.Nome))
            {
                return BadRequest("O nome da categoria é obrigatório.");
            }

            bool categoriaJaExiste = _categoriaRepository.GetAll()
                .Any(c => c.Nome == categoria.Nome);

            if (categoriaJaExiste)
            {
                return BadRequest("Já existe uma categoria com esse nome.");
            }

            await _categoriaRepository.CreateAsync(categoria);

            return Ok(new
            {
                categoria.IdCategoria,
                categoria.Nome,
                categoria.Descricao
            });
        }

        [HttpPut]
        public async Task<IActionResult> Put(int id, [FromBody] Categoria categoria)
        {
            if (categoria == null)
            {
                return BadRequest("Dados da categoria inválidos.");
            }

            if (string.IsNullOrWhiteSpace(categoria.Nome))
            {
                return BadRequest("O nome da categoria é obrigatório.");
            }

            var categoriaExistente = await _categoriaRepository.GetByIdAsync(id);

            if (categoriaExistente == null)
            {
                return NotFound();
            }

            bool categoriaJaExiste = _categoriaRepository.GetAll()
                .Any(c => c.Nome == categoria.Nome && c.IdCategoria != id);

            if (categoriaJaExiste)
            {
                return BadRequest("Já existe outra categoria com esse nome.");
            }

            categoriaExistente.Nome = categoria.Nome;
            categoriaExistente.Descricao = categoria.Descricao;

            await _categoriaRepository.UpdateAsync(categoriaExistente);

            return Ok(categoriaExistente);
        }

        [HttpDelete]
        public async Task<IActionResult> Delete(int id)
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
    }
}