using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Collections.ObjectModel;

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
            return BitConverter.GetBytes(str.Length).Concat(Encoding.UTF8.GetBytes(str)).ToArray();
        }

        private byte[] MakeMessage(NetworkPacketId id, byte[] data)
        {
            return BitConverter.GetBytes((short)id).Concat(data).ToArray();
        }

        public int ConnectToServer(string host, int portControl, int portAudio, string username)
        {
            socketControl.BeginConnect(Dns.GetHostEntry(host).AddressList[0], portControl, new AsyncCallback(ConnectCallback), socketControl);
            connectDone.WaitOne();
            socketAudio.BeginConnect(Dns.GetHostEntry(host).AddressList[0], portAudio, new AsyncCallback(ConnectCallback), socketAudio);
            connectDone.WaitOne();
            SendAsync(socketControl, MakeMessage(NetworkPacketId.CONNECT, StringToBytes(username)));
            sendDone.WaitOne();
            //Wait for reply
            ReceiveAsync(socketControl);
            receiveDone.WaitOne();

            if(BitConverter.ToInt16(recvBuffer.DequeueMany(2).Reverse().ToArray()) != (short)NetworkPacketId.CONNECT_OK)
            {
                //Server didn't like us, just give up
                logger.Add("Failed to connect to server! Server responded:");
                logger.Add(Encoding.UTF8.GetString(recvBuffer.Skip(5).ToArray()));
                return -1;
            }

            logger.Add("Connected successfully!");

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
                    for(int i = 0; i < bytesRead; i++)
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
    }
}
