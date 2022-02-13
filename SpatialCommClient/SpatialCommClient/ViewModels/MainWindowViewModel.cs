using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using OpenTK.Audio.OpenAL;
using System.Threading;
using CircularBuffer;
using SpatialCommClient.Models;

namespace SpatialCommClient.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private static readonly int AUDIO_CHUNK_TIME_MS = 20;
        private static readonly int AUDIO_CAPTURE_BUFFER_SIZE = Models.OpenALManager.ComputeBufferSize(Models.AudioTranscoder.SAMPLE_RATE, AUDIO_CHUNK_TIME_MS);

        private Models.NetworkMarshal networkMarshal;
        private Models.OpenALManager alManager;
        private Models.AudioTranscoder audioTranscoder;

        private Task audioRXThread;
        private Task audioTXThread;

        private Task controlRXThread;
        private Task controlTXThread;

        private Timer audioCaptureLoop;

        //Initialize a double sized circular buffer to store audio data.
        private CircularBuffer<byte> capturedAudio = new CircularBuffer<byte>(AUDIO_CAPTURE_BUFFER_SIZE*2);

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
            
            networkMarshal = new Models.NetworkMarshal(LoggerText, Users);
            alManager = new Models.OpenALManager();
            audioTranscoder = new Models.AudioTranscoder(1);

            foreach (string d in alManager.ListInputDevices())
                AudioInputDevices.Add(d);
            foreach (string d in alManager.ListOutputDevices())
                AudioOutputDevices.Add(d);

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
            ConnectionButtonEnabled = false;
            ConnectionButtonText = "Connecting";
            LoggerText.Add($"Connecting to {IPAddressText}:{PortControlText}...");
            var t = Task.Run(() =>
                networkMarshal.ConnectToServer(IPAddressText, int.Parse(PortControlText), int.Parse(PortAudioText), UsernameText));
            _ = t.ContinueWith(_ =>
            {
                LoggerText.Add("Connected! Result: " + t.Result);
                ConnectionButtonEnabled = true;
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
                Task.Run(() => { new WebcamEstimator(this); });

            });
        }

        private void StartAudioService()
        {
            alManager.OpenDevice(SelectedAudioOutputDevice);
            int buffSize = Models.OpenALManager.ComputeBufferSize(Models.AudioTranscoder.SAMPLE_RATE, AUDIO_CHUNK_TIME_MS);
            // Double size buffer for safety
            alManager.OpenCaptureDevice(SelectedAudioInputDevice, Models.AudioTranscoder.SAMPLE_RATE, buffSize * 2);

            //Start capture loop
            //var audioCaptureLoop = new Timer(CaptureAudioCallback, null, AUDIO_CHUNK_TIME_MS, AUDIO_CHUNK_TIME_MS/2);
            var audioCaptureLoop = Task.Run(()=>CaptureAudioCallback(null));
            //CaptureAudioCallback(null);
        }

        // Called every 10ms (AUDIO_CHUNK_TIME_MS/2), should capture audio samples from openAL do any processing
        // and then encode and send them to the output stream
        //TODO: Rewrite this with unsafe code to avoid the numerous array copies.
        private void CaptureAudioCallback(object state)
        {
            while (true)
            {
                var samples = alManager.CaptureSamples();

                if (samples.Length == 0)
                    continue;

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
    }
}
