package tech.spatialcomm;

import tech.spatialcomm.server.ServerState;

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
        try (ServerSocket socket = new ServerSocket(25567)) {
            while (true) {
                Connection conn = new Connection(serverState, socket.accept(), System.currentTimeMillis());
                System.out.println("tech.spatialcomm.Connection received from " + conn.toString());
                SERVICE.submit(() -> handleClient(conn));
            }
        }
    }

    public static void handleClient(Connection connection) {
        try {
            connection.initializeUser();
            while (!connection.isAlive()) {
                Thread.sleep(5000L);
                connection.ping();
            }
        } catch (Exception ex) {
            ex.printStackTrace();
        }
    }

}
