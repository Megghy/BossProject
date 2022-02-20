using System;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Threading;
using Terraria;
using Terraria.Net.Sockets;

namespace BossFramework.BNet
{
    public delegate void ConnectionDisconnectHandler(AsyncClientSocket connection);
    public class AsyncClientSocket
    {
        protected byte[] recvSrcBuffer;

        protected volatile int error = 0;

        protected bool sending = false;

        protected ConcurrentQueue<SendRequest> _sendQueue = new();

        protected int recvBytes;

        public Socket Source { get; private set; }

        public AsyncServerSocket Server { get; private set; }

        public RemoteClient RemoteClient { get; private set; }

        public string IpAddress { get; private set; }

        public bool IsActive { get; private set; }

        public bool IsDataAvailable => recvBytes > 0;

        public int MaxPacketsPerOperation { get; set; } = 4096;

        public event ConnectionDisconnectHandler OnDisconnect;

        public AsyncClientSocket(AsyncServerSocket server, Socket source)
        {
            Server = server;
            Source = source;
            source.LingerState = new LingerOption(enable: true, 10);
        }

        public void StartReading()
        {
            IpAddress = Source.RemoteEndPoint.ToString();
            recvSrcBuffer = new byte[4096];
            BeginReadFromSource();
            IsActive = true;
        }

        private void BeginReadFromSource()
        {
            ReceiveArgs receiveArgs = Server.ReceiveSocketPool.PopFront();
            receiveArgs.SetBuffer(recvSrcBuffer, 0, recvSrcBuffer.Length);
            receiveArgs.origin = Source;
            receiveArgs.conn = this;
            if (!Source.ReceiveAsync(receiveArgs))
            {
                ReceiveCompleted(receiveArgs);
            }
        }

        private void HandleError(SocketError err)
        {
            if (Interlocked.CompareExchange(ref error, (int)err, 0) == 0)
            {
                Close();
            }
        }

        public void SetRemoteClient(RemoteClient remoteClient)
        {
            RemoteClient = remoteClient;
        }

        public void Close()
        {
            if (IsActive)
            {
                IsActive = false;
                try
                {
                    Source.Close();
                }
                catch (SocketException)
                {
                }
                catch (ObjectDisposedException)
                {
                }
                OnDisconnect?.Invoke(this);
            }
        }

        protected virtual void DespatchData(ReceiveArgs args)
        {
            int id = RemoteClient.Id;
            lock (NetMessage.buffer[id])
            {
                if (!RemoteClient.IsActive)
                {
                    RemoteClient.IsActive = true;
                    RemoteClient.State = 0;
                }
                Buffer.BlockCopy(args.Buffer, args.Offset, NetMessage.buffer[id].readBuffer, NetMessage.buffer[id].totalData, recvBytes);
                NetMessage.buffer[id].totalData += recvBytes;
                NetMessage.buffer[id].checkBytes = true;
                recvBytes = 0;
            }
        }

        public virtual void ReceiveCompleted(ReceiveArgs args)
        {
            try
            {
                bool flag = false;
                if (args.SocketError != 0)
                {
                    flag = true;
                    HandleError(args.SocketError);
                }
                else if (args.BytesTransferred == 0)
                {
                    flag = true;
                    HandleError(SocketError.Disconnecting);
                }
                else
                {
                    bool flag2 = false;
                    while (!flag2)
                    {
                        recvBytes += args.BytesTransferred;
                        DespatchData(args);
                        int num = args.Buffer.Length - recvBytes;
                        if (num <= 0)
                        {
                            return;
                        }
                        args.SetBuffer(args.Buffer, recvBytes, num);
                        try
                        {
                            flag2 = args.origin.ReceiveAsync(args);
                        }
                        catch (ObjectDisposedException)
                        {
                            flag2 = false;
                        }
                    }
                    if (!flag2)
                    {
                        flag = true;
                    }
                }
                if (flag)
                {
                    args.conn = null;
                    args.origin = null;
                    Server.ReceiveSocketPool.PushBack(args);
                }
                Netplay.Connection.IsReading = true;
            }
            catch (Exception value)
            {
                Console.WriteLine(value);
            }
        }

        public virtual void SendCompleted(SendArgs outgoing)
        {
            outgoing.NotifySent();
            lock (_sendQueue)
            {
                sending = SendMore(outgoing);
                if (!sending)
                {
                    outgoing.conn = null;
                    Server.SendSocketPool.PushBack(outgoing);
                }
            }
        }

        public void AsyncReceive(byte[] data, int offset, int size, SocketReceiveCallback callback, object state = null)
        {
        }

        public void AsyncSend(byte[] data, int offset, int size, SocketSendCallback callback, object state = null)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }
            if (offset < 0 || size < 0 || size > data.Length - offset)
            {
                throw new ArgumentOutOfRangeException(nameof(data));
            }
            _sendQueue.Enqueue(new SendRequest
            {
                segment = new(data, offset, size),
                callback = callback,
                state = state
            });
            lock (_sendQueue)
            {
                if (!sending)
                {
                    sending = SendMore();
                }
            }
        }

        private bool SendMore(SendArgs preallocated = null)
        {
            bool result = false;
            if (preallocated == null)
            {
                preallocated = Server.SendSocketPool.PopFront();
                preallocated.SetBuffer(null, 0, 0);
            }
            preallocated.conn = this;
            int num = 0;
            while (_sendQueue.TryDequeue(out SendRequest result2) && num++ < MaxPacketsPerOperation)
            {
                preallocated.Enqueue(result2);
            }
            if (preallocated.Prepare())
            {
                try
                {
                    result = Source.SendAsync(preallocated);
                }
                catch (SocketException ex)
                {
                    HandleError(ex.SocketErrorCode);
                }
                catch (ObjectDisposedException)
                {
                    HandleError(SocketError.OperationAborted);
                }
            }
            return result;
        }
    }
}
