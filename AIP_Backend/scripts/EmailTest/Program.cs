using System;
using System.Net;
using System.Net.Mail;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace EmailTest
{
    class Program
    {
        static async Task Main(string[] args)
        {
            string toEmail = args.Length > 0 ? args[0] : "dibanga2800@gmail.com";
            
            Console.WriteLine("========================================");
            Console.WriteLine("  EMAIL TEST PROGRAM");
            Console.WriteLine("========================================");
            Console.WriteLine();
            
            string smtpHost = "mail.advantage1.co.uk";
            int smtpPort = 587;
            bool enableSsl = true;
            string smtpUsername = "noreply@advantage1.co.uk";
            string smtpPassword = "Powermyapp247!";
            string fromEmail = "noreply@advantage1.co.uk";
            string fromName = "AIP Test Email";
            
            Console.WriteLine("SMTP Configuration:");
            Console.WriteLine($"  Host: {smtpHost}");
            Console.WriteLine($"  Port: {smtpPort}");
            Console.WriteLine($"  SSL: {enableSsl}");
            Console.WriteLine($"  Username: {smtpUsername}");
            Console.WriteLine($"  From: {fromEmail}");
            Console.WriteLine($"  To: {toEmail}");
            Console.WriteLine();
            
            // Handle certificate validation (for testing - accept all certificates)
            ServicePointManager.ServerCertificateValidationCallback = 
                delegate (object s, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) 
                { 
                    return true; // Accept all certificates for testing
                };
            
            try
            {
                Console.WriteLine("Creating SMTP client...");
                
                var smtpClient = new SmtpClient
                {
                    Host = smtpHost,
                    Port = smtpPort,
                    EnableSsl = enableSsl,
                    Credentials = new NetworkCredential(smtpUsername, smtpPassword),
                    Timeout = 30000,
                    DeliveryMethod = SmtpDeliveryMethod.Network
                };
                
                Console.WriteLine("Creating test email message...");
                
                using (var mailMessage = new MailMessage
                {
                    From = new MailAddress(fromEmail, fromName),
                    Subject = "Test Email - AIP Backend SMTP Test",
                    Body = $@"
<html>
<body>
    <h2>Test Email from AIP Backend</h2>
    <p>This is a test email to verify SMTP configuration.</p>
    <p><strong>Configuration Used:</strong></p>
    <ul>
        <li>Host: {smtpHost}</li>
        <li>Port: {smtpPort}</li>
        <li>SSL Enabled: {enableSsl}</li>
        <li>From: {fromEmail}</li>
    </ul>
    <p>If you received this email, the SMTP configuration is working correctly!</p>
    <p><em>Sent at: {DateTime.Now:yyyy-MM-dd HH:mm:ss}</em></p>
</body>
</html>",
                    IsBodyHtml = true
                })
                {
                    mailMessage.To.Add(toEmail);
                    
                    Console.WriteLine("Sending test email...");
                    Console.WriteLine();
                    
                    await smtpClient.SendMailAsync(mailMessage);
                    
                    Console.WriteLine("========================================");
                    Console.WriteLine("  EMAIL SENT SUCCESSFULLY!");
                    Console.WriteLine("========================================");
                    Console.WriteLine();
                    Console.WriteLine($"Test email sent to: {toEmail}");
                    Console.WriteLine("Please check the recipient's inbox (and spam folder).");
                    Console.WriteLine();
                }
            }
            catch (SmtpException smtpEx)
            {
                Console.WriteLine("========================================");
                Console.WriteLine("  SMTP ERROR");
                Console.WriteLine("========================================");
                Console.WriteLine();
                Console.WriteLine($"Error Type: SMTP Exception");
                Console.WriteLine($"StatusCode: {smtpEx.StatusCode}");
                Console.WriteLine($"Message: {smtpEx.Message}");
                if (smtpEx.InnerException != null)
                {
                    Console.WriteLine($"Inner Exception: {smtpEx.InnerException.GetType().Name}");
                    Console.WriteLine($"Inner Message: {smtpEx.InnerException.Message}");
                }
                Console.WriteLine();
                Console.WriteLine("Common issues:");
                Console.WriteLine("  - Incorrect username/password");
                Console.WriteLine("  - Port blocked by firewall");
                Console.WriteLine("  - SSL/TLS certificate issues");
                Console.WriteLine("  - Mail server requires authentication");
                Console.WriteLine();
                Environment.Exit(1);
            }
            catch (Exception ex)
            {
                Console.WriteLine("========================================");
                Console.WriteLine("  ERROR");
                Console.WriteLine("========================================");
                Console.WriteLine();
                Console.WriteLine($"Error Type: {ex.GetType().Name}");
                Console.WriteLine($"Message: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner Exception: {ex.InnerException.GetType().Name}");
                    Console.WriteLine($"Inner Message: {ex.InnerException.Message}");
                }
                Console.WriteLine($"Stack Trace:");
                Console.WriteLine(ex.StackTrace);
                Console.WriteLine();
                Environment.Exit(1);
            }
        }
    }
}
