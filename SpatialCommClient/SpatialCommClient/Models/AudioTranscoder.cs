using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FragLabs.Audio.Codecs;

namespace SpatialCommClient.Models
{
    /// <summary>
    /// This class handles all the encoding and decoding of Opus encoded data.
    /// It supports single channel encoding and multi-channel decoding.
    /// The sample rate is a constant.
    /// </summary>
    public class AudioTranscoder : IDisposable
    {
        public static readonly int SAMPLE_RATE = 48000;

        private OpusMultiStreamDecoder decoder;
        private OpusEncoder encoder;

        public int AudioChannels 
        {
            set 
            {
                decoder.Dispose();
                decoder = OpusMultiStreamDecoder.Create(SAMPLE_RATE, value);
            } 
        }

        public AudioTranscoder(int outputChannels)
        {
            decoder = OpusMultiStreamDecoder.Create(SAMPLE_RATE, outputChannels);
            encoder = OpusEncoder.Create(SAMPLE_RATE, 1, FragLabs.Audio.Codecs.Opus.Application.Voip);
        }

        public float[] DecodeSamples(byte[] data)
        {
            return decoder.DecodeFloat(data, data.Length, out int decodedLength).Take(decodedLength).ToArray();
        }

        public Span<byte> EncodeSamples(float[] data)
        {
            return encoder.EncodeFloat(data, data.Length, out int decodedLength).AsSpan()[..decodedLength];
        }

        public Span<byte> EncodeSamples(byte[] data)
        {
            return encoder.Encode(data, data.Length, out int decodedLength).AsSpan()[..decodedLength];
        }

        public void Dispose()
        {
            decoder.Dispose();
            encoder.Dispose();
        }
    }
}
