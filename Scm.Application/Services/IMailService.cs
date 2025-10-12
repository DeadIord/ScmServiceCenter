using System.Threading;
using System.Threading.Tasks;

namespace Scm.Application.Services;

public interface IMailService
{
    Task SendAsync(
        string in_to,
        string in_subject,
        string in_body,
        bool in_isHtml = false,
        CancellationToken in_cancellationToken = default);
}
