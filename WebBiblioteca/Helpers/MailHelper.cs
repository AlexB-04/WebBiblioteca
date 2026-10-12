using MimeKit;
using MailKit.Net.Smtp;
using MailKit.Security;

namespace WebBiblioteca.Helpers
{
    public class MailHelper : IMailHelper
    {
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<MailHelper> _logger;

        public MailHelper(IConfiguration configuration, IWebHostEnvironment environment, ILogger<MailHelper> logger)
        {
            _configuration = configuration;
            _environment = environment;
            _logger = logger;
        }

        public Response SendEmail(string to, string subject, string body)
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(_configuration["Mail:NameFrom"] ?? "WebBiblioteca",
                    _configuration["Mail:From"] ?? "biblioteca@example.test"));
                message.To.Add(new MailboxAddress(to, to));
                message.Subject = subject;
                message.Body = new BodyBuilder { HtmlBody = body }.ToMessageBody();

                if (_configuration.GetValue<bool>("Mail:UsePickupDirectory"))
                {
                    if (!_environment.IsDevelopment())
                    {
                        return new Response { IsSuccess = false, Message = "Configure o envio de email por SMTP." };
                    }

                    // Teste local: ficheiros privados, fora de wwwroot. Não envia emails reais.
                    string directory = Path.Combine(_environment.ContentRootPath, "App_Data", "Mail");
                    Directory.CreateDirectory(directory);
                    message.WriteTo(Path.Combine(directory, Guid.NewGuid().ToString("N") + ".eml"));
                    return new Response { IsSuccess = true };
                }

                string? smtp = _configuration["Mail:Smtp"];
                string? from = _configuration["Mail:From"];
                string? password = _configuration["Mail:Password"];
                if (string.IsNullOrWhiteSpace(smtp) || string.IsNullOrWhiteSpace(from) ||
                    string.IsNullOrWhiteSpace(password) ||
                    !int.TryParse(_configuration["Mail:Port"], out int port))
                {
                    return new Response { IsSuccess = false, Message = "O envio de email não está configurado." };
                }

                using var client = new SmtpClient();
                client.Timeout = 15000;
                client.Connect(smtp, port, port == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls);
                client.Authenticate(_configuration["Mail:Username"] ?? from, password);
                client.Send(message);
                client.Disconnect(true);
                return new Response { IsSuccess = true };
            }
            catch (Exception exception)
            {
                _logger.LogWarning("Falha no envio de email: {ErrorType}.", exception.GetType().Name);
                return new Response { IsSuccess = false, Message = "Não foi possível enviar o email. Tente novamente mais tarde." };
            }
        }
    }
}