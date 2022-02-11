package tech.spatialcomm.commands;

import tech.spatialcomm.io.IOHelpers;

import java.io.IOException;
import java.io.InputStream;
import java.io.OutputStream;

public class CmdConnect extends Command {

    public String username;

    public CmdConnect() {
    }

    public CmdConnect(String username) {
        this.username = username;
    }

    @Override
    public Commands cmdType() {
        return Commands.CONNECT;
    }

    @Override
    public void readFrom(InputStream stream) throws IOException {
        this.username = IOHelpers.readUTF8String(stream);
    }

    @Override
    public void writeTo(OutputStream stream) throws IOException {
        IOHelpers.writeUTF8String(stream, this.username);
    }

}
