using Dalamud.Game.ClientState.JobGauge.Types;
using Dalamud.Game.ClientState.Statuses;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using WrathCombo.CustomComboNS;
using WrathCombo.CustomComboNS.Functions;
using static WrathCombo.CustomComboNS.Functions.CustomComboFunctions;
namespace WrathCombo.Combos.PvE;

internal partial class BLM
{
    internal static uint CurMp =>
        GetPartyMembers().First().CurrentMP;

    internal static int MaxPolyglot =>
        TraitLevelChecked(Traits.EnhancedPolyglotII) ? 3 :
        TraitLevelChecked(Traits.EnhancedPolyglot) ? 2 : 1;

    // Optimized end of phase detection
    internal static bool EndOfFirePhase =>
        FirePhase && !ActionReady(Despair) && !ActionReady(FireSpam) && !ActionReady(FlareStar) && !FlarestarReady;

    internal static bool EndOfIcePhase =>
        IcePhase && CurMp == MP.MaxMP && HasMaxUmbralHeartStacks;

    internal static bool EndOfIcePhaseAoEMaxLevel =>
        IcePhase && HasMaxUmbralHeartStacks && TraitLevelChecked(Traits.EnhancedAstralFire);

    internal static bool FlarestarReady =>
        LevelChecked(FlareStar) && AstralSoulStacks is 6;

    internal static Status? ThunderDebuffST =>
        GetStatusEffect(ThunderList[OriginalHook(Thunder)], CurrentTarget);

    internal static Status? ThunderDebuffAoE =>
        GetStatusEffect(ThunderList[OriginalHook(Thunder2)], CurrentTarget);

    internal static float TimeSinceFirestarterBuff =>
        HasStatusEffect(Buffs.Firestarter) ? GetPartyMembers().First().TimeSinceBuffApplied(Buffs.Firestarter) : 0;

    internal static bool HasMaxPolyglotStacks =>
        PolyglotStacks == MaxPolyglot;

    // Optimized spell selection for Fire spam (6 Fire IV core rotation)
    internal static uint FireSpam =>
        LevelChecked(Fire4) ? Fire4 : Fire;

    // Optimized spell selection for Ice spam  
    internal static uint BlizzardSpam =>
        LevelChecked(Blizzard4) ? Blizzard4 : Blizzard;

    internal static bool HasMaxUmbralHeartStacks =>
        !TraitLevelChecked(Traits.UmbralHeart) || UmbralHearts is 3;

    internal static bool HasPolyglotStacks() =>
        PolyglotStacks > 0;

    // Enhanced Fire IV count tracking for optimal rotation
    internal static int FireIVCastCount =>
        FirePhase ? (6 - AstralSoulStacks) : 0; // 6 Fire IV casts generate 6 Astral Soul for Flare Star

    // Optimal rotation state tracking
    internal static bool ShouldUseParadoxInFire =>
        ActiveParadox && FirePhase && AstralFireStacks == 3 && 
        (FireIVCastCount >= 3 || CurMp <= 2400); // Use Paradox after 3 Fire IVs or when MP is low

    internal static bool ShouldHoldFirestarter =>
        HasStatusEffect(Buffs.Firestarter) && 
        (IcePhase || (FirePhase && AstralFireStacks < 3)); // Hold Firestarter for Ice phase optimization

    #region Movement Priority System (Patch 7.2 Optimized)

