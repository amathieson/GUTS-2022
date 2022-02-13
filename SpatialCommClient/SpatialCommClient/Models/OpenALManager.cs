using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using OpenTK.Audio.OpenAL;
using OpenTK.Mathematics;

namespace SpatialCommClient.Models
{
    class OpenALManager : IDisposable
    {
        private ALCaptureDevice captureDevice;
        private ALDevice outputDevice;
        private ALContext alContext;
        private byte[] buffer;

        //TODO: Make this private once the connected users list is stored in the ViewModel or NetworkMarshal
        /// <summary>
        /// Dictionary mapping server userIDs to OpenAL source IDs
        /// </summary>
        public Dictionary<int, AudioSourceState> Users2Source { get { return users2Sources; } }
        private Dictionary<int, AudioSourceState> users2Sources = new();

        public OpenALManager()
        {
            
        }

        public void Dispose()
        {
            if (captureDevice.Handle != IntPtr.Zero) 
                ALC.CaptureCloseDevice(captureDevice);
            if (outputDevice.Handle != IntPtr.Zero) 
                ALC.CloseDevice(outputDevice);
        }

        /// <summary>
        /// Lists all the available audio input devices
        /// </summary>
        /// <returns></returns>
        public string[] ListInputDevices()
        {
            return ALC.GetString(AlcGetStringList.CaptureDeviceSpecifier).ToArray();
        }

        /// <summary>
        /// Lists all the available audio output devices
        /// </summary>
        /// <returns></returns>
        public string[] ListOutputDevices()
        {
            return ALC.GetString(AlcGetStringList.AllDevicesSpecifier).ToArray();
        }

        /// <summary>
        /// Works out how big the buffer needs to be (in samples) given the sampling frequency and the length of the buffer in milliseconds
        /// </summary>
        /// <param name="freq">sampling frequency</param>
        /// <param name="timeMs">buffer length in milliseconds</param>
        /// <returns>size of the buffer in samples</returns>
        public static int ComputeBufferSize(int freq, int timeMs)
        {
            return timeMs * freq / 1000;
        }

        /// <summary>
        /// Opens a named audio input device and begins capturing audio
        /// </summary>
        /// <param name="device">device name to open</param>
        /// <param name="freq">sampling frequency</param>
        /// <param name="buffSize">sampling buffer size in samples</param>
        public void OpenCaptureDevice(string device, int freq, int buffSize)
        {
            captureDevice = ALC.CaptureOpenDevice(device, freq, ALFormat.Mono16, buffSize);
            ALC.CaptureStart(captureDevice);
            buffer = new byte[buffSize*16];//We always run in 4-bytes per sample hence the array is multiplied by 2
        }

        /// <summary>
        /// Opens a named audio output device
        /// </summary>
        /// <param name="device">device name to open</param>
        public void OpenDevice(string device)
        {
            outputDevice = ALC.OpenDevice(device);
            var attribs = new ALContextAttributes();
            alContext = ALC.CreateContext(outputDevice, attribs);
            ALC.MakeContextCurrent(alContext);

            var vecAt = OpenTK.Mathematics.Vector3.UnitZ;
            var vecUp = OpenTK.Mathematics.Vector3.UnitY;

            AL.Listener(ALListener3f.Position, 0, 0, 0);
            AL.Listener(ALListener3f.Velocity, 0, 0, 0);
            AL.Listener(ALListenerfv.Orientation, ref vecAt, ref vecUp);
        }

        /// <summary>
        /// Updates the rotation of the listener in 3D space
        /// </summary>
        /// <param name="fwd">Forward vector of the listener</param>
        /// <param name="up">Up vector of the listener</param>
        public void UpdateListener(Vector3 fwd, Vector3 up)
        {
            AL.Listener(ALListenerfv.Orientation, ref fwd, ref up);
        }

        /// <summary>
        /// Adds a source at a given location in 3D space to the OpenAL scene.
        /// Also generates the associated audio buffer.
        /// </summary>
        /// <param name="pos">Source position</param>
        /// <param name="userID">Server provided userID</param>
        public void AddSource(Vector3 pos, int userID)
        {
            int source = AL.GenSource();

            AL.Source(source, ALSourcef.Pitch, 1);
            AL.Source(source, ALSourcef.Gain, 1);
            AL.Source(source, ALSource3f.Position, pos.X, pos.Y, pos.Z);
            AL.Source(source, ALSource3f.Velocity, 0, 0, 0);
            AL.Source(source, ALSourceb.Looping, false);

            int bufferA = AL.GenBuffer();
            int bufferB = AL.GenBuffer();
            int bufferC = AL.GenBuffer();
            //AL.BindBufferToSource(source, buffer);
            //AL.SourceQueueBuffer(source, bufferB);

            users2Sources.Add(userID, (source, bufferA, bufferB, bufferC));
        }

