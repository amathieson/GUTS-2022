package tech.spatialcomm.commands;

import java.io.IOException;
import java.io.InputStream;
import java.io.OutputStream;

public abstract class Command {

    public abstract Commands cmdType();

    protected abstract void readFrom(InputStream stream) throws IOException;

    protected abstract void writeTo(OutputStream stream) throws IOException;

}