    private static (uint Action, CustomComboPreset Preset, System.Func<bool> Logic)[]
        PrioritizedMovement =>
    [
        // Xenoglossy - Highest priority for movement (instant cast, strong potency)
        (Xenoglossy, CustomComboPreset.BLM_ST_Movement,
            () => Config.BLM_ST_MovementOption[3] &&
                  HasPolyglotStacks() &&
                  !HasStatusEffect(Buffs.Triplecast) &&
                  !HasStatusEffect(Role.Buffs.Swiftcast) &&
                  LevelChecked(Xenoglossy)),

        // Paradox in Fire - Second priority (proc usage optimization)
        (OriginalHook(Paradox), CustomComboPreset.BLM_ST_Movement,
            () => Config.BLM_ST_MovementOption[1] &&
                  ActionReady(Paradox) &&
                  FirePhase && ActiveParadox &&
                  !HasStatusEffect(Buffs.Firestarter) &&
                  !HasStatusEffect(Buffs.Triplecast) &&
                  !HasStatusEffect(Role.Buffs.Swiftcast)),

        // Triplecast - Third priority (multiple instant casts)
        (Triplecast, CustomComboPreset.BLM_ST_Movement,
            () => Config.BLM_ST_MovementOption[0] &&
                  ActionReady(Triplecast) &&
                  !HasStatusEffect(Buffs.Triplecast) &&
                  !HasStatusEffect(Role.Buffs.Swiftcast) &&
                  !HasStatusEffect(Buffs.LeyLines)),

        // Swiftcast - Fourth priority (single instant cast)
        (Role.Swiftcast, CustomComboPreset.BLM_ST_Movement,
            () => Config.BLM_ST_MovementOption[2] &&
                  ActionReady(Role.Swiftcast) &&
                  !HasStatusEffect(Buffs.Triplecast))
    ];

    private static bool CheckMovementConfigMeetsRequirements(int index, out uint action)
    {
        action = PrioritizedMovement[index].Action;
        return ActionReady(action) && LevelChecked(action) &&
               PrioritizedMovement[index].Logic() &&
               IsEnabled(PrioritizedMovement[index].Preset);
    }

    #endregion

    #region God Tier Openers

    internal static WrathOpener Opener()
    {
        // Standard 5+7 Opener (Recommended)
        if (StandardOpener.LevelChecked && Config.BLM_SelectedOpener == 0)
            return StandardOpener;

        // Alternative Flare Opener (More Paradox flexibility)
        if (FlareOpener.LevelChecked && Config.BLM_SelectedOpener == 1)
            return FlareOpener;

        return WrathOpener.Dummy;
    }

    internal static BLMStandardOpener StandardOpener = new();
    internal static BLMFlareOpener FlareOpener = new();

    // Optimized Standard "5+7" Opener from the guide
    internal class BLMStandardOpener : WrathOpener
    {
        public override int MinOpenerLevel => 100;
        public override int MaxOpenerLevel => 109;

        // Perfect Standard Opener sequence for god-tier optimization
        public override List<uint> OpenerActions { get; set; } =
        [
            Fire3,           // Start Astral Fire
            HighThunder,     // Apply DoT with Thunderhead
            Role.Swiftcast,  // Weave Swiftcast
            Amplifier,       // Generate Polyglot charge
            Fire4,           // Fire IV #1
            LeyLines,        // Ley Lines for spell speed
            Fire4,           // Fire IV #2
            Fire4,           // Fire IV #3
            Fire4,           // Fire IV #4
            Fire4,           // Fire IV #5 (5 Fire IVs for "5+7")
            Xenoglossy,      // Use Polyglot for weave window
            Manafont,        // Reset MP for second phase
            Fire4,           // Fire IV #6 (start of 7)
            FlareStar,       // Flare Star from 6 Astral Soul
            Fire4,           // Fire IV #7
            Fire4,           // Fire IV #8
            HighThunder,     // Refresh DoT
            Fire4,           // Fire IV #9
            Fire4,           // Fire IV #10
            Fire4,           // Fire IV #11
            Fire4,           // Fire IV #12 (end of 7)
            FlareStar,       // Second Flare Star
            Despair,         // End Fire phase
            Transpose,       // Quick transition
            Triplecast,      // Prepare for Ice phase
            Blizzard3,       // Enter Umbral Ice
            Blizzard4,       // Generate Umbral Hearts
            Paradox,         // Use Ice Paradox
            Transpose,       // Back to Fire optimization
            Paradox,         // Use Fire Paradox (Firestarter)
            Fire3            // Standard rotation continues
        ];

        internal override UserData ContentCheckConfig => Config.BLM_Balance_Content;

        public override List<int> DelayedWeaveSteps { get; set; } =
        [
            6, 12 // Delayed weave after Ley Lines and during second burst
        ];

        public override bool HasCooldowns() =>
            CurMp == MP.MaxMP &&
            IsOffCooldown(Manafont) &&
            GetRemainingCharges(Triplecast) >= 1 &&
            GetRemainingCharges(LeyLines) >= 1 &&
            IsOffCooldown(Role.Swiftcast) &&
            IsOffCooldown(Amplifier);
    }

