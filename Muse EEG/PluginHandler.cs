﻿using System;
using System.Diagnostics;
using System.IO.Ports;
using System.Threading;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Linq;
using System.IO;
using System.Xml;
using Bespoke.Common.Osc;
using System.Net;

namespace lucidcode.LucidScribe.Plugin.InteraXon.Muse
{

  public static class Device
  {
    private static bool m_boolInitialized;
    private static bool m_boolInitError;
    public static String Algorithm;
    public static int Threshold;

    private static double m_dblAttention;
    private static double m_dblLastAttention;
    private static double m_dblBlinkStrength;
    private static double m_dblMeditation;
    private static double m_dblLastMeditation;
    private static double m_dblAlpha;
    private static double m_dblLastAlpha;
    private static double m_dblBeta;
    private static double m_dblLastBeta;
    private static double m_dblDelta;
    private static double m_dblLastDelta;
    private static double m_dblGamma;
    private static double m_dblLastGamma;
    private static double m_dblTheta;
    private static double m_dblLastTheta;
    private static double m_dblRaw;

    private static double ACC1;
    private static double ACC2;
    private static double ACC3;

    private static double EEG1;
    private static double EEG2;
    private static double EEG3;
    private static double EEG4;

    private static bool ClearDisplay;
    private static bool ClearHighscore;
    private static double DisplayValue;
    private static double HighscoreValue;

    public static Boolean REMDetected = false;

    private static OscServer oscServer;
    private static Boolean disposing = false;

    public static EventHandler<EventArgs> ThinkGearChanged;

    private static readonly string AliveMethod = "/osctest/alive";
    private static readonly string TestMethod = "/osctest/test";
    private static int sBundlesReceivedCount;
    private static int sMessagesReceivedCount;

    private static Process museIO;

    public static Boolean Initialize()
    {
      try
      {
        if (!m_boolInitialized && !m_boolInitError)
        {

          // Check if Muse-IO is running

          // Launch Muse-IO
          String musePath = "";
          if (File.Exists(@"C:\Program Files (x86)\Muse\muse-io.exe"))
          {
            musePath = @"C:\Program Files (x86)\Muse\muse-io.exe";
          }
          else if (File.Exists(@"C:\Program Files\Muse\muse-io.exe"))
          {
            musePath = @"C:\Program Files\Muse\muse-io.exe";
          }
          else
          {
            if (MessageBox.Show("Could not find Muse-IO. Would you like to install it?", "Install Muse SDK", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1) == DialogResult.Yes)
            {
              System.Diagnostics.Process.Start("http://storage.googleapis.com/musesdk/musesdk-2.4.2-windows-installer.exe");
            }
            else
            {
              MessageBox.Show("You can manually launch Muse-IO with this command: \r\nmuse-io --preset 14 --device Muse --osc osc.tcp://localhost:5000", "Muse IO Command Line", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1);
            }
          }

          if (musePath != "")
          {
            museIO = Process.Start(musePath, "muse-io --preset 14 --device Muse --osc osc.tcp://localhost:5000");
          }

          oscServer = new OscServer(TransportType.Tcp, IPAddress.Loopback, 5000);

          oscServer.FilterRegisteredMethods = false;
          oscServer.RegisterMethod(AliveMethod);
          oscServer.RegisterMethod(TestMethod);
          oscServer.BundleReceived += oscServer_BundleReceived;
          oscServer.MessageReceived += oscServer_MessageReceived;
          oscServer.ConsumeParsingExceptions = false;

          oscServer.Start();

          m_boolInitialized = true;
          return true;
        }

        if (m_boolInitialized)
          return true;
      }
      catch (Exception ex)
      {
        m_boolInitError = true;
        throw (new Exception("The 'Muse' plugin failed to initialize: " + ex.Message));
      }
      return false;
    }

