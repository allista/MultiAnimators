//   MultiLights.cs
//
//  Author:
//       Allis Tauri <allista@gmail.com>
//
//  Copyright (c) 2016 Allis Tauri

using System;
using System.Collections.Generic;
using UnityEngine;

namespace AT_Utils
{
    /// <summary>
    /// This is, again, a much simpler module than the stock ModuleLight, but with
    /// all the benefits of the MultiAnimator. 
    /// </summary>
    public class MultiLights : MultiAnimator
    {
        [KSPField] public float RangeMultiplier = 1f;
        [KSPField] public string LightNames = string.Empty;
        readonly List<Light> lights = new List<Light>();
        readonly Dictionary<int,float> ranges = new Dictionary<int, float>();

        public override string GetInfo()
        {
            var info = base.GetInfo();
            if(info != string.Empty) info += "\n";
            info += string.Format("Energy Consumption: {0}/sec", EnergyConsumption);
            return info;
        }

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            if(EnergyConsumption <= 0f) 
                EnergyConsumption = 0.01f;
        }

        public override void OnStart (StartState state)
        {
            //get lights, save their ranges and apply range multiplier
            foreach(var l in LightNames.Split(new []{' '}, StringSplitOptions.RemoveEmptyEntries))
                lights.AddRange(part.FindModelComponents<Light>(l));
            lights.ForEach(l => ranges[l.GetInstanceID()] = l.range);
#if DEBUG
            this.Log("OnStart: '{}' lights: {}", LightNames, lights);
            lights.ForEach(l => Utils.Log($"{l} [{l.enabled}]: range {l.range}, intensity {l.intensity}, color {l.color}"));
#endif
            UpdateLights();
            //default labels
            if(OpenEventGUIName  == string.Empty) OpenEventGUIName  = "Lights On";
            if(CloseEventGUIName == string.Empty) CloseEventGUIName = "Lights Off";
            if(ActionGUIName     == string.Empty) ActionGUIName     = "Toggle Lights";
            Actions["ToggleAction"].actionGroup = KSPActionGroup.Light;
            AllowWhileShielded = true;
            base.OnStart(state);
        }

        public void UpdateLights()
        { lights.ForEach(l => l.range = ranges[l.GetInstanceID()]*RangeMultiplier); }

        protected override void consume_energy()
        {
            if(State != AnimatorState.Opened && State != AnimatorState.Opening) return;
            socket.RequestTransfer(EnergyConsumption*TimeWarp.fixedDeltaTime);
            if(!socket.TransferResource()) return;
            if(socket.PartialTransfer) { Close(); update_events(); socket.Clear(); }
        }

        public override void FixedUpdate()
        { if(HighLogic.LoadedSceneIsFlight) consume_energy(); }
    }

    public class HangarLightUpdater : ModuleUpdater<MultiLights>
    {
        protected override void on_rescale(ModulePair<MultiLights> mp, Scale scale)
        { 
            mp.module.RangeMultiplier = mp.base_module.RangeMultiplier * scale;
            mp.module.UpdateLights(); 
        }
    }
}

