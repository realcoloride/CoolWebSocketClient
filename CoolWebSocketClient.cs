using System;
using System.Buffers;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

#nullable disable
namespace CoolWebSocketClient
{
    public enum CoolWebSocketMessageType
    {
        Text,
        Binary,
        Close
    }

    public delegate void CoolWebSocketOpenEvent();
    public delegate void CoolWebSocketErrorEvent(WebSocketError errorCode, string errorMessage);
    public delegate void CoolWebSocketCloseEvent(WebSocketCloseStatus closeStatus, string closeMessage);
    public delegate void CoolWebSocketMessageEvent(CoolWebSocketMessageType messageType, byte[] message);

    public sealed class CoolWebSocket : IDisposable
    {
        private readonly ClientWebSocket WebSocket = new();

        public WebSocketState State => WebSocket.State;
        public ClientWebSocketOptions Options => WebSocket.Options;
        public string SubProtocol => WebSocket.SubProtocol;

        public Uri Uri { get; private set; }

        private readonly CancellationTokenSource CancellationTokenSource = new();
        private CancellationToken CancellationToken => CancellationTokenSource.Token;

        public bool IsOpen => State == WebSocketState.Open || State == WebSocketState.Connecting;

        #region Events

        public event CoolWebSocketOpenEvent OnOpen;
        public event CoolWebSocketErrorEvent OnError;
        public event CoolWebSocketCloseEvent OnClose;
        public event CoolWebSocketMessageEvent OnMessage;

        private void ThrowIfCloseError()
        {
            if (IsOpen || !WebSocket.CloseStatus.HasValue) return;
            OnClose?.Invoke(WebSocket.CloseStatus.Value, WebSocket.CloseStatusDescription);
        }

        #endregion

        #region Connection

        private Thread Thread;
        public async Task Open(Uri uri)
        {
            if (IsOpen) return;

            Uri = uri;

            try
            {
                await WebSocket.ConnectAsync(uri, CancellationToken);

                Thread = new(new ThreadStart(async () =>
                {
                    while (IsOpen) await Poll();
                })) { Name = "CoolWebSocketClientThread" };
                Thread.Start();

                OnOpen?.Invoke();
            }
            catch (WebSocketException exception)
            {
                OnError?.Invoke(exception.WebSocketErrorCode, exception.Message);
                ThrowIfCloseError();
            }
            catch (Exception exception)
            {
                OnError?.Invoke(WebSocketError.Faulted, exception.Message);
                ThrowIfCloseError();
            }
        }

        public async Task Close(
            WebSocketCloseStatus closeStatus = WebSocketCloseStatus.NormalClosure,
            string closeMessage = null
        ) {
            if (IsOpen) return;

            try
            {
                await WebSocket.CloseAsync(closeStatus, closeMessage, CancellationToken);
            }
            catch (WebSocketException exception)
            {
                OnError?.Invoke(exception.WebSocketErrorCode, exception.Message);
                ThrowIfCloseError();
                return;
            }
            catch (Exception exception)
            {
                OnError?.Invoke(WebSocketError.Faulted, exception.Message);
                ThrowIfCloseError();
                return;
            }
            finally
            {
                CancellationTokenSource.Cancel();
            }

            OnClose?.Invoke(closeStatus, closeMessage);
        }

        #endregion

        #region Sending

        private async Task Send(dynamic data, WebSocketMessageType messageType = WebSocketMessageType.Binary)
        {
            if (!IsOpen) return;

            try
            {
                await WebSocket.SendAsync(data, messageType, true, CancellationToken);
            }
            catch (WebSocketException exception)
            {
                OnError?.Invoke(exception.WebSocketErrorCode, exception.Message);
                ThrowIfCloseError();
            }
            catch (Exception ex)
            {
                OnError?.Invoke(WebSocketError.Faulted, ex.Message);
                ThrowIfCloseError();
            }
        }

        public async Task Send(ReadOnlyMemory<byte> data) => await Send(data);
        public async Task Send(ArraySegment<byte> data) => await Send(data);

        public async Task Send(string text)
            => await Send(Encoding.UTF8.GetBytes(text), WebSocketMessageType.Text);

        #endregion

        #region Receiving

        private const int PollBufferSize = 16384;
        private readonly MemoryStream MemoryStream = new();

        private async Task Poll()
        {
            // keep writing until eom
            WebSocketReceiveResult result;
            do
            {
                // rent a temporary buffer
                byte[] buffer = ArrayPool<byte>.Shared.Rent(PollBufferSize);

                try
                {
                    result = await WebSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken);

                    MemoryStream.Write(buffer, 0, result.Count);

                    if (result.EndOfMessage)
                    {
                        OnMessage?.Invoke((CoolWebSocketMessageType)result.MessageType, MemoryStream.ToArray());
                        MemoryStream.SetLength(0);
                    }
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                }
            } while (!result.EndOfMessage);
        }

        public string ReadString(ArraySegment<byte> message) => Encoding.UTF8.GetString(message);

        #endregion

        public void Dispose()
        {
            CancellationTokenSource?.Cancel();
            WebSocket?.Dispose();
            CancellationTokenSource?.Dispose();
            MemoryStream?.Dispose();
        }
    }
}
