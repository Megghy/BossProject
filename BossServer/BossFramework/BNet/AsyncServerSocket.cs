using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Terraria;
using Terraria.Net.Sockets;

namespace BossFramework.BNet
{
    public delegate void SocketAccepted(Socket socket);
    public class SocketServer
    {
        private readonly IPAddress _ipaddress;

        private readonly int _port;

        private TcpListener _listener;

        private volatile bool _disconnect;

        private SocketAccepted _socketAccepted;

        private readonly object _sync = new();

        public SocketServer(IPAddress ipaddress, int port)
        {
            _ipaddress = ipaddress;
            _port = port;
        }

        public void SetConnectionAcceptedCallback(SocketAccepted callback)
        {
            _socketAccepted = callback;
        }

        public bool Start()
        {
            if (_socketAccepted == null)
            {
                throw new InvalidOperationException("Please specify the accepted callback using SetConnectionAcceptedCallback before starting the server");
            }
            _listener = new TcpListener(_ipaddress, _port);
            try
            {
                _listener.Start();
            }
            catch (Exception)
            {
                Console.WriteLine($"Failed to start server on ip:port {_ipaddress}:{_port}");
                return false;
            }
            new Thread(ListenThread).Start();
            return true;
        }

        public void Stop()
        {
            _disconnect = true;
            _listener.Stop();
        }

        private void ListenThread(object state)
        {
            try
            {
                AutoResetEvent reset = new(initialState: false);
                while (!_disconnect)
                {
                    _listener.AcceptSocketAsync().ContinueWith(async task =>
                    {
                        reset.Set();
                        if ((task?.IsCompleted ?? false) && task.Result != null && task.Result.Connected)
                        {
                            Socket socket = task.Result;
                            await Task.Run(() => ClientConnect(socket));
                        }
                    });
                    reset.WaitOne();
                }
            }
            catch (Exception arg)
            {
                Console.WriteLine(string.Format("{0} terminated with exception\n{1}", "ListenThread", arg));
            }
            Netplay.IsListening = false;
            Console.WriteLine("ListenThread has exited");
        }
        void ClientConnect(Socket socket)
        {
            try
            {
                lock (_sync)
                {
                    if (!_disconnect)
                    {
                        BLog.Log($"{socket.RemoteEndPoint} 正在连接...");
                        _socketAccepted(socket);
                    }
                    else
                    {
                        socket.Close();
                    }
                }
            }
            catch (Exception value)
            {
                BLog.Warn(value);
            }
        }
    }
    public class AsyncServerSocket
    {
        private SocketServer _server;

        public AsyncArgsPool<ReceiveArgs> ReceiveSocketPool = new("ReceiveArgs");

        public AsyncArgsPool<SendArgs> SendSocketPool = new("SendArgs");

        private readonly SocketConnectionAccepted _callback;

        public AsyncServerSocket(SocketConnectionAccepted callback)
        {
            _callback = callback;
        }

        public void Stop()
        {
            _server.Stop();
            _server = null;
        }

        public bool Listen()
        {
            if (_server == null)
            {
                IPAddress address = IPAddress.Any;
                if (Program.LaunchParameters.TryGetValue("-ip", out var value) && !IPAddress.TryParse(value, out address))
                {
                    address = IPAddress.Any;
                }
                _server = new SocketServer(address, Netplay.ListenPort);
                AsyncSocket imp;
                _server.SetConnectionAcceptedCallback(delegate (Socket socket)
                {
                    try
                    {
                        imp = new AsyncSocket(this, socket);
                        _callback(imp);
                        RemoteClient remoteClient = Netplay.Clients.SingleOrDefault((RemoteClient x) => x != null && x.Socket == imp);
                        if (remoteClient != null)
                        {
                            imp.SetRemoteClient(remoteClient);
                            imp.StartReading();
                        }
                        else
                        {
                            socket.Close();
                        }
                    }
                    catch (Exception value2)
                    {
                        Console.WriteLine(value2);
                    }
                });
                return _server.Start();
            }
            return false;
        }
    }

}
