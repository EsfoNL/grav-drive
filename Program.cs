using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {


        public Program()
        {

            Runtime.UpdateFrequency = UpdateFrequency.Update1;
        }

        public void Save()
        {

        }

        Vector3D Vp = new Vector3();
        Vector3D pastCumControl = new Vector3D(1, 1, 1);

        public void Main(string argument, UpdateType updateSource)
        {
            var list = new List<IMyCockpit>();
            var gravs = new List<IMyGravityGenerator>();
            bool stablize = false;

            GridTerminalSystem.GetBlockGroupWithName("gravity drivers").GetBlocksOfType(gravs);
            GridTerminalSystem.GetBlocksOfType(list);

            var cumControl = new Vector3D();

            foreach (var controller in list)
            {
                if (controller.SurfaceCount > 0)
                {
                    var surf = controller.GetSurface(0);
                    stablize = controller.DampenersOverride;

                    var indicator = controller.MoveIndicator;

                    var orientation = new Quaternion();
                    controller.Orientation.GetQuaternion(out orientation);
                    var invertedorientation = Quaternion.Conjugate(orientation);

                    // Echo(Quaternion.Concatenate(orientation, invertedorientation).ToString());

                    var correctedIndicator = Vector3.Transform(indicator, orientation);
                    cumControl += correctedIndicator;



                    surf.ContentType = ContentType.TEXT_AND_IMAGE;
                    surf.WriteText($"{correctedIndicator}, {controller.GetShipVelocities().LinearVelocity}", false);
                }
            }

            var V = list[0].GetShipVelocities().LinearVelocity;
            var dV = Vp - V;

            var effect = dV / pastCumControl * 20;
            if (!effect.IsValid()) effect = new Vector3D(0.0001, 0.0001, 0.0001);
            Echo(effect.ToString());
            const float Epsilon = 0.01f;


            if (list[0].DampenersOverride && V.LengthSquared() > 1) {
                if(Math.Abs(cumControl.X) < Epsilon && Math.Abs(V.X) > Epsilon) {
                    cumControl.X = V.X/effect.X;
                }
                if(Math.Abs(cumControl.Y) < Epsilon && Math.Abs(V.Y) > Epsilon) {
                    cumControl.Y = V.Y/effect.Y;
                }
                if(Math.Abs(cumControl.Z) < Epsilon && Math.Abs(V.Z) > Epsilon) {
                    cumControl.Z = V.Z/effect.Z;
                }
                
            }


            Vp = V;
            pastCumControl = cumControl;
            if (!pastCumControl.IsValid()) pastCumControl = new Vector3D(1, 1, 1);

            if (!cumControl.IsValid()) return;

            foreach (var grav in gravs)
            {
                var val = Base6Directions.GetVector(grav.Orientation.Up).Dot(cumControl) * -9.81f;
                if (float.IsNaN(val) || float.IsInfinity(val)) {
                    grav.GravityAcceleration = 0;
                } else {
                    grav.GravityAcceleration = val;
                }

            }

        }
    }
}
