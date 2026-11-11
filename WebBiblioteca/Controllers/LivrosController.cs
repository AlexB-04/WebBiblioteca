using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.IO;
using WebBiblioteca.Data;
using WebBiblioteca.Helpers;
using WebBiblioteca.Models;

namespace WebBiblioteca.Controllers
{
    [Authorize(Roles = "Admin,Funcionario")]
    public class LivrosController : Controller
    {
        private readonly ILivroRepository _livroRepository;
        private readonly ICategoriaRepository _categoriaRepository;
        private readonly IReservaRepository _reservaRepository;
        private readonly IImageHelper _imageHelper;
        private readonly ILogger<LivrosController> _logger;

        public LivrosController(ILivroRepository livroRepository,
            ICategoriaRepository categoriaRepository, IReservaRepository reservaRepository,
            IImageHelper imageHelper, ILogger<LivrosController> logger)
        {
            _livroRepository = livroRepository;
            _categoriaRepository = categoriaRepository;
            _reservaRepository = reservaRepository;
            _imageHelper = imageHelper;
            _logger = logger;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index(string? pesquisa, int? idCategoria, string? estado)
        {
            if (idCategoria.HasValue && idCategoria.Value <= 0)
            {
                idCategoria = null;
            }

            if (estado != "Disponiveis" && estado != "Emprestados" && estado != "Reservados")
            {
                estado = null;
            }

            // Os prazos vencidos devem ser processados antes de consultar a fila de reservas.
            if (estado == "Disponiveis" || estado == "Reservados")
            {
                await _reservaRepository.AtualizarReservasExpiradasAsync();
            }

            var categorias = await _categoriaRepository.GetAll()
                .OrderBy(categoria => categoria.Nome)
                .ToListAsync();

            ViewBag.Categorias = new SelectList(categorias, "IdCategoria", "Nome", idCategoria);
            ViewBag.Pesquisa = pesquisa?.Trim();
            ViewBag.Estado = estado;

            var livros = _livroRepository.PesquisarLivros(pesquisa, idCategoria, estado);

            return View(await livros.OrderBy(livro => livro.Titulo).ToListAsync());
        }

        [AllowAnonymous]
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
            await ValidarLivroAsync(model);

            if (ModelState.IsValid)
            {
                string? novaCapa = null;
                bool guardado = false;

                try
                {
                    if (model.ImageFile != null && model.ImageFile.Length > 0)
                    {
                        novaCapa = await _imageHelper.UploadImageAsync(model.ImageFile, "livros");
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
                        ImageUrl = novaCapa ?? string.Empty
                    };

                    await _livroRepository.CreateAsync(livro);
                    guardado = true;
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
                finally
                {
                    // Se a base de dados não guardar o livro, eliminar a nova capa.
                    if (!guardado)
                    {
                        _imageHelper.TryDeleteImage(novaCapa, "livros");
                    }
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
            await ValidarLivroAsync(model, model.IdLivro);

            if (ModelState.IsValid)
            {
                string? capaAnterior = livroExistente.ImageUrl;
                string? novaCapa = null;
                bool guardado = false;

                try
                {
                    if (model.ImageFile != null && model.ImageFile.Length > 0)
                    {
                        novaCapa = await _imageHelper.UploadImageAsync(model.ImageFile, "livros");
                    }

                    livroExistente.Titulo = model.Titulo;
                    livroExistente.Autor = model.Autor;
                    livroExistente.Editora = model.Editora;
                    livroExistente.AnoPublicacao = model.AnoPublicacao;
                    livroExistente.Genero = model.Genero;
                    livroExistente.ExemplaresDisponiveis = model.ExemplaresDisponiveis;
                    livroExistente.IdCategoria = model.IdCategoria;
                    livroExistente.ImageUrl = novaCapa ?? capaAnterior;

                    // Mantém a atualização das reservas no LivroRepository.
                    await _livroRepository.UpdateAsync(livroExistente);
                    guardado = true;
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
                finally
                {
                    if (!guardado)
                    {
                        _imageHelper.TryDeleteImage(novaCapa, "livros");
                    }
                }

                if (guardado)
                {
                    if (novaCapa != null)
                    {
                        await TentarEliminarCapaNaoUtilizadaAsync(capaAnterior);
                    }

                    return RedirectToAction("Index");
                }
            }

            model.Categorias = _categoriaRepository.GetComboCategorias();
            return View(model);
        }

        private async Task ValidarLivroAsync(LivroViewModel model, int? idLivroAtual = null)
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
                var livrosExistentes = _livroRepository.GetAll()
                    .Where(outroLivro => outroLivro.Titulo == model.Titulo && outroLivro.Autor == model.Autor);

                if (idLivroAtual.HasValue)
                {
                    livrosExistentes = livrosExistentes.Where(outroLivro => outroLivro.IdLivro != idLivroAtual.Value);
                }

                bool livroJaExiste = await livrosExistentes.AnyAsync();

                if (livroJaExiste)
                {
                    string mensagem = idLivroAtual.HasValue
                        ? "Já existe outro livro com esse título e autor."
                        : "Já existe um livro com esse título e autor.";

                    ModelState.AddModelError("Titulo", mensagem);
                }
            }

            if (model.ImageFile != null)
            {
                if (model.ImageFile.Length == 0)
                {
                    ModelState.AddModelError("ImageFile", "O ficheiro selecionado está vazio.");
                }
                else if (model.ImageFile.Length > 5 * 1024 * 1024)
                {
                    ModelState.AddModelError("ImageFile", "A capa não pode ultrapassar 5 MB.");
                }
                else
                {
                    string extensao = Path.GetExtension(model.ImageFile.FileName).ToLowerInvariant();
                    bool jpegValido = (extensao == ".jpg" || extensao == ".jpeg") &&
                        model.ImageFile.ContentType == "image/jpeg";

                    if (jpegValido)
                    {
                        try
                        {
                            using var reader = new BinaryReader(model.ImageFile.OpenReadStream());
                            byte[] assinatura = reader.ReadBytes(3);
                            jpegValido = assinatura.Length == 3 &&
                                assinatura[0] == 0xFF && assinatura[1] == 0xD8 && assinatura[2] == 0xFF;
                        }
                        catch (IOException)
                        {
                            jpegValido = false;
                        }
                        catch (UnauthorizedAccessException)
                        {
                            jpegValido = false;
                        }
                    }

                    if (!jpegValido)
                    {
                        ModelState.AddModelError("ImageFile", "Selecione uma imagem JPEG válida (.jpg ou .jpeg).");
                    }
                }
            }
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

            if (await _livroRepository.TemHistoricoEmprestimosAsync(id))
            {
                ModelState.AddModelError(string.Empty, "Não é possível eliminar um livro com histórico de empréstimos.");
                return View("Delete", livro);
            }

            if (await _livroRepository.TemReservasAsync(id))
            {
                ModelState.AddModelError(string.Empty, "Não é possível eliminar um livro com reservas associadas.");
                return View("Delete", livro);
            }

            try
            {
                await _livroRepository.DeleteAsync(livro);
            }
            catch (DbUpdateException)
            {
                // Mantém a proteção contra alterações concorrentes entre a verificação e o DELETE.
                ModelState.AddModelError(string.Empty, "Não foi possível eliminar o livro. Verifique os registos associados.");
                return View("Delete", livro);
            }

            await TentarEliminarCapaNaoUtilizadaAsync(livro.ImageUrl);
            return RedirectToAction("Index");
        }

        private async Task TentarEliminarCapaNaoUtilizadaAsync(string? imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl))
            {
                return;
            }

            try
            {
                // Não apagar capas ainda utilizadas por outros livros.
                bool emUso = await _livroRepository.GetAll()
                    .AnyAsync(livro => livro.ImageUrl == imageUrl);

                if (!emUso)
                {
                    _imageHelper.TryDeleteImage(imageUrl, "livros");
                }
            }
            catch (Exception ex)
            {
                // A operação na base já foi concluída: a limpeza não deve provocar erro 500.
                _logger.LogWarning(ex, "Não foi possível verificar a utilização da capa {ImageUrl}.", imageUrl);
            }
        }
    }
}