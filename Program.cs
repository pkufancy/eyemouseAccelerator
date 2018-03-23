using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tobii.Interaction;
using Tobii.Interaction.Framework;
using System.Runtime.InteropServices;

namespace Interaction_Streams_102
{
    /// <summary>
    /// The data streams provide nicely filtered eye-gaze data from the eye tracker 
    /// transformed to a convenient coordinate system. The point on the screen where 
    /// your eyes are looking (gaze point), and the points on the screen where your 
    /// eyes linger to focus on something (fixations) are given as pixel coordinates 
    /// on the screen. The positions of your eyeballs (eye positions) are given in 
    /// space coordinates in millimeters relative to the center of the screen.
    /// 
    /// The Fixation data stream provides information about when the user is fixating
    /// his/her eyes at a single location. This data stream can be used to get an 
    /// understanding of where the user’s attention is. In most cases, when a person
    /// is fixating at something for a long time, this means that the person’s brain 
    /// is processing the information at the fixation point.
    /// </summary>
    public class Program
    {

        //Mouse Position & speed settings
        public const UInt32 SPI_GETMOUSESPEED = 0x0070;
        public const UInt32 SPI_SETMOUSESPEED = 0x0071;
        const UInt32 SPIF_UPDATEINIFILE = 0x01;
        const UInt32 SPIF_SENDWININICHANGE = 0x02;


        [DllImport("User32.dll")]
        static extern Boolean SystemParametersInfo(
            UInt32 uiAction,
            UInt32 uiParam,
            IntPtr pvParam,
            UInt32 fWinIni);

        [DllImport("User32")]
        public extern static void mouse_event(int dwFlags, int dx, int dy, int dwData, IntPtr dwExtraInfo);
        [DllImport("User32")]
        public extern static void SetCursorPos(int x, int y);
        [DllImport("User32")]
        public extern static bool GetCursorPos(out POINT p);
        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }

        public enum MouseEventFlags
        {
            Move = 0x0001,
            LeftDown = 0x0002,
            LeftUp = 0x0004,
            RightDown = 0x0008,
            RightUp = 0x0010,
            MiddleDown = 0x0020,
            MiddleUp = 0x0040,
            Wheel = 0x0800,
            Absolute = 0x8000
        }
        public static void SetMouseSpeed(uint speed)
        {
            uint mouseSpeed = speed;
            if (mouseSpeed < 1)
            {
                mouseSpeed = 1;
            }
            else if (mouseSpeed > 20)
            {
                mouseSpeed = 20;
            }
            SystemParametersInfo(SPI_SETMOUSESPEED, 0, new IntPtr(mouseSpeed), 0);
        }


        public static void Main(string[] args)
        {
            // Everything starts with initializing Host, which manages the connection to the 
            // Tobii Engine and provides all the Tobii Core SDK functionality.
            // NOTE: Make sure that Tobii.EyeX.exe is running
            var host = new Host();

            // Initialize Fixation data stream.
            var fixationDataStream = host.Streams.CreateFixationDataStream();

            // Because timestamp of fixation events is relative to the previous ones
            // only, we will store them in this variable.
            var fixationBeginTime = 0d;

            fixationDataStream.Next += (o, fixation) =>
            {
                // On the Next event, data comes as FixationData objects, wrapped in a StreamData<T> object.
                var fixationPointX = fixation.Data.X;
                var fixationPointY = fixation.Data.Y;

                switch (fixation.Data.EventType)
                {
                    case FixationDataEventType.Begin:
                        fixationBeginTime = fixation.Data.Timestamp;
                        Console.WriteLine("Begin fixation at X: {0}, Y: {1}", fixationPointX, fixationPointY);
                        break;

                    case FixationDataEventType.Data:

                        POINT p = new POINT();
                        GetCursorPos(out p);
                        var mousePointX = p.X;
                        var mousePointY = p.Y;
                        Console.WriteLine("The Mouse Position At X: {0}, Y: {1}", mousePointX, mousePointY);

                        Console.WriteLine("During fixation, currently at X: {0}, Y: {1}", fixationPointX, fixationPointY);

                        var distancesq = (fixationPointX - mousePointX) * (fixationPointX - mousePointX) + (fixationPointY - mousePointY) * (fixationPointY - mousePointY);
                        if (distancesq < 10000)
                        {
                            SetMouseSpeed(1);
                        }
                        else
                        {
                            SetMouseSpeed(20);
                        }

                        break;

                    case FixationDataEventType.End:

                        SetMouseSpeed(14);
                        Console.WriteLine("End fixation at X: {0}, Y: {1}", fixationPointX, fixationPointY);
                        Console.WriteLine("Fixation duration: {0}",
                            fixationBeginTime > 0
                                ? TimeSpan.FromMilliseconds(fixation.Data.Timestamp - fixationBeginTime)
                                : TimeSpan.Zero);
                        Console.WriteLine();
                        break;

                    default:
                        throw new InvalidOperationException("Unknown fixation event type, which doesn't have explicit handling.");
                }
            };

            Console.ReadKey();

            // we will close the coonection to the Tobii Engine before exit.
            host.DisableConnection();
        }
    }
}
