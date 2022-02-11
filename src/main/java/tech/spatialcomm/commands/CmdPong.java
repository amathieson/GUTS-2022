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
    public void readFrom(InputStream stream) throws IOException {
    }

    @Override
    public void writeTo(OutputStream stream) throws IOException {
    }
}
