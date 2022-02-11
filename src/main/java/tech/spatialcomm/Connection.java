package tech.spatialcomm;

import java.net.Socket;

public class Connection {
    Socket socket;
    public int username;
    public int counter;

    public Connection(Socket socket) {
        this.socket = socket;
    }

    public String toString() {
        return socket.getRemoteSocketAddress().toString();
    }

    public void initializeUser() {
        try {
            var code = socket.getInputStream();


        }
        catch (Exception ignored) {}
    }
}
