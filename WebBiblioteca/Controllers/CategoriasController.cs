using Microsoft.EntityFrameworkCore;
using WebBiblioteca.Models;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using WebBiblioteca.Data;

namespace WebBiblioteca.Controllers
{
    public class CategoriasController : Controller
    {
        private readonly ICategoriaRepository _categoriaRepository;

        public CategoriasController(ICategoriaRepository categoriaRepository)
        {
            _categoriaRepository = categoriaRepository;
        }

        public IActionResult Index()
        {
            return View(_categoriaRepository.GetAll().OrderBy(c => c.Nome));
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Categoria categoria)
        {
            if (string.IsNullOrWhiteSpace(categoria.Nome))
            {
                ModelState.AddModelError("Nome", "O nome da categoria é obrigatório.");
            }
            else
            {
                bool categoriaJaExiste = _categoriaRepository.GetAll().Any(c => c.Nome == categoria.Nome);

                if (categoriaJaExiste)
                {
                    ModelState.AddModelError("Nome", "Já existe uma categoria com esse nome.");
                }
            }

            if (ModelState.IsValid)
            {
                categoria.IdCategoria = 0;

                await _categoriaRepository.CreateAsync(categoria);

                return RedirectToAction("Index");
            }

            return View(categoria);
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

            if (string.IsNullOrWhiteSpace(categoria.Nome))
            {
                ModelState.AddModelError("Nome", "O nome da categoria é obrigatório.");
            }
            else
            {
                bool categoriaJaExiste = _categoriaRepository.GetAll()
                    .Any(c => c.Nome == categoria.Nome &&
                              c.IdCategoria != categoria.IdCategoria);

                if (categoriaJaExiste)
                {
                    ModelState.AddModelError("Nome", "Já existe outra categoria com esse nome.");
                }
            }

            if (ModelState.IsValid)
            {
                categoriaExistente.Nome = categoria.Nome;
                categoriaExistente.Descricao = categoria.Descricao;

                await _categoriaRepository.UpdateAsync(categoriaExistente);

                return RedirectToAction("Index");
            }

            return View(categoria);
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
                ModelState.AddModelError(string.Empty,"Não é possível eliminar uma categoria com livros associados.");

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
    }
}