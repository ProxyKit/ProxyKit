using System.Threading;
using System.Threading.Tasks;

namespace ProxyKit.Examples
{
    public abstract class ExampleBase
    {
        public void Run(CancellationToken cancellationToken)
        {
            Task.Run(() => RunAsync(cancellationToken)).GetAwaiter().GetResult();
        }

        protected abstract Task RunAsync(CancellationToken cancellationToken);
    }
}