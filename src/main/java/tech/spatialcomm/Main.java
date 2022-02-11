package tech.spatialcomm;

import tech.spatialcomm.commands.CmdConnectOk;
import tech.spatialcomm.commands.CmdPing;
import tech.spatialcomm.server.ServerState;

import java.net.DatagramPacket;
import java.net.DatagramSocket;
import java.net.ServerSocket;
import java.nio.ByteBuffer;
import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;

public class Main {

    private static final ExecutorService SERVICE = Executors.newCachedThreadPool();

    public static void main(String[] main) throws Exception {
        ServerState serverState = new ServerState(new DatagramSocket(25567));
        // TCP LOGIC
        SERVICE.submit(() -> {
            try (ServerSocket socket = new ServerSocket(25567)) {
                System.out.println("TCP Listening on: " + socket.getLocalSocketAddress());
                while (true) {
                    Connection conn = new Connection(serverState, socket.accept(), System.currentTimeMillis());
                    System.out.println("tech.spatialcomm.Connection received from " + conn.toString());
                    SERVICE.submit(() -> handleClient(conn));
                }
            }
        });
        // UDP LOGIC
        SERVICE.submit(() -> {
            try (DatagramSocket socket = serverState.datagramSocket) {
                System.out.println("UDP Listening on: " + socket.getLocalSocketAddress());
                byte[] buffer = new byte[65536];
                while (true) {
                    var packet = new DatagramPacket(buffer, 0, buffer.length);
                    if (packet.getLength() >= 8) {
                        byte[] body = new byte[packet.getLength()];
                        socket.receive(packet);
                        System.arraycopy(packet, 0, body, 0, body.length);

                        // get ID
                        var buf = ByteBuffer.wrap(body, 0, 4);
                        var id = buf.getInt();
                        var connection = serverState.connections.get(id);
                        if (connection != null) {
                            connection.socketAddress = packet.getSocketAddress();
                            connection.onUDPPacketRecv(body);
                        } else {
                            System.err.println("bruh i have no idea who is sending this packet: " + id);
                        }
                    } else {
                        System.err.println("bruh why is the packet so small");
                    }
                }
            }
        });
    }

    public static void handleClient(Connection connection) {
        try {
            SERVICE.submit(connection::recvLoop);
            connection.sendCommand(new CmdConnectOk(connection.userID));
            while (connection.isAlive()) {
                Thread.sleep(5000L);
                // timeout after 7 second of non activity
                if (System.currentTimeMillis() - connection.lastPing > 7000L) {
                    connection.socket.close();
                    break;
                } else {
                    connection.sendCommand(new CmdPing());
                }
            }
        } catch (Exception ex) {
            ex.printStackTrace();
        }
    }

}
