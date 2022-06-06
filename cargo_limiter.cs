
using System.Text;
using Sandbox.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI;
using VRage.Game.GUI.TextPanel;

partial class Program: MyGridProgram
{

/////////////////////////////////////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////////////////////////////////    
    
//limit cargo by triggering connector on large grid
//Configuration variables

    string mainTag = "[CRG]";

    float cargo_high_limit = 88.0f;
    float cargo_low_limit = 68.0f;

    IMyTextSurface me_lcd;

    bool ready;
    bool ejectorStatus;

////////////////////////////////////////
//Variables stored in the Storage string

////////////////////////////////////////

//Block defining stuff

    List<IMyTerminalBlock> cargos;
    List<IMyShipConnector> ejectors;
    List<IMyTimerBlock> timers;

    public Program()
    {
        LoadFromStorage();

        //if (!ReadCustomData()) SetCustomData();

        // if (run)
        // {
        //     Runtime.UpdateFrequency = UpdateFrequency.Update100;
        //     if (use_dynamic_rotor_tensor)
        //     {
        //         tensor_counter = 38;
        //     }
        // }

        ready = RefreshBlocks();
        Save();

        if (ready)
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
        }
        else
        {
            Runtime.UpdateFrequency = UpdateFrequency.None;
        }

        me_lcd = Me.GetSurface(0);
        if (me_lcd.ContentType != ContentType.TEXT_AND_IMAGE)
        {
            me_lcd.ContentType = ContentType.TEXT_AND_IMAGE;
        }

        me_lcd.FontSize = 1.2F;
        //Echo = (String msg) => me_lcd.WriteText(msg, false);
    }

    public void Save()
    {
        Storage = $"{ready};{ejectorStatus}";
        Echo("Data Saved!");
    }


    public void Main(string argument, UpdateType updateSource)
    {
        try
        {
            if ((updateSource | UpdateType.Terminal) == UpdateType.Terminal)
            {
                ready = RefreshBlocks();
                Save();
            }
            if (ready)
            {
                float load = GetCargoLoad();
                var output = new StringBuilder($"Cargo {mainTag}: {BuildBar(load, 5f)}");
                output.AppendLine($"Cargo(s): {cargos.Count}");
                if (ejectors.Count > 0) output.AppendLine($"Ejector(s): {ejectors.Count}");
                if (timers.Count > 0) output.AppendLine($"Timer(s): {timers.Count}");
                if (!ejectorStatus && (load > cargo_high_limit))
                {
                    ejectorStatus = true;
                    Save();
                    SetEjectors(ejectorStatus);
                }
                else if (ejectorStatus && (load < cargo_low_limit))
                {
                    ejectorStatus = false;
                    Save();
                    SetEjectors(ejectorStatus);
                }

                output.AppendLine(ejectorStatus ? "Ejectors Activated" : "Ejectors Deactivated");
                me_lcd.WriteText(output, false);
            }
        }
        catch (Exception ex)
        {
            Echo(ex.ToString());
        }
    }

    void SetEjectors(bool throwOutState)
    {
        foreach (IMyShipConnector c in ejectors)
        {
            c.ThrowOut = throwOutState;
        }

        foreach (IMyTimerBlock t in timers)
        {
            StartTimer(t);
        }
    }

    void LoadFromStorage()
    {
        String[] data = Storage.Split(';');
        if (data.Length > 1)
        {
            if (!Boolean.TryParse(data[0], out ready))
            {
                Echo("Converting |ready| failed!");
            }

            if (!Boolean.TryParse(data[1], out ejectorStatus))
            {
                Echo("Converting |ejectorStatus| failed!");
            }

            Echo("Data Loaded!");
        }
        else
        {
            Echo("Load Failed!");
        }
    }


    bool RefreshBlocks()
    {
        List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
        cargos = new List<IMyTerminalBlock>();
        ejectors = new List<IMyShipConnector>();
        timers = new List<IMyTimerBlock>();

        GridTerminalSystem.SearchBlocksOfName(mainTag, blocks);
        foreach (IMyTerminalBlock a in blocks)
        {
            IMyShipConnector connector = a as IMyShipConnector;
            if (connector != null)
            {
                ejectors.Add(connector);
            }
            else if (a.HasInventory)
            {
                cargos.Add(a);
            }
            else
            {
                var timer = a as IMyTimerBlock;
                if (timer != null)
                {
                    timers.Add(timer);
                }
            }
        }

        if (cargos.Count >= 1)
        {
            Echo("Cargo Module: " + cargos.Count);
        }
        else
        {
            Echo("No cargo detected");
            return false;
        }

        if (ejectors.Count >= 1)
        {
            Echo("Connector Module: " + ejectors.Count);
            return true;
        }
        else
        {
            Echo("No connector detected");
            return false;
        }
    }


    float GetCargoLoad()
    {
        VRage.MyFixedPoint maxVolume = 0;
        VRage.MyFixedPoint curVolume = 0;
        foreach (IMyTerminalBlock a in cargos)
        {
            maxVolume += a.GetInventory(0).MaxVolume;
            curVolume += a.GetInventory(0).CurrentVolume;
        }

        return (float)Math.Round((float)curVolume / (float)maxVolume * 100.0f);
    }

    String BuildBar(float cargoLoad, float barSize = 2.5f)
    {
        int progr = (int)Math.Round(cargoLoad / barSize);
        var message = new StringBuilder("[");
        message.Append(new string('|', progr));
        message.Append(new string('\'', (int)(100f / barSize) - progr));
        message.AppendLine($"] {cargoLoad}%");
        return message.ToString();
    }

    void StartTimer(IMyTimerBlock timer)
    {
        timer.Enabled = true;
        timer.GetActionWithName("Start").Apply(timer);
    }
}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////////////////////////////////

