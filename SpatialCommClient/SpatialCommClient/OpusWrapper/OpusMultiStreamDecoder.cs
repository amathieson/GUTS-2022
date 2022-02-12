using System;
using System.Linq;
using FragLabs.Audio.Codecs.Opus;

namespace FragLabs.Audio.Codecs
{
    /// <summary>
    /// Opus multi stream-capable audio decoder.
    /// </summary>
    public class OpusMultiStreamDecoder : IDisposable
    {
        /// <summary>
        /// Creates a new Opus multi-stream decoder.
        /// The decoder is always set up to decode mono-audio streams.
        /// </summary>
        /// <param name="outputSampleRate">Sample rate to decode at (Hz). This must be one of 8000, 12000, 16000, 24000, or 48000.</param>
        /// <param name="outputStreams">Number of streams to decode.</param>
        /// <returns>A new <c>OpusDecoder</c>.</returns>
        public static OpusMultiStreamDecoder Create(int outputSampleRate, int outputStreams)
        {
            if (outputSampleRate != 8000 &&
                outputSampleRate != 12000 &&
                outputSampleRate != 16000 &&
                outputSampleRate != 24000 &&
                outputSampleRate != 48000)
                throw new ArgumentOutOfRangeException("inputSamplingRate");
            if (outputStreams < 1 || outputStreams > 255)
                throw new ArgumentOutOfRangeException("inputChannels");

            var mapping = Enumerable.Range(0, outputStreams).Select(x=>(byte)x).ToArray();
            IntPtr decoder = API.opus_multistream_decoder_create(outputSampleRate, outputStreams, outputStreams, 0, mapping, out IntPtr error);
            if ((Errors)error != Errors.OK)
            {
                throw new Exception("Exception occured while creating decoder");
            }
            return new OpusMultiStreamDecoder(decoder, outputSampleRate, outputStreams);
        }

        private IntPtr _decoder;

        private OpusMultiStreamDecoder(IntPtr decoder, int outputSamplingRate, int outputChannels)
        {
            _decoder = decoder;
            OutputSamplingRate = outputSamplingRate;
            OutputChannels = outputChannels;
            MaxDataBytes = 40000;//TODO: I'm looking forward to having this number come back an haunt me...
        }

        /// <summary>
        /// Produces PCM samples from Opus encoded data.
        /// These samples should have each channel interlaced.
        /// </summary>
        /// <param name="inputOpusData">Opus encoded data to decode, null for dropped packet.</param>
        /// <param name="dataLength">Length of data to decode.</param>
        /// <param name="decodedLength">Set to the length of the decoded sample data.</param>
        /// <returns>PCM audio samples.</returns>
        public unsafe byte[] Decode(byte[] inputOpusData, int dataLength, out int decodedLength)
        {
            if (disposed)
                throw new ObjectDisposedException("OpusMultiStreamDecoder");

            IntPtr decodedPtr;
            byte[] decoded = new byte[MaxDataBytes];
            int frameCount = FrameCount(MaxDataBytes);
            int length = 0;
            fixed (byte* bdec = decoded)
            {
                decodedPtr = new IntPtr((void*)bdec);

                if (inputOpusData != null)
                    length = API.opus_multistream_decode(_decoder, inputOpusData, dataLength, decodedPtr, frameCount, 0);
                else
                    length = API.opus_multistream_decode(_decoder, null, 0, decodedPtr, frameCount, (ForwardErrorCorrection) ? 1 : 0);
            }
            decodedLength = length * 2;//TODO: Where does this 2 come from (does it assume stereo?)
            if (length < 0)
                throw new Exception("Decoding failed - " + ((Errors)length).ToString());

            return decoded;
        }

        /// <summary>
        /// Produces PCM (float) samples from Opus encoded data.
        /// These samples should have each channel interlaced.
        /// </summary>
        /// <param name="inputOpusData">Opus encoded data to decode, null for dropped packet.</param>
        /// <param name="dataLength">Length of data to decode.</param>
        /// <param name="decodedLength">Set to the length of the decoded sample data.</param>
        /// <returns>PCM audio samples.</returns>
        public unsafe float[] DecodeFloat(byte[] inputOpusData, int dataLength, out int decodedLength)
        {
            if (disposed)
                throw new ObjectDisposedException("OpusMultiStreamDecoder");

            IntPtr decodedPtr;
            float[] decoded = new float[MaxDataBytes/4];
            int frameCount = FrameCount(MaxDataBytes);
            int length = 0;
            fixed (float* bdec = decoded)
            {
                decodedPtr = new IntPtr((void*)bdec);

                if (inputOpusData != null)
                    length = API.opus_multistream_decode_float(_decoder, inputOpusData, dataLength, decodedPtr, frameCount, 0);
                else
                    length = API.opus_multistream_decode_float(_decoder, null, 0, decodedPtr, frameCount, (ForwardErrorCorrection) ? 1 : 0);
            }
            decodedLength = length * 2;
            if (length < 0)
                throw new Exception("Decoding failed - " + ((Errors)length).ToString());

            return decoded;
        }

        /// <summary>
        /// Determines the number of frames that can fit into a buffer of the given size.
        /// </summary>
        /// <param name="bufferSize"></param>
        /// <returns></returns>
        public int FrameCount(int bufferSize)
        {
            //  seems like bitrate should be required
            int bitrate = 16;
            int bytesPerSample = (bitrate / 8) * OutputChannels;
            return bufferSize / bytesPerSample;
        }

        /// <summary>
        /// Gets the output sampling rate of the decoder.
        /// </summary>
        public int OutputSamplingRate { get; private set; }

        /// <summary>
        /// Gets the number of channels of the decoder.
        /// </summary>
        public int OutputChannels { get; private set; }

        /// <summary>
        /// Gets or sets the size of memory allocated for decoding data.
        /// </summary>
        public int MaxDataBytes { get; set; }

        /// <summary>
        /// Gets or sets whether forward error correction is enabled or not.
        /// </summary>
        public bool ForwardErrorCorrection { get; set; }

        ~OpusMultiStreamDecoder()
        {
            Dispose();
        }

        private bool disposed;
        public void Dispose()
        {
            if (disposed)
                return;

            GC.SuppressFinalize(this);

            if (_decoder != IntPtr.Zero)
            {
                API.opus_multistream_decoder_destroy(_decoder);
                _decoder = IntPtr.Zero;
            }

            disposed = true;
        }
    }
}
