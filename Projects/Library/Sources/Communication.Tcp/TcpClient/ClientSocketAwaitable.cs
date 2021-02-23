using System;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Mabna.Communication.Tcp.TcpClient
{
    public class ClientSocketAwaitable : INotifyCompletion
    {
        private readonly static Action _sentinel = () => { };

        private Action _continuation;

        public SocketAsyncEventArgs EventArgs
        {
            get;
            internal set;
        }

        public bool IsCompleted
        {
            get;
            internal set;
        }

        public ClientSocketAwaitable GetAwaiter() => this;

        public ClientSocketAwaitable(SocketAsyncEventArgs eventArgs)
        {
            if (eventArgs == null)
                throw new ArgumentNullException("eventArgs");
            EventArgs = eventArgs;
            EventArgs.Completed += delegate
            {
                var prev = _continuation ?? Interlocked.CompareExchange(ref _continuation, _sentinel, null);
                if (prev != null)
                    prev();
            };
        }

        public void OnCompleted(Action continuation)
        {
            if (_continuation == _sentinel || Interlocked.CompareExchange(ref _continuation, continuation, null) == _sentinel)
                Task.Run(continuation);
        }

        public void Reset()
        {
            _continuation = null;
        }

        public void GetResult()
        {
            if (EventArgs.SocketError != SocketError.Success)
                throw new SocketException((int)EventArgs.SocketError);
        }
    }
}
