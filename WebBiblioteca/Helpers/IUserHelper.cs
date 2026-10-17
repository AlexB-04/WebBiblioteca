using Microsoft.AspNetCore.Identity;
using WebBiblioteca.Models;

namespace WebBiblioteca.Helpers
{
    public interface IUserHelper
    {
        Task<User?> GetUserByEmailAsync(string email);

        Task<IdentityResult> AddUserAsync(User user, string password);

        Task<SignInResult> LoginAsync(LoginViewModel model);

        Task LogoutAsync();

        Task<User?> GetUserByIdAsync(string id);

        Task<IList<User>> GetFuncionariosAsync();

        Task<IdentityResult> RegisterLeitorAsync(User user, Leitor leitor);

        Task<IdentityResult> UpdateLeitorAsync(Leitor leitor, LeitorViewModel model);

        Task<IdentityResult> DeleteLeitorAsync(Leitor leitor);

        Task<IdentityResult> CreateFuncionarioAsync(User user);

        Task<IdentityResult> UpdateFuncionarioAsync(User funcionario, EditFuncionarioViewModel model);

        Task<IdentityResult> DeleteFuncionarioAsync(User funcionario);

        Task<IdentityResult> UpdateProfileAsync(User user, ProfileViewModel model, string? imageUrl);

        Task<IdentityResult> ApproveLeitorAsync(int idLeitor);

        Task<string> GeneratePasswordResetTokenAsync(User user);

        Task<IdentityResult> ResetPasswordAsync(User user, string token, string password);

        Task<IdentityResult> CheckRoleAsync(string roleName);

        Task<IdentityResult> AddUserToRoleAsync(User user, string roleName);

        Task<bool> IsUserInRoleAsync(User user, string roleName);
    }
}