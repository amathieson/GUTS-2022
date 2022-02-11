import java.net.ServerSocket;
import java.net.Socket;
import java.nio.charset.StandardCharsets;
import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;

public class Main {

    private static final ExecutorService SERVICE = Executors.newCachedThreadPool();

    public static void main(String[] main) throws Exception {
        System.out.println("Among us");
        try (ServerSocket socket = new ServerSocket(25567)) {
            while (true) {
                var client = socket.accept();
                System.out.println("Connection received from " + client.getRemoteSocketAddress());
                SERVICE.submit(() -> handleClient(client));
            }
        }
    }

    public static void handleClient(Socket client) {
        try {
            System.out.println("handling client");
            client.getOutputStream().write("bye".getBytes(StandardCharsets.UTF_8));
            Thread.sleep(5000L);
            client.close();
        } catch (Exception ex) {
            ex.printStackTrace();
        }
    }

}
