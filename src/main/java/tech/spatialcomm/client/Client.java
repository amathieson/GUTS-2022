package tech.spatialcomm.client;

import tech.spatialcomm.commands.*;
import tech.spatialcomm.io.IOHelpers;

import java.io.IOException;
import java.net.*;
import java.nio.ByteBuffer;
import java.nio.charset.StandardCharsets;
import java.util.Base64;
import java.util.Random;

public class Client {
    String ip;
    int tcp_port;

    private Socket clientSocket;
    private DatagramSocket udpClientSocket;
    private final SocketAddress udpServerAddr;

    private int clientId;
    long counter = 0;

    public Client(String ip, int port, String name) throws IOException {
        this.ip = ip;
        this.tcp_port = port;
        this.udpServerAddr = new InetSocketAddress(ip, port);
        Socket clientSocket = new Socket(ip, port);
        Commands.writeCommand(new CmdConnect(name), clientSocket.getOutputStream());

        // Get the clientID from the server
        clientId = -1;
        do {
            var cmd = Commands.readCommand(clientSocket.getInputStream());
            if (cmd instanceof CmdConnectOk cmdConnectOk) {
                clientId = cmdConnectOk.userID;
            } else if (cmd instanceof CmdConnectFailed cmdConnectFailed) {
                System.err.println(cmdConnectFailed.reason);
                System.exit(-1);
            }
        } while (clientId < 0);

        udpClientSocket = new DatagramSocket();
        System.out.println("Connected! ID: " + this.clientId);
    }

    public void listenToPing() {
        while (true) {
            try {
                var cmd = Commands.readCommand(clientSocket.getInputStream());
                if (cmd instanceof CmdPing) {
                    Commands.writeCommand(new CmdPong(), clientSocket.getOutputStream());
                }
                Thread.sleep(100);
            }
            catch (Exception ignored) {}
        }
    }

    public void sendAudio() {
        try {
            byte[] audio = new byte[400];
            Random rand = new Random();
            rand.nextBytes(audio);
            byte[] message = ByteBuffer.allocate(audio.length + 4 + 8).putInt(clientId).putLong(counter).put(audio).array();
            System.out.println("Sending: " + Base64.getEncoder().encodeToString(message));
            counter++;
            // System.out.println(clientId);
            DatagramPacket dp = new DatagramPacket(message, message.length);
            dp.setSocketAddress(this.udpServerAddr);
            udpClientSocket.send(dp);
        } catch (Exception e) {
            e.printStackTrace();
        }
    }

    public void recvAudio() {
        try {
            byte[] audio = new byte[400];
            DatagramPacket dp = new DatagramPacket(audio, audio.length);
            this.udpClientSocket.receive(dp);

            byte[] body = new byte[dp.getLength()];
            System.arraycopy(audio, 0, body, 0, body.length);
            // get ID
            var buf = ByteBuffer.wrap(body, 0, 4);
            var id = buf.getInt();
            System.out.println(id + ": " + Base64.getEncoder().encodeToString(audio));
            // System.out.println(clientId);
        } catch (Exception e) {
            e.printStackTrace();
        }
    }

}
