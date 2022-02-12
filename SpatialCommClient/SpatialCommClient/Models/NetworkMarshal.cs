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
        // State object for receiving data from remote device.  
        public class StateObject
        {
            // Client socket.  
            public Socket workSocket = null;
            // Size of receive buffer.  
            public const int BufferSize = 256;
            // Receive buffer.  
            public byte[] buffer = new byte[BufferSize];
        }

        public class Event
        {
            public delegate void EventHandler_AudioData(byte[] AudioData);
        }


        private Socket socketAudio;
        private Socket socketControl;
        // ManualResetEvent instances signal completion.  
        private static ManualResetEvent connectDone =
            new ManualResetEvent(false);
        private static ManualResetEvent sendDone =
            new ManualResetEvent(false);
        private static ManualResetEvent receiveDone =
            new ManualResetEvent(false);
        private static readonly Queue<byte> recvBuffer = new(2048);

        private static ObservableCollection<string> logger;

        public event Event.EventHandler_AudioData AudioDataRecived;

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

            byte[] buffer = new byte[1024];
            int bytesRead = 0;

            while (recvBuffer.Count < 2)
            {
                bytesRead = socketControl.Receive(buffer, SocketFlags.None);
                if (bytesRead > 0)
                    recvBuffer.EnqueueMany(buffer, bytesRead);
            }

            if (BitConverter.ToInt16(recvBuffer.DequeueMany(2).Reverse().ToArray()) != (short)NetworkPacketId.CONNECT_OK)
            {
                //Server didn't like us, just give up
                logger.Add("Failed to connect to server! Server responded:");
                logger.Add(Encoding.UTF8.GetString(recvBuffer.Skip(5).ToArray()));
                return -1;
            }


            logger.Add("Connected successfully! - UserID: " + BitConverter.ToInt32(recvBuffer.DequeueMany(4).Reverse().ToArray()));

            //Ready to start sending data
            return 1;
        }

        private static void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.  
                Socket client = (Socket)ar.AsyncState;

                // Complete the connection.  
                client.EndConnect(ar);

                System.Diagnostics.Debug.WriteLine("Socket connected to {0}",
                    client.RemoteEndPoint.ToString());

                // Signal that the connection has been made.  
                connectDone.Set();
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.ToString());
            }
        }

        private static void ReceiveAsync(Socket client)
        {
            try
            {
                // Create the state object.  
                StateObject state = new StateObject();
                state.workSocket = client;

                // Begin receiving the data from the remote device.  
                client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                    new AsyncCallback(ReceiveCallback), state);
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.ToString());
            }
        }

        private static void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the state object and the client socket
                // from the asynchronous state object.  
                StateObject state = (StateObject)ar.AsyncState;
                Socket client = state.workSocket;

                // Read data from the remote device.  
                int bytesRead = client.EndReceive(ar);

                if (bytesRead > 0)
                {
                    // There might be more data, so store the data received so far.  
                    for (int i = 0; i < bytesRead; i++)
                        recvBuffer.Enqueue(state.buffer[i]);

                    // Get the rest of the data.  
                    client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                        new AsyncCallback(ReceiveCallback), state);
                }
                else
                {
                    // All the data has arrived
                    // Signal that all bytes have been received.  
                    receiveDone.Set();
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.ToString());
            }
        }

        private static void SendAsync(Socket client, byte[] data)
        {
            // Begin sending the data to the remote device.  
            client.BeginSend(data, 0, data.Length, 0,
                new AsyncCallback(SendCallback), client);
        }

        private static void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.  
                Socket client = (Socket)ar.AsyncState;

                // Complete sending the data to the remote device.  
                int bytesSent = client.EndSend(ar);
                System.Diagnostics.Debug.WriteLine("Sent {0} bytes to server.", bytesSent);

                // Signal that all bytes have been sent.  
                sendDone.Set();
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.ToString());
            }
        }


        public void AudioListener()
        {
            while (socketAudio.Connected)
            {
                byte[] buffer = new byte[4096 * 100];
                socketControl.Receive(buffer);
                AudioDataRecived?.Invoke(buffer);
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
                    logger.Add("Recived " + buffer);
                    switch (BitConverter.ToInt16(new byte[] { buffer[1], buffer[0] }))
                    {
                        case (short)NetworkPacketId.PING:
                            logger.Add("Recived ping");
                            this.SendControlMessage(MakeMessage(NetworkPacketId.PONG, new byte[] { }));
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
                    _ = Queue.TryDequeue(out byte[] buff);
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

    }
}
