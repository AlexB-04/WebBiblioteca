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

        public async Task<IdentityResult> UpdateFuncionarioAsync(User funcionario, EditFuncionarioViewModel model)
        {
            if (!await _userManager.IsInRoleAsync(funcionario, "Funcionario") ||
                await _userManager.IsInRoleAsync(funcionario, "Admin") ||
                await _userManager.IsInRoleAsync(funcionario, "Leitor"))
            {
                return IdentityResult.Failed(new IdentityError
                {
                    Code = "InvalidFuncionario",
                    Description = "Esta conta não pode ser alterada na gestão de funcionários."
                });
            }

            string email = model.Email.Trim();
            string contacto = model.PhoneNumber.Trim();
            bool emailAlterado = !string.Equals(funcionario.Email, email, StringComparison.OrdinalIgnoreCase);

            // Guardar os dados e invalidar os links antigos na mesma operação.
            await using var transaction = await _context.Database.BeginTransactionAsync();

            funcionario.FirstName = model.Nome.Trim();
            funcionario.LastName = model.Apelido.Trim();
            funcionario.Email = email;
            funcionario.UserName = email;

            if (funcionario.PhoneNumber != contacto)
            {
                funcionario.PhoneNumberConfirmed = false;
            }
            funcionario.PhoneNumber = contacto;

            if (emailAlterado)
            {
                funcionario.EmailConfirmed = false;
                funcionario.DeveAlterarPassword = true;
            }

            // Identity também atualiza os valores normalizados usados no login.
            var result = await _userManager.UpdateAsync(funcionario);
            if (!result.Succeeded)
            {
                return result;
            }

            if (emailAlterado)
            {
                // Um link emitido antes da mudança não deve ativar o novo email.
                result = await _userManager.UpdateSecurityStampAsync(funcionario);
                if (!result.Succeeded)
                {
                    return result;
                }
            }

            await transaction.CommitAsync();
            return IdentityResult.Success;
        }

        public async Task<IdentityResult> DeleteFuncionarioAsync(User funcionario)
        {
            if (!await _userManager.IsInRoleAsync(funcionario, "Funcionario") ||
                await _userManager.IsInRoleAsync(funcionario, "Admin") ||
                await _userManager.IsInRoleAsync(funcionario, "Leitor"))
            {
                return IdentityResult.Failed(new IdentityError
                {
                    Code = "InvalidFuncionario",
                    Description = "Esta conta não pode ser eliminada na gestão de funcionários."
                });
            }

            if (await _context.Leitores.AnyAsync(leitor => leitor.UserId == funcionario.Id))
            {
                return IdentityResult.Failed(new IdentityError
                {
                    Code = "LeitorAssociado",
                    Description = "Não é possível eliminar este funcionário porque tem uma ficha de leitor associada."
                });
            }

            // A conta é eliminada através do Identity, sem apagar dados da biblioteca.
            return await _userManager.DeleteAsync(funcionario);
        }

        public async Task<IdentityResult> UpdateProfileAsync(User user, ProfileViewModel model, string? imageUrl)
        {
            var leitor = await _leitorRepository.GetByUserIdAsync(user.Id);
            if (leitor == null && await _userManager.IsInRoleAsync(user, "Leitor"))
            {
                return IdentityResult.Failed(new IdentityError
                {
                    Description = "A ficha de leitor não foi encontrada. Contacte a biblioteca."
                });
            }

            string nome = model.Nome.Trim();
            string apelido = model.Apelido.Trim();
            string contacto = model.PhoneNumber.Trim();
            if (leitor != null && $"{nome} {apelido}".Length > 100)
            {
                return IdentityResult.Failed(new IdentityError
                {
                    Description = "O nome completo do leitor pode conter no máximo 100 caracteres."
                });
            }

            // O perfil e a ficha de leitor são guardados juntos.
            await using var transaction = await _context.Database.BeginTransactionAsync();
            user.FirstName = nome;
            user.LastName = apelido;
            if (user.PhoneNumber != contacto)
            {
                user.PhoneNumberConfirmed = false;
            }
            user.PhoneNumber = contacto;
            user.ImageUrl = imageUrl;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                return result;
            }

            if (leitor != null)
            {
                leitor.Nome = user.FullName;
                leitor.Contacto = contacto;
                await _leitorRepository.UpdateProfileAsync(leitor);
            }

            await transaction.CommitAsync();
            return IdentityResult.Success;
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
