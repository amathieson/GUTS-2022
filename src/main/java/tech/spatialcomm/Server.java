package tech.spatialcomm;

import tech.spatialcomm.commands.CmdByeUser;
import tech.spatialcomm.commands.CmdPing;
import tech.spatialcomm.server.ServerState;

import java.io.IOException;
import java.net.DatagramPacket;
import java.net.DatagramSocket;
import java.net.ServerSocket;
import java.net.SocketException;
import java.nio.ByteBuffer;
import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;

public class Server {

    private static final ExecutorService SERVICE = Executors.newCachedThreadPool();

    private final ServerState serverState;
    private final int portNumber;

    public Server(int portNumber) throws SocketException {
        this.serverState = new ServerState(new DatagramSocket(portNumber));
        this.portNumber = portNumber;
    }

    public void start() throws Exception {
        // TCP LOGIC
        SERVICE.submit(() -> {
            try (ServerSocket socket = new ServerSocket(this.portNumber)) {
                System.out.println("TCP Listening on: " + socket.getLocalSocketAddress());
                while (true) {
                    Connection conn = new Connection(serverState, socket.accept(), System.currentTimeMillis());
                    System.out.println("tech.spatialcomm.Connection received from " + conn.toString());
                    SERVICE.submit(() -> handleClient(conn));
                }
            } catch (Exception ex) {
                ex.printStackTrace();
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
                        socket.receive(packet);
                        byte[] body = new byte[packet.getLength()];
                        System.arraycopy(buffer, 0, body, 0, body.length);

                        // get ID
                        var buf = ByteBuffer.wrap(body, 0, 4);
                        var id = buf.getInt();
                        var connection = serverState.connections.get(id);
                        if (connection != null && connection.handshaked) {
                            connection.socketAddress = packet.getSocketAddress();
                            connection.onUDPPacketRecv(body);
                        } else {
                            System.err.println("bruh i have no idea who is sending this packet: " + id);
                        }
                    } else {
                        System.err.println("bruh why is the packet so small");
                    }
                }
            } catch (Exception ex) {
                ex.printStackTrace();
            }
        });
    }

    public void handleClient(Connection connection) {
        try {
            this.serverState.connections.put(connection.userID, connection);
            System.out.println("ID: " + connection.userID);

            SERVICE.submit(connection::recvLoop);
            while (connection.isAlive()) {
                Thread.sleep(1000L);
                // timeout after 15 second of non activity
                if (System.currentTimeMillis() - connection.lastPing > 5000L) {
                    connection.socket.close();
                    break;
                } else {
                    connection.sendCommand(new CmdPing());
                }
            }
        } catch (Exception ex) {
            ex.printStackTrace();
        } finally {
            this.serverState.connections.remove(connection.userID);
            for (var conn : this.serverState.connections.values()) {
                try {
                    conn.sendCommand(new CmdByeUser(conn.userID));
                } catch (IOException e) {
                    e.printStackTrace();
                }
            }
        }
    }

    public static void main(String[] args) throws Exception {
        new Server(25567).start();
    }

}