    private static void oscServer_BundleReceived(object sender, OscBundleReceivedEventArgs e)
    {
      sBundlesReceivedCount++;

      OscBundle bundle = e.Bundle;
      Console.WriteLine(string.Format("\nBundle Received [{0}:{1}]: Nested Bundles: {2} Nested Messages: {3}", bundle.SourceEndPoint.Address, bundle.TimeStamp, bundle.Bundles.Count, bundle.Messages.Count));
      Console.WriteLine("Total Bundles Received: {0}", sBundlesReceivedCount);
    }

    private static void oscServer_MessageReceived(object sender, OscMessageReceivedEventArgs e)
    {
      sMessagesReceivedCount++;

      OscMessage message = e.Message;

      //Console.WriteLine(string.Format("\nMessage Received [{0}]: {1}", message.SourceEndPoint.Address, message.Address));
      //Console.WriteLine(string.Format("Message contains {0} objects.", message.Data.Count));

      for (int i = 0; i < message.Data.Count; i++)
      {
        string dataString;

        if (message.Data[i] == null)
        {
          dataString = "Nil";
        }
        else
        {
          dataString = (message.Data[i] is byte[] ? BitConverter.ToString((byte[])message.Data[i]) : message.Data[i].ToString());
          
        }

        if (message.Address == "/muse/acc")
        {
          if (i == 0)
          {
            ACC1 = Convert.ToDouble(dataString);
          }
          if (i == 1)
          {
            ACC2 = Convert.ToDouble(dataString);
          }
          if (i == 3)
          {
            ACC3 = Convert.ToDouble(dataString);
          }
        }
        if (message.Address == "/muse/eeg")
        {
          if (i == 0)
          {
            EEG1 = Convert.ToDouble(dataString);
          }
          if (i == 1)
          {
            EEG2 = Convert.ToDouble(dataString);
          }
          if (i == 2)
          {
            EEG3 = Convert.ToDouble(dataString);
          }
          if (i == 3)
          {
            EEG4 = Convert.ToDouble(dataString);
          }
        }
        //Console.WriteLine(string.Format("[{0}]: {1}", i, dataString));
      }

      //Console.WriteLine("Total Messages Received: {0}", sMessagesReceivedCount);
    }

    static void _thinkGearWrapper_ThinkGearChanged(object sender, EventArgs e)
    {
      //m_dblLastAttention = e.ThinkGearState.Attention * 10;
      //m_dblBlinkStrength = e.ThinkGearState.BlinkStrength * 10;
      //m_dblLastMeditation = e.ThinkGearState.Meditation * 10;
      //m_dblLastAlpha = ((e.ThinkGearState.Alpha1 / 100) + (e.ThinkGearState.Alpha2 / 100)) / 2;
      //m_dblLastBeta = ((e.ThinkGearState.Beta1 / 100) + (e.ThinkGearState.Beta2 / 100)) / 2;
      //m_dblLastDelta = e.ThinkGearState.Delta / 10000;
      //m_dblLastGamma = ((e.ThinkGearState.Gamma1 / 100) + (e.ThinkGearState.Gamma2 / 100)) / 2;
      //m_dblLastTheta = e.ThinkGearState.Theta / 1000;

      if (ClearDisplay)
      {
        ClearDisplay = false;
        DisplayValue = 0;
      }

      if (ClearHighscore)
      {
        ClearHighscore = false;
        DisplayValue = 0;
      }

      //m_dblRaw += e.ThinkGearState.Raw;

      //if (e.ThinkGearState.Raw >= HighscoreValue)
      //{
      //  HighscoreValue = e.ThinkGearState.Raw;
      //}

      //if (e.ThinkGearState.Raw >= DisplayValue)
      //{
      //  DisplayValue = e.ThinkGearState.Raw;
      //}

      if (ThinkGearChanged != null)
      {
        ThinkGearChanged(sender, e);
      }
    }



