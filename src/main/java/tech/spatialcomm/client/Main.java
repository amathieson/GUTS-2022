package tech.spatialcomm.client;

public class Main {
    public static void main(String args[]) {
        Client client = new Client("127.0.0.1", 25567, "boi");
        Thread t = new Thread(client::listenToPing);
        t.start();
        client.sendAudio();
    }
}