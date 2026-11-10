using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using WebBiblioteca.Models;
using Microsoft.AspNetCore.Mvc;
using WebBiblioteca.Data;

namespace WebBiblioteca.Controllers
{
    [Authorize(Roles = "Admin")]
    public class CategoriasController : Controller
    {
        private readonly ICategoriaRepository _categoriaRepository;

        public CategoriasController(ICategoriaRepository categoriaRepository)
        {
            _categoriaRepository = categoriaRepository;
        }

        public IActionResult Index()
        {
            return View(_categoriaRepository.GetAll().OrderBy(categoria => categoria.Nome));
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Categoria categoria)
        {
            await ValidarNomeCategoriaAsync(categoria);

            if (!ModelState.IsValid)
            {
                return View(categoria);
            }

            categoria.IdCategoria = 0;

            try
            {
                await _categoriaRepository.CreateAsync(categoria);

                return RedirectToAction("Index");
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(string.Empty, "Não foi possível guardar a categoria. Tente novamente.");

                return View(categoria);
            }
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var categoria = await _categoriaRepository.GetByIdAsync(id.Value);

            if (categoria == null)
            {
                return NotFound();
            }

            return View(categoria);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Categoria categoria)
        {
            var categoriaExistente = await _categoriaRepository
                .GetByIdAsync(categoria.IdCategoria);

            if (categoriaExistente == null)
            {
                return NotFound();
            }

            await ValidarNomeCategoriaAsync(categoria, categoria.IdCategoria);

            if (!ModelState.IsValid)
            {
                return View(categoria);
            }

            categoriaExistente.Nome = categoria.Nome;
            categoriaExistente.Descricao = categoria.Descricao;

            try
            {
                await _categoriaRepository.UpdateAsync(categoriaExistente);

                return RedirectToAction("Index");
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(string.Empty, "Não foi possível atualizar a categoria. Tente novamente.");

                return View(categoria);
            }
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var categoria = await _categoriaRepository.GetByIdAsync(id.Value);

            if (categoria == null)
            {
                return NotFound();
            }

            return View(categoria);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var categoria = await _categoriaRepository.GetByIdAsync(id);

            if (categoria == null)
            {
                return NotFound();
            }

            bool possuiLivros = await _categoriaRepository.HasLivrosAsync(id);

            if (possuiLivros)
            {
                ModelState.AddModelError(string.Empty, "Não é possível eliminar uma categoria com livros associados.");

                return View("Delete", categoria);
            }

            try
            {
                await _categoriaRepository.DeleteAsync(categoria);

                return RedirectToAction("Index");
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(string.Empty, "Não foi possível eliminar a categoria. Verifique os dados associados.");

                return View("Delete", categoria);
            }
        }

        private async Task ValidarNomeCategoriaAsync(Categoria categoria, int? idCategoriaAtual = null)
        {
            if (string.IsNullOrWhiteSpace(categoria.Nome))
            {
                ModelState.AddModelError("Nome", "O nome da categoria é obrigatório.");

                return;
            }

            categoria.Nome = categoria.Nome.Trim();

            IQueryable<Categoria> categorias = _categoriaRepository.GetAll();

            if (idCategoriaAtual != null)
            {
                int id = idCategoriaAtual.Value;

                categorias = categorias.Where(outraCategoria => outraCategoria.IdCategoria != id);
            }

            bool categoriaJaExiste = await categorias.AnyAsync(outraCategoria =>
                outraCategoria.Nome == categoria.Nome);

            if (categoriaJaExiste)
            {
                ModelState.AddModelError("Nome", "Já existe uma categoria com esse nome.");
            }
        }
    }
}