package tech.spatialcomm;

import tech.spatialcomm.commands.Commands;
import tech.spatialcomm.io.IOHelpers;

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
            var code = IOHelpers.readInt16(socket.getInputStream());
            assert code == Commands.CONNECT.id;
            var length = IOHelpers.readInt32(socket.getInputStream());
            var name = IOHelpers.readUTF8String(socket.getInputStream());
            System.out.println(name);
        }
        catch (Exception ignored) {}
    }
}
