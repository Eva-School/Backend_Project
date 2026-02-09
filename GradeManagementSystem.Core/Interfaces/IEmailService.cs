using System.Threading.Tasks;

namespace GradeManagementSystem.Core.Interfaces
{
    public interface IEmailService
    {
        Task SendEmailAsync(string to, string subject, string body);
    }
}
