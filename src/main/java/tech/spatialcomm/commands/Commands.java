package tech.spatialcomm.commands;

import tech.spatialcomm.io.IOHelpers;

import java.io.IOException;
import java.io.InputStream;
import java.util.*;

public enum Commands {
    CONNECT(0x00),
    CONNECT_OK(0x01),
    CONNECT_FAILED(0x02),
    PING(0x03),
    PONG(0x04);

    public final short id;

    Commands(int id) {
        // java moment
        this.id = (short) id;
    }

    public static final Map<Commands, Class<? extends Command>> REGISTRY = new HashMap<>();

    static {
        REGISTRY.put(CONNECT, CmdConnect.class);
        REGISTRY.put(CONNECT_OK, CmdConnectOk.class);
        REGISTRY.put(CONNECT_FAILED, CmdConnectFailed.class);
        REGISTRY.put(PING, CmdPing.class);
        REGISTRY.put(PONG, CmdPong.class);
    }

    public static Command readCommand(InputStream is) throws IOException {
        var cmd = IOHelpers.readCommandType(is);
    }

}
