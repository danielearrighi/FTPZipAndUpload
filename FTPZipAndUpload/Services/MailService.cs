using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using FTPZipAndUpload.Infrastructure;

namespace FTPZipAndUpload.Services
{
    public static class MailService
    {
        private static string _mailServer    = ConfigurationManager.AppSettings["MailServer"];
        private static string _mailUser      = ConfigurationManager.AppSettings["MailUser"];
        private static string _mailPassword  = ConfigurationManager.AppSettings["MailPassword"];
        private static string _mailRecipient = ConfigurationManager.AppSettings["MailRecipient"];

        public static bool Send(string subject, string body, bool verbose = false)
        {
            try
            {
                _mailPassword = Security.DecryptText(_mailPassword);

                SmtpClient smtpClient = new SmtpClient();
                MailMessage message = new MailMessage();

                smtpClient.UseDefaultCredentials = false;
                smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;
                smtpClient.Port = 25;
                smtpClient.Host = _mailServer;
                smtpClient.Credentials = new System.Net.NetworkCredential(_mailUser, _mailPassword);

                MailAddress fromAddress = new MailAddress(_mailUser);
                message.From = fromAddress;
                message.To.Add(_mailRecipient);
                message.Subject = subject;

                message.IsBodyHtml = false;
                message.Body = body;

                smtpClient.Send(message);
                message.Dispose();

                return true;
            }
            catch (Exception e)
            {
                if (verbose) Console.WriteLine();
                if (verbose) Console.WriteLine("Error in uploading. Check Logs.");
                Utilities.WriteToFile(string.Format("Upload Exception: {0}\n", e));

                return false;
            }
        }
    }
}
