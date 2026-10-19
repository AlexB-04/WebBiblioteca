using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data.Common;
using System.Security.Claims;
using WebBiblioteca.Data;
using WebBiblioteca.Helpers;
using WebBiblioteca.Models;

namespace WebBiblioteca.Controllers
{
    [Authorize(Roles = "Funcionario,Leitor")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public class ReservasController : Controller
    {
        private readonly IReservaRepository _reservaRepository;
        private readonly ILeitorRepository _leitorRepository;
        private readonly ILivroRepository _livroRepository;
        private readonly IUserHelper _userHelper;
        private readonly ILogger<ReservasController> _logger;

        public ReservasController(
            IReservaRepository reservaRepository,
            ILeitorRepository leitorRepository,
            ILivroRepository livroRepository,
            IUserHelper userHelper,
            ILogger<ReservasController> logger)
        {
            _reservaRepository = reservaRepository;
            _leitorRepository = leitorRepository;
            _livroRepository = livroRepository;
            _userHelper = userHelper;
            _logger = logger;
        }

        public async Task<IActionResult> Index(int? idLivro, int? idLeitor, bool? ativa)
        {
            try
            {
                await _reservaRepository.AtualizarReservasExpiradasAsync();
                var reservas = _reservaRepository.GetAll();

                if (!User.IsInRole("Funcionario"))
                {
                    var leitor = await ObterLeitorAtualAsync();
                    if (leitor == null)
                    {
                        return AcessoLeitorInvalido();
                    }

                    // O leitor nunca escolhe o IdLeitor da consulta.
                    reservas = reservas.Where(reserva => reserva.IdLeitor == leitor.IdLeitor);
                }
                else if (idLeitor.HasValue)
                {
                    reservas = reservas.Where(reserva => reserva.IdLeitor == idLeitor.Value);
                }

                if (idLivro.HasValue)
                {
                    reservas = reservas.Where(reserva => reserva.IdLivro == idLivro.Value);
                }

                if (ativa.HasValue)
                {
                    reservas = reservas.Where(reserva => reserva.Ativa == ativa.Value);
                }

                ViewBag.IdLivro = idLivro;
                ViewBag.IdLeitor = User.IsInRole("Funcionario") ? idLeitor : null;
                ViewBag.Ativa = ativa;

                var lista = await reservas
                    .OrderBy(reserva => reserva.IdLivro)
                    .ThenBy(reserva => reserva.Ordem)
                    .ThenBy(reserva => reserva.IdReserva)
                    .ToListAsync();

                return View(lista);
            }
            catch (DbUpdateException ex)
            {
                return ErroDados(ex);
            }
            catch (DbException ex)
            {
                return ErroDados(ex);
            }
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || id.Value <= 0)
            {
                return NaoEncontrado();
            }

            try
            {
                await _reservaRepository.AtualizarReservasExpiradasAsync();
                var reserva = await ObterReservaAutorizadaAsync(id.Value);
                if (reserva == null)
                {
                    return NaoEncontrado();
                }

                return View(reserva);
            }
            catch (DbUpdateException ex)
            {
                return ErroDados(ex);
            }
            catch (DbException ex)
            {
                return ErroDados(ex);
            }
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Create(int? idLivro, int? idLeitor)
        {
            // Uma intenção anónima não cria qualquer reserva na base de dados.
            if (User.Identity?.IsAuthenticated != true)
            {
                return RedirectToAction("Register", "Account");
            }

            if (!User.IsInRole("Funcionario") && !User.IsInRole("Leitor"))
            {
                return Forbid();
            }

            try
            {
                var model = new ReservaFormViewModel
                {
                    IdLivro = idLivro ?? 0
                };

                if (User.IsInRole("Funcionario"))
                {
                    model.IdLeitor = idLeitor ?? 0;
                }
                else
                {
                    var leitor = await ObterLeitorAtualAsync();
                    if (leitor == null)
                    {
                        return AcessoLeitorInvalido();
                    }

                    model.IdLeitor = leitor.IdLeitor;
                }

                await PreencherListasAsync(model);
                return View(model);
            }
            catch (DbUpdateException ex)
            {
                return ErroDados(ex);
            }
            catch (DbException ex)
            {
                return ErroDados(ex);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("IdLeitor,IdLivro")] ReservaFormViewModel model)
        {
            try
            {
                if (!User.IsInRole("Funcionario"))
                {
                    var leitorAtual = await ObterLeitorAtualAsync();
                    if (leitorAtual == null)
                    {
                        return AcessoLeitorInvalido();
                    }

                    // Ignorar o IdLeitor do formulário: a identidade vem da sessão.
                    model.IdLeitor = leitorAtual.IdLeitor;
                }

                await _reservaRepository.AtualizarReservasExpiradasAsync();

                if (model.IdLeitor <= 0)
                {
                    ModelState.AddModelError("IdLeitor", "Selecione um leitor válido.");
                }
                else
                {
                    var leitor = await _leitorRepository.GetByIdAsync(model.IdLeitor);
                    if (leitor == null || !await LeitorConfirmadoAsync(leitor))
                    {
                        ModelState.AddModelError("IdLeitor", "O leitor não existe ou a conta ainda não está confirmada.");
                    }
                }

                if (model.IdLivro <= 0)
                {
                    ModelState.AddModelError("IdLivro", "Selecione um livro válido.");
                }
                else
                {
                    var livro = await _livroRepository.GetByIdAsync(model.IdLivro);
                    if (livro == null)
                    {
                        ModelState.AddModelError("IdLivro", "O livro indicado não existe.");
                    }
                }

                if (ModelState.IsValid)
                {
                    string? erro = await ObterErroLivroAsync(model.IdLeitor, model.IdLivro);
                    if (erro != null)
                    {
                        ModelState.AddModelError(string.Empty, erro);
                    }
                    else if (await _reservaRepository.AtingiuLimiteReservasAsync(model.IdLeitor))
                    {
                        ModelState.AddModelError(string.Empty, "O leitor atingiu o limite de reservas ativas.");
                    }
                    else
                    {
                        var reserva = new Reserva
                        {
                            IdLeitor = model.IdLeitor,
                            IdLivro = model.IdLivro
                        };

                        bool criada = await _reservaRepository.CreateAsync(reserva);
                        if (criada)
                        {
                            TempData["ReservaMessage"] = "Reserva criada com sucesso.";
                            return RedirectToAction("Details", new { id = reserva.IdReserva });
                        }

                        ModelState.AddModelError(string.Empty,
                            "Não foi possível criar a reserva. Os dados ou a disponibilidade podem ter mudado. Atualize a página.");
                    }
                }

                await PreencherListasAsync(model);
                return View(model);
            }
            catch (DbUpdateException ex)
            {
                return ErroDados(ex);
            }
            catch (DbException ex)
            {
                return ErroDados(ex);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || id.Value <= 0)
            {
                return NaoEncontrado();
            }

            try
            {
                await _reservaRepository.AtualizarReservasExpiradasAsync();
                var reserva = await ObterReservaAutorizadaAsync(id.Value);
                if (reserva == null)
                {
                    return NaoEncontrado();
                }

                if (!reserva.Ativa || reserva.DataDisponivel.HasValue)
                {
                    return OperacaoInvalida("Esta reserva não pode ser alterada. Consulte o estado da reserva.");
                }

                var model = new ReservaFormViewModel
                {
                    IdReserva = reserva.IdReserva,
                    IdLeitor = reserva.IdLeitor,
                    IdLivro = reserva.IdLivro
                };

                await PreencherListasAsync(model);
                return View(model);
            }
            catch (DbUpdateException ex)
            {
                return ErroDados(ex);
            }
            catch (DbException ex)
            {
                return ErroDados(ex);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("IdLivro")] ReservaFormViewModel model)
        {
            if (id <= 0)
            {
                return NaoEncontrado();
            }

            try
            {
                await _reservaRepository.AtualizarReservasExpiradasAsync();
                var reserva = await ObterReservaAutorizadaAsync(id);
                if (reserva == null)
                {
                    return NaoEncontrado();
                }

                if (!reserva.Ativa || reserva.DataDisponivel.HasValue)
                {
                    return OperacaoInvalida("Esta reserva já não pode ser alterada. Atualize os dados.");
                }

                model.IdReserva = reserva.IdReserva;
                model.IdLeitor = reserva.IdLeitor;

                if (model.IdLivro <= 0)
                {
                    ModelState.AddModelError("IdLivro", "Selecione um livro válido.");
                }
                else if (reserva.IdLivro == model.IdLivro)
                {
                    ModelState.AddModelError("IdLivro", "Escolha um livro diferente do atual.");
                }
                else if (await _livroRepository.GetByIdAsync(model.IdLivro) == null)
                {
                    ModelState.AddModelError("IdLivro", "O livro indicado não existe.");
                }

                if (ModelState.IsValid)
                {
                    string? erro = await ObterErroLivroAsync(reserva.IdLeitor, model.IdLivro);
                    if (erro != null)
                    {
                        ModelState.AddModelError(string.Empty, erro);
                    }
                    else
                    {
                        bool alterada = await _reservaRepository.UpdateAsync(id, model.IdLivro);
                        if (alterada)
                        {
                            TempData["ReservaMessage"] = "Reserva alterada. A posição na fila foi atualizada.";
                            return RedirectToAction("Details", new { id });
                        }

                        ModelState.AddModelError(string.Empty,
                            "Não foi possível alterar a reserva. A disponibilidade ou o estado podem ter mudado.");
                    }
                }

                await PreencherListasAsync(model);
                return View(model);
            }
            catch (DbUpdateException ex)
            {
                return ErroDados(ex);
            }
            catch (DbException ex)
            {
                return ErroDados(ex);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Cancelar(int? id)
        {
            if (id == null || id.Value <= 0)
            {
                return NaoEncontrado();
            }

            try
            {
                await _reservaRepository.AtualizarReservasExpiradasAsync();
                var reserva = await ObterReservaAutorizadaAsync(id.Value);
                if (reserva == null)
                {
                    return NaoEncontrado();
                }

                return View("Delete", reserva);
            }
            catch (DbUpdateException ex)
            {
                return ErroDados(ex);
            }
            catch (DbException ex)
            {
                return ErroDados(ex);
            }
        }

        [HttpPost]
        [ActionName("Cancelar")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelarConfirmado(int id)
        {
            if (id <= 0)
            {
                return NaoEncontrado();
            }

            try
            {
                await _reservaRepository.AtualizarReservasExpiradasAsync();
                var reserva = await ObterReservaAutorizadaAsync(id);
                if (reserva == null)
                {
                    return NaoEncontrado();
                }

                if (!reserva.Ativa)
                {
                    return OperacaoInvalida("Esta reserva já foi cancelada ou expirou.");
                }

                bool cancelada = await _reservaRepository.DeleteAsync(id);
                if (!cancelada)
                {
                    return OperacaoInvalida("Não foi possível cancelar. O estado da reserva pode ter mudado.");
                }

                TempData["ReservaMessage"] = "Reserva cancelada. A fila foi atualizada.";
                return RedirectToAction("Details", new { id });
            }
            catch (DbUpdateException ex)
            {
                return ErroDados(ex);
            }
            catch (DbException ex)
            {
                return ErroDados(ex);
            }
        }

        private async Task<Leitor?> ObterLeitorAtualAsync()
        {
            string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return null;
            }

            var user = await _userHelper.GetUserByIdAsync(userId);
            if (user == null || !user.Aprovado || user.DeveAlterarPassword || !user.EmailConfirmed)
            {
                return null;
            }

            return await _leitorRepository.GetByUserIdAsync(userId);
        }

        private async Task<Reserva?> ObterReservaAutorizadaAsync(int id)
        {
            if (!User.IsInRole("Funcionario"))
            {
                var leitor = await ObterLeitorAtualAsync();
                if (leitor == null)
                {
                    return null;
                }

                // Filtrar já na consulta evita expor reservas alheias.
                return await _reservaRepository.GetAll()
                    .FirstOrDefaultAsync(reserva =>
                        reserva.IdReserva == id && reserva.IdLeitor == leitor.IdLeitor);
            }

            return await _reservaRepository.GetByIdAsync(id);
        }

        private async Task<bool> LeitorConfirmadoAsync(Leitor leitor)
        {
            if (leitor.UserId == null)
            {
                // Fichas antigas sem conta podem ser tratadas pelo funcionário.
                return User.IsInRole("Funcionario");
            }

            var user = await _userHelper.GetUserByIdAsync(leitor.UserId);
            return user != null && user.Aprovado && !user.DeveAlterarPassword && user.EmailConfirmed;
        }

        private async Task<string?> ObterErroLivroAsync(int idLeitor, int idLivro)
        {
            if (await _reservaRepository.TemExemplaresLivresAsync(idLivro))
            {
                return "Este livro ainda tem exemplares livres. Não é possível reservar.";
            }

            if (await _reservaRepository.ReservaAtivaExisteAsync(idLeitor, idLivro))
            {
                return "Este leitor já tem uma reserva ativa deste livro.";
            }

            return null;
        }

        private async Task PreencherListasAsync(ReservaFormViewModel model)
        {
            if (User.IsInRole("Funcionario"))
            {
                model.Leitores = await _leitorRepository.GetAll()
                    .Where(leitor => leitor.UserId == null ||
                        (leitor.User != null && leitor.User.Aprovado &&
                         !leitor.User.DeveAlterarPassword && leitor.User.EmailConfirmed))
                    .OrderBy(leitor => leitor.Nome)
                    .ToListAsync();
            }

            model.Livros = await _livroRepository.GetAll()
                .OrderBy(livro => livro.Titulo)
                .ToListAsync();
        }

        private IActionResult NaoEncontrado()
        {
            Response.StatusCode = 404;
            return View("Erro", "A reserva indicada não foi encontrada.");
        }

        private IActionResult AcessoLeitorInvalido()
        {
            Response.StatusCode = 403;
            return View("Erro", "A conta de leitor não está confirmada ou não tem ficha de leitor associada.");
        }

        private IActionResult OperacaoInvalida(string mensagem)
        {
            Response.StatusCode = 409;
            return View("Erro", mensagem);
        }

        private IActionResult ErroDados(Exception ex)
        {
            _logger.LogError(ex, "Falha numa operação MVC de reservas.");
            Response.StatusCode = 500;
            return View("Erro", "Não foi possível concluir a operação. Consulte os dados atuais e tente novamente.");
        }
    }
}