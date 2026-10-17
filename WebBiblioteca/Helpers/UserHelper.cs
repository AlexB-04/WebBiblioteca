using Microsoft.AspNetCore.Identity;
using WebBiblioteca.Models;
using WebBiblioteca.Data;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

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

        public async Task<IdentityResult> UpdateLeitorAsync(Leitor leitor, LeitorViewModel model)
        {
            model.TemConta = leitor.UserId != null;
            var result = await ValidateLeitorAsync(model, leitor.IdLeitor, leitor.UserId);
            if (!result.Succeeded)
            {
                return result;
            }

            int livrosEmprestados = await _leitorRepository.ContarLivrosEmprestadosAsync(leitor.IdLeitor);
            if (model.LimiteEmprestimos < livrosEmprestados)
            {
                return IdentityResult.Failed(new IdentityError
                {
                    Code = "LimiteEmprestimos",
                    Description = $"O leitor tem {livrosEmprestados} livro(s) por devolver. O limite não pode ser inferior a esse número."
                });
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();
            string email = model.Email.Trim();
            string contacto = model.Contacto.Trim();
            string nome = model.Nome.Trim();

            if (leitor.UserId != null)
            {
                var user = await _userManager.FindByIdAsync(leitor.UserId);
                if (user == null || !await _userManager.IsInRoleAsync(user, "Leitor") ||
                    await _userManager.IsInRoleAsync(user, "Admin") ||
                    await _userManager.IsInRoleAsync(user, "Funcionario"))
                {
                    return IdentityResult.Failed(new IdentityError { Description = "A conta associada não é uma conta de leitor válida." });
                }

                bool emailAlterado = !string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase);
                user.FirstName = nome;
                user.LastName = model.Apelido!.Trim();
                if (user.PhoneNumber != contacto)
                {
                    user.PhoneNumberConfirmed = false;
                }
                user.PhoneNumber = contacto;
                user.Email = email;
                user.UserName = email;
                if (emailAlterado)
                {
                    user.Aprovado = false;
                    user.DeveAlterarPassword = true;
                    user.EmailConfirmed = false;
                }

                result = await _userManager.UpdateAsync(user);
                if (!result.Succeeded)
                {
                    return result;
                }
                if (emailAlterado)
                {
                    result = await _userManager.UpdateSecurityStampAsync(user);
                    if (!result.Succeeded)
                    {
                        return result;
                    }
                }
                nome = user.FullName;
            }

            leitor.Nome = nome;
            leitor.Contacto = contacto;
            leitor.Email = email;
            leitor.TipoUtilizador = model.TipoUtilizador;
            leitor.LimiteEmprestimos = model.LimiteEmprestimos;
            await _leitorRepository.UpdateDadosAsync(leitor);
            await transaction.CommitAsync();
            return IdentityResult.Success;
        }

        public async Task<IdentityResult> DeleteLeitorAsync(Leitor leitor)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            if (await _leitorRepository.ContarLivrosEmprestadosAsync(leitor.IdLeitor) > 0)
            {
                return IdentityResult.Failed(new IdentityError { Description = "Não é possível eliminar um leitor com empréstimos ativos. Registe primeiro as devoluções." });
            }
            if (await _leitorRepository.TemHistoricoAsync(leitor.IdLeitor))
            {
                return IdentityResult.Failed(new IdentityError { Description = "Não é possível eliminar este leitor porque tem histórico de empréstimos ou reservas. O histórico deve ser conservado." });
            }

            User? user = null;
            if (leitor.UserId != null)
            {
                user = await _userManager.FindByIdAsync(leitor.UserId);
                if (user == null || !await _userManager.IsInRoleAsync(user, "Leitor") ||
                    await _userManager.IsInRoleAsync(user, "Admin") ||
                    await _userManager.IsInRoleAsync(user, "Funcionario"))
                {
                    return IdentityResult.Failed(new IdentityError { Description = "A conta associada não é uma conta de leitor válida." });
                }
            }

            // Retirar primeiro a ficha; a relação com Identity não permite apagar em cascata.
            await _leitorRepository.DeleteAsync(leitor);
            if (user != null)
            {
                var result = await _userManager.DeleteAsync(user);
                if (!result.Succeeded)
                {
                    return result;
                }
            }
            await transaction.CommitAsync();
            return IdentityResult.Success;
        }

        private async Task<IdentityResult> ValidateLeitorAsync(LeitorViewModel model, int id, string? userId)
        {
            var errors = new List<ValidationResult>();
            Validator.TryValidateObject(model, new ValidationContext(model), errors, true);
            if (model.TemConta)
            {
                if (string.IsNullOrWhiteSpace(model.Apelido))
                {
                    errors.Add(new ValidationResult("O apelido é obrigatório para uma conta de leitor."));
                }
                if (model.Nome?.Trim().Length > 50 ||
                    $"{model.Nome?.Trim()} {model.Apelido?.Trim()}".Length > 100)
                {
                    errors.Add(new ValidationResult("O nome pode ter até 50 caracteres e o nome completo até 100 caracteres."));
                }
            }
            if (errors.Count > 0)
            {
                return IdentityResult.Failed(errors.Select(error => new IdentityError
                {
                    Description = error.ErrorMessage ?? "Verifique os dados do leitor."
                }).ToArray());
            }

            string email = model.Email.Trim();
            bool emailDeLeitor = await _leitorRepository.GetAll().AnyAsync(leitor =>
                leitor.IdLeitor != id && leitor.Email != null && leitor.Email.ToUpper() == email.ToUpper());
            var otherUser = await _userManager.FindByEmailAsync(email);
            if (emailDeLeitor || (otherUser != null && otherUser.Id != userId))
            {
                return IdentityResult.Failed(new IdentityError { Code = "DuplicateEmail", Description = "Este email já está registado." });
            }
            return IdentityResult.Success;
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

            leitor.CodigoLeitor ??= "L" + leitor.IdLeitor.ToString("D6");
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