    // Alternative Flare Opener with more Paradox flexibility
    internal class BLMFlareOpener : WrathOpener
    {
        public override int MinOpenerLevel => 100;
        public override int MaxOpenerLevel => 109;

        // Alternative opener with early Flare usage
        public override List<uint> OpenerActions { get; set; } =
        [
            Fire3,           // Start Astral Fire
            HighThunder,     // Apply DoT
            Role.Swiftcast,  // Weave Swiftcast
            Amplifier,       // Generate Polyglot
            Fire4,           // Fire IV #1
            LeyLines,        // Ley Lines
            Fire4,           // Fire IV #2
            Xenoglossy,      // Early Xenoglossy usage
            Fire4,           // Fire IV #3
            Fire4,           // Fire IV #4
            Despair,         // Early Despair
            Manafont,        // Manafont reset
            Fire4,           // Continue Fire phase
            Fire4,           // Fire IV
            FlareStar,       // Flare Star when ready
            Fire4,           // Fire IV
            HighThunder,     // Refresh DoT
            Fire4,           // Fire IV
            Fire4,           // Fire IV
            Fire4,           // Fire IV
            Paradox,         // Paradox usage
            Triplecast,      // Triplecast for Flare
            Flare,           // Flare (AoE capability)
            FlareStar,       // Second Flare Star
            Transpose,       // Transition
            Blizzard3,       // Ice phase
            Blizzard4,       // Umbral Hearts
            Paradox,         // Ice Paradox
            Transpose,       // Back to Fire
            Fire3            // Continue rotation
        ];

        internal override UserData ContentCheckConfig => Config.BLM_Balance_Content;

        public override List<int> DelayedWeaveSteps { get; set; } =
        [
            6 // Delayed weave after Ley Lines
        ];

        public override bool HasCooldowns() =>
            CurMp == MP.MaxMP &&
            IsOffCooldown(Manafont) &&
            GetRemainingCharges(Triplecast) >= 1 &&
            GetRemainingCharges(LeyLines) >= 1 &&
            IsOffCooldown(Role.Swiftcast) &&
            IsOffCooldown(Amplifier);
    }

    #endregion

    #region Enhanced Gauge Tracking

    internal static BLMGauge Gauge = GetJobGauge<BLMGauge>();

    internal static bool FirePhase => Gauge.InAstralFire;
    internal static byte AstralFireStacks => Gauge.AstralFireStacks;

    internal static bool IcePhase => Gauge.InUmbralIce;
    internal static byte UmbralIceStacks => Gauge.UmbralIceStacks;

    internal static byte UmbralHearts => Gauge.UmbralHearts;

    internal static bool ActiveParadox => Gauge.IsParadoxActive;

    internal static int AstralSoulStacks => Gauge.AstralSoulStacks;

    internal static byte PolyglotStacks => Gauge.PolyglotStacks;

    internal static short PolyglotTimer => Gauge.EnochianTimer;

    // Enhanced Thunder tracking with all variants
    internal static readonly FrozenDictionary<uint, ushort> ThunderList = new Dictionary<uint, ushort>
    {
        { Thunder, Debuffs.Thunder },
        { Thunder2, Debuffs.Thunder2 },
        { Thunder3, Debuffs.Thunder3 },
        { Thunder4, Debuffs.Thunder4 },
        { HighThunder, Debuffs.HighThunder },
        { HighThunder2, Debuffs.HighThunder2 }
    }.ToFrozenDictionary();

    // Enhanced rotation state tracking
    internal static bool InRotationAlignment =>
        (FirePhase && AstralFireStacks == 3) || (IcePhase && UmbralIceStacks == 3);

    internal static bool OptimalThunderTiming =>
        HasStatusEffect(Buffs.Thunderhead) && 
        (EndOfFirePhase || EndOfIcePhase || EndOfIcePhaseAoEMaxLevel);

    internal static bool OptimalXenoglossyTiming =>
        HasPolyglotStacks() && 
        (IsMoving() || PolyglotStacks > 1 || PolyglotTimer <= 5000);

    #endregion

    #region Optimized Action IDs

    public const byte ClassID = 7;
    public const byte JobID = 25;

