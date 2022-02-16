using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Terraria;
using Terraria.Net;
using Terraria.Net.Sockets;

namespace BossFramework.BNet
{
	public sealed class AsyncSocket : ISocket
	{
		private AsyncServerSocket _server;

		private readonly AsyncClientSocket _client;

		private readonly RemoteAddress _remoteAddress;

		public AsyncSocket()
		{
		}

		public AsyncSocket(AsyncServerSocket server, Socket socket)
		{
			_client = new AsyncClientSocket(server, socket);
			IPEndPoint iPEndPoint = (IPEndPoint)socket.RemoteEndPoint;
			_remoteAddress = new TcpAddress(iPEndPoint.Address, iPEndPoint.Port);
		}

		public void SetRemoteClient(RemoteClient remoteClient)
		{
			_client.SetRemoteClient(remoteClient);
		}

		public void AsyncReceive(byte[] data, int offset, int size, SocketReceiveCallback callback, object state = null)
		{
			_client?.AsyncReceive(data, offset, size, callback, state);
		}

		public void AsyncSend(byte[] data, int offset, int size, SocketSendCallback callback, object state = null)
		{
			_client?.AsyncSend(data, offset, size, callback, state);
		}

		public void Close()
		{
			_client?.Close();
		}

		public void Connect(RemoteAddress address)
		{
			throw new NotImplementedException();
		}

		public RemoteAddress GetRemoteAddress()
		{
			return _remoteAddress;
		}

		public bool IsConnected()
		{
			return _client?.IsActive ?? false;
		}

		public bool IsDataAvailable()
		{
			return _client?.IsDataAvailable ?? false;
		}

		public void SendQueuedPackets()
		{
		}

		public bool StartListening(SocketConnectionAccepted callback)
		{
			if (_server == null)
			{
				_server = new AsyncServerSocket(callback);
			}
			return _server.Listen();
		}

		public void StopListening()
		{
			_server.Stop();
		}

		public void StartReading()
		{
			_client?.StartReading();
		}
	}
	public class AsyncSocketEventArgs : SocketAsyncEventArgs
	{
		public volatile AsyncClientSocket conn;

		protected override void OnCompleted(SocketAsyncEventArgs e)
		{
		}
	}
	public struct SendArgCallback
	{
		public SocketSendCallback callback;

		public object state;
	}
	public struct SendRequest
	{
		public ArraySegment<byte> segment;

		public SocketSendCallback callback;

		public object state;
	}
	public class ReceiveArgs : AsyncSocketEventArgs
	{
		public volatile Socket origin;

		protected override void OnCompleted(SocketAsyncEventArgs e)
		{
			conn?.ReceiveCompleted(this);
		}
	}
	public class SendArgs : AsyncSocketEventArgs
	{
		private readonly Queue<SendArgCallback> callbacks = new();

		private readonly List<ArraySegment<byte>> segments = new();

		protected override void OnCompleted(SocketAsyncEventArgs e)
		{
			conn?.SendCompleted(this);
		}

		public void Enqueue(SendRequest request)
		{
			callbacks.Enqueue(new SendArgCallback
			{
				callback = request.callback,
				state = request.state
			});
			segments.Add(request.segment);
		}

		public bool Prepare()
		{
			if (segments.Count > 0)
			{
				BufferList = segments;
				return true;
			}
			return false;
		}

		internal void NotifySent()
		{
			while (callbacks.Count > 0)
			{
				SendArgCallback sendArgCallback = callbacks.Dequeue();
				sendArgCallback.callback(sendArgCallback.state);
			}
			BufferList.Clear();
			BufferList = null;
		}
	}
	public class AsyncArgsPool<TSocketAsyncEventArgs> where TSocketAsyncEventArgs : AsyncSocketEventArgs, new()
	{
		private readonly ConcurrentStack<TSocketAsyncEventArgs> _pool = new();

		public string Prefix { get; set; }

		public AsyncArgsPool(string prefix)
		{
			Prefix = prefix;
		}

		public TSocketAsyncEventArgs PopFront()
		{
			if (!_pool.TryPop(out var result))
			{
				return new TSocketAsyncEventArgs();
			}
			return result;
		}

		public void PushBack(TSocketAsyncEventArgs args)
		{
			if (args.conn != null)
			{
				throw new InvalidOperationException("Cannot push in a non released socket. Please reset conn");
			}
			_pool.Push(args);
		}
	}
}