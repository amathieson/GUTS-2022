using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System.Threading;
using CircularBuffer;
using System.Linq;
using OpenTK.Mathematics;
using SpatialCommClient.Models;
using System.IO;

namespace SpatialCommClient.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private static readonly int AUDIO_CHUNK_TIME_MS = 40;
        private static readonly int AUDIO_CAPTURE_BUFFER_SIZE = OpenALManager.ComputeBufferSize(AudioTranscoder.SAMPLE_RATE, AUDIO_CHUNK_TIME_MS);
        private static readonly float WEBCAM_POSE_AVERAGING = 0.65f; // 0-1

        private NetworkMarshal networkMarshal;
        private OpenALManager alManager;
        private AudioTranscoder audioTranscoder;
        private WebcamEstimator webcamEstimator;

        private Task audioRXThread;
        private Task audioTXThread;

        private Task controlRXThread;
        private Task controlTXThread;

        private Timer audioCaptureLoop;

        //Initialize a double sized circular buffer to store audio data.
        private CircularBuffer<byte> capturedAudio = new CircularBuffer<byte>(AUDIO_CAPTURE_BUFFER_SIZE*2);

        private Vector3 prevFwd = Vector3.UnitZ;
        private Vector3 prevUp = Vector3.UnitY;

        #region Bindable Objects
        [Reactive] public string IPAddressText { get; set; } = "spatialcomm.tech";
        [Reactive] public string PortControlText { get; set; } = "25567";
        [Reactive] public string PortAudioText { get; set; } = "25567";
        [Reactive] public string UsernameText { get; set; } = "anonymous";
        [Reactive] public string HeadPosition { get; set; } = "~NOT CONNECTED~";
        [Reactive] public string HeadRotation { get; set; } = "~NOT CONNECTED~";
        [Reactive] public string ConnectionButtonText { get; private set; } = "Connect";
        [Reactive] public bool ConnectionButtonEnabled { get; private set; } = true;
        public ObservableCollection<string> AudioInputDevices { get; } = new();
        public ObservableCollection<string> AudioOutputDevices { get; } = new();
        [Reactive] public string SelectedAudioInputDevice { get; set; }
        [Reactive] public string SelectedAudioOutputDevice { get; set; }
        [Reactive] public int SelectedCamera { get; set; } = 0;
        public ObservableCollection<string> LoggerText { get; } = new();
        public ObservableCollection<string> Cameras { get; } = new();
        public ObservableCollection<User> Users { get; } = new();
        public ICommand ConnectCommand { get; private set; }
        #endregion

        public MainWindowViewModel()
        {
            CreateCommands();
            

            networkMarshal = new NetworkMarshal(LoggerText, Users);
            alManager = new OpenALManager();
            audioTranscoder = new AudioTranscoder(1);
            webcamEstimator = new WebcamEstimator(this);

            webcamEstimator.FaceUpdateEvent += WebcamEstimator_FaceUpdateEvent;


            foreach (string d in alManager.ListInputDevices())
                AudioInputDevices.Add(d);
            foreach (string d in alManager.ListOutputDevices())
                AudioOutputDevices.Add(d);
            GenerateUsername();

            LoggerText.Add("Initilised!");


        }

        //TODO: Find something to call this
        public void OnExit()
        {
            networkMarshal.Dispose();
        }

        private void CreateCommands()
        {
            ConnectCommand = ReactiveCommand.Create(()=>ConnectToServer());
        }

        private void ConnectToServer()
        {
            //StartAudioService();
            //return;

            ConnectionButtonEnabled = false;
            ConnectionButtonText = "Connecting";
            LoggerText.Add($"Connecting to {IPAddressText}:{PortControlText}...");
            var t = Task.Run(() =>
                networkMarshal.ConnectToServer(IPAddressText, int.Parse(PortControlText), int.Parse(PortAudioText), UsernameText));
            _ = t.ContinueWith(_ =>
            {
                LoggerText.Add("Connected! Result: " + t.Result);
                ConnectionButtonEnabled = false;
                ConnectionButtonText = "Connected";

                if (t.Result == -1)
                {
                    ConnectionButtonText = "Connect";
                    return;
                }

                StartAudioService();

                audioRXThread = Task.Run(() => { networkMarshal.AudioListener(); });
                audioTXThread = Task.Run(() => { networkMarshal.AudioSocketEmitter(); });
                controlRXThread = Task.Run(() => { networkMarshal.ControlListener(); });
                controlTXThread = Task.Run(() => { networkMarshal.SocketEmitter(true); });
                Task.Run(() => { webcamEstimator.RunEstimator(); });
            });
        }

        private void StartAudioService()
        {
            alManager.OpenDevice(SelectedAudioOutputDevice);
            int buffSize = OpenALManager.ComputeBufferSize(AudioTranscoder.SAMPLE_RATE, AUDIO_CHUNK_TIME_MS);
            // Double size buffer for safety
            alManager.OpenCaptureDevice(SelectedAudioInputDevice, AudioTranscoder.SAMPLE_RATE, buffSize * 2);

            //Start capture loop
            //var audioCaptureLoop = new Timer(CaptureAudioCallback, null, AUDIO_CHUNK_TIME_MS, AUDIO_CHUNK_TIME_MS/2);
            var audioCaptureLoop = Task.Run(()=>CaptureAudioCallback(null));

            networkMarshal.AudioDataReceived += NetworkMarshal_AudioDataReceived;
        }

        private void NetworkMarshal_AudioDataReceived(byte[] audioData, int userID, long packetID)
        {
            var decodedSamples = audioTranscoder.DecodeSamples(audioData);

            //TODO: Manage new users somewhere else (use the user discovery packet handler)
            if(!alManager.HasSource(userID))
            {
                //Create a new source
                alManager.AddSource(userID);

                //Rejiggle all the sources
                ReJiggleSources();
            }

            alManager.PlayBuffer(decodedSamples, userID);
        }

        private void WebcamEstimator_FaceUpdateEvent(Emgu.CV.Matrix<float> rotation)
        {
            var mat = rotation.Data;
            var tkMat = new Matrix3(mat[0, 0], mat[0, 1], mat[0, 2], 
                                    mat[1, 0], mat[1, 1], mat[1, 2], 
                                    mat[2, 0], mat[2, 1], mat[2, 2]);
            var fwd = tkMat * Vector3.UnitZ;
            var up  = tkMat * Vector3.UnitY;
            float diff = 1-Math.Abs(Vector3.Dot(fwd, prevFwd));
            fwd = Vector3.Lerp(fwd, prevFwd, Math.Clamp(WEBCAM_POSE_AVERAGING + diff*WEBCAM_POSE_AVERAGING*0.5f, 0, 0.9f));
            up =  Vector3.Lerp(up,  prevUp,  Math.Clamp(WEBCAM_POSE_AVERAGING + diff*WEBCAM_POSE_AVERAGING*0.5f, 0, 0.9f));
            prevFwd = fwd;
            prevUp = up;

            alManager.UpdateListener(fwd, up);

            HeadRotation = fwd.ToStringFormatted();
        }

        private void ReJiggleSources()
        {
            //Reposition all the sources around the listner
            int nsources = alManager.Users2Source.Count;
            var sources = alManager.Users2Source.Keys.AsEnumerable().OrderBy(x => x).ToList();

            for(int i = 0; i < nsources; i++)
            {
                float arcPos = (i + 1) / ((float)nsources + 1)*2*MathF.PI-MathF.PI;
                float x = MathF.Sin(arcPos);
                float z = MathF.Cos(arcPos);

                alManager.PlaceSource(x, 0, z, sources[i]);
            }
        }

        // Called every 10ms (AUDIO_CHUNK_TIME_MS/2), should capture audio samples from openAL do any processing
        // and then encode and send them to the output stream
        //TODO: Rewrite this with unsafe code to avoid the numerous array copies.
        private void CaptureAudioCallback(object state)
        {
            //Part of the sine wave test
            /*alManager.AddSource(20);
            ReJiggleSources();
            long count = 0;*/

            while (true)
            {
                var samples = alManager.CaptureSamples();

                if (samples.Length == 0)
                    continue;

                //Test sine wave
                /*samples = new byte[8192];
                for (int i = 0; i < samples.Length; i+=2)
                {
                    short v = (short)((Math.Sin((i / 2 + count)*Math.PI / 10.5d)*.5+.5)*short.MaxValue);
                    //short v = (short)((Models.ExtensionMethods.Frac((i / 2 + count) / 100d))*short.MaxValue*2);
                    var bytes = BitConverter.GetBytes(v);
                    samples[i] = bytes[1];
                    samples[i+1] = bytes[1];
                }
                count += samples.Length/2;
                alManager.PlayBuffer(samples, 20);*/

                foreach (byte b in samples)
                    capturedAudio.PushBack(b);

                if (capturedAudio.Size >= AUDIO_CAPTURE_BUFFER_SIZE)
                {
                    var encData = audioTranscoder.EncodeSamples(capturedAudio.ToArray(AUDIO_CAPTURE_BUFFER_SIZE, true));
                    networkMarshal.SendAudioData(encData.ToArray());
                }

                Thread.Sleep(10);
            }
        }

        private void GenerateUsername()
        {
            try
            {
                string[] adj = File.ReadAllLines("Assets/english-adjectives.txt");
                string[] noun = File.ReadAllLines("Assets/english-nouns.txt");
                Random r = new Random();
                UsernameText = adj[r.Next(0, adj.Length - 1)] + "-" + noun[r.Next(0, noun.Length - 1)] + r.Next(1, 999);
            } catch (Exception)
            {
                UsernameText = "anonymous";
            }
        }
    }
}