    public static void Dispose()
    {
      if (!disposing)
      {
        disposing = true;
        oscServer.Stop();
        oscServer.BundleReceived -= oscServer_BundleReceived;
        oscServer.MessageReceived -= oscServer_MessageReceived;

        if (museIO != null)
        {
          museIO.CloseMainWindow();
        }
      }
    }

    public static Double GetEEG()
    {
      double temp = DisplayValue;
      ClearDisplay = true;
      return DisplayValue;
    }

    public static Double GetHighscore()
    {
      double temp = HighscoreValue;
      ClearHighscore = true;
      return HighscoreValue;
    }

    public static Double GetREM()
    {
      return 0;
    }

    public static Double GetAttention()
    {
      if (m_dblLastAttention > m_dblAttention)
      {
        m_dblAttention += (m_dblLastAttention / 100);
      }
      else
      {
        m_dblAttention -= (m_dblLastAttention / 100);
      }
      return m_dblAttention;
    }

    public static Double GetMeditation()
    {
      if (m_dblLastMeditation > m_dblMeditation)
      {
        m_dblMeditation += (m_dblLastMeditation / 100);
      }
      else
      {
        m_dblMeditation -= (m_dblLastMeditation / 100);
      }
      return m_dblMeditation;
    }

    public static Double GetAlpha()
    {
      if (m_dblLastAlpha > m_dblAlpha)
      {
        m_dblAlpha += (m_dblLastAlpha / 100);
      }
      else
      {
        m_dblAlpha -= (m_dblLastAlpha / 100);
      }
      return m_dblAlpha;
    }

    public static Double GetBeta()
    {
      if (m_dblLastBeta > m_dblBeta)
      {
        m_dblBeta += (m_dblLastBeta / 100);
      }
      else
      {
        m_dblBeta -= (m_dblLastBeta / 100);
      }
      return m_dblBeta;
    }

    public static Double GetDelta()
    {
      if (m_dblLastDelta > m_dblDelta)
      {
        m_dblDelta += (m_dblLastDelta / 100);
      }
      else
      {
        m_dblDelta -= (m_dblLastDelta / 100);
      }
      return m_dblDelta;
    }

    public static Double GetGamma()
    {
      if (m_dblLastGamma > m_dblGamma)
      {
        m_dblGamma += (m_dblLastGamma / 100);
      }
      else
      {
        m_dblGamma -= (m_dblLastGamma / 100);
      }
      return m_dblGamma;
    }

    public static Double GetTheta()
    {
      if (m_dblLastTheta > m_dblTheta)
      {
        m_dblTheta += (m_dblLastTheta / 100);
      }
      else
      {
        m_dblTheta -= (m_dblLastTheta / 100);
      }
      return m_dblTheta;
    }

    public static Double GetBlinkStrength()
    {
      return m_dblBlinkStrength;
    }



    public static Double GetACC1()
    {
      return ACC1;
    }
    public static Double GetACC2()
    {
      return ACC2;
    }
    public static Double GetACC3()
    {
      return ACC3;
    }

    public static Double GetEEG1()
    {
      return EEG1;
    }
    public static Double GetEEG2()
    {
      return EEG2;
    }
    public static Double GetEEG3()
    {
      return EEG3;
    }
    public static Double GetEEG4()
    {
      return EEG4;
    }
  }

  namespace EEG1
  {
    public class PluginHandler : lucidcode.LucidScribe.Interface.LucidPluginBase
    {
      public override string Name
      {
        get { return "Muse EEG1"; }
      }
      public override bool Initialize()
      {
        return Device.Initialize();
      }
      public override void Dispose()
      {
        Device.Dispose();
      }
      public override double Value
      {
        get
        {
          double dblValue = Device.GetEEG1();
          if (dblValue > 999) { dblValue = 999; }
          if (dblValue < 0) { dblValue = 0; }
          return dblValue;
        }
      }
    }
  }

