package tech.spatialcomm.commands;

public enum Commands {
    CONNECT(0x00),
    CONNECT_OK(0x01),
    CONNECT_FAILED(0x02),
    PING(0x03),
    PONG(0x04);

    public final short id;

    Commands(short id) {
        // java moment
        this.id = id;
    }
}
