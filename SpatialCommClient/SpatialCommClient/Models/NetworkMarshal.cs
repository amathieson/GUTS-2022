using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Collections.ObjectModel;
using System.Collections.Concurrent;

namespace SpatialCommClient.Models
{
    public class NetworkMarshal : IDisposable
    {
        public delegate void EventHandler_AudioData(Span<byte> AudioData);

        private Socket socketAudio;
        private Socket socketControl;
        // ManualResetEvent instances signal completion.  
        private static ManualResetEvent connectDone =
            new ManualResetEvent(false);
        private static ManualResetEvent sendDone =
            new ManualResetEvent(false);
        private static ManualResetEvent receiveDone =
            new ManualResetEvent(false);

        private static ObservableCollection<string> logger;

        public event EventHandler_AudioData AudioDataRecived;

        //TODO: To avoid unnecessary memory copies we might want to use Span<byte> which allows data like this to be efficiently passed around.
        private ConcurrentQueue<byte[]> ControlMessageQueue = new ConcurrentQueue<byte[]>();
        private ConcurrentQueue<byte[]> AudioMessageQueue = new ConcurrentQueue<byte[]>();


        public NetworkMarshal(ObservableCollection<string> myLogger)
        {
            socketAudio = new Socket(SocketType.Dgram, ProtocolType.Udp);
            socketControl = new Socket(SocketType.Stream, ProtocolType.Tcp);

            logger = myLogger;
        }

        public void Dispose()
        {
            socketAudio.Close();
            socketAudio.Dispose();
            socketControl.Close();
            socketControl.Dispose();
        }

        private byte[] StringToBytes(string str)
        {
            return BitConverter.GetBytes(str.Length).Reverse().Concat(Encoding.UTF8.GetBytes(str)).ToArray();
        }

        private byte[] MakeMessage(NetworkPacketId id, byte[] data)
        {
            return BitConverter.GetBytes((short)id).Reverse().Concat(data).ToArray();
        }

        public int ConnectToServer(string host, int portControl, int portAudio, string username)
        {
            socketControl.Connect(Dns.GetHostEntry(host).AddressList[0], portControl);
            socketAudio.Connect(Dns.GetHostEntry(host).AddressList[0], portAudio);
            socketControl.Send(MakeMessage(NetworkPacketId.CONNECT, StringToBytes(username)));

            //Wait for reply
            //ReceiveAsync(socketControl);
            //receiveDone.WaitOne();

            Span<byte> buffer = new byte[1024];
            int bytesRead = 0;

            //This is a TCP message we can relatively sure it will arrive in it's entireity
            bytesRead = socketControl.Receive(buffer, SocketFlags.None);

            if (BitConverter.ToInt16(buffer[0..2].ReverseSpan()) != (short)NetworkPacketId.CONNECT_OK)
            {
                //Server didn't like us, just give up
                logger.Add("Failed to connect to server! Server responded:");
                logger.Add(Encoding.UTF8.GetString(buffer[7..].ToArray()));
                return -1;
            }

            logger.Add("Connected successfully! - UserID: " + BitConverter.ToUInt32(buffer.Slice(2, 4).ReverseSpan().ToArray()));

            //Ready to start sending data
            return 1;
        }

        public void AudioListener()
        {
            while (socketAudio.Connected)
            {
                Span<byte> buffer = new byte[4096 * 100];
                int recv = socketAudio.Receive(buffer);
                System.Diagnostics.Debug.WriteLine(recv);
                AudioDataRecived?.Invoke(buffer.Slice(0, recv));
            }
        }

        public void ControlListener()
        {
            while (socketControl.Connected)
            {
                byte[] buffer = new byte[2];
                int len = socketControl.Receive(buffer, 2, SocketFlags.None);

                if (len > 0)
                {
                    switch (BitConverter.ToInt16(new byte[] { buffer[1], buffer[0] }))
                    {
                        case (short)NetworkPacketId.PING:
                            logger.Add("Received ping!");
                            SendControlMessage(MakeMessage(NetworkPacketId.PONG, new byte[] { }));
                            break;
                        case (short)NetworkPacketId.NEW_USER:
                            logger.Add("Received NEW_USER!");
                            break;
                        case (short)NetworkPacketId.BYE_USER:
                            logger.Add("Received BYE_USER!");
                            break;
                        case (short)NetworkPacketId.USER_LIST:
                            logger.Add("Received USER_LIST!");
                            ReadUserList(socketControl);
                            break;

                    }
                }

            }
        }

        public void SocketEmitter(bool isControl)
        {
            Socket socket = (isControl ? socketControl : socketAudio);
            ConcurrentQueue<byte[]> Queue = (isControl ? ControlMessageQueue : AudioMessageQueue);
            while (socket.Connected)
            {
                if (Queue.Count > 0)
                {
                    if(Queue.TryDequeue(out byte[] buff))
                        if (buff != null)
                            socket.Send(buff);
                }
            }
        }


        public void SendAudioData(byte[] data)
        {
            AudioMessageQueue.Enqueue(data);
        }

        public void SendControlMessage(byte[] data)
        {
            ControlMessageQueue.Enqueue(data);
        }


        private void ReadUserList(Socket SocketControl)
        {
            Span<byte> buffer = new byte[4096];
            int len = socketControl.Receive(buffer, SocketFlags.None);
            if (len > 0)
            {
                int baseAddr = 0;
                int userID = -1;
                while (userID!=0) {
                    userID = BitConverter.ToInt32(buffer.Slice(baseAddr + 0, 4).ReverseSpan().ToArray());
                    if (userID == 0)
                        break;
                    int strlength = BitConverter.ToInt32(buffer.Slice(baseAddr + 4, 4).ReverseSpan().ToArray());
                    string username = Encoding.UTF8.GetString(buffer.Slice(baseAddr + 8, strlength).ToArray());
                    logger.Add(userID + " - " + username);

                    baseAddr += strlength + 8;
                }
            }
        }

    }
}
