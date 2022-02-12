package tech.spatialcomm.io;

import tech.spatialcomm.server.ServerState;

import java.nio.ByteBuffer;

public class AudioData {

    public final int userID;
    public final long counter;
    public final byte[] opusData;

    public AudioData(int userID, long counter, byte[] opusData) {
        this.userID = userID;
        this.counter = counter;
        this.opusData = opusData;
    }

    public AudioData(byte[] data) {
        var buf = ByteBuffer.wrap(data);
        this.userID = buf.getInt();
        this.counter = buf.getLong();
        this.opusData = buf.array();
    }

    public byte[] toBytes() {
        var buf = ByteBuffer.allocate(opusData.length + 12);
        return buf.putInt(this.userID).putLong(this.counter).put(this.opusData).array();
    }

    public static AudioData concat(ServerState serverState, Iterable<AudioData> iterable) {

        return null; // TODO
    }

}