  namespace EEG2
  {
    public class PluginHandler : lucidcode.LucidScribe.Interface.LucidPluginBase
    {
      public override string Name
      {
        get { return "Muse EEG2"; }
      }
      public override bool Initialize()
      {
        return Device.Initialize();
      }
      public override void Dispose()
      {
        Device.Dispose();
      }
      public override double Value
      {
        get
        {
          double dblValue = Device.GetEEG2();
          if (dblValue > 999) { dblValue = 999; }
          if (dblValue < 0) { dblValue = 0; }
          return dblValue;
        }
      }
    }
  }

  namespace EEG3
  {
    public class PluginHandler : lucidcode.LucidScribe.Interface.LucidPluginBase
    {
      public override string Name
      {
        get { return "Muse EEG3"; }
      }
      public override bool Initialize()
      {
        return Device.Initialize();
      }
      public override void Dispose()
      {
        Device.Dispose();
      }
      public override double Value
      {
        get
        {
          double dblValue = Device.GetEEG3();
          if (dblValue > 999) { dblValue = 999; }
          if (dblValue < 0) { dblValue = 0; }
          return dblValue;
        }
      }
    }
  }

  namespace EEG4
  {
    public class PluginHandler : lucidcode.LucidScribe.Interface.LucidPluginBase
    {
      public override string Name
      {
        get { return "Muse EEG4"; }
      }
      public override bool Initialize()
      {
        return Device.Initialize();
      }
      public override void Dispose()
      {
        Device.Dispose();
      }
      public override double Value
      {
        get
        {
          double dblValue = Device.GetEEG4();
          if (dblValue > 999) { dblValue = 999; }
          if (dblValue < 0) { dblValue = 0; }
          return dblValue;
        }
      }
    }
  }

  namespace ACC1
  {
    public class PluginHandler : lucidcode.LucidScribe.Interface.LucidPluginBase
    {
      public override string Name
      {
        get { return "Muse ACC1"; }
      }
      public override bool Initialize()
      {
        return Device.Initialize();
      }
      public override void Dispose()
      {
        Device.Dispose();
      }
      public override double Value
      {
        get
        {
          double dblValue = Device.GetACC1();
          if (dblValue > 999) { dblValue = 999; }
          if (dblValue < 0) { dblValue = 0; }
          return dblValue;
        }
      }
    }
  }

  namespace ACC2
  {
    public class PluginHandler : lucidcode.LucidScribe.Interface.LucidPluginBase
    {
      public override string Name
      {
        get { return "Muse ACC2"; }
      }
      public override bool Initialize()
      {
        return Device.Initialize();
      }
      public override void Dispose()
      {
        Device.Dispose();
      }
      public override double Value
      {
        get
        {
          double dblValue = Device.GetACC2();
          if (dblValue > 999) { dblValue = 999; }
          if (dblValue < 0) { dblValue = 0; }
          return dblValue;
        }
      }
    }
  }

  namespace ACC3
  {
    public class PluginHandler : lucidcode.LucidScribe.Interface.LucidPluginBase
    {
      public override string Name
      {
        get { return "Muse ACC3"; }
      }
      public override bool Initialize()
      {
        return Device.Initialize();
      }
      public override void Dispose()
      {
        Device.Dispose();
      }
      public override double Value
      {
        get
        {
          double dblValue = Device.GetACC3();
          if (dblValue > 999) { dblValue = 999; }
          if (dblValue < 0) { dblValue = 0; }
          return dblValue;
        }
      }
    }
  }

  //namespace RAW
  //{
  //  public class PluginHandler : lucidcode.LucidScribe.Interface.ILluminatedPlugin
  //  {
  //    public string Name
  //    {
  //      get { return "Muse RAW"; }
  //    }
  //    public bool Initialize()
  //    {
  //      bool initialized = Device.Initialize();
  //      Device.ThinkGearChanged += _thinkGearWrapper_ThinkGearChanged;
  //      return initialized;
  //    }

