using System.Text;
using System.Text.RegularExpressions;
using Sandbox.Engine.Physics;
using Sandbox.ModAPI.Ingame;
using VRage.Game;
using VRage.Game.GUI.TextPanel;
using VRageMath;

namespace SpaceEngineersScripts.raycast
{

   public class Program : MyGridProgram
   {

      /////////////////////////////////////////////////////////////////////////////////////////////////////////////
      /////////////////////////////////////////////////////////////////////////////////////////////////////////////
      //////////////////////////////////////////////////////////////////////////////////////////////////////////

      private string mainTag = "[RAY]";
      private IMyCameraBlock camera;
      private List<IMyPistonBase> pistons;
      private List<IMyTextSurface> displays;
      private const float PISTON_VELOCITY = 0.4f;

      private bool ready;
      private float blockSize;
      private float drillLength;
      private float maxRange;
      private float minRange; 

      //Large Block 2.5m3
      //Small Block 0.5m3

      public Program()
      {
         ready = RefreshBlocks();
         if (ready)
         {
            Initialize();
         }
      }

      private void Initialize()
      {
         if (!ready) return;
         displays.Add(Me.GetSurface(0));
         foreach (IMyTextSurface lcd in displays)
         {
            if (lcd.ContentType != ContentType.TEXT_AND_IMAGE)
            {
               lcd.ContentType = ContentType.TEXT_AND_IMAGE;
            }

            lcd.FontSize = .8f;
            lcd.Font = "DEBUG";
         }

         Echo("all components are set");
         if (!camera.EnableRaycast)
            camera.EnableRaycast = true;
         blockSize = camera.CubeGrid.GridSizeEnum == MyCubeSize.Large ? 2.5f : 0.5f;
         drillLength = 6;
         maxRange = pistons.Count * 6 * blockSize + drillLength;
         minRange = pistons.Count * 2 * blockSize + drillLength;
      }

      public void Main(string argument, UpdateType updateSource)
      {
         switch ((argument ?? "").ToLower())
         {
            case "loop":
               ready = RefreshBlocks();
               if (ready)
               {
                  Initialize();
                  Execute();
                  Runtime.UpdateFrequency = UpdateFrequency.Update100;
               }

               break;
            case "stop":
               Runtime.UpdateFrequency = UpdateFrequency.None;
               break;
            case "run":
            case "step":
               Runtime.UpdateFrequency = UpdateFrequency.None;
               ready = RefreshBlocks();
               Initialize();
               Execute();
               break;
            case "":
               if ((updateSource & UpdateType.Terminal) == UpdateType.Terminal)
               {
                  ready = RefreshBlocks();
                  Initialize();
                  Execute();
               }
               else
               {
                  Execute();
               }

               break;
         }
      }

      private void Execute()
      {
         if (!ready) return;
         float currentLength =
            pistons.Select(p => p.CurrentPosition).Sum() + pistons.Count * 2 * blockSize + drillLength;
         StringBuilder output = new StringBuilder();
         output.AppendLine($"maxRange: {maxRange}");
         output.AppendLine($"minRange: {minRange}");
         output.AppendLine("camera");
         output.AppendLine($"Gposition: {camera.Position.X:F2} {camera.Position.Y:F2} {camera.Position.Z:F2}");
         var wPosition = camera.GetPosition();
         output.AppendLine($"Wposition: {wPosition.X:F2} {wPosition.Y:F2} {wPosition.Z:F2}");
         output.AppendLine($"current length: {currentLength}");
         HitDetectionInfo? hitInfo = RunScan();
         Color? textColor = null;
         if (hitInfo.HasValue)
         {
            
            var distance = hitInfo.Value.Distance;
            output.AppendLine($"Hit (angle): {hitInfo.Value.Angle}");
            output.AppendLine($"Hit (world): {hitInfo.Value.Position.X:F2} {hitInfo.Value.Position.Y:F2} {hitInfo.Value.Position.Z:F2}");
            output.AppendLine($"distance: {distance}");
            float delta = distance - currentLength;
            if (Math.Abs(delta) > 1f)
            {
               if (currentLength < distance && (hitInfo.Value.Type == MyDetectedEntityType.Planet ||
                                                hitInfo.Value.Type == MyDetectedEntityType.Asteroid))
               {
                  float extension = (distance - currentLength + .5f) / pistons.Count;
                  output.AppendLine($"Extending each piston by {extension}");
                  textColor = Color.Aqua;
                  foreach (IMyPistonBase piston in pistons)
                  {
                     float maxLimit = Math.Min(10f, piston.MaxLimit + extension);
                     piston.MaxLimit = maxLimit;
                     piston.Velocity = PISTON_VELOCITY;
                  }
               }
               else if (currentLength > distance)
               {
                  float extension = (currentLength - distance - .5f) / pistons.Count;
                  output.AppendLine($"retracting each piston by {extension}");
                  textColor = Color.Yellow;
                  foreach (IMyPistonBase piston in pistons)
                  {
                     float minLimit = Math.Min(0f, piston.MinLimit - extension);
                     piston.MinLimit = minLimit;
                     piston.Velocity = -2;
                  }
               }
            }
         }
         else
         {
            textColor = Color.DarkGray;
            output.AppendLine("no rock in range");
         }

         WriteText(output, textColor);
      }

