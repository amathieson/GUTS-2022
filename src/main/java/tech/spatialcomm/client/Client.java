package tech.spatialcomm.client;

import tech.spatialcomm.commands.CmdPong;
import tech.spatialcomm.commands.Commands;
import tech.spatialcomm.io.IOHelpers;

import java.net.*;
import java.nio.ByteBuffer;
import java.nio.charset.StandardCharsets;
import java.util.Random;

public class Client {
    String ip;
    int tcp_port;
    final int udp_port = 25567;

    private Socket clientSocket;
    private DatagramSocket udpClientSocket;

    private int clientId;
    long counter = 0;

    public Client(String ip, int port, String name) {
        this.ip = ip;
        this.tcp_port = port;
        Socket clientSocket = null;
        try {
            // Send a connection request
            clientSocket = new Socket(ip, port);
            IOHelpers.writeInt16(clientSocket.getOutputStream(), Commands.CONNECT.id);
            String connect_str = new String("CONNECT " + name);
            IOHelpers.writeInt32(clientSocket.getOutputStream(), connect_str.getBytes(StandardCharsets.UTF_8).length);
            IOHelpers.writeUTF8String(clientSocket.getOutputStream(), name);

            // Get the clientID from the server
            assert IOHelpers.readInt16(clientSocket.getInputStream()) == Commands.CONNECT_OK.id;
            IOHelpers.readInt32(clientSocket.getInputStream());
            clientId = IOHelpers.readInt32(clientSocket.getInputStream());

            udpClientSocket = new DatagramSocket(udp_port, InetAddress.getByName(ip));
        }
        catch (Exception e) {
            e.printStackTrace();
        }
        System.out.println("Connected!");
    }

    public void listenToPing() {
        while (true) {
            try {
                if (Commands.PING.id == Commands.readCommand(clientSocket.getInputStream()).cmdType().id) {
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
            byte[] message = ByteBuffer.allocate(800).putInt(clientId).putLong(counter).put(audio).array();
            DatagramPacket dp = new DatagramPacket(message, message.length);
            udpClientSocket.send(dp);
        } catch (Exception e) {
            e.printStackTrace();
        }
    }

}
