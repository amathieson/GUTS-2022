package tech.spatialcomm;

import tech.spatialcomm.commands.*;
import tech.spatialcomm.io.IOHelpers;
import tech.spatialcomm.server.ServerState;

import java.io.IOException;
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

    public void recvLoop() {
        try {
            while (this.isAlive()) {
                var cmd = Commands.readCommand(this.socket.getInputStream());
                this.onPacketRecv(cmd);
            }
        } catch (IOException e) {
            e.printStackTrace();
        }
    }

    public boolean isAlive() {
        return this.socket.isConnected();
    }

    public void sendCommand(Command command) throws IOException {
        Commands.writeCommand(command, socket.getOutputStream());
        socket.getOutputStream().flush();
    }

    public void onPacketRecv(Command command) throws IOException {
        if (userID == 0) {
            if (command instanceof CmdConnect cmd) {
                var username = cmd.username;
                this.userID = this.serverState.addUser(username);
                sendCommand(new CmdConnectOk(this.userID));
            } else {
                sendCommand(new CmdConnectFailed("CONNECT expected"));
            }
        } else if (command instanceof CmdPong) {
            this.lastPing = System.currentTimeMillis();
        }
    }

}
