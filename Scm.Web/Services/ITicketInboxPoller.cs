using System.Threading;
using System.Threading.Tasks;

namespace Scm.Web.Services;

public interface ITicketInboxPoller
{
    Task<TicketInboxPollResult> PollAsync(CancellationToken in_cancellationToken);
}