        /// <summary>
        /// Adds a source at a (0,0,1) in 3D space to the OpenAL scene
        /// </summary>
        /// <param name="userID">Server provided userID</param>
        public void AddSource(int userID)
        {
            AddSource(Vector3.UnitZ, userID);
        }

        /// <summary>
        /// Moves a source to the given location in 3D space in the OpenAL scene
        /// </summary>
        /// <param name="pos">Source position</param>
        /// <param name="userID">Server provided userID</param>
        public void PlaceSource(float x, float y, float z, int userID)
        {
            AL.Source(users2Sources[userID].source, ALSource3f.Position, x, y, z);
        }

        /// <summary>
        /// Checks whether or not an OpenAL source is setup for the given user.
        /// </summary>
        /// <param name="userID"></param>
        /// <returns></returns>
        public bool HasSource(int userID) => users2Sources.ContainsKey(userID);

        /// <summary>
        /// Gets the next buffer of data from the capture device and returns it as a byte span.
        /// </summary>
        /// <returns>a span containing the captured audio</returns>
        public byte[] CaptureSamples()
        {
            if (captureDevice.Handle == IntPtr.Zero)
                return Array.Empty<byte>();

            ALC.GetInteger(captureDevice, AlcGetInteger.CaptureSamples, 1, out int capturedSamples);
            ALC.CaptureSamples(captureDevice, buffer, capturedSamples);

            return buffer.Take(capturedSamples*2).ToArray();
        }

        public void PlayBuffer(byte[] buff, int userID)
        {
            (int source, int bufferA, int bufferB, int bufferC) = users2Sources[userID];

            //Once OpenAL plays a buffer it adds it to a list of processed buffers.
            //We dequeue any processed buffers to refill them with data and then requeue.
            AL.GetSource(source, ALGetSourcei.BuffersProcessed, out int buffersProcessed);
            while (buffersProcessed > 0)
            {
                int oldbuff = AL.SourceUnqueueBuffers(source, 1)[0];

                AL.BufferData(oldbuff, ALFormat.Mono16, buff, AudioTranscoder.SAMPLE_RATE);
                AL.SourceQueueBuffer(source, oldbuff);

                buffersProcessed--;
            }

            ALSourceState state = AL.GetSourceState(source);
            if (state == ALSourceState.Initial)
            {
                //This mostly handles the initial state before any buffers have been filled
                AL.BufferData(bufferA, ALFormat.Mono16, buff, AudioTranscoder.SAMPLE_RATE);
                AL.BufferData(bufferB, ALFormat.Mono16, buff, AudioTranscoder.SAMPLE_RATE);
                AL.BufferData(bufferC, ALFormat.Mono16, buff, AudioTranscoder.SAMPLE_RATE);
                AL.SourceQueueBuffers(source, 3, new int[] { bufferA, bufferB, bufferC });
            }

            //In case we reach the end of a buffer
            if(state != ALSourceState.Playing)
            {
                AL.SourcePlay(source);
                System.Diagnostics.Debug.WriteLine("Warning: Audio playback engine can't keep up!");
            }
        }
    }

    internal struct AudioSourceState
    {
        public int source;
        public int buffer;
        public int bufferB;
        public int bufferC;

        public AudioSourceState(int source, int buffer, int bufferB, int bufferC)
        {
            this.source = source;
            this.buffer = buffer;
            this.bufferB = bufferB;
            this.bufferC = bufferC;
        }

        public override bool Equals(object? obj)
        {
            return obj is AudioSourceState other &&
                   source == other.source &&
                   buffer == other.buffer &&
                   bufferB == other.bufferB &&
                   bufferC == other.bufferC;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(source, buffer, bufferB, bufferC);
        }

        public void Deconstruct(out int source, out int buffer, out int bufferB, out int bufferC)
        {
            source = this.source;
            buffer = this.buffer;
            bufferB = this.bufferB;
            bufferC = this.bufferC;
        }

        public static implicit operator (int source, int buffer, int bufferB, int bufferC)(AudioSourceState value)
        {
            return (value.source, value.buffer, value.bufferB, value.bufferC);
        }

        public static implicit operator AudioSourceState((int source, int buffer, int bufferB, int bufferC) value)
        {
            return new AudioSourceState(value.source, value.buffer, value.bufferB, value.bufferC);
        }
    }
}