  //    public event Interface.SenseHandler Sensed;
  //    public void _thinkGearWrapper_ThinkGearChanged(object sender, Object e)
  //    {
  //      if (ClearTicks)
  //      {
  //        ClearTicks = false;
  //        TickCount = "";
  //      }
  //      //TickCount += e.ThinkGearState.Raw + ",";

  //      if (ClearBuffer)
  //      {
  //        ClearBuffer = false;
  //        BufferData = "";
  //      }
  //      //BufferData += e.ThinkGearState.Raw + ",";
  //    }

  //    public void Dispose()
  //    {
  //      Device.ThinkGearChanged -= _thinkGearWrapper_ThinkGearChanged;
  //      Device.Dispose();
  //    }

  //    public Boolean isEnabled = false;
  //    public Boolean Enabled
  //    {
  //      get
  //      {
  //        return isEnabled;
  //      }
  //      set
  //      {
  //        isEnabled = value;
  //      }
  //    }

  //    public Color PluginColor = Color.White;
  //    public Color Color
  //    {
  //      get
  //      {
  //        return Color;
  //      }
  //      set
  //      {
  //        Color = value;
  //      }
  //    }

  //    private Boolean ClearTicks = false;
  //    public String TickCount = "";
  //    public String Ticks
  //    {
  //      get
  //      {
  //        ClearTicks = true;
  //        return TickCount;
  //      }
  //      set
  //      {
  //        TickCount = value;
  //      }
  //    }

  //    private Boolean ClearBuffer = false;
  //    public String BufferData = "";
  //    public String Buffer
  //    {
  //      get
  //      {
  //        ClearBuffer = true;
  //        return BufferData;
  //      }
  //      set
  //      {
  //        BufferData = value;
  //      }
  //    }

  //    int lastHour;
  //    public int LastHour
  //    {
  //      get
  //      {
  //        return lastHour;
  //      }
  //      set
  //      {
  //        lastHour = value;
  //      }
  //    }
  //  }
  //}

  //namespace RapidEyeMovement
  //{
  //  public class PluginHandler : lucidcode.LucidScribe.Interface.LucidPluginBase
  //  {
  //    Thread ArduinoThread;
  //    List<int> m_arrHistory = new List<int>();
  //    public override string Name
  //    {
  //      get { return "Muse REM"; }
  //    }
  //    public override bool Initialize()
  //    {
  //      return Device.Initialize();
  //    }
  //    public override double Value
  //    {
  //      get
  //      {
  //        double dblEEG = Device.GetEEG();
  //        if (dblEEG > 999) { dblEEG = 999; }
  //        if (dblEEG < 0) { dblEEG = 0; }

  //        if (Device.Algorithm == "REM Detector")
  //        {
  //          // Update the mem list
  //          m_arrHistory.Add(Convert.ToInt32(dblEEG));
  //          if (m_arrHistory.Count > 512) { m_arrHistory.RemoveAt(0); }

  //          // Check for blinks
  //          int intBlinks = 0;
  //          bool boolBlinking = false;

  //          int intBelow = 0;
  //          int intAbove = 0;

  //          bool boolDreaming = false;
  //          foreach (Double dblValue in m_arrHistory)
  //          {
  //            if (dblValue > 800)
  //            {
  //              intAbove += 1;
  //              intBelow = 0;
  //            }
  //            else
  //            {
  //              intBelow += 1;
  //              intAbove = 0;
  //            }

  //            if (!boolBlinking)
  //            {
  //              if (intAbove >= 1)
  //              {
  //                boolBlinking = true;
  //                intBlinks += 1;
  //                intAbove = 0;
  //                intBelow = 0;
  //              }
  //            }
  //            else
  //            {
  //              if (intBelow >= 28)
  //              {
  //                boolBlinking = false;
  //                intBelow = 0;
  //                intAbove = 0;
  //              }
  //              else
  //              {
  //                if (intAbove >= 12)
  //                {
  //                  // reset
  //                  boolBlinking = false;
  //                  intBlinks = 0;
  //                  intBelow = 0;
  //                  intAbove = 0;
  //                }
  //              }
  //            }