      private void WriteText(StringBuilder output, Color? color = null)
      {
         if (!color.HasValue)
         {
            color = Color.White;
         }

         foreach (IMyTextSurface lcd in displays)
         {
            lcd.FontColor = color.Value;
            lcd.WriteText(output);
         }
      }

      private HitDetectionInfo? RunScan()
      {
         MyDetectedEntityInfo? minHitInfo = null;
         float minPitch = 0f;
         if (camera.CanScan(maxRange + blockSize))
         {
            float minDistance = float.MaxValue;
            float angleRange = Math.Min(25, camera.RaycastConeLimit);
            for (float pitch = -angleRange; pitch < angleRange; pitch += 10)
            {
               var hitInfo = camera.Raycast(maxRange + blockSize, pitch);
               if (!hitInfo.IsEmpty())
               {
                  var distance = (float)Vector3D.Distance(camera.GetPosition(), hitInfo.HitPosition.Value) ;
                  if (distance < minDistance)
                  {
                     minPitch = pitch;
                     minDistance = distance;
                     minHitInfo = hitInfo;
                  }
               }
            }
         }
         else
         {
            Echo("out of Camera Range !!");
         }
         return minHitInfo.HasValue ? (HitDetectionInfo?)new HitDetectionInfo(camera.GetPosition(), minPitch, minHitInfo.Value) : null;
      }

      bool RefreshBlocks()
      {
         var blocks = new List<IMyTerminalBlock>();
         displays = new List<IMyTextSurface>();
         pistons = new List<IMyPistonBase>();
         camera = null;

         GridTerminalSystem.SearchBlocksOfName(mainTag, blocks);
         foreach (IMyTerminalBlock a in blocks)
         {
            IMyCameraBlock cameraInput = a as IMyCameraBlock;
            if (null != cameraInput)
            {
               if (null == camera)
               {
                  camera = cameraInput;
               }
               else
               {
                  Echo("too many camera");
                  return false;
               }
            }
            else
            {
               IMyPistonBase piston = a as IMyPistonBase;
               if (null != piston)
               {
                  pistons.Add(piston);
               }
               else
               {
                  IMyTextSurface lcdInput = a as IMyTextSurface;
                  if (null != lcdInput)
                  {
                     displays.Add(lcdInput);
                  }
                  else
                  {
                     Echo($"{a.CustomName}: unhandled tagged block");
                  }
               }
            }
         }

         if (null == camera)
         {
            Echo("no camera tagged");
            return false;
         }

         if (pistons.Count == 0)
         {
            Echo("no piston tagged");
            return false;
         }
         else
         {
            Echo($"{pistons.Count} piston(s) identified");
         }

         Echo($"{displays.Count} display(s) identified");

         return true;
      }

      private struct HitDetectionInfo
      {
         public readonly Vector3D Position;
         public readonly float Distance;
         public readonly MyDetectedEntityType Type;
         public readonly float Angle;

         public HitDetectionInfo (Vector3D camera, float cameraRayCastAngle, MyDetectedEntityInfo hitInfo)
         {
            Position = hitInfo.HitPosition.Value;
            var cosinus = cameraRayCastAngle == 0f ? 1f : (float)Math.Cos(MathHelper.ToRadians(cameraRayCastAngle));
            Distance = (float)Vector3D.Distance(camera, hitInfo.HitPosition.Value) * cosinus;
            Type = hitInfo.Type;
            Angle = cameraRayCastAngle;
         }
      }

      /////////////////////////////////////////////////////////////////////////////////////////////////////////////
      /////////////////////////////////////////////////////////////////////////////////////////////////////////////
      //////////////////////////////////////////////////////////////////////////////////////////////////////////

      IEnumerable<TBlock> GetBlocks<TBlock>(Func<IMyTerminalBlock, bool> filter) where TBlock : class
      {
         var blocks = new List<IMyTerminalBlock>();
         GridTerminalSystem.GetBlocksOfType<TBlock>(blocks, filter);
         return blocks.Cast<TBlock>();
      }

      Func<IMyTerminalBlock, bool> WithName(string namePattern)
      {
         return b => Regex.IsMatch(b.CustomName, namePattern,
            RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);
      }
   }
}