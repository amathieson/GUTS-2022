package tech.spatialcomm.commands;

import tech.spatialcomm.io.IOHelpers;

import java.io.IOException;
import java.io.InputStream;
import java.io.OutputStream;

public class CmdConnectOk extends Command {

    public int userID;

    public CmdConnectOk(int userID) {
        this.userID = userID;
    }

    @Override
    public Commands cmdType() {
        return Commands.CONNECT_OK;
    }

    @Override
    public void readFrom(InputStream stream) throws IOException {
        this.userID = IOHelpers.readInt32(stream);
    }

    @Override
    public void writeTo(OutputStream stream) throws IOException {
        IOHelpers.writeInt32(stream, this.userID);
    }

}
