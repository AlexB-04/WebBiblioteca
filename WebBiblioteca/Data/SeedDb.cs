using WebBiblioteca.Helpers;
using WebBiblioteca.Models;

namespace WebBiblioteca.Data
{
    public class SeedDb
    {
        private readonly IUserHelper _userHelper;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<SeedDb> _logger;

        public SeedDb(
            IUserHelper userHelper,
            IConfiguration configuration,
            IWebHostEnvironment environment,
            ILogger<SeedDb> logger)
        {
            _userHelper = userHelper;
            _configuration = configuration;
            _environment = environment;
            _logger = logger;
        }

        public async Task SeedAsync()
        {
            string[] roles = { "Admin", "Funcionario", "Leitor" };

            foreach (string role in roles)
            {
                var result = await _userHelper.CheckRoleAsync(role);

                if (!result.Succeeded)
                {
                    _logger.LogError("Não foi possível criar o perfil {Role}.", role);
                    return;
                }
            }

            // Estas contas servem apenas para testar os acessos durante o desenvolvimento.
            if (!_environment.IsDevelopment() || !_configuration.GetValue<bool>("TestUsers:Enabled"))
            {
                return;
            }

            foreach (string role in roles)
            {
                await CreateTestUserAsync(role);
            }
        }

        private async Task CreateTestUserAsync(string role)
        {
            string? email = _configuration["TestUsers:" + role + ":Email"];
            string? password = _configuration["TestUsers:" + role + ":Password"];

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                _logger.LogWarning("Configure o email e a palavra-passe de teste para {Role} nos segredos do utilizador.", role);
                return;
            }

            email = email.Trim();
            var user = await _userHelper.GetUserByEmailAsync(email);

            if (user != null)
            {
                // Uma conta já existente não recebe uma nova role nem uma nova palavra-passe.
                bool hasRole = await _userHelper.IsUserInRoleAsync(user, role);

                if (!hasRole)
                {
                    _logger.LogWarning("Já existe uma conta com o email de teste de {Role}, mas com outro perfil. Escolha outro email de teste.", role);
                }

                return;
            }

            user = new User
            {
                Aprovado = true,
                DeveAlterarPassword = false,
                FirstName = "Teste",
                LastName = role,
                Email = email,
                UserName = email
            };

            var createResult = await _userHelper.AddUserAsync(user, password);

            if (!createResult.Succeeded)
            {
                foreach (var error in createResult.Errors)
                {
                    _logger.LogWarning("Não foi possível criar a conta de teste {Role}: {Code}.", role, error.Code);
                }

                return;
            }

            var roleResult = await _userHelper.AddUserToRoleAsync(user, role);

            if (!roleResult.Succeeded)
            {
                _logger.LogError("A conta de teste {Role} foi criada, mas não foi possível atribuir o perfil.", role);
            }
        }
    }
}