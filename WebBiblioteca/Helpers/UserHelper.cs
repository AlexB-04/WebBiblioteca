using Microsoft.AspNetCore.Identity;
using WebBiblioteca.Models;
using WebBiblioteca.Data;
using Microsoft.EntityFrameworkCore;

namespace WebBiblioteca.Helpers
{
    public class UserHelper : IUserHelper
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly DataContext _context;
        private readonly ILeitorRepository _leitorRepository;

        public UserHelper(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            RoleManager<IdentityRole> roleManager,
            DataContext context,
            ILeitorRepository leitorRepository)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _context = context;
            _leitorRepository = leitorRepository;
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            return await _userManager.FindByEmailAsync(email);
        }

        public async Task<IdentityResult> AddUserAsync(User user, string password)
        {
            return await _userManager.CreateAsync(user, password);
        }

        public async Task<SignInResult> LoginAsync(LoginViewModel model)
        {
            var user = await GetUserByEmailAsync(model.Username.Trim());

            if (user == null)
            {
                return SignInResult.Failed;
            }

            if (!user.Aprovado || user.DeveAlterarPassword)
            {
                return SignInResult.NotAllowed;
            }

            return await _signInManager.PasswordSignInAsync(
                user,
                model.Password,
                model.RememberMe,
                lockoutOnFailure: true);
        }

        public async Task<IList<User>> GetFuncionariosAsync()
        {
            return await _userManager.GetUsersInRoleAsync("Funcionario");
        }

        public async Task<User?> GetUserByIdAsync(string id)
        {
            return await _userManager.FindByIdAsync(id);
        }

        public async Task<IdentityResult> RegisterLeitorAsync(User user, Leitor leitor)
        {
            user.Aprovado = false;
            user.DeveAlterarPassword = true;
            user.EmailConfirmed = false;
            return await CreateAccountAsync(user, leitor, "Leitor");
        }

        public async Task<IdentityResult> CreateFuncionarioAsync(User user)
        {
            user.Aprovado = true;
            user.DeveAlterarPassword = true;
            user.EmailConfirmed = false;
            return await CreateAccountAsync(user, null, "Funcionario");
        }

        private async Task<IdentityResult> CreateAccountAsync(User user, Leitor? leitor, string role)
        {
            // User, role e Leitor pertencem à mesma operação na mesma base de dados.
            await using var transaction = await _context.Database.BeginTransactionAsync();

            // Palavra-passe provisória aleatória, nunca enviada nem apresentada.
            // O utilizador define a sua própria palavra-passe pelo link recebido.
            var result = await _userManager.CreateAsync(user, Guid.NewGuid().ToString("N") + "aA1!");
            if (!result.Succeeded)
            {
                return result;
            }

            result = await _userManager.AddToRoleAsync(user, role);
            if (!result.Succeeded)
            {
                return result;
            }

            if (leitor != null)
            {
                leitor.UserId = user.Id;
                await _leitorRepository.CreateWithUserAsync(leitor);
            }

            await transaction.CommitAsync();
            return IdentityResult.Success;
        }

        public async Task<IdentityResult> ApproveLeitorAsync(int idLeitor)
        {
            var leitor = await _leitorRepository.GetByIdAsync(idLeitor);
            if (leitor == null || leitor.UserId == null)
            {
                return IdentityResult.Failed(new IdentityError { Description = "O leitor não tem uma conta associada." });
            }

            var user = await _userManager.FindByIdAsync(leitor.UserId);
            if (user == null || !await _userManager.IsInRoleAsync(user, "Leitor"))
            {
                return IdentityResult.Failed(new IdentityError { Description = "Conta de leitor inválida." });
            }

            if (user.Aprovado)
            {
                return IdentityResult.Failed(new IdentityError { Description = "Este leitor já foi confirmado." });
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();
            user.Aprovado = true;
            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                return result;
            }

            leitor.CodigoLeitor = "L" + leitor.IdLeitor.ToString("D6");
            await _leitorRepository.UpdateAsync(leitor);
            await transaction.CommitAsync();
            return IdentityResult.Success;
        }

        public async Task<string> GeneratePasswordResetTokenAsync(User user)
        {
            return await _userManager.GeneratePasswordResetTokenAsync(user);
        }

        public async Task<IdentityResult> ResetPasswordAsync(User user, string token, string password)
        {
            if (!user.Aprovado)
            {
                return IdentityResult.Failed(new IdentityError { Description = "Aguarde a confirmação do administrador." });
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();
            var result = await _userManager.ResetPasswordAsync(user, token, password);
            if (!result.Succeeded)
            {
                return result;
            }

            // A posse do link enviado por email confirma o endereço.
            user.EmailConfirmed = true;
            user.DeveAlterarPassword = false;
            result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                return result;
            }

            await transaction.CommitAsync();
            return IdentityResult.Success;
        }

        public async Task LogoutAsync()
        {
            await _signInManager.SignOutAsync();
        }

        public async Task<IdentityResult> CheckRoleAsync(string roleName)
        {
            bool roleExists = await _roleManager.RoleExistsAsync(roleName);

            if (roleExists)
            {
                return IdentityResult.Success;
            }

            return await _roleManager.CreateAsync(new IdentityRole
            {
                Name = roleName
            });
        }

        public async Task<IdentityResult> AddUserToRoleAsync(User user, string roleName)
        {
            return await _userManager.AddToRoleAsync(user, roleName);
        }

        public async Task<bool> IsUserInRoleAsync(User user, string roleName)
        {
            return await _userManager.IsInRoleAsync(user, roleName);
        }
    }
}