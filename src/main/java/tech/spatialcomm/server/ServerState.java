package tech.spatialcomm.server;

import tech.spatialcomm.Connection;

import java.net.DatagramSocket;
import java.util.Map;
import java.util.Random;
import java.util.concurrent.ConcurrentHashMap;

public class ServerState {

    private static final Random RANDOM = new Random();

    public final Map<Integer, Connection> connections = new ConcurrentHashMap<>();
    public final DatagramSocket datagramSocket;

    public ServerState(DatagramSocket datagramSocket) {
        this.datagramSocket = datagramSocket;
    }

    /**
     * add a user, returns the assigned id
     */
    public int assignUserId() {
        return connections.size();
    }

}
