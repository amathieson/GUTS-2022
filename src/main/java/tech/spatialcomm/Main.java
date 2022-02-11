package tech.spatialcomm;

import tech.spatialcomm.commands.CmdPing;
import tech.spatialcomm.server.ServerState;

import javax.xml.crypto.Data;
import java.net.DatagramSocket;
import java.net.ServerSocket;
import java.net.Socket;
import java.nio.charset.StandardCharsets;
import java.util.ArrayList;
import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;

public class Main {

    private static final ExecutorService SERVICE = Executors.newCachedThreadPool();

    public static void main(String[] main) throws Exception {
        ArrayList<Connection> connections = new ArrayList<>();
        ServerState serverState = new ServerState();
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
            try (DatagramSocket socket = new DatagramSocket(25567)) {
                System.out.println("UDP Listening on: " + socket.getLocalSocketAddress());
                while (true) {
                }
            }
        });
    }

    public static void handleClient(Connection connection) {
        try {
            SERVICE.submit(connection::recvLoop);
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
