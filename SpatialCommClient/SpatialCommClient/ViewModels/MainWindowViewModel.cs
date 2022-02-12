using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace SpatialCommClient.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private Models.NetworkMarshal networkMarshal;

        private Task AudioRXThread;
        private Task AudioTXThread;

        private Task ControlRXThread;
        private Task ControlTXThread;

        #region Bindable Objects
        [Reactive] public string IPAddressText { get; set; } = "spatialcomm.tech";
        [Reactive] public string PortControlText { get; set; } = "25567";
        [Reactive] public string PortAudioText { get; set; } = "25567";
        [Reactive] public string UsernameText { get; set; } = "anonymous";
        [Reactive] public string HeadPosition { get; set; } = "-000.00, -000.00, -000.00";
        [Reactive] public string HeadRotation { get; set; } = "-000.00, -000.00, -000.00";
        [Reactive] public string ConnectionButtonText { get; private set; } = "Connect";
        [Reactive] public bool ConnectionButtonEnabled { get; private set; } = true;
        [Reactive] public int SelectedCamera { get; set; } = 0;
        public ObservableCollection<string> LoggerText { get; } = new();
        public ObservableCollection<string> Cameras { get; } = new();
        public ObservableCollection<string> Players { get; } = new();
        public ICommand ConnectCommand { get; private set; }
        #endregion

        public MainWindowViewModel()
        {
            CreateCommands();
            
            networkMarshal = new Models.NetworkMarshal(LoggerText);

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
            t.ContinueWith(_=> {
                LoggerText.Add("Connected! Result: " + t.Result);
                ConnectionButtonEnabled = true;
                ConnectionButtonText = "Connected";

                if(t.Result == -1)
                {
                    ConnectionButtonText = "Connect";
                    return;
                }

                AudioRXThread = Task.Run(() => { networkMarshal.AudioListener(); });
                AudioTXThread = Task.Run(() => { networkMarshal.SocketEmitter(false); });
                ControlRXThread = Task.Run(() => { networkMarshal.ControlListener(); });
                ControlTXThread = Task.Run(() => { networkMarshal.SocketEmitter(true); });
                Task.Run(() => { new Models.WebcamEstimator(this); });

            });
            
        }
    }
}
