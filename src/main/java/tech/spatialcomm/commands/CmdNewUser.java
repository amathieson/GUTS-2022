package tech.spatialcomm.commands;

import tech.spatialcomm.io.IOHelpers;

import java.io.IOException;
import java.io.InputStream;
import java.io.OutputStream;

public class CmdNewUser extends Command {

    public int userID;
    public String username;

    public CmdNewUser() {
    }

    public CmdNewUser(int userID, String username) {
        this.userID = userID;
        this.username = username;
    }

    @Override
    public Commands cmdType() {
        return Commands.NEW_USER;
    }

    @Override
    protected void readFrom(InputStream stream) throws IOException {
        this.userID = IOHelpers.readInt32(stream);
        this.username = IOHelpers.readUTF8String(stream);
    }

    @Override
    protected void writeTo(OutputStream stream) throws IOException {
        IOHelpers.writeInt32(stream, this.userID);
        IOHelpers.writeUTF8String(stream, this.username);
    }

}
