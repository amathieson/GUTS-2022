package tech.spatialcomm.server;

import java.util.HashMap;
import java.util.Random;

public class ServerState {

    private static final Random RANDOM = new Random();

    public final HashMap<Integer, String> users = new HashMap<>();

    /**
     * add a user, returns the assigned id
     */
    public int addUser(String username) {
        int id;
        do {
            id = RANDOM.nextInt(Integer.MAX_VALUE);
        } while (users.containsKey(id));
        users.put(id, username);
        return id;
    }

}
