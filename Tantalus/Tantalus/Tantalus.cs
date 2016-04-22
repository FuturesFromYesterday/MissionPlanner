
using System;
using FreeSMO;

namespace Tantalus
{
    public class Tantalus : MissionPlanner.Plugin.Plugin
    {
        //Initial Timer and then looping timers to write out to SMOs. 
        private System.Timers.Timer SetupTimer, t50hz, t10hz, t1hz;

        //Class and struct for writing out.
        //These are defined in FreeSmo.cs 
        FreeSmoWriter outputSMOWriter;
        stMAVData outData; 

        public override string Author
        {
            get { return "BenLikesAirplanes@gmail.com"; }
        }

        public override string Name
        {
            get { return "Tantalus"; }
        }

        public override string Version
        {
            get { return "0.8"; }
        }

        public override bool Exit()
        {
            t50hz.Stop();
            t10hz.Stop();
            t1hz.Stop();
            return true; 
        }

        public override bool Init()
        {
            return true; 
        }

        public override bool Loaded()
        {
            //This is probably redundant. 
            System.Console.WriteLine("Tantalus Loaded!");

            SetupTheSetupTimer();
            return true;
        }

        private void SetupTheSetupTimer()
        {
            SetupTimer = new System.Timers.Timer(10000);
            SetupTimer.Elapsed += attemptToStartMavDump;
            SetupTimer.AutoReset = true;
            SetupTimer.Start();
        }

        private void attemptToStartMavDump(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (Host.comPort.BaseStream.IsOpen == true)
            {
                System.Console.WriteLine("Now Starting Mav Dump to TRACS");
                getSMOHolders();
                initTimers();
                SetupTimer.Stop();
            }
            else
            {
                System.Console.WriteLine("No Connected Drone; Trying again in 10 seconds. \n" 
                    + System.DateTime.Now.ToShortTimeString());
            }
        }

        private void initTimers()
        {
            t1hz = new System.Timers.Timer(1000);
            t10hz = new System.Timers.Timer(100);
            t50hz = new System.Timers.Timer(20);

            t1hz.AutoReset = true;
            t10hz.AutoReset = true;
            t50hz.AutoReset = true; 

            t1hz.Elapsed += Spit1;
            t10hz.Elapsed += Spit10;
            t50hz.Elapsed += Spit50;

            t50hz.Start();
            t10hz.Start();
            t1hz.Start();
        }

        private void getSMOHolders()
        {
            outputSMOWriter = new FreeSmoWriter();
        }
        
        /// Function to actually do the writing out from 
        /// MissionPlanner's database and into shared memory.
        /// This one is used for doing data reads as fast as we possibly can. 
        private void Spit50(System.Object source, System.Timers.ElapsedEventArgs e)
        {
            if (Host.comPort.BaseStream.IsOpen == true)
            {
                outData.altitude = Host.cs.alt;
                outData.pitch = Host.cs.pitch;
                outData.roll = Host.cs.roll; 

                //You could be more nuanced with this one since we're most likely using a quadcopter rather than a fixed wing. 
                outData.trueheading = outData.heading = Host.cs.nav_bearing;
            }
            else
            {
                return;
            }
        }

        private void Spit10(System.Object source, System.Timers.ElapsedEventArgs e)
        {
            if (Host.comPort.BaseStream.IsOpen == true)
            {
                outData.airspeed = Host.cs.airspeed;
                outData.groundspeed = Host.cs.groundspeed;
            }
            else
            {
                return;
            }
        }

        private void Spit1(System.Object source, System.Timers.ElapsedEventArgs e)
        {
            ///Whether it's worth it to update anything at 1hz is debatable. 
            ///Using Lat/Long here is mostly a demonstration of the method.
            
            //These could be used to fake something for a demo quite easily. 
            float defaultLat = 0;
            float defaultLng = 0; 

            if (Host.comPort.BaseStream.IsOpen == true)
            {
                //If either value is real, use that, preferring lat & lng over lat2 and lng2
                //Otherwise, use default value. 
                outData.latitude = (float)((Host.cs.lat != 0.0) ? Host.cs.lat : (Host.cs.lat2 != 0.0) ? Host.cs.lat2 : defaultLat);
                outData.longitude = (float)((Host.cs.lng != 0.0) ? Host.cs.lng : (Host.cs.lng2 != 0.0) ? Host.cs.lng2 : defaultLng);
            }
            else
            {
                //This one is slow enough to be a good reminder 'thread'. 
                Console.WriteLine("Drone seems to have disconnected.");
                return; 
            }
        }
    }
}
