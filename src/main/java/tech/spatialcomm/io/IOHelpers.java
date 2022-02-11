package tech.spatialcomm.io;

import tech.spatialcomm.commands.Commands;

import java.io.IOException;
import java.io.InputStream;
import java.io.OutputStream;
import java.nio.ByteBuffer;
import java.nio.charset.StandardCharsets;

public class IOHelpers {

    public static short int16FromByteArray(byte[] bytes) {
        return (short) (((bytes[0] & 0xFF) << 8) | ((bytes[1] & 0xFF) << 0));
    }

    public static int int32FromByteArray(byte[] bytes) {
        return ((bytes[0] & 0xFF) << 24) |
                ((bytes[1] & 0xFF) << 16) |
                ((bytes[2] & 0xFF) << 8 ) |
                ((bytes[3] & 0xFF));
    }

    public static long int64FromByteArray(byte[] buf) {
        return  ((buf[0] & 0xFFL) << 56) |
                ((buf[1] & 0xFFL) << 48) |
                ((buf[2] & 0xFFL) << 40) |
                ((buf[3] & 0xFFL) << 32) |
                ((buf[4] & 0xFFL) << 24) |
                ((buf[5] & 0xFFL) << 16) |
                ((buf[6] & 0xFFL) <<  8) |
                ((buf[7] & 0xFFL) <<  0) ;
    }

    public static void writeInt16(OutputStream os, short i) throws IOException {
        var buf = ByteBuffer.allocate(2);
        buf.putShort(i);
        os.write(buf.array());
    }

    public static void writeCommandType(OutputStream os, Commands commands) throws IOException {
        writeInt16(os, commands.id);
    }

    public static void writeInt32(OutputStream os, int i) throws IOException {
        var buf = ByteBuffer.allocate(4);
        buf.putInt(i);
        os.write(buf.array());
    }

    public static void writeInt64(OutputStream os, long i) throws IOException {
        var buf = ByteBuffer.allocate(8);
        buf.putLong(i);
        os.write(buf.array());
    }

    public static void writeUTF8String(OutputStream os, String string) throws IOException {
        var bytes = string.getBytes(StandardCharsets.UTF_8);
        writeInt32(os, bytes.length);
        os.write(bytes);
    }

    public static short readInt16(InputStream is) throws IOException {
        return int16FromByteArray(is.readNBytes(2));
    }

    public static Commands readCommandType(InputStream is) throws IOException {
        var i = readInt16(is);
        for (var cmd : Commands.values()) {
            if (cmd.id == i) return cmd;
        }
        throw new IOException("malformed commands");
    }

    public static int readInt32(InputStream is) throws IOException {
        return int32FromByteArray(is.readNBytes(4));
    }

    public static long readInt64(InputStream is) throws IOException {
        return int64FromByteArray(is.readNBytes(8));
    }

    public static String readUTF8String(InputStream is) throws IOException {
        var len = readInt32(is);
        var string = is.readNBytes(len);
        return new String(string, StandardCharsets.UTF_8);
    }

}
