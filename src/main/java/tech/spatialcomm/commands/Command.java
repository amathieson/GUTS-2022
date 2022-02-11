package tech.spatialcomm.commands;

import java.io.IOException;
import java.io.InputStream;
import java.io.OutputStream;

public abstract class Command {

    public abstract Commands cmdType();
    public abstract void readFrom(InputStream stream) throws IOException;
    public abstract void writeTo(OutputStream stream) throws IOException;

}
