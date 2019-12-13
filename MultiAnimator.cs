//   MultiAnimator.cs
//
//  Author:
//       Allis Tauri <allista@gmail.com>
//
//  Copyright (c) 2016 Allis Tauri

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AT_Utils
{
    /// <summary>
    /// It is much less sophisticated than the stock ModuleAnimateGeneric, but has two key differences:
    /// first, it supports multiple different animations and it uses ALL the animations of the same name 
    /// (think of a composite part that uses the same model with an animation several times);
    /// second, it also allows for sound and particle emitter to accompany the animation, 
    /// and not preconfigured event-based, but realtime adjusted.
    /// </summary>
    public class MultiAnimator : SerializableFiledsPartModule, IAnimator, IResourceConsumer, IScalarModule
    {
        //animation
        [KSPField(isPersistant = true)]  public AnimatorState State;
        [KSPField(isPersistant = false)] public string AnimatorID = "_none_";

        public string GetAnimatorID() => AnimatorID;
        public AnimatorState GetAnimatorState() => State;

        [KSPField(isPersistant = false)] public string OpenEventGUIName = "";
        [KSPField(isPersistant = false)] public string CloseEventGUIName = "";
        [KSPField(isPersistant = false)] public string ActionGUIName = "";
        [KSPField(isPersistant = false)] public string StopTimeGUIName = "";

        [KSPField(isPersistant = false)] public string AnimationNames = "";
        [KSPField(isPersistant = false)] public float  ForwardSpeed = 1f;
        [KSPField(isPersistant = false)] public float  ReverseSpeed = 1f;
        [KSPField(isPersistant = false)] public bool   Loop;
        [KSPField(isPersistant = false)] public bool   Reverse;
        [KSPField(isPersistant = false)] public float  EnergyConsumption;
        [KSPField(isPersistant = false)] public bool   AllowWhileShielded;
        [KSPField(isPersistant = true)]  public float  progress;
        protected float last_progress;

        [KSPField(isPersistant=true, guiActiveEditor=false, guiActive = false, guiName="Stop At", guiFormat="F1")]
        [UI_FloatEdit(scene=UI_Scene.All, minValue=0f, maxValue=100f, incrementLarge=20f, incrementSmall=10f, incrementSlide=1f, unit = "%")]
        public float StopTime = 100.0f;

        public float Duration { get; protected set; }
        public bool  Playing { get { return State == AnimatorState.Opening || State == AnimatorState.Closing; } }
        protected List<AnimationState> animation_states = new List<AnimationState>();
        //emitter
        protected KSPParticleEmitter emitter;
        protected readonly int[] base_emission = new int[2];
        float speed_multiplier = 1f;
        //sound
        [KSPField] public string Sound = string.Empty;
        [KSPField] public float  MaxDistance = 30f;
        [KSPField] public float  MaxVolume   = 1f;
        [KSPField] public float  MinVolume   = 0.1f;
        [KSPField] public float  MaxPitch    = 1f;
        [KSPField] public float  MinPitch    = 0.1f;
        public FXGroup fxSound;
        //energy consumption
        protected ResourcePump socket;

        public virtual void SetSpeedMultiplier(float mult)
        {
            speed_multiplier = mult;
            update_sound_params();
            update_emitter();
        }

        protected virtual void onPause()
        {
            if(fxSound.audio == null) return;
            if(fxSound.audio.isPlaying) fxSound.audio.Pause();
        }

        protected virtual void onUnpause()
        {
            if(fxSound.audio == null) return;
            if(Playing) fxSound.audio.Play();
        }

        public override void OnAwake()
        { 
            base.OnAwake();
            GameEvents.onGamePause.Add(onPause);
            GameEvents.onGameUnpause.Add(onUnpause);
        }

        public virtual void OnDestroy()
        {
            GameEvents.onGamePause.Remove(onPause);
            GameEvents.onGameUnpause.Remove(onUnpause);
        }

        protected void setup_animation()
        {
            //animations
            animation_states.Clear();
            foreach(var aname in Utils.ParseLine(AnimationNames, Utils.Whitespace))
            {
                var animations = part.FindModelAnimators(aname);
#if DEBUG
                this.Log("setup_animation: '{}' animation: {}", aname, animations);
#endif
                if(animations == null || animations.Length == 0)
                {
                    this.Log("setup_animation: there's no '{}' animation in {}", 
                             aname, part.Title());
                    continue;
                }
                foreach(Animation anim in animations)
                {
                    var animationState = anim[aname];
                    if(animationState == null) continue;
                    animationState.speed = 0;
                    animationState.enabled = true;
                    animationState.wrapMode = WrapMode.ClampForever;
                    anim.Blend(aname);
                    animation_states.Add(animationState);
                }
            }
            Duration = animation_states.Aggregate(0f, (d, s) => Math.Max(d, s.length));
            if(Duration.Equals(0))
                this.Log("setup_animation: animation length is zero!\n" +
                         "Part: {}, AnimationNames: {}", 
                         part.Title(), AnimationNames);
            //emitter
            emitter = part.FindModelComponents<KSPParticleEmitter>().FirstOrDefault();
            if(emitter != null) 
            {
                base_emission[0] = emitter.minEmission;
                base_emission[1] = emitter.maxEmission;
            }
            //initialize sound
            if(Sound != string.Empty)
            {
                Utils.createFXSound(part, fxSound, Sound, true, MaxDistance);
                fxSound.audio.volume = GameSettings.SHIP_VOLUME * MaxVolume;
            }
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            Duration = 0f;
            if(State == AnimatorState.Opened) progress = 1f;
            setup_animation();
            set_progress(progress);
            if(EnergyConsumption > 0) 
                socket = part.CreateSocket();
            //GUI
            Events["Toggle"].guiName        = OpenEventGUIName;
            Actions["ToggleAction"].guiName = ActionGUIName;
            Fields["StopTime"].guiName      = StopTimeGUIName;
            update_events();
        }

        protected float ntime 
        { 
            get 
            { 
                var p = Reverse? 1-progress : progress;
                return Mathf.Clamp01(p*StopTime/100f);
            }
        }

        protected void set_progress(float p, bool update_state = true)
        {
            progress = p;
            seek(ntime, update_state);
            if(update_state) on_stop.Fire(progress);
        }

        protected void seek(float t, bool update_state = true)
        {
            animation_states.ForEach(s => s.normalizedTime = t);
            if(update_state) on_norm_time(t);
        }

        protected virtual void on_norm_time(float t) {}

        public virtual void Update()
        {
            if(!Playing) return;
            //calculate animation speed
            float speed = (State == AnimatorState.Opening || State == AnimatorState.Opened)? 
                ForwardSpeed : -ReverseSpeed;
            if(Reverse) speed *= -1;
            if(HighLogic.LoadedSceneIsEditor) 
                speed *= 1 - 10 * (progress - 1) * progress;
            else
                speed *= speed_multiplier * TimeWarp.CurrentRate;
            //set animation speed, compute total progress
            float _progress = 1;
            for(int i = 0, count = animation_states.Count; i < count; i++)
            {
                var state = animation_states[i];
                float time = Mathf.Clamp01(state.normalizedTime);
                state.normalizedTime = time;
                _progress = Math.Min(_progress, time);
                state.speed = speed;
            }
            on_norm_time(_progress);
            last_progress = progress;
            progress = Mathf.Clamp01(_progress/StopTime*100f);
            if(Reverse) progress = 1-progress;
            on_stop.Fire(progress);
            //check progress
            if(State == AnimatorState.Opening && progress >= 1)
            { if(Loop) set_progress(0); else State = AnimatorState.Opened; }
            else if(State == AnimatorState.Closing && progress <= 0) 
                State = AnimatorState.Closed;
            //stop the animation if not playing anymore
            if(!Playing) animation_states.ForEach(s => s.speed = 0);
        }

        protected virtual void consume_energy()
        {
            if(State != AnimatorState.Closing && State != AnimatorState.Opening) return;
            socket.RequestTransfer(EnergyConsumption*TimeWarp.fixedDeltaTime);
            if(!socket.TransferResource()) return;
            speed_multiplier = socket.Ratio;
            if(speed_multiplier < 0.01f) 
                speed_multiplier = 0;
            update_sound_params();
            update_emitter();
        }

        protected void update_emitter()
        {
            if(emitter != null)
            {
                emitter.minEmission = (int)Mathf.Ceil(base_emission[0]*speed_multiplier);
                emitter.maxEmission = (int)Mathf.Ceil(base_emission[1]*speed_multiplier);
            }
        }

        void update_sound_params()
        {
            if(fxSound.audio == null) return;
            fxSound.audio.pitch = Mathf.Lerp(MinPitch, MaxPitch, speed_multiplier);
            fxSound.audio.volume = GameSettings.SHIP_VOLUME * Mathf.Lerp(MinVolume, MaxVolume, speed_multiplier);
        }

        public virtual void FixedUpdate()
        {
            //consume energy if playing
            if(HighLogic.LoadedSceneIsFlight && socket != null)    consume_energy();
        }

        #region Events & Actions
        void enable_emitter(bool enable = true)
        {
            if(emitter == null) return;
            update_emitter();
            emitter.emit = enable;
            emitter.enabled = enable;
        }

        protected void update_events()
        {
            update_sound_params();
            switch(State)
            {
            case AnimatorState.Closed:
            case AnimatorState.Closing:
                enable_emitter(false);
                if(fxSound.audio != null)
                    fxSound.audio.Stop();
                Events["Toggle"].guiName = OpenEventGUIName;
                Events["Toggle"].active = !string.IsNullOrEmpty(OpenEventGUIName);
                break;
            case AnimatorState.Opened:
            case AnimatorState.Opening:
                enable_emitter();
                if(fxSound.audio != null) 
                    fxSound.audio.Play();
                Events["Toggle"].guiName = CloseEventGUIName;
                Events["Toggle"].active = !string.IsNullOrEmpty(CloseEventGUIName);
                break;
            }
            Actions["ToggleAction"].active = !string.IsNullOrEmpty(ActionGUIName);
            Utils.EnableField(Fields["StopTime"], !string.IsNullOrEmpty(StopTimeGUIName));
        }

        public void Open() 
        { 
            if(!AllowWhileShielded && part.ShieldedFromAirstream) return;
            State = AnimatorState.Opening;
            on_move.Fire(0, 1);
            update_events();
        }

        public void Close() 
        { 
            if(!AllowWhileShielded && part.ShieldedFromAirstream) return;
            State = AnimatorState.Closing; 
            on_move.Fire(1, 0);
            update_events();
        }

        [KSPEvent (guiActiveEditor = true, guiActive = true, guiName = "Open", active = false)]
        public void Toggle()
        {
            if(State == AnimatorState.Closed || State == AnimatorState.Closing) Open();
            else Close();
        }

        [KSPAction("Toggle")]
        public void ToggleAction(KSPActionParam param)
        {
            var thisAction = Actions[nameof(ToggleAction)];
            if(thisAction.actionGroup != KSPActionGroup.None)
            {
                if(vessel.ActionGroups[thisAction.actionGroup])
                    Open();
                else
                    Close();
            }
            else
                Toggle();
        }
        #endregion

        #region ResourceConsumer
        static readonly List<PartResourceDefinition> consumed_resources = new List<PartResourceDefinition>{Utils.ElectricCharge.def};
        public List<PartResourceDefinition> GetConsumedResources() 
        { return EnergyConsumption > 0? consumed_resources : new List<PartResourceDefinition>(); }
        #endregion

        #region ScalarModule
        protected EventData<float, float> on_move = new EventData<float, float>("OnMove");
        protected EventData<float> on_stop = new EventData<float>("OnStop");

        public string ScalarModuleID { get { return "ModuleAnimator."+AnimatorID; } }
        public float GetScalar { get { return progress; } }
        public bool CanMove { get { return AllowWhileShielded || !part.ShieldedFromAirstream; } }
        public EventData<float, float> OnMoving { get { return on_move; } }
        public EventData<float> OnStop { get { return on_stop; } }
        public void SetScalar(float t) { set_progress(t); }
        public bool IsMoving() { return Playing; }

        public void SetUIRead(bool state) {}
        public void SetUIWrite(bool state)
        {
            var evt = Events["Toggle"];
            evt.guiActive = evt.guiActiveUnfocused = state;
        }
        #endregion
    }

    public class AnimatorUpdater : ModuleUpdater<MultiAnimator>
    { 
        protected override void on_rescale(ModulePair<MultiAnimator> mp, Scale scale)
        { mp.module.EnergyConsumption = mp.base_module.EnergyConsumption * scale.absolute.quad * scale.absolute.aspect; }
    }
}

