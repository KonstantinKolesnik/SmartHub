//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.34011
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace AquaExpert {
    using Gadgeteer;
    using GTM = Gadgeteer.Modules;
    
    
    public partial class Program : Gadgeteer.Program {
        
        /// <summary>The UsbClientDP module using socket 8 of the mainboard.</summary>
        private Gadgeteer.Modules.GHIElectronics.UsbClientDP usbClientDP;
        
        /// <summary>The LED Strip module using socket 18 of the mainboard.</summary>
        private Gadgeteer.Modules.GHIElectronics.LED_Strip indicators;
        
        /// <summary>The SDCard module using socket 9 of the mainboard.</summary>
        private Gadgeteer.Modules.GHIElectronics.SDCard sdCard;
        
        /// <summary>The Extender module (not connected).</summary>
        private Gadgeteer.Modules.GHIElectronics.Extender extender;
        
        /// <summary>The WiFi_RS21 (Premium) module (not connected).</summary>
        private Gadgeteer.Modules.GHIElectronics.WiFi_RS21 wifi_RS21;
        
        /// <summary>The Display TE35 module using sockets 15, 16, 17 and 14 of the mainboard.</summary>
        private Gadgeteer.Modules.GHIElectronics.Display_TE35 display_TE35;
        
        /// <summary>This property provides access to the Mainboard API. This is normally not necessary for an end user program.</summary>
        protected new static GHIElectronics.Gadgeteer.FEZRaptor Mainboard {
            get {
                return ((GHIElectronics.Gadgeteer.FEZRaptor)(Gadgeteer.Program.Mainboard));
            }
            set {
                Gadgeteer.Program.Mainboard = value;
            }
        }
        
        /// <summary>This method runs automatically when the device is powered, and calls ProgramStarted.</summary>
        public static void Main() {
            // Important to initialize the Mainboard first
            Program.Mainboard = new GHIElectronics.Gadgeteer.FEZRaptor();
            Program p = new Program();
            p.InitializeModules();
            p.ProgramStarted();
            // Starts Dispatcher
            p.Run();
        }
        
        private void InitializeModules() {
            this.usbClientDP = new GTM.GHIElectronics.UsbClientDP(8);
            this.indicators = new GTM.GHIElectronics.LED_Strip(18);
            this.sdCard = new GTM.GHIElectronics.SDCard(9);
            Microsoft.SPOT.Debug.Print("The module \'extender\' was not connected in the designer and will be null.");
            Microsoft.SPOT.Debug.Print("The module \'wifi_RS21\' was not connected in the designer and will be null.");
            this.display_TE35 = new GTM.GHIElectronics.Display_TE35(15, 16, 17, 14);
        }
    }
}
