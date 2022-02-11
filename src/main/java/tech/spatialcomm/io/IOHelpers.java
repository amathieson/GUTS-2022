package tech.spatialcomm.io;

import tech.spatialcomm.commands.Commands;

import java.io.IOException;
import java.io.InputStream;
import java.io.OutputStream;
import java.nio.ByteBuffer;
import java.nio.charset.StandardCharsets;

public class IOHelpers {

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
        var buf = ByteBuffer.allocate(2);
        buf.put(is.readNBytes(2));
        return buf.getShort();
    }

    public static Commands readCommandType(InputStream is) throws IOException {
        var i = readInt16(is);
        for (var cmd : Commands.values()) {
            if (cmd.id == i) return cmd;
        }
        throw new IOException("malformed commands");
    }

    public static int readInt32(InputStream is) throws IOException {
        var buf = ByteBuffer.allocate(4);
        buf.put(is.readNBytes(4));
        return buf.getInt();
    }

    public static long readInt64(InputStream is) throws IOException {
        var buf = ByteBuffer.allocate(8);
        buf.put(is.readNBytes(8));
        return buf.getLong();
    }

    public static String readUTF8String(InputStream is) throws IOException {
        var len = readInt32(is);
        var string = is.readNBytes(len);
        return new String(string, StandardCharsets.UTF_8);
    }

}
