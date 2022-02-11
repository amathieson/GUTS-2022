package tech.spatialcomm;

import tech.spatialcomm.commands.Commands;
import tech.spatialcomm.io.IOHelpers;
import tech.spatialcomm.server.ServerState;

import java.io.IOException;
import java.io.InputStream;
import java.net.Socket;

public class Connection {
    Socket socket;
    public final ServerState serverState;
    public int userID;
    public int counter;
    public long lastPing;

    public Connection(ServerState state, Socket socket, long lastPing) {
        this.serverState = state;
        this.socket = socket;
        this.lastPing = lastPing;
    }

    public String toString() {
        return socket.getRemoteSocketAddress().toString();
    }

    public void initializeUser() {
        try {
            var is = socket.getInputStream();
            var os = socket.getOutputStream();
            var code = IOHelpers.readCommandType(is);
            if (code == Commands.CONNECT) {
                var length = IOHelpers.readInt32(is);
                var name = IOHelpers.readUTF8String(is);
                this.userID = this.serverState.addUser(name);
                System.out.println(name + " " + this.userID);
            } else {
                IOHelpers.writeCommandType(os, Commands.CONNECT_FAILED);
                IOHelpers.writeUTF8String(os, "CONNECT expected");
                socket.close();
            }
        }
        catch (Exception ignored) {}
    }

    public void ping() throws IOException {
        IOHelpers.writeCommandType(socket.getOutputStream(), Commands.PING);
    }

    public boolean isAlive() {
        return this.socket.isConnected();
    }

    public void onPacketRecv(Commands command, InputStream is) {

    }

}
