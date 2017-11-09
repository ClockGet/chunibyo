using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace WebApplication1.Services
{
    public class EmailService
    {
        private MailConfig _mailConfig;
        public EmailService(MailConfig mailConfig)
        {
            _mailConfig = mailConfig;
        }
        public async Task SendAsync(string destination, string body, string subject)
        {
            var mail = new MailMessage(
                new MailAddress(_mailConfig.EmailAddress, _mailConfig.EmailUserName),
                new MailAddress(destination)
                );
            mail.Subject = subject;
            mail.Body = body;
            mail.IsBodyHtml = true;
            mail.BodyEncoding = Encoding.UTF8;

            var smtp = new SmtpClient(_mailConfig.SmtpServer, _mailConfig.SmtpPort);
            smtp.EnableSsl = true;
            smtp.UseDefaultCredentials = false;
            smtp.Credentials = new System.Net.NetworkCredential(_mailConfig.EmailAddress, _mailConfig.EmailPwd);
            smtp.DeliveryMethod = SmtpDeliveryMethod.Network;

            await smtp.SendMailAsync(mail);
        }
    }
}
