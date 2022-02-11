package tech.spatialcomm;

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
        try (ServerSocket socket = new ServerSocket(25567)) {
            while (true) {
                Connection conn = new Connection(socket.accept());
                System.out.println("tech.spatialcomm.Connection received from " + conn.toString());
                SERVICE.submit(() -> handleClient(conn));
            }
        }
    }

    public static void handleClient(Connection connection) {
        try {
            /*
            System.out.println("handling client");
            client.getOutputStream().write("bye".getBytes(StandardCharsets.UTF_8));
            Thread.sleep(5000L);
            client.close(); */
        } catch (Exception ex) {
            ex.printStackTrace();
        }
    }

}
