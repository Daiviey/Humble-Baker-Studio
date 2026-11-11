namespace HumbleBakerStudio.Audio;


// Builds a wall of knobs (plus drop-downs and check boxes) for any [Rng] parameter object, in step with the property grid.
public static class KnobPanel {
    static System.Collections.Generic.HashSet<string> ModuleProps(string module) {
        System.Collections.Generic.HashSet<string> s = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (module != null) {
            string m = module.ToLowerInvariant();
            if (m == "filter") { s.Add("Filter"); s.Add("Cutoff"); s.Add("Resonance"); s.Add("FilterLfoRate"); s.Add("FilterLfoDepth"); }
            else if (m == "drive") { s.Add("Drive"); }
            else if (m == "crush") { s.Add("Bits"); s.Add("Downsample"); }
            else if (m == "ring") { s.Add("RingMix"); s.Add("RingFreq"); }
            else if (m == "stutter") { s.Add("StutterBeats"); s.Add("StutterDiv"); s.Add("StutterChance"); }
            else if (m == "chorus") { s.Add("Chorus"); s.Add("ChorusRate"); s.Add("ChorusDepth"); }
            else if (m == "delay") { s.Add("DelayMix"); s.Add("DelayBeats"); s.Add("DelayFeedback"); }
            else if (m == "reverb") { s.Add("ReverbMix"); s.Add("ReverbSize"); }
            else if (m == "phaser") { s.Add("PhaserMix"); s.Add("PhaserRate"); s.Add("PhaserDepth"); s.Add("PhaserStages"); }
            else if (m == "flanger") { s.Add("FlangerMix"); s.Add("FlangerRate"); s.Add("FlangerDepth"); s.Add("FlangerFb"); }
            else if (m == "tapestop") { s.Add("TapeStop"); s.Add("TapeStopRate"); }
            else if (m == "tapedelay") { s.Add("TapeDelayMix"); s.Add("TapeDelayTime"); s.Add("TapeDelayFb"); s.Add("TapeDelayWow"); }
            else if (m == "plate") { s.Add("PlateMix"); s.Add("PlateSize"); s.Add("PlateDamping"); }
            else if (m == "vinyl") { s.Add("VinylCrackle"); s.Add("TapeHiss"); s.Add("TapeWow"); }
            else if (m == "comp") { s.Add("CompThresh"); s.Add("CompRatio"); s.Add("CompAttack"); s.Add("CompRelease"); s.Add("CompMakeup"); s.Add("SidechainSource"); s.Add("SidechainMix"); s.Add("SidechainAttack"); s.Add("SidechainRelease"); }
            else if (m == "eq") { s.Add("LowShelf"); s.Add("HighShelf"); s.Add("MidPeak"); s.Add("Width"); s.Add("PitchShift"); }
            else if (m == "fold") { s.Add("Fold"); }
            else if (m == "width") { s.Add("Width"); s.Add("PitchShift"); }
            else if (m == "saturator") { s.Add("Saturator"); s.Add("SatBias"); s.Add("SatTone"); s.Add("SatMix"); }
            else if (m == "exciter") { s.Add("Exciter"); s.Add("ExciterFreq"); s.Add("ExciterMix"); }
            else if (m == "rectify") { s.Add("RectifyMix"); s.Add("RectifyHalf"); s.Add("RectifyDrive"); }
            else if (m == "gate") { s.Add("GateAmount"); s.Add("GateThreshold"); s.Add("GateAttack"); s.Add("GateRelease"); }
            else if (m == "transient") { s.Add("TransientAttack"); s.Add("TransientSustain"); s.Add("TransientSensitivity"); }
            else if (m == "tremolo") { s.Add("TremoloMix"); s.Add("TremoloRate"); s.Add("TremoloDepth"); }
            else if (m == "autopan") { s.Add("AutoPanMix"); s.Add("AutoPanRate"); s.Add("AutoPanDepth"); }
            else if (m == "vibrato") { s.Add("VibratoMix"); s.Add("VibratoRate"); s.Add("VibratoDepth"); }
            else if (m == "autowah") { s.Add("AutoWahMix"); s.Add("AutoWahRate"); s.Add("AutoWahDepth"); s.Add("AutoWahResonance"); }
            else if (m == "comb") { s.Add("CombMix"); s.Add("CombFreq"); s.Add("CombFeedback"); }
            else if (m == "spring") { s.Add("SpringMix"); s.Add("SpringDecay"); s.Add("SpringTone"); }
            else if (m == "shimmer") { s.Add("ShimmerMix"); s.Add("ShimmerPitch"); s.Add("ShimmerDecay"); }
            else if (m == "haas") { s.Add("HaasMix"); s.Add("HaasDelay"); }
            else if (m == "limiter") { s.Add("LimiterOn"); s.Add("LimiterThreshold"); s.Add("LimiterRelease"); s.Add("LimiterCeiling"); }
            else if (m == "ensemble") { s.Add("EnsembleMix"); s.Add("EnsembleRate"); s.Add("EnsembleDepth"); }
            else if (m == "rotary") { s.Add("RotaryMix"); s.Add("RotaryRate"); s.Add("RotaryDepth"); }
            else if (m == "octaver") { s.Add("OctaverMix"); }
            else if (m == "harmonizer") { s.Add("HarmonizerMix"); s.Add("HarmonizerSemi"); }
            else if (m == "resonator") { s.Add("ResonatorMix"); s.Add("ResonatorFreq"); s.Add("ResonatorFeedback"); }
            else if (m == "tilteq") { s.Add("TiltGain"); }
            else if (m == "midside") { s.Add("MidGain"); s.Add("SideGain"); }
            else if (m == "deesser") { s.Add("DeEsserAmount"); s.Add("DeEsserFreq"); }
            else if (m == "mbcomp") { s.Add("MBOn"); s.Add("MBLowThresh"); s.Add("MBMidThresh"); s.Add("MBHighThresh"); s.Add("MBRatio"); s.Add("MBMakeup"); }
            else if (m == "clipper") { s.Add("ClipperAmount"); s.Add("ClipperDrive"); }
            else if (m == "autofilter") { s.Add("AutoFilterMix"); s.Add("AutoFilterSensitivity"); s.Add("AutoFilterResonance"); }
            else if (m == "gatedverb") { s.Add("GateVerbMix"); s.Add("GateVerbDecay"); s.Add("GateVerbHold"); }
            else if (m == "reverseverb") { s.Add("ReverseVerbMix"); s.Add("ReverseVerbDecay"); }
            else if (m == "multitap") { s.Add("MultiTapMix"); s.Add("MultiTapTime"); s.Add("MultiTapFb"); }
            else if (m == "doubler") { s.Add("DoublerMix"); s.Add("DoublerDelay"); }
            else if (m == "formant") { s.Add("FormantMix"); s.Add("FormantVowel"); s.Add("FormantShift"); }
            else if (m == "ladder") { s.Add("LadderMix"); s.Add("LadderCutoff"); s.Add("LadderReso"); s.Add("LadderDrive"); }
            else if (m == "tube") { s.Add("TubeDrive"); s.Add("TubeBias"); s.Add("TubeMix"); }
            else if (m == "tapesat") { s.Add("TapeSatDrive"); s.Add("TapeSatTone"); s.Add("TapeSatMix"); }
            else if (m == "diode") { s.Add("DiodeDrive"); s.Add("DiodeTone"); s.Add("DiodeMix"); }
            else if (m == "fuzz") { s.Add("FuzzAmount"); s.Add("FuzzGate"); s.Add("FuzzMix"); }
            else if (m == "chiptune") { s.Add("ChipMix"); s.Add("ChipBits"); s.Add("ChipRate"); }
            else if (m == "subbass") { s.Add("SubBassMix"); s.Add("SubBassFreq"); s.Add("SubBassDrive"); }
            else if (m == "bassenh") { s.Add("BassEnhMix"); s.Add("BassEnhFreq"); s.Add("BassEnhAmount"); }
            else if (m == "freqshift") { s.Add("FreqShiftMix"); s.Add("FreqShiftHz"); }
            else if (m == "airband") { s.Add("AirMix"); s.Add("AirFreq"); s.Add("AirGain"); }
            else if (m == "stereoimage") { s.Add("StereoWidth"); s.Add("BassMonoFreq"); }
            else if (m == "pingpong") { s.Add("PingPongMix"); s.Add("PingPongTime"); s.Add("PingPongFb"); }
            else if (m == "dubdelay") { s.Add("DubMix"); s.Add("DubTime"); s.Add("DubFb"); s.Add("DubTone"); }
            else if (m == "cathedral") { s.Add("CathedralMix"); s.Add("CathedralSize"); s.Add("CathedralDamp"); }
            else if (m == "roomverb") { s.Add("RoomMix"); s.Add("RoomSize"); s.Add("RoomDamp"); }
            else if (m == "ambience") { s.Add("AmbienceMix"); s.Add("AmbienceSize"); s.Add("AmbienceDamp"); }
            else if (m == "wahpedal") { s.Add("WahPedalMix"); s.Add("WahPedalRate"); s.Add("WahPedalDepth"); s.Add("WahPedalQ"); }
            else if (m == "talkbox") { s.Add("TalkMix"); s.Add("TalkRate"); s.Add("TalkVowel"); }
            else if (m == "ducker") { s.Add("DuckerMix"); s.Add("DuckerThreshold"); s.Add("DuckerDepth"); s.Add("DuckerRelease"); }
            else if (m == "rider") { s.Add("RiderAmount"); s.Add("RiderTarget"); s.Add("RiderSpeed"); }
            else if (m == "harmtrem") { s.Add("HarmTremMix"); s.Add("HarmTremRate"); s.Add("HarmTremDepth"); }
            else if (m == "beatrepeat") { s.Add("BeatRepeatMix"); s.Add("BeatRepeatDiv"); s.Add("BeatRepeatChance"); }
            else if (m == "grain") { s.Add("GrainMix"); s.Add("GrainSize"); s.Add("GrainDensity"); }
            else if (m == "glitchgate") { s.Add("GlitchMix"); s.Add("GlitchDensity"); s.Add("GlitchSeed"); }
            else if (m == "sweep") { s.Add("SweepMix"); s.Add("SweepRate"); s.Add("SweepDepth"); s.Add("SweepQ"); s.Add("SweepCenter"); }
            else if (m == "optocomp") { s.Add("OptoMix"); s.Add("OptoThresh"); s.Add("OptoRatio"); s.Add("OptoRelease"); }
            else if (m == "fetcomp") { s.Add("FetMix"); s.Add("FetThresh"); s.Add("FetRatio"); s.Add("FetAttack"); }
            else if (m == "vcacomp") { s.Add("VcaMix"); s.Add("VcaThresh"); s.Add("VcaRatio"); s.Add("VcaRelease"); }
            else if (m == "expander") { s.Add("ExpMix"); s.Add("ExpThresh"); s.Add("ExpRatio"); s.Add("ExpRange"); }
            else if (m == "mscomp") { s.Add("MSCompMix"); s.Add("MSCompThresh"); s.Add("MSCompRatio"); s.Add("MSCompRelease"); }
            else if (m == "dyneq") { s.Add("DynEqMix"); s.Add("DynEqFreq"); s.Add("DynEqThresh"); s.Add("DynEqAmount"); }
            else if (m == "pulteceq") { s.Add("PultecLow"); s.Add("PultecHigh"); s.Add("PultecAtten"); }
            else if (m == "overdrive") { s.Add("OverdriveDrive"); s.Add("OverdriveTone"); s.Add("OverdriveMix"); }
            else if (m == "metaldrive") { s.Add("MetalDrive"); s.Add("MetalMid"); s.Add("MetalMix"); }
            else if (m == "cabsim") { s.Add("CabDrive"); s.Add("CabTone"); s.Add("CabMix"); }
            else if (m == "mbdist") { s.Add("MBDistMix"); s.Add("MBDistLow"); s.Add("MBDistMid"); s.Add("MBDistHigh"); }
            else if (m == "tapeflutter") { s.Add("FlutterMix"); s.Add("FlutterRate"); s.Add("FlutterDepth"); }
            else if (m == "dimension") { s.Add("DimMix"); s.Add("DimRate"); s.Add("DimDepth"); }
            else if (m == "envphaser") { s.Add("EnvPhaserMix"); s.Add("EnvPhaserThresh"); s.Add("EnvPhaserDepth"); s.Add("EnvPhaserRate"); }
            else if (m == "envflanger") { s.Add("EnvFlangeMix"); s.Add("EnvFlangeThresh"); s.Add("EnvFlangeDepth"); s.Add("EnvFlangeFb"); }
            else if (m == "chopper") { s.Add("ChopMix"); s.Add("ChopRate"); s.Add("ChopDepth"); s.Add("ChopShape"); }
            else if (m == "barberpole") { s.Add("BarberMix"); s.Add("BarberRate"); s.Add("BarberDepth"); s.Add("BarberStages"); }
            else if (m == "halfspeed") { s.Add("HalfSpeedMix"); s.Add("HalfSpeedAmount"); }
            else if (m == "pitchecho") { s.Add("PitchEchoMix"); s.Add("PitchEchoTime"); s.Add("PitchEchoFb"); s.Add("PitchEchoSemi"); }
            else if (m == "spectralfreeze") { s.Add("FreezeMix"); s.Add("FreezeThreshold"); s.Add("FreezeDecay"); }
            else if (m == "revdelay") { s.Add("RevDelayMix"); s.Add("RevDelayTime"); s.Add("RevDelayFb"); }
            else if (m == "echoverb") { s.Add("EchoVerbMix"); s.Add("EchoVerbTime"); s.Add("EchoVerbFb"); s.Add("EchoVerbSize"); }
            else if (m == "bloomverb") { s.Add("BloomMix"); s.Add("BloomSize"); s.Add("BloomThresh"); s.Add("BloomAttack"); }
            else if (m == "nonlinverb") { s.Add("NonLinVerbMix"); s.Add("NonLinVerbSize"); s.Add("NonLinVerbDrive"); s.Add("NonLinVerbGate"); }
            else if (m == "vocoder") { s.Add("VocoderMix"); s.Add("VocoderBands"); s.Add("VocoderRelease"); s.Add("VocoderCarrier"); }
            else if (m == "specgate") { s.Add("SpecGateMix"); s.Add("SpecGateThresh"); s.Add("SpecGateBands"); }
            else if (m == "specblur") { s.Add("SpecBlurMix"); s.Add("SpecBlurAmount"); s.Add("SpecBlurSpread"); }
            else if (m == "specdelay") { s.Add("SpecDelayMix"); s.Add("SpecDelayTime"); s.Add("SpecDelayFb"); s.Add("SpecDelaySpread"); }
            else if (m == "convreverb") { s.Add("ConvMix"); s.Add("ConvSize"); s.Add("ConvDamp"); s.Add("ConvPre"); }
            else if (m == "tankreverb") { s.Add("TankMix"); s.Add("TankDecay"); s.Add("TankTone"); s.Add("TankDrip"); }
            else if (m == "autotune") { s.Add("TuneMix"); s.Add("TuneSpeed"); s.Add("TuneScale"); s.Add("TuneMinHz"); }
            else if (m == "octaveup") { s.Add("OctUpMix"); s.Add("OctUpTone"); }
            else if (m == "chordgen") { s.Add("ChordMix"); s.Add("ChordType"); s.Add("ChordSpread"); }
            else if (m == "upcomp") { s.Add("UpCompMix"); s.Add("UpCompThresh"); s.Add("UpCompRatio"); s.Add("UpCompRange"); }
            else if (m == "trangate") { s.Add("TranGateMix"); s.Add("TranGateThresh"); s.Add("TranGateHold"); s.Add("TranGateRelease"); }
            else if (m == "shuffle") { s.Add("ShuffleMix"); s.Add("ShuffleLow"); s.Add("ShuffleHigh"); s.Add("ShuffleFreq"); }
            else if (m == "monomaker") { s.Add("MonoMix"); s.Add("MonoFreq"); s.Add("MonoWidth"); }
            else if (m == "phasealign") { s.Add("PhaseMix"); s.Add("PhaseFreq"); s.Add("PhaseAmount"); }
            else if (m == "tapeloop") { s.Add("LoopMix"); s.Add("LoopLength"); s.Add("LoopSpeed"); s.Add("LoopFb"); }
            else if (m == "stuttergate") { s.Add("StutterGateMix"); s.Add("StutterGateRate"); s.Add("StutterGateDiv"); s.Add("StutterGateChance"); }
            s.Add("Gain");
        }
        return s;
    }
    public static void Build(object o, FlowLayoutPanel host, EventHandler changed, Color back, Color fore, Color accent, string moduleFilter = null) {
        foreach (Control c in new List<Control>(host.Controls.Cast<Control>())) { host.Controls.Remove(c); c.Dispose(); }
        if (o == null) return;
        object def = Activator.CreateInstance(o.GetType()); ToolTip tip = new ToolTip();
        string cat = null; host.SuspendLayout();
        foreach (System.Reflection.PropertyInfo pi in o.GetType().GetProperties()) {
            if (!pi.CanWrite) continue;
            object[] ra = pi.GetCustomAttributes(typeof(RngAttribute), true); RngAttribute r = ra.Length > 0 ? (RngAttribute)ra[0] : null;
            bool isEnum = pi.PropertyType.IsEnum, isBool = pi.PropertyType == typeof(bool), isNum = r != null && !isEnum && !isBool && pi.PropertyType != typeof(string);
            if (!isEnum && !isBool && !isNum) continue;
            if (moduleFilter != null && o is Fx) { if (!ModuleProps(moduleFilter).Contains(pi.Name)) { if (!string.Equals(pi.Name, "Gain", StringComparison.OrdinalIgnoreCase)) { continue; } } }
            object[] ca = pi.GetCustomAttributes(typeof(CategoryAttribute), true); string c2 = ca.Length > 0 ? ((CategoryAttribute)ca[0]).Category : "";
            object[] da = pi.GetCustomAttributes(typeof(DescriptionAttribute), true); string desc = da.Length > 0 ? ((DescriptionAttribute)da[0]).Description : "";
            if (c2 != cat) {
                cat = c2; Label hl = new Label(); hl.Text = cat; hl.ForeColor = accent; hl.BackColor = back; hl.AutoSize = false; hl.Size = new Size(Math.Max(300, host.ClientSize.Width - 30), 20); hl.Margin = new Padding(2, 10, 0, 0);
                host.Controls.Add(hl); host.SetFlowBreak(hl, true);
            }
            System.Reflection.PropertyInfo prop = pi; object target = o;
            if (isNum) {
                Knob k = new Knob(); k.BackColor = back; k.ForeColor = accent; k.Min = r.Min; k.Max = r.Max; k.Log = r.Min > 0 && r.Max / r.Min > 20; k.Caption = pi.Name;
                k.Size = new Size(108, 96);
                k.Default = Convert.ToDouble(pi.GetValue(def, null), CultureInfo.InvariantCulture); k.Value = Convert.ToDouble(pi.GetValue(o, null), CultureInfo.InvariantCulture);
                k.Font = host.Font; tip.SetToolTip(k, pi.Name + ": " + desc);
                k.ValueChanged += delegate(object s, EventArgs ev) { prop.SetValue(target, Convert.ChangeType(((Knob)s).Value, prop.PropertyType, CultureInfo.InvariantCulture), null); if (changed != null) changed(s, ev); };
                host.Controls.Add(k);
            } else {
                Panel cell = new Panel(); cell.Size = new Size(118, 56); cell.BackColor = back; cell.Margin = new Padding(3);
                Label l = new Label(); l.Text = pi.Name; l.ForeColor = Color.FromArgb(150, 158, 180); l.SetBounds(2, 30, 114, 16); l.Font = host.Font; cell.Controls.Add(l);
                if (isEnum) {
                    ComboBox cb = new ComboBox(); cb.DropDownStyle = ComboBoxStyle.DropDownList; cb.FlatStyle = FlatStyle.Flat; cb.BackColor = Color.FromArgb(30, 34, 46); cb.ForeColor = fore;
                    foreach (string n in Enum.GetNames(pi.PropertyType)) cb.Items.Add(n);
                    cb.SelectedItem = pi.GetValue(o, null).ToString(); cb.SetBounds(2, 4, 112, 24);
                    cb.SelectedIndexChanged += delegate(object s, EventArgs ev) { prop.SetValue(target, Enum.Parse(prop.PropertyType, (string)((ComboBox)s).SelectedItem), null); if (changed != null) changed(s, ev); };
                    tip.SetToolTip(cb, pi.Name + ": " + desc); cell.Controls.Add(cb);
                } else {
                    CheckBox ck = new CheckBox(); ck.Text = "on"; ck.ForeColor = fore; ck.Checked = (bool)pi.GetValue(o, null); ck.SetBounds(4, 6, 100, 22);
                    ck.CheckedChanged += delegate(object s, EventArgs ev) { prop.SetValue(target, ((CheckBox)s).Checked, null); if (changed != null) changed(s, ev); };
                    tip.SetToolTip(ck, pi.Name + ": " + desc); cell.Controls.Add(ck);
                }
                host.Controls.Add(cell);
            }
        }
        host.ResumeLayout();
    }
}
