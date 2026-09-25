using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBiblioteca.Data;
using WebBiblioteca.Models;

namespace WebBiblioteca.Controllers
{
    public class LivrosController : Controller
    {
        private readonly ILivroRepository _livroRepository;
        private readonly ICategoriaRepository _categoriaRepository;

        public LivrosController(ILivroRepository livroRepository, ICategoriaRepository categoriaRepository)
        {
            _livroRepository = livroRepository;
            _categoriaRepository = categoriaRepository;
        }

        public IActionResult Index()
        {
            return View(_livroRepository.GetAll().OrderBy(l => l.Titulo));
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var livro = await _livroRepository.GetByIdAsync(id.Value);

            if (livro == null)
            {
                return NotFound();
            }

            return View(livro);
        }

        public IActionResult Create()
        {
            var model = new LivroViewModel
            {
                AnoPublicacao = DateTime.Now.Year,
                ExemplaresDisponiveis = 1,
                Categorias = _categoriaRepository.GetComboCategorias()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(LivroViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Titulo))
            {
                ModelState.AddModelError("Titulo", "O título do livro é obrigatório.");
            }
            else
            {
                model.Titulo = model.Titulo.Trim();
            }

            if (string.IsNullOrWhiteSpace(model.Autor))
            {
                ModelState.AddModelError("Autor", "O autor do livro é obrigatório.");
            }
            else
            {
                model.Autor = model.Autor.Trim();
            }

            if (string.IsNullOrWhiteSpace(model.Editora))
            {
                ModelState.AddModelError("Editora", "A editora do livro é obrigatória.");
            }
            else
            {
                model.Editora = model.Editora.Trim();
            }

            if (string.IsNullOrWhiteSpace(model.Genero))
            {
                ModelState.AddModelError("Genero", "O género do livro é obrigatório.");
            }
            else
            {
                model.Genero = model.Genero.Trim();
            }

            if (model.AnoPublicacao <= 0)
            {
                ModelState.AddModelError("AnoPublicacao", "O ano de publicação deve ser superior a zero.");
            }

            if (model.ExemplaresDisponiveis < 0)
            {
                ModelState.AddModelError("ExemplaresDisponiveis", "O número de exemplares não pode ser negativo.");
            }

            bool categoriaExiste = await _livroRepository.CategoriaExisteAsync(model.IdCategoria);

            if (!categoriaExiste)
            {
                ModelState.AddModelError("IdCategoria", "Selecione uma categoria existente.");
            }

            if (!string.IsNullOrWhiteSpace(model.Titulo) && !string.IsNullOrWhiteSpace(model.Autor))
            {
                bool livroJaExiste = _livroRepository.GetAll().Any(l => l.Titulo == model.Titulo && l.Autor == model.Autor);

                if (livroJaExiste)
                {
                    ModelState.AddModelError("Titulo", "Já existe um livro com esse título e autor.");
                }
            }

            if (ModelState.IsValid)
            {
                var livro = new Livro
                {
                    Titulo = model.Titulo,
                    Autor = model.Autor,
                    Editora = model.Editora,
                    AnoPublicacao = model.AnoPublicacao,
                    Genero = model.Genero,
                    ExemplaresDisponiveis = model.ExemplaresDisponiveis,
                    IdCategoria = model.IdCategoria
                };

                try
                {
                    await _livroRepository.CreateAsync(livro);

                    return RedirectToAction("Index");
                }
                catch (DbUpdateException)
                {
                    ModelState.AddModelError(string.Empty, "Não foi possível guardar o livro. Verifique os dados e a categoria.");
                }
            }

            model.Categorias = _categoriaRepository.GetComboCategorias();

            return View(model);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var livro = await _livroRepository.GetByIdAsync(id.Value);

            if (livro == null)
            {
                return NotFound();
            }

            var model = new LivroViewModel
            {
                IdLivro = livro.IdLivro,
                Titulo = livro.Titulo,
                Autor = livro.Autor,
                Editora = livro.Editora,
                AnoPublicacao = livro.AnoPublicacao,
                Genero = livro.Genero,
                ExemplaresDisponiveis = livro.ExemplaresDisponiveis,
                IdCategoria = livro.IdCategoria,
                Categorias = _categoriaRepository.GetComboCategorias()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(LivroViewModel model)
        {
            var livroExistente = await _livroRepository.GetByIdAsync(model.IdLivro);

            if (livroExistente == null)
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(model.Titulo))
            {
                ModelState.AddModelError("Titulo", "O título do livro é obrigatório.");
            }
            else
            {
                model.Titulo = model.Titulo.Trim();
            }

            if (string.IsNullOrWhiteSpace(model.Autor))
            {
                ModelState.AddModelError("Autor", "O autor do livro é obrigatório.");
            }
            else
            {
                model.Autor = model.Autor.Trim();
            }

            if (string.IsNullOrWhiteSpace(model.Editora))
            {
                ModelState.AddModelError("Editora", "A editora do livro é obrigatória.");
            }
            else
            {
                model.Editora = model.Editora.Trim();
            }

            if (string.IsNullOrWhiteSpace(model.Genero))
            {
                ModelState.AddModelError("Genero", "O género do livro é obrigatório.");
            }
            else
            {
                model.Genero = model.Genero.Trim();
            }

            if (model.AnoPublicacao <= 0)
            {
                ModelState.AddModelError("AnoPublicacao", "O ano de publicação deve ser superior a zero.");
            }

            if (model.ExemplaresDisponiveis < 0)
            {
                ModelState.AddModelError("ExemplaresDisponiveis", "O número de exemplares não pode ser negativo.");
            }

            bool categoriaExiste = await _livroRepository.CategoriaExisteAsync(model.IdCategoria);

            if (!categoriaExiste)
            {
                ModelState.AddModelError("IdCategoria", "Selecione uma categoria existente.");
            }

            if (!string.IsNullOrWhiteSpace(model.Titulo) && !string.IsNullOrWhiteSpace(model.Autor))
            {
                bool livroJaExiste = _livroRepository.GetAll()
                    .Any(l => l.Titulo == model.Titulo &&
                              l.Autor == model.Autor &&
                              l.IdLivro != model.IdLivro);

                if (livroJaExiste)
                {
                    ModelState.AddModelError("Titulo", "Já existe outro livro com esse título e autor.");
                }
            }

            if (ModelState.IsValid)
            {
                livroExistente.Titulo = model.Titulo;
                livroExistente.Autor = model.Autor;
                livroExistente.Editora = model.Editora;
                livroExistente.AnoPublicacao = model.AnoPublicacao;
                livroExistente.Genero = model.Genero;
                livroExistente.ExemplaresDisponiveis = model.ExemplaresDisponiveis;
                livroExistente.IdCategoria = model.IdCategoria;

                try
                {
                    await _livroRepository.UpdateAsync(livroExistente);

                    return RedirectToAction("Index");
                }
                catch (DbUpdateException)
                {
                    ModelState.AddModelError(string.Empty, "Não foi possível alterar o livro. Verifique os dados e a categoria.");
                }
            }

            model.Categorias = _categoriaRepository.GetComboCategorias();

            return View(model);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var livro = await _livroRepository.GetByIdAsync(id.Value);

            if (livro == null)
            {
                return NotFound();
            }

            return View(livro);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var livro = await _livroRepository.GetByIdAsync(id);

            if (livro == null)
            {
                return NotFound();
            }

            try
            {
                await _livroRepository.DeleteAsync(livro);

                return RedirectToAction("Index");
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(string.Empty, "Não foi possível eliminar o livro. Verifique os registos associados.");

                return View("Delete", livro);
            }
        }
    }
}