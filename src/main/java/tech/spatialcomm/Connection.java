package tech.spatialcomm;

import tech.spatialcomm.commands.*;
import tech.spatialcomm.io.IOHelpers;
import tech.spatialcomm.server.ServerState;

import java.io.ByteArrayInputStream;
import java.io.IOException;
import java.net.DatagramPacket;
import java.net.Socket;
import java.net.SocketAddress;

public class Connection {
    Socket socket;
    public final ServerState serverState;
    public final int userID;
    public long counter = Long.MIN_VALUE;
    public long lastPing;
    public String username;
    public SocketAddress socketAddress = null;

    public Connection(ServerState state, Socket socket, long lastPing) {
        this.serverState = state;
        this.socket = socket;
        this.lastPing = lastPing;
        this.userID = state.assignUserId();
        state.connections.put(this.userID, this);
        System.out.println("ID: " + userID);
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
        var b = this.socket.isConnected();
        if (!b)
            this.serverState.connections.remove(this.userID);
        return b;
    }

    public void sendCommand(Command command) throws IOException {
        Commands.writeCommand(command, socket.getOutputStream());
        socket.getOutputStream().flush();
    }

    public void onPacketRecv(Command command) throws IOException {
        if (userID == 0) {
            if (command instanceof CmdConnect cmd) {
                this.username = cmd.username;
                sendCommand(new CmdConnectOk(this.userID));
            } else {
                sendCommand(new CmdConnectFailed("CONNECT expected"));
            }
        } else if (command instanceof CmdPong) {
            this.lastPing = System.currentTimeMillis();
        }
    }

    public void onUDPPacketRecv(byte[] body) throws IOException {
        try (var bais = new ByteArrayInputStream(body)) {
            var id = IOHelpers.readInt32(bais);
            var counter = IOHelpers.readInt64(bais);
            if (this.counter >= counter) {
                // out of order packet
                return;
            }
            this.counter = counter;

            for (var entry : this.serverState.connections.entrySet()) {
                if (entry.getKey() != this.userID && entry.getValue().socketAddress != null) {
                    var packet = new DatagramPacket(body, 0, body.length);
                    packet.setSocketAddress(entry.getValue().socketAddress);
                    this.serverState.datagramSocket.send(packet);
                }
            }
        }
    }

}
