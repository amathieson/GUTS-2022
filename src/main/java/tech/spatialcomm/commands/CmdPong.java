package tech.spatialcomm.commands;

import java.io.IOException;
import java.io.InputStream;
import java.io.OutputStream;

public class CmdPong extends Command {

    @Override
    public Commands cmdType() {
        return Commands.PONG;
    }

    @Override
    protected void readFrom(InputStream stream) throws IOException {
    }

    @Override
    protected void writeTo(OutputStream stream) throws IOException {
    }
}