//
//
// public void write_screen(bool log = false)
// {
//     if (!log)
//     {
//         if (use_lcd_color_coding)
//         {
//             foreach (IMyTextSurface a in screens)
//             {
//                 a.FontColor = lcd_color;
//                 a.WriteText(message, false);
//             }
//         }
//         else
//         {
//             foreach (IMyTextSurface a in screens)
//             {
//                 a.WriteText(message, false);
//             }
//         }
//     }
//     else
//     {
//         foreach (IMyTextPanel a in screens)
//         {
//             a.WriteText(message, true);
//         }
//     }
//
// }
//
// public void SendMessage()
// {
//     if (antenna != null && antenna.Enabled && antenna.EnableBroadcasting)
//     {
//         IGC.SendUnicastMessage(receiver_address, mainTag, BuildMessage(GetCargoLoad()));
//         Echo("Message Sent!");
//     }
//     else
//     {
//         Echo("Error! Antenna isn't broadcasting!");
//     }
// }
//
// public bool ReadCustomData()
// {
//     if (Me.CustomData.StartsWith("@Configuration"))
//     {
//         string[] config = Me.CustomData.Split('|');
//         if (config.Length == 64)
//         {
//             bool result = true;
//             mainTag = config[2];
//
//             if (!Boolean.TryParse(config[4], out use_auto_pause)) { Echo("Getting use_auto_pause failed!"); result = false; }
//             if (!Single.TryParse(config[6], out cargo_high_limit)) { Echo("Getting cargo_high_limit failed!"); result = false; }
//             if (!Single.TryParse(config[8], out cargo_low_limit)) { Echo("Getting cargo_low_limit failed!"); result = false; }
//             if (!Boolean.TryParse(config[10], out show_advanced_data)) { Echo("Getting show_advanced_data failed!"); result = false; }
//             if (!Boolean.TryParse(config[12], out use_lcd_color_coding)) { Echo("Getting use_lcd_color_coding failed!"); result = false; }
//             if (!Boolean.TryParse(config[14], out share_inertia_tensor)) { Echo("Getting share_inertia_tensor failed!"); result = false; }
//             if (!Boolean.TryParse(config[16], out use_dynamic_rotor_tensor)) { Echo("Getting use_dynamic_rotor_tensor failed!"); result = false; }
//             if (!Int64.TryParse(config[18], out receiver_address)) { Echo("Getting receiver_address failed!"); result = false; }
//
//             if (!Single.TryParse(config[20], out max_rot_angle)) { Echo("Getting max_rot_angle failed!"); result = false; }
//             if (!Single.TryParse(config[22], out min_rot_angle)) { Echo("Getting min_rot_angle failed!"); result = false; }
//             if (!Single.TryParse(config[24], out excess_meters)) { Echo("Getting excess_meters failed!"); result = false; }
//             if (!Single.TryParse(config[26], out vp_step_length)) { Echo("Getting vp_step_length failed!"); result = false; }
//             if (!Boolean.TryParse(config[28], out use_unique_hp_step_length)) { Echo("Getting use_unique_hp_step_length failed!"); result = false; }
//             if (!Single.TryParse(config[30], out hp_step_length)) { Echo("Getting hp_step_length failed!"); result = false; }
//             if (!Boolean.TryParse(config[32], out always_retract_hpistons)) { Echo("Getting always_retract_hpistons failed!"); result = false; }
//
//             if (!Boolean.TryParse(config[34], out use_unique_piston_limits)) { Echo("Getting use_unique_piston_limits failed!"); result = false; }
//             if (!Single.TryParse(config[36], out max_vp_limit)) { Echo("Getting max_vp_limit failed!"); result = false; }
//             if (!Single.TryParse(config[38], out min_vp_limit)) { Echo("Getting min_vp_limit failed!"); result = false; }
//             if (!Single.TryParse(config[40], out max_vp_limit_inv)) { Echo("Getting max_vp_limit_inv failed!"); result = false; }
//             if (!Single.TryParse(config[42], out min_vp_limit_inv)) { Echo("Getting min_vp_limit_inv failed!"); result = false; }
//             if (!Single.TryParse(config[44], out max_hp_limit)) { Echo("Getting max_hp_limit failed!"); result = false; }
//             if (!Single.TryParse(config[46], out min_hp_limit)) { Echo("Getting min_hp_limit failed!"); result = false; }
//             if (!Single.TryParse(config[48], out vp_vel)) { Echo("Getting vp_vel failed!"); result = false; }
//             if (!Single.TryParse(config[50], out hp_vel)) { Echo("Getting hp_vel failed!"); result = false; }
//             if (!Single.TryParse(config[52], out rotor_vel_at_10m)) { Echo("Getting rotor_vel_at_10m failed!"); result = false; }
//             if (!Single.TryParse(config[54], out piston_length)) { Echo("Getting piston_length failed!"); result = false; }
//
//             hp_tag = config[56];
//             vp_tag = config[58];
//             inv_tag = config[60];
//             adv_tag = config[62];
//             if (result)
//             {
//                 Echo("Configuration Done!");
//                 return true;
//             }
//             else
//             {
//                 Echo("Configuration Error!");
//                 return false;
//             }
//         }
//         else
//         {
//             Echo("Getting Configuration failed!");
//             return false;
//         }
//     }
//     else
//     {
//         Echo("Getting Configuration failed!");
//         return false;
//     }
// }
//
// public void SetCustomData()
// {
//     Me.CustomData = "@Configuration\n" +
//                     "You can configure the script right below here,\n" +
//                     "by changing the values between then | characters.\n\n" +
//
//                     "The configuration will be loaded if you click Check Code\n" +
//                     "in the Code Editor inside the Programmable Block,\n" +
//                     "when the game Saves/Loads or if you use the\n" +
//                     "Set or the Refresh command.\n\n" +
//
//                     "There is a detailed explanation about what's what, inside the script.\n\n" +
//
//                     "Main Tag: |" + mainTag + "|\n\n" +   //2 string
//
//                     "///////////////////////////////////////////\n" +
//                     "2.) Basic Configuration\n\n" +
//
//                     "- You can change these at any point:\n\n" +
//
//                     "Use Auto Pause: |" + use_auto_pause + "|\n" +   //4 bool
//                     "High Cargo Threshold: |" + cargo_high_limit + "|\n" +   //6 double
//                     "Low Cargo Threshold: |" + cargo_low_limit + "|\n" +   //8  double
//                     "Show Advanced Data: |" + show_advanced_data + "|\n" +   //10 bool
//                     "Use LCD Color Coding: |" + use_lcd_color_coding + "|\n" +    //12 bool
//                     "Use Share Inertia Tensor: |" + share_inertia_tensor + "|\n" +   //14 bool
//                     "Use Dynamic Rotor Inertia Tensor: |" + use_dynamic_rotor_tensor + "|\n" +   //16 bool
//                     "Transmission Receiver Address: |" + receiver_address + "|\n\n" +  //18 long int
//
//                     "- Don't change these while a mining is in progress:\n\n" +
//
//                     "Max Rotor Angle: |" + max_rot_angle + "|\n" + //20 double
//                     "Min Rotor Angle: |" + min_rot_angle + "|\n" + //22 double
//                     "Non-Piston Blocks in Rotating Arm in Meters: |" + excess_meters + "|\n" +   //24 double
//                     "Vertical Piston Step Length: |" + vp_step_length + "|\n" +  //26 double
//                     "Use Unique Horizontal Step Length: |" + use_unique_hp_step_length + "|\n" +    //28 bool
//                     "Horizontal Piston Step Length: |" + hp_step_length + "|\n" +    //30 double
//                     "Retract Horizontal Piston before Vertical Step: |" + always_retract_hpistons + "|\n" +  //32 bool
//                     "///////////////////////////////////////////\n" +
//                     "4.) Advanced Configuration\n\n" +
//
//                     "- Don't change these while a mining is in progress:\n\n" +
//
//                     "Use Unique Piston Limits: |" + use_unique_piston_limits + "|\n" +   //34    bool
//                     "Max Vertical Piston Limit: |" + max_vp_limit + "|\n" +  //36    double
//                     "Min Vertical Piston Limit: |" + min_vp_limit + "|\n" +  //38    double
//                     "Max Vertical Inv Piston Limit: |" + max_vp_limit_inv + "|\n" +  //40    double
//                     "Min Vertical Inv Piston Limit: |" + min_vp_limit_inv + "|\n" +  //42    double
//                     "Max Horizontal Piston Limit: |" + max_hp_limit + "|\n" +  //44    double
//                     "Min Horizontal Piston Limit: |" + min_hp_limit + "|\n" +  //46    double
//                     "Vertical Piston Speed: |" + vp_vel + "|\n" +    //48    double
//                     "Horizontal Piston Speed: |" + hp_vel + "|\n" +    //50    double
//                     "Rotor Rotation Speed at 10m: |" + rotor_vel_at_10m + "|\n" +    //52    double
//                     "Piston Body Length in Meters: |" + piston_length + "|\n\n" +    //54    double
//
//                     "Horizontal Piston Tag: |" + hp_tag + "|\n" +  //56 string
//                     "Vertical Piston Tag: |" + vp_tag + "|\n" +  //58 string
//                     "Inverted Piston Tag: |" + inv_tag + "|\n" +  //60 string
//                     "Timer Advanced Tag: |" + adv_tag + "|";   //62 string
//
//     Echo("Configuration Set to Custom Data!");
// }