  //            if (intBlinks > 6)
  //            {
  //              boolDreaming = true;
  //              break;
  //            }

  //            if (intAbove > 12)
  //            { // reset
  //              boolBlinking = false;
  //              intBlinks = 0;
  //              intBelow = 0;
  //              intAbove = 0; ;
  //            }
  //            if (intBelow > 80)
  //            { // reset
  //              boolBlinking = false;
  //              intBlinks = 0;
  //              intBelow = 0;
  //              intAbove = 0; ;
  //            }
  //          }

  //          if (boolDreaming)
  //          {
  //            Device.REMDetected = true;
  //            return 888;
  //          }
  //          else
  //          {
  //            if (Device.REMDetected)
  //            {
  //              Device.REMDetected = false;
  //            }
  //          }

  //          if (intBlinks > 10) { intBlinks = 10; }
  //          return intBlinks * 100;
  //        }

  //        return 0;
  //      }
  //    }
  //  }
  //}

  //namespace Alpha
  //{
  //  public class PluginHandler : lucidcode.LucidScribe.Interface.LucidPluginBase
  //  {
  //    public override string Name
  //    {
  //      get { return "Alpha"; }
  //    }
  //    public override bool Initialize()
  //    {
  //      return Device.Initialize();
  //    }
  //    public override double Value
  //    {
  //      get
  //      {
  //        double dblValue = Device.GetAlpha();
  //        if (dblValue > 999) { dblValue = 999; }
  //        return dblValue;
  //      }
  //    }
  //  }
  //}

  //namespace Beta
  //{
  //  public class PluginHandler : lucidcode.LucidScribe.Interface.LucidPluginBase
  //  {
  //    public override string Name
  //    {
  //      get { return "Beta"; }
  //    }
  //    public override bool Initialize()
  //    {
  //      return Device.Initialize();
  //    }
  //    public override double Value
  //    {
  //      get
  //      {
  //        double dblValue = Device.GetBeta();
  //        if (dblValue > 999) { dblValue = 999; }
  //        return dblValue;
  //      }
  //    }
  //  }
  //}

  //namespace Delta
  //{
  //  public class PluginHandler : lucidcode.LucidScribe.Interface.LucidPluginBase
  //  {
  //    public override string Name
  //    {
  //      get { return "Delta"; }
  //    }
  //    public override bool Initialize()
  //    {
  //      return Device.Initialize();
  //    }
  //    public override double Value
  //    {
  //      get
  //      {
  //        double dblValue = Device.GetDelta();
  //        if (dblValue > 999) { dblValue = 999; }
  //        return dblValue;
  //      }
  //    }
  //  }
  //}

  //namespace Gamma
  //{
  //  public class PluginHandler : lucidcode.LucidScribe.Interface.LucidPluginBase
  //  {
  //    public override string Name
  //    {
  //      get { return "Gamma"; }
  //    }
  //    public override bool Initialize()
  //    {
  //      return Device.Initialize();
  //    }
  //    public override double Value
  //    {
  //      get
  //      {
  //        double dblValue = Device.GetGamma();
  //        if (dblValue > 999) { dblValue = 999; }
  //        return dblValue;
  //      }
  //    }
  //  }
  //}

  //namespace Theta
  //{
  //  public class PluginHandler : lucidcode.LucidScribe.Interface.LucidPluginBase
  //  {
  //    public override string Name
  //    {
  //      get { return "Theta"; }
  //    }
  //    public override bool Initialize()
  //    {
  //      return Device.Initialize();
  //    }
  //    public override double Value
  //    {
  //      get
  //      {
  //        double dblValue = Device.GetTheta();
  //        if (dblValue > 999) { dblValue = 999; }
  //        return dblValue;
  //      }
  //    }
  //  }
  //}

}