using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebBiblioteca.Data;
using WebBiblioteca.Models;

namespace WebBiblioteca.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AvisosFuncionariosController : Controller
    {
        private readonly IAvisoFuncionarioRepository _avisoRepository;

        public AvisosFuncionariosController(IAvisoFuncionarioRepository avisoRepository)
        {
            _avisoRepository = avisoRepository;
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new CriarAvisoFuncionarioViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CriarAvisoFuncionarioViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Mensagem))
            {
                ModelState.AddModelError(nameof(model.Mensagem), "A mensagem é obrigatória.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var aviso = new AvisoFuncionario
            {
                Mensagem = model.Mensagem.Trim()
            };

            await _avisoRepository.CreateAsync(aviso);

            TempData["MensagemSucesso"] = "Aviso criado com sucesso.";

            return RedirectToAction(nameof(Create));
        }
    }
}