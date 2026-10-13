using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;
using WebBiblioteca.Data;
using WebBiblioteca.Helpers;
using WebBiblioteca.Models;

namespace WebBiblioteca.Controllers
{
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public class AccountController : Controller
    {
        private readonly IUserHelper _userHelper;
        private readonly ILeitorRepository _leitorRepository;
        private readonly IMailHelper _mailHelper;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;

        public AccountController(IUserHelper userHelper, ILeitorRepository leitorRepository,
            IMailHelper mailHelper, IConfiguration configuration, IWebHostEnvironment environment)
        {
            _userHelper = userHelper;
            _leitorRepository = leitorRepository;
            _mailHelper = mailHelper;
            _configuration = configuration;
            _environment = environment;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register()
        {
            return View(new RegisterNewUserViewModel());
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterNewUserViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            string email = model.Email.Trim();
            var existingUser = await _userHelper.GetUserByEmailAsync(email);
            bool existingLeitor = await _leitorRepository.GetAll()
                .AnyAsync(leitor => leitor.Email != null && leitor.Email.ToUpper() == email.ToUpper());

            if (existingUser != null || existingLeitor)
            {
                ModelState.AddModelError("Email", "Este email já está registado. Contacte a biblioteca se já tem uma ficha de leitor.");
                return View(model);
            }

            var user = new User
            {
                FirstName = model.Nome.Trim(),
                LastName = model.Apelido.Trim(),
                Email = email,
                UserName = email,
                PhoneNumber = model.PhoneNumber.Trim()
            };
            var leitor = new Leitor
            {
                Nome = user.FullName,
                Contacto = user.PhoneNumber,
                Email = email,
                TipoUtilizador = model.TipoLeitor,
                // O PDF não fixa um valor: o administrador configura o limite inicial.
                LimiteEmprestimos = Math.Max(0, _configuration.GetValue<int?>("Leitores:LimiteEmprestimosInicial") ?? 3)
            };

            try
            {
                var result = await _userHelper.RegisterLeitorAsync(user, leitor);
                if (!result.Succeeded)
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    return View(model);
                }
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(string.Empty, "Não foi possível criar a conta. Verifique se o email já está registado.");
                return View(model);
            }

            // Não iniciar sessão antes da confirmação e da alteração da palavra-passe.
            return RedirectToAction("RegisterConfirmation");
        }

        [AllowAnonymous]
        public IActionResult RegisterConfirmation()
        {
            return View();
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Readers()
        {
            return View(await _leitorRepository.GetRegisteredReadersAsync());
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ApproveReader(int id)
        {
            var leitor = await _leitorRepository.GetByIdAsync(id);
            if (leitor == null || leitor.UserId == null)
            {
                return NotFound();
            }
            ViewBag.Approved = (await _userHelper.GetUserByIdAsync(leitor.UserId))?.Aprovado == true;
            return View(leitor);
        }

        [HttpPost, ActionName("ApproveReader")]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveReaderConfirmed(int id)
        {
            var leitor = await _leitorRepository.GetByIdAsync(id);
            if (leitor == null || leitor.UserId == null)
            {
                return NotFound();
            }

            try
            {
                var result = await _userHelper.ApproveLeitorAsync(id);
                if (!result.Succeeded)
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                }
                else
                {
                    leitor = await _leitorRepository.GetByIdAsync(id);
                    var user = await _userHelper.GetUserByIdAsync(leitor!.UserId!);
                    var response = await SendPasswordEmailAsync(user!, leitor.CodigoLeitor);
                    ViewBag.Message = response.IsSuccess
                        ? "Leitor confirmado. Foi preparado o email para definir a palavra-passe."
                        : "Leitor confirmado, mas o email não foi enviado. Corrija a configuração e use Reenviar email.";
                }
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(string.Empty, "Não foi possível confirmar o leitor. Atualize a página e tente novamente.");
            }

            ViewBag.Approved = (await _userHelper.GetUserByIdAsync(leitor!.UserId!))?.Aprovado == true;
            return View("ApproveReader", leitor);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Funcionarios()
        {
            return View(await _userHelper.GetFuncionariosAsync());
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult CreateFuncionario()
        {
            return View(new CreateFuncionarioViewModel());
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateFuncionario(CreateFuncionarioViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            string email = model.Email.Trim();
            var existingUser = await _userHelper.GetUserByEmailAsync(email);
            bool existingLeitor = await _leitorRepository.GetAll()
                .AnyAsync(leitor => leitor.Email != null && leitor.Email.ToUpper() == email.ToUpper());
            if (existingUser != null || existingLeitor)
            {
                ModelState.AddModelError("Email", "Este email já está registado.");
                return View(model);
            }

            var user = new User
            {
                FirstName = model.Nome.Trim(),
                LastName = model.Apelido.Trim(),
                Email = email,
                UserName = email,
                PhoneNumber = model.PhoneNumber.Trim()
            };

            try
            {
                var result = await _userHelper.CreateFuncionarioAsync(user);
                if (!result.Succeeded)
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    return View(model);
                }
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(string.Empty, "Não foi possível criar o funcionário. Verifique os dados e tente novamente.");
                return View(model);
            }

            var response = await SendPasswordEmailAsync(user, null);
            TempData["AccountMessage"] = response.IsSuccess
                ? "Funcionário criado. Foi preparado o email para definir a palavra-passe."
                : "Funcionário criado, mas o email não foi enviado. Corrija a configuração e use Reenviar email.";
            return RedirectToAction("Funcionarios");
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> EditFuncionario(string? id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }

            var funcionario = await _userHelper.GetUserByIdAsync(id);
            if (funcionario == null ||
                !await _userHelper.IsUserInRoleAsync(funcionario, "Funcionario") ||
                await _userHelper.IsUserInRoleAsync(funcionario, "Admin") ||
                await _userHelper.IsUserInRoleAsync(funcionario, "Leitor"))
            {
                return NotFound();
            }

            var model = new EditFuncionarioViewModel
            {
                Id = funcionario.Id,
                Nome = funcionario.FirstName ?? string.Empty,
                Apelido = funcionario.LastName ?? string.Empty,
                Email = funcionario.Email ?? string.Empty,
                PhoneNumber = funcionario.PhoneNumber ?? string.Empty
            };

            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditFuncionario([FromRoute] string? id, EditFuncionarioViewModel model)
        {
            if (string.IsNullOrWhiteSpace(id) || id != model.Id)
            {
                return NotFound();
            }

            var funcionario = await _userHelper.GetUserByIdAsync(id);
            if (funcionario == null ||
                !await _userHelper.IsUserInRoleAsync(funcionario, "Funcionario") ||
                await _userHelper.IsUserInRoleAsync(funcionario, "Admin") ||
                await _userHelper.IsUserInRoleAsync(funcionario, "Leitor"))
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            string email = model.Email.Trim();
            var outroUser = await _userHelper.GetUserByEmailAsync(email);
            bool emailDeLeitor = await _leitorRepository.GetAll()
                .AnyAsync(leitor => leitor.Email != null && leitor.Email.ToUpper() == email.ToUpper());

            if ((outroUser != null && outroUser.Id != funcionario.Id) || emailDeLeitor)
            {
                ModelState.AddModelError("Email", "Este email já está registado.");
                return View(model);
            }

            bool emailAlterado = !string.Equals(funcionario.Email, email, StringComparison.OrdinalIgnoreCase);

            try
            {
                var result = await _userHelper.UpdateFuncionarioAsync(funcionario, model);
                if (!result.Succeeded)
                {
                    foreach (var error in result.Errors)
                    {
                        if (error.Code == "DuplicateEmail" || error.Code == "DuplicateUserName")
                        {
                            ModelState.AddModelError("Email", "Este email já está registado.");
                        }
                        else if (error.Code == "InvalidEmail" || error.Code == "InvalidUserName")
                        {
                            ModelState.AddModelError("Email", "Este email não pode ser utilizado para iniciar sessão.");
                        }
                        else if (error.Code == "ConcurrencyFailure")
                        {
                            ModelState.AddModelError(string.Empty, "Os dados foram alterados por outro pedido. Volte à lista e abra novamente o funcionário.");
                        }
                        else
                        {
                            ModelState.AddModelError(string.Empty, "Não foi possível alterar o funcionário. Verifique os dados e tente novamente.");
                        }
                    }
                    return View(model);
                }
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(string.Empty, "Não foi possível guardar as alterações. Verifique o email e tente novamente.");
                return View(model);
            }

            if (emailAlterado)
            {
                var response = await SendPasswordEmailAsync(funcionario, null);
                TempData["AccountMessage"] = response.IsSuccess
                    ? "Funcionário alterado. Foi preparado um email para definir a palavra-passe no novo endereço antes de voltar a iniciar sessão."
                    : "Funcionário alterado, mas o email não foi enviado. Corrija a configuração e use Reenviar email para ativar o novo endereço.";
            }
            else
            {
                TempData["AccountMessage"] = "Funcionário alterado com sucesso.";
            }

            return RedirectToAction("Funcionarios");
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendInitialPassword(string id)
        {
            var user = await _userHelper.GetUserByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            bool funcionario = await _userHelper.IsUserInRoleAsync(user, "Funcionario");
            bool leitorRole = await _userHelper.IsUserInRoleAsync(user, "Leitor");
            if (!funcionario && !leitorRole)
            {
                return Forbid();
            }

            if (!user.Aprovado || !user.DeveAlterarPassword)
            {
                TempData["AccountMessage"] = "O reenvio inicial só se aplica a contas confirmadas que ainda não definiram a palavra-passe.";
            }
            else
            {
                var leitor = await _leitorRepository.GetByUserIdAsync(user.Id);
                var response = await SendPasswordEmailAsync(user, leitor?.CodigoLeitor);
                TempData["AccountMessage"] = response.IsSuccess
                    ? "Email preparado novamente."
                    : response.Message;
            }
            return RedirectToAction(funcionario ? "Funcionarios" : "Readers");
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult RecoverPassword()
        {
            return View(new RecoverPasswordViewModel());
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RecoverPassword(RecoverPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userHelper.GetUserByEmailAsync(model.Email.Trim());
            if (user != null && user.Aprovado)
            {
                var leitor = await _leitorRepository.GetByUserIdAsync(user.Id);
                await SendPasswordEmailAsync(user, leitor?.CodigoLeitor);
            }

            // A mesma resposta evita divulgar se um email tem conta ou está por aprovar.
            ViewBag.Message = "Se existir uma conta confirmada com esse email, receberá instruções. Verifique também o spam. Se não receber, contacte a biblioteca.";
            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPassword(string? email, string? token)
        {
            return View(new ResetPasswordViewModel
            {
                Email = email ?? string.Empty,
                Token = token ?? string.Empty
            });
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userHelper.GetUserByEmailAsync(model.Email.Trim());
            if (user == null || !user.Aprovado)
            {
                ModelState.AddModelError(string.Empty, "Não foi possível alterar a palavra-passe. Verifique o link e a confirmação da conta.");
                return View(model);
            }

            try
            {
                var result = await _userHelper.ResetPasswordAsync(user, model.Token, model.Password);
                if (!result.Succeeded)
                {
                    ModelState.AddModelError(string.Empty, "Não foi possível alterar a palavra-passe. O link pode ter expirado ou já ter sido usado. Use pelo menos 6 caracteres, maiúscula, minúscula, número e símbolo.");
                    return View(model);
                }
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(string.Empty, "Não foi possível guardar a palavra-passe. Tente novamente.");
                return View(model);
            }

            // O token deixa de funcionar após a alteração. O login é um passo separado.
            await _userHelper.LogoutAsync();
            TempData["AccountMessage"] = "Palavra-passe definida. Já pode iniciar sessão.";
            return RedirectToAction("Login");
        }

        private async Task<Helpers.Response> SendPasswordEmailAsync(User user, string? codigoLeitor)
        {
            string? baseUrl = _configuration["Mail:PublicBaseUrl"];
            if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var address) ||
                (address.Scheme != "https" && !(_environment.IsDevelopment() && address.Scheme == "http")))
            {
                return new Helpers.Response { IsSuccess = false, Message = "Configure Mail:PublicBaseUrl com o endereço HTTPS da aplicação." };
            }

            var token = await _userHelper.GeneratePasswordResetTokenAsync(user);
            var path = Url.Action("ResetPassword", "Account", new { email = user.Email, token });
            var link = baseUrl!.TrimEnd('/') + path;
            string codeText = codigoLeitor == null ? string.Empty : "<p>Código de leitor: " + WebUtility.HtmlEncode(codigoLeitor) + "</p>";
            return _mailHelper.SendEmail(user.Email!, "WebBiblioteca - definir palavra-passe",
                "<h1>WebBiblioteca</h1>" + codeText +
                "<p>Para definir a palavra-passe, abra o seguinte link. O link é temporário e só pode ser utilizado uma vez.</p>" +
                "<p><a href=\"" + WebUtility.HtmlEncode(link) + "\">Definir palavra-passe</a></p>" +
                "<p>Se não pediu esta operação, ignore esta mensagem.</p>");
        }

        [AllowAnonymous]
        public IActionResult Login(string? returnUrl)
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewBag.ReturnUrl = returnUrl;
            return View(new LoginViewModel());
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            if (ModelState.IsValid)
            {
                var result = await _userHelper.LoginAsync(model);
                if (result.Succeeded)
                {
                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    {
                        return Redirect(returnUrl);
                    }
                    return RedirectToAction("Index", "Home");
                }

                if (result.IsNotAllowed)
                {
                    return RedirectToAction("ResetPassword", new { email = model.Username.Trim() });
                }

                if (result.IsLockedOut)
                {
                    ModelState.AddModelError(string.Empty, "Demasiadas tentativas. Tente novamente dentro de alguns minutos.");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Não foi possível iniciar sessão. Verifique o email e a palavra-passe.");
                }
            }
            return View(model);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _userHelper.LogoutAsync();
            return RedirectToAction("Index", "Home");
        }

        [AllowAnonymous]
        public IActionResult NotAuthorized()
        {
            Response.StatusCode = StatusCodes.Status403Forbidden;
            return View();
        }
    }
}