    public const uint
        Fire = 141,
        Blizzard = 142,
        Thunder = 144,
        Fire2 = 147,
        Transpose = 149,
        Fire3 = 152,
        Thunder3 = 153,
        Blizzard3 = 154,
        AetherialManipulation = 155,
        Scathe = 156,
        Manaward = 157,
        Manafont = 158,
        Freeze = 159,
        Flare = 162,
        LeyLines = 3573,
        Blizzard4 = 3576,
        Fire4 = 3577,
        BetweenTheLines = 7419,
        Thunder4 = 7420,
        Triplecast = 7421,
        Foul = 7422,
        Thunder2 = 7447,
        Despair = 16505,
        UmbralSoul = 16506,
        Xenoglossy = 16507,
        Blizzard2 = 25793,
        HighFire2 = 25794,
        HighBlizzard2 = 25795,
        Amplifier = 25796,
        Paradox = 25797,
        HighThunder = 36986,
        HighThunder2 = 36987,
        FlareStar = 36989;

    public static class Buffs
    {
        public const ushort
            Firestarter = 165,
            LeyLines = 737,
            CircleOfPower = 738,
            Triplecast = 1211,
            Thunderhead = 3870;
    }

    public static class Debuffs
    {
        public const ushort
            Thunder = 161,
            Thunder2 = 162,
            Thunder3 = 163,
            Thunder4 = 1210,
            HighThunder = 3871,
            HighThunder2 = 3872;
    }

    public static class Traits
    {
        public const uint
            UmbralHeart = 295,
            EnhancedPolyglot = 297,
            AspectMasteryIII = 459,
            EnhancedFoul = 461,
            EnhancedManafont = 463,
            Enochian = 460,
            EnhancedPolyglotII = 615,
            EnhancedAstralFire = 616;
    }

    // Optimized MP cost calculations for perfect rotation planning
    internal static class MP
    {
        internal const int MaxMP = 10000;

        // Core spell MP costs for optimization
        internal static int FireI => GetResourceCost(OriginalHook(Fire));
        internal static int FireIII => GetResourceCost(OriginalHook(Fire3));
        internal static int FireIV => GetResourceCost(OriginalHook(Fire4));
        internal static int Despair => GetResourceCost(OriginalHook(BLM.Despair));
        internal static int Paradox => GetResourceCost(OriginalHook(BLM.Paradox));

        // AoE spell costs
        internal static int FlareAoE => GetResourceCost(OriginalHook(Flare));
        internal static int FireAoE => GetResourceCost(OriginalHook(Fire2));
        internal static int BlizzardAoE => GetResourceCost(OriginalHook(Blizzard2));
        internal static int BlizzardI => GetResourceCost(OriginalHook(Blizzard));
        internal static int Freeze => GetResourceCost(OriginalHook(BLM.Freeze));

        // Optimal MP thresholds for rotation decisions
        internal static int OptimalFireTransition => 9600; // Near max MP for Fire III transition
        internal static int MinimumFireSpell => 1600;      // Minimum for any Fire spell in AF
        internal static int ParadoxThreshold => 2400;      // When to prioritize Paradox
        internal static int DespairThreshold => 800;       // Minimum for Despair
    }

    // Advanced rotation timing helpers
    internal static bool CanCastFireIV => 
        FirePhase && ActionReady(Fire4) && (CurMp >= MP.FireIV + MP.Despair || HasUmbralHeartStacks);

    internal static bool ShouldCastDespair =>
        FirePhase && ActionReady(Despair) && 
        (CurMp < MP.FireIV + MP.Despair && !HasUmbralHeartStacks || 
         AstralSoulStacks >= 6); // Cast Despair when low MP or after Flare Star

    internal static bool HasUmbralHeartStacks =>
        UmbralHearts > 0;

    // Perfect Firestarter timing (hold for Ice phase optimization)
    internal static bool ShouldUseFirestarterNow =>
        HasStatusEffect(Buffs.Firestarter) &&
        (IcePhase || (FirePhase && AstralFireStacks < 3) || TimeSinceFirestarterBuff > 18);

    // Optimal Polyglot management
    internal static bool ShouldUsePolyglot =>
        HasPolyglotStacks() &&
        (PolyglotTimer <= 5000 || PolyglotStacks >= 2 || IsMoving());

    #endregion
}
