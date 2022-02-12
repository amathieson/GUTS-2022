package tech.spatialcomm.commands;

import tech.spatialcomm.io.IOHelpers;
import tech.spatialcomm.server.ServerState;

import java.io.IOException;
import java.io.InputStream;
import java.io.OutputStream;

public class CmdUserList extends Command {

    public ServerState serverState;

    public CmdUserList() {
    }

    public CmdUserList(ServerState serverState) {
        this.serverState = serverState;
    }

    @Override
    public Commands cmdType() {
        return Commands.USER_LIST;
    }

    @Override
    protected void readFrom(InputStream stream) throws IOException {
//        this.serverState = IOHelpers.readUTF8String(stream);
        throw new IOException("WRITE ONLY COMMAND");
    }

    @Override
    protected void writeTo(OutputStream stream) throws IOException {
//        IOHelpers.writeInt32(stream, this.serverState.userNames.size());
        this.serverState.userNames.forEach(((id, name) -> {
            if (this.serverState.connections.get(id) != null) {
                if (this.serverState.connections.get(id).isAlive()) {
                    try {
                        IOHelpers.writeInt32(stream, id);
                        IOHelpers.writeUTF8String(stream, name);
                    } catch (IOException e) {
                        e.printStackTrace();
                    }
                }
            }
        }));
    }

}
