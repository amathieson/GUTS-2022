package tech.spatialcomm.commands;

import tech.spatialcomm.io.IOHelpers;

import java.io.IOException;
import java.io.InputStream;
import java.io.OutputStream;

public class CmdConnectFailed extends Command {

    public String reason;

    public CmdConnectFailed() {}

    public CmdConnectFailed(String username) {
        this.reason = username;
    }

    @Override
    public Commands cmdType() {
        return Commands.CONNECT_FAILED;
    }

    @Override
    protected void readFrom(InputStream stream) throws IOException {
        this.reason = IOHelpers.readUTF8String(stream);
    }

    @Override
    protected void writeTo(OutputStream stream) throws IOException {
        IOHelpers.writeUTF8String(stream, this.reason);
    }

}
