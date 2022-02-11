package tech.spatialcomm.server;

import tech.spatialcomm.Connection;

import java.util.Map;
import java.util.Random;
import java.util.concurrent.ConcurrentHashMap;

public class ServerState {

    private static final Random RANDOM = new Random();

    public final Map<Integer, Connection> connections = new ConcurrentHashMap<>();

    /**
     * add a user, returns the assigned id
     */
    public int assignUserId() {
        int id;
        do {
            id = RANDOM.nextInt(Integer.MAX_VALUE);
        } while (connections.containsKey(id));
        return id;
    }

}
