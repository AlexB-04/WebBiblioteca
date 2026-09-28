using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IO;
using WebBiblioteca.Data;
using WebBiblioteca.Helpers;
using WebBiblioteca.Models;

namespace WebBiblioteca.Controllers
{
    public class LivrosController : Controller
    {
        private readonly ILivroRepository _livroRepository;
        private readonly ICategoriaRepository _categoriaRepository;
        private readonly IImageHelper _imageHelper;

        public LivrosController(ILivroRepository livroRepository,
            ICategoriaRepository categoriaRepository, IImageHelper imageHelper)
        {
            _livroRepository = livroRepository;
            _categoriaRepository = categoriaRepository;
            _imageHelper = imageHelper;
        }

        public IActionResult Index()
        {
            return View(_livroRepository.GetAll().OrderBy(livro => livro.Titulo));
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
                bool livroJaExiste = _livroRepository.GetAll().Any(outroLivro => outroLivro.Titulo == model.Titulo && outroLivro.Autor == model.Autor);

                if (livroJaExiste)
                {
                    ModelState.AddModelError("Titulo", "Já existe um livro com esse título e autor.");
                }
            }

            if (model.ImageFile != null)
            {
                if (model.ImageFile.Length == 0)
                {
                    ModelState.AddModelError("ImageFile", "O ficheiro selecionado está vazio.");
                }

                if (model.ImageFile.Length > 5 * 1024 * 1024)
                {
                    ModelState.AddModelError("ImageFile", "A capa não pode ultrapassar 5 MB.");
                }

                if (model.ImageFile.ContentType != "image/jpeg")
                {
                    ModelState.AddModelError("ImageFile", "Selecione uma imagem JPEG (.jpg ou .jpeg).");
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var path = string.Empty;

                    if (model.ImageFile != null && model.ImageFile.Length > 0)
                    {
                        path = await _imageHelper.UploadImageAsync(model.ImageFile, "livros");
                    }

                    var livro = new Livro
                    {
                        Titulo = model.Titulo,
                        Autor = model.Autor,
                        Editora = model.Editora,
                        AnoPublicacao = model.AnoPublicacao,
                        Genero = model.Genero,
                        ExemplaresDisponiveis = model.ExemplaresDisponiveis,
                        IdCategoria = model.IdCategoria,
                        ImageUrl = path
                    };

                    await _livroRepository.CreateAsync(livro);

                    return RedirectToAction("Index");
                }
                catch (DbUpdateException)
                {
                    ModelState.AddModelError(string.Empty, "Não foi possível guardar o livro. Verifique os dados e a categoria.");
                }
                catch (IOException)
                {
                    ModelState.AddModelError("ImageFile", "Não foi possível guardar a capa. Selecione o ficheiro e tente novamente.");
                }
                catch (UnauthorizedAccessException)
                {
                    ModelState.AddModelError("ImageFile", "Não foi possível guardar a capa. Contacte o administrador.");
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
                ImageUrl = livro.ImageUrl,
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

            model.ImageUrl = livroExistente.ImageUrl;

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
                    .Any(outroLivro => outroLivro.Titulo == model.Titulo &&
                                      outroLivro.Autor == model.Autor &&
                                      outroLivro.IdLivro != model.IdLivro);

                if (livroJaExiste)
                {
                    ModelState.AddModelError("Titulo", "Já existe outro livro com esse título e autor.");
                }
            }

            if (model.ImageFile != null)
            {
                if (model.ImageFile.Length == 0)
                {
                    ModelState.AddModelError("ImageFile", "O ficheiro selecionado está vazio.");
                }

                if (model.ImageFile.Length > 5 * 1024 * 1024)
                {
                    ModelState.AddModelError("ImageFile", "A capa não pode ultrapassar 5 MB.");
                }

                if (model.ImageFile.ContentType != "image/jpeg")
                {
                    ModelState.AddModelError("ImageFile", "Selecione uma imagem JPEG (.jpg ou .jpeg).");
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var path = livroExistente.ImageUrl;

                    if (model.ImageFile != null && model.ImageFile.Length > 0)
                    {
                        path = await _imageHelper.UploadImageAsync(model.ImageFile, "livros");
                    }

                    livroExistente.Titulo = model.Titulo;
                    livroExistente.Autor = model.Autor;
                    livroExistente.Editora = model.Editora;
                    livroExistente.AnoPublicacao = model.AnoPublicacao;
                    livroExistente.Genero = model.Genero;
                    livroExistente.ExemplaresDisponiveis = model.ExemplaresDisponiveis;
                    livroExistente.IdCategoria = model.IdCategoria;
                    livroExistente.ImageUrl = path;

                    await _livroRepository.UpdateAsync(livroExistente);

                    return RedirectToAction("Index");
                }
                catch (DbUpdateException)
                {
                    ModelState.AddModelError(string.Empty, "Não foi possível alterar o livro. Verifique os dados e a categoria.");
                }
                catch (IOException)
                {
                    ModelState.AddModelError("ImageFile", "Não foi possível guardar a capa. Selecione o ficheiro e tente novamente.");
                }
                catch (UnauthorizedAccessException)
                {
                    ModelState.AddModelError("ImageFile", "Não foi possível guardar a capa. Contacte o administrador.");
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