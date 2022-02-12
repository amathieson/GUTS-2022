using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Audio.OpenAL;

namespace SpatialCommClient.Models
{
    class OpenALManager : IDisposable
    {
        private ALCaptureDevice captureDevice;
        private ALDevice outputDevice;
        private ALContext alContext;
        private byte[] buffer;

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
            return ALC.GetString(AlcGetStringList.DeviceSpecifier).ToArray();
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

            var vecAt = OpenTK.Mathematics.Vector3.UnitZ;
            var vecUp = OpenTK.Mathematics.Vector3.UnitY;

            AL.Listener(ALListener3f.Position, 0, 0, 1.0f);
            AL.Listener(ALListener3f.Velocity, 0, 0, 0);
            AL.Listener(ALListenerfv.Orientation, ref vecAt, ref vecUp);
            //TODO: Create output buffers and bind them
        }

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

            return buffer.Take(capturedSamples).ToArray();
        }

        public void PlayBuffer()
        {

        }
    }
}
