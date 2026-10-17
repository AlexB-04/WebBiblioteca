using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBiblioteca.Data;
using WebBiblioteca.Helpers;
using WebBiblioteca.Models;

namespace WebBiblioteca.Controllers
{
    [Authorize(Roles = "Funcionario")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public class LeitoresController : Controller
    {
        private readonly ILeitorRepository _leitorRepository;
        private readonly IUserHelper _userHelper;
        private readonly IImageHelper _imageHelper;

        public LeitoresController(ILeitorRepository leitorRepository, IUserHelper userHelper,
            IImageHelper imageHelper)
        {
            _leitorRepository = leitorRepository;
            _userHelper = userHelper;
            _imageHelper = imageHelper;
        }

        public async Task<IActionResult> Index(string? pesquisa)
        {
            var leitores = _leitorRepository.GetAll();
            if (!string.IsNullOrWhiteSpace(pesquisa))
            {
                pesquisa = pesquisa.Trim();
                string texto = pesquisa.ToUpper();
                int.TryParse(pesquisa, out int id);
                leitores = leitores.Where(leitor =>
                    (leitor.Nome != null && leitor.Nome.ToUpper().Contains(texto)) ||
                    (leitor.CodigoLeitor != null && leitor.CodigoLeitor.ToUpper().Contains(texto)) ||
                    leitor.IdLeitor == id);
            }
            ViewBag.Pesquisa = pesquisa;
            return View(await leitores.OrderBy(leitor => leitor.Nome).ToListAsync());
        }

        public async Task<IActionResult> Details(int id)
        {
            var leitor = await _leitorRepository.GetByIdAsync(id);
            if (leitor == null)
            {
                return NotFound();
            }
            if (leitor.UserId != null)
            {
                leitor.User = await _userHelper.GetUserByIdAsync(leitor.UserId);
            }
            ViewBag.LivrosEmprestados = await _leitorRepository.ContarLivrosEmprestadosAsync(id);
            return View(leitor);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var leitor = await _leitorRepository.GetByIdAsync(id);
            if (leitor == null)
            {
                return NotFound();
            }
            var user = leitor.UserId == null ? null : await _userHelper.GetUserByIdAsync(leitor.UserId);
            if (leitor.UserId != null && user == null)
            {
                return NotFound();
            }

            return View(new LeitorViewModel
            {
                IdLeitor = leitor.IdLeitor,
                Nome = user?.FirstName ?? leitor.Nome ?? string.Empty,
                Apelido = user?.LastName,
                Contacto = leitor.Contacto ?? string.Empty,
                Email = leitor.Email ?? string.Empty,
                TipoUtilizador = leitor.TipoUtilizador ?? string.Empty,
                LimiteEmprestimos = leitor.LimiteEmprestimos,
                TemConta = leitor.UserId != null,
                CodigoLeitor = leitor.CodigoLeitor
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit([FromRoute] int id, LeitorViewModel model)
        {
            if (id != model.IdLeitor)
            {
                return NotFound();
            }
            var leitor = await _leitorRepository.GetByIdAsync(id);
            if (leitor == null)
            {
                return NotFound();
            }
            model.TemConta = leitor.UserId != null;
            model.CodigoLeitor = leitor.CodigoLeitor;
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            bool emailAlterado = model.TemConta &&
                !string.Equals(leitor.Email, model.Email.Trim(), StringComparison.OrdinalIgnoreCase);
            try
            {
                var result = await _userHelper.UpdateLeitorAsync(leitor, model);
                if (result.Succeeded)
                {
                    TempData["LeitorMessage"] = emailAlterado
                        ? "Leitor alterado. O administrador deve confirmar o novo email em Contas de leitores; depois o leitor define novamente a palavra-passe."
                        : "Leitor alterado com sucesso.";
                    return RedirectToAction("Details", new { id });
                }
                AddErrors(result);
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(string.Empty, "Não foi possível alterar o leitor. Verifique os dados e tente novamente.");
            }
            return View(model);
        }

        public async Task<IActionResult> Delete(int id)
        {
            var leitor = await _leitorRepository.GetByIdAsync(id);
            if (leitor == null)
            {
                return NotFound();
            }
            return View(leitor);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed([FromRoute] int id)
        {
            var leitor = await _leitorRepository.GetByIdAsync(id);
            if (leitor == null)
            {
                return NotFound();
            }
            var user = leitor.UserId == null ? null : await _userHelper.GetUserByIdAsync(leitor.UserId);
            try
            {
                var result = await _userHelper.DeleteLeitorAsync(leitor);
                if (result.Succeeded)
                {
                    _imageHelper.TryDeleteImage(user?.ImageUrl, "users");
                    TempData["LeitorMessage"] = "Leitor eliminado com sucesso.";
                    return RedirectToAction("Index");
                }
                AddErrors(result);
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(string.Empty, "Não foi possível eliminar o leitor. Verifique os registos associados e atualize a página.");
            }
            return View("Delete", leitor);
        }

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                string field = error.Code == "DuplicateEmail" || error.Code == "DuplicateUserName"
                    ? "Email" : error.Code == "LimiteEmprestimos" ? "LimiteEmprestimos" : string.Empty;
                ModelState.AddModelError(field, error.Code == "ConcurrencyFailure"
                    ? "Os dados foram alterados por outro pedido. Atualize a página e tente novamente."
                    : error.Description);
            }
        }
    }
}