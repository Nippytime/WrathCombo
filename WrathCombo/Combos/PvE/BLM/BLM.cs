using System.Linq;
using WrathCombo.CustomComboNS;
using static WrathCombo.Combos.PvE.BLM.Config;
using static WrathCombo.Data.ActionWatching;
namespace WrathCombo.Combos.PvE;

internal partial class BLM : Caster
{
    internal class BLM_ST_SimpleMode : CustomCombo
    {
        protected internal override CustomComboPreset Preset => CustomComboPreset.BLM_ST_SimpleMode;

        protected override uint Invoke(uint actionID)
        {
            if (actionID is not Fire)
                return actionID;

            if (Variant.CanCure(CustomComboPreset.BLM_Variant_Cure, BLM_VariantCure))
                return Variant.Cure;

            if (Variant.CanRampart(CustomComboPreset.BLM_Variant_Rampart))
                return Variant.Rampart;

            if (OccultCrescent.ShouldUsePhantomActions())
                return OccultCrescent.BestPhantomAction();

            // OGCD Weaving - Optimized Priority
            if (CanSpellWeave() && !HasDoubleWeaved())
            {
                // Amplifier on cooldown for max Polyglot generation
                if (ActionReady(Amplifier) && !HasMaxPolyglotStacks)
                    return Amplifier;

                // Ley Lines without restriction for maximum uptime
                if (ActionReady(LeyLines) && !HasStatusEffect(Buffs.LeyLines))
                    return LeyLines;

                // End of Fire Phase actions
                if (EndOfFirePhase)
                {
                    // Manafont for additional Astral Fire cycle
                    if (ActionReady(Manafont))
                        return Manafont;

                    // Swiftcast after Despair for smooth transition
                    if (ActionReady(Role.Swiftcast) && JustUsed(Despair) &&
                        !ActionReady(Manafont) && !HasStatusEffect(Buffs.Triplecast))
                        return Role.Swiftcast;

                    // Transpose with instant cast buffs for optimization
                    if (ActionReady(Transpose) && (HasStatusEffect(Role.Buffs.Swiftcast) || HasStatusEffect(Buffs.Triplecast)))
                        return Transpose;
                }

                // Ice Phase optimizations
                if (IcePhase)
                {
                    // Quick Transpose after Paradox when MP is full
                    if (JustUsed(Paradox) && CurMp is MP.MaxMP)
                        return Transpose;

                    // Swiftcast for Blizzard III when not at 3 stacks
                    if (ActionReady(Blizzard3) && UmbralIceStacks < 3 &&
                        ActionReady(Role.Swiftcast) && !HasStatusEffect(Buffs.Triplecast))
                        return Role.Swiftcast;
                }

                // Emergency Manaward
                if (ActionReady(Manaward) && PlayerHealthPercentageHp() < 25)
                    return Manaward;
            }

            // Movement handling for low levels
            if (IsMoving() && !LevelChecked(Triplecast))
                return Scathe;

            // Polyglot overcap protection - CRITICAL
            if (HasMaxPolyglotStacks && PolyglotTimer <= 5000)
                return LevelChecked(Xenoglossy) ? Xenoglossy : Foul;

            // Thunder DoT management - Use when falling off, not on schedule
            if (LevelChecked(Thunder) && HasStatusEffect(Buffs.Thunderhead) &&
                CanApplyStatus(CurrentTarget, ThunderList[OriginalHook(Thunder)]) &&
                (ThunderDebuffST is null && ThunderDebuffAoE is null ||
                 ThunderDebuffST?.RemainingTime <= 3 ||
                 ThunderDebuffAoE?.RemainingTime <= 3) &&
                GetTargetHPPercent() > 0)
                return OriginalHook(Thunder);

            // Polyglot usage before Amplifier comes off cooldown
            if (LevelChecked(Amplifier) &&
                GetCooldownRemainingTime(Amplifier) < 5 &&
                HasMaxPolyglotStacks)
                return Xenoglossy;

            // Movement priority system (Patch 7.2 optimized)
            if (IsMoving() && InCombat())
            {
                // Triplecast for movement (no longer saves time, just for mobility)
                if (ActionReady(Triplecast) &&
                    !HasStatusEffect(Buffs.Triplecast) &&
                    !HasStatusEffect(Role.Buffs.Swiftcast) &&
                    !HasStatusEffect(Buffs.LeyLines))
                    return Triplecast;

                // Paradox movement in Fire Phase
                if (ActionReady(Paradox) &&
                    FirePhase && ActiveParadox &&
                    !HasStatusEffect(Buffs.Firestarter) &&
                    !HasStatusEffect(Buffs.Triplecast) &&
                    !HasStatusEffect(Role.Buffs.Swiftcast))
                    return OriginalHook(Paradox);

                // Swiftcast for movement
                if (ActionReady(Role.Swiftcast) && !HasStatusEffect(Buffs.Triplecast))
                    return Role.Swiftcast;

                // Xenoglossy for movement (instant cast)
                if (HasPolyglotStacks() &&
                    !HasStatusEffect(Buffs.Triplecast) &&
                    !HasStatusEffect(Role.Buffs.Swiftcast))
                    return LevelChecked(Xenoglossy) ? Xenoglossy : Foul;
            }

            // FIRE PHASE - Optimized 6 Fire IV + 1 Paradox rotation
            if (FirePhase)
            {
                // Use Xenoglossy if we have multiple stacks (better than overcapping)
                if (PolyglotStacks > 1)
                    return LevelChecked(Xenoglossy) ? Xenoglossy : Foul;

                // Firestarter proc usage - prioritize early use for better flow
                if ((LevelChecked(Paradox) && HasStatusEffect(Buffs.Firestarter) ||
                     TimeSinceFirestarterBuff >= 2) && AstralFireStacks < 3 ||
                    !LevelChecked(Fire4) && TimeSinceFirestarterBuff >= 2 && ActionReady(Fire3))
                    return Fire3;

                // Paradox usage in Astral Fire (part of the 6+1 rotation)
                if (ActiveParadox &&
                    CurMp > 1600 &&
                    (AstralFireStacks < 3 ||
                     JustUsed(FlareStar, 5) ||
                     !LevelChecked(FlareStar) && ActionReady(Despair)))
                    return OriginalHook(Paradox);

                // Flare Star when ready (6 Astral Soul stacks)
                if (FlarestarReady)
                    return FlareStar;

                // Fire IV spam (the core of the rotation)
                if (ActionReady(FireSpam) && (LevelChecked(Despair) && CurMp - MP.FireI >= 800 || !LevelChecked(Despair)))
                    return FireSpam;

                // Despair to end the Fire phase
                if (ActionReady(Despair))
                    return Despair;

                // Transition to Ice phase
                if (ActionReady(Blizzard3) &&
                    !HasStatusEffect(Role.Buffs.Swiftcast) && !HasStatusEffect(Buffs.Triplecast))
                    return Blizzard3;

                // Low level Transpose
                if (ActionReady(Transpose))
                    return Transpose; //Level 4-34
            }

            // ICE PHASE - Optimized for quick MP recovery and Umbral Hearts
            if (IcePhase)
            {
                // Use Paradox when at max Umbral Ice and Hearts
                if (UmbralHearts is 3 &&
                    UmbralIceStacks is 3 &&
                    ActiveParadox)
                    return OriginalHook(Paradox);

                // Transition back to Fire when MP is full
                if (CurMp == MP.MaxMP)
                {
                    if (ActionReady(Fire3))
                        return Fire3; //35-100, proper Fire III usage

                    if (ActionReady(Transpose))
                        return Transpose; //Levels 4-34
                }

                // Build Umbral Ice stacks quickly
                if (ActionReady(Blizzard3) && UmbralIceStacks < 3 &&
                    (JustUsed(Transpose, 5f) || JustUsed(Freeze, 10f)))
                    return Blizzard3;

                // Blizzard IV for Umbral Hearts (core mechanic)
                if (ActionReady(BlizzardSpam))
                    return BlizzardSpam;
            }

            // Starting rotation logic
            if (LevelChecked(Fire3))
                return CurMp >= 7500 ? Fire3 : Blizzard3;

            return actionID;
        }
    }

    internal class BLM_ST_AdvancedMode : CustomCombo
    {
        protected internal override CustomComboPreset Preset => CustomComboPreset.BLM_ST_AdvancedMode;

        protected override uint Invoke(uint actionID)
        {
            if (actionID is not Fire)
                return actionID;

            // Optimized opener implementation
            if (IsEnabled(CustomComboPreset.BLM_ST_Opener) &&
                Opener().FullOpener(ref actionID))
                return actionID;

            if (Variant.CanCure(CustomComboPreset.BLM_Variant_Cure, BLM_VariantCure))
                return Variant.Cure;

            if (Variant.CanRampart(CustomComboPreset.BLM_Variant_Rampart))
                return Variant.Rampart;

            if (OccultCrescent.ShouldUsePhantomActions())
                return OccultCrescent.BestPhantomAction();

            // Advanced OGCD management
            if (CanSpellWeave() && !HasDoubleWeaved())
            {
                // Maximum Amplifier usage for Polyglot generation
                if (IsEnabled(CustomComboPreset.BLM_ST_Amplifier) &&
                    ActionReady(Amplifier) && !HasMaxPolyglotStacks)
                    return Amplifier;

                // Aggressive Ley Lines usage for maximum uptime
                if (IsEnabled(CustomComboPreset.BLM_ST_LeyLines) &&
                    ActionReady(LeyLines) && !HasStatusEffect(Buffs.LeyLines) &&
                    GetRemainingCharges(LeyLines) > BLM_ST_LeyLinesCharges)
                    return LeyLines;

                // End of Fire Phase optimizations
                if (EndOfFirePhase)
                {
                    // Manafont for extra Astral Fire cycles
                    if (IsEnabled(CustomComboPreset.BLM_ST_Manafont) &&
                        ActionReady(Manafont))
                        return Manafont;

                    // Perfect Swiftcast timing
                    if (IsEnabled(CustomComboPreset.BLM_ST_Swiftcast) &&
                        ActionReady(Role.Swiftcast) && JustUsed(Despair) &&
                        !ActionReady(Manafont) && !HasStatusEffect(Buffs.Triplecast))
                        return Role.Swiftcast;

                    // Advanced Triplecast usage
                    if (IsEnabled(CustomComboPreset.BLM_ST_Triplecast) &&
                        ActionReady(Triplecast) && IsOnCooldown(Role.Swiftcast) &&
                        !HasStatusEffect(Role.Buffs.Swiftcast) && !HasStatusEffect(Buffs.Triplecast) &&
                        (BLM_ST_Triplecast_SubOption == 0 || !HasStatusEffect(Buffs.LeyLines)) &&
                        ((BLM_ST_MovementOption[0] && GetRemainingCharges(Triplecast) > BLM_ST_Triplecast_Movement) ||
                         !BLM_ST_MovementOption[0]) && JustUsed(Despair) && !ActionReady(Manafont))
                        return Triplecast;

                    // Optimized Transpose usage
                    if (IsEnabled(CustomComboPreset.BLM_ST_Transpose) &&
                        ActionReady(Transpose) && (HasStatusEffect(Role.Buffs.Swiftcast) || HasStatusEffect(Buffs.Triplecast)))
                        return Transpose;
                }

                // Ice Phase optimizations
                if (IcePhase)
                {
                    // Quick Transpose after Paradox
                    if (IsEnabled(CustomComboPreset.BLM_ST_Transpose) &&
                        JustUsed(Paradox) && CurMp is MP.MaxMP)
                        return Transpose;

                    // Ice phase instant cast management
                    if (ActionReady(Blizzard3) && UmbralIceStacks < 3)
                    {
                        if (IsEnabled(CustomComboPreset.BLM_ST_Swiftcast) &&
                            ActionReady(Role.Swiftcast) && !HasStatusEffect(Buffs.Triplecast))
                            return Role.Swiftcast;

                        if (IsEnabled(CustomComboPreset.BLM_ST_Triplecast) &&
                            ActionReady(Triplecast) && IsOnCooldown(Role.Swiftcast) &&
                            !HasStatusEffect(Role.Buffs.Swiftcast) && !HasStatusEffect(Buffs.Triplecast) && !HasStatusEffect(Buffs.LeyLines) &&
                            ((BLM_ST_MovementOption[0] && GetRemainingCharges(Triplecast) > BLM_ST_Triplecast_Movement) ||
                             !BLM_ST_MovementOption[0]) && JustUsed(Despair) && !ActionReady(Manafont))
                            return Triplecast;
                    }
                }

                // Emergency defensive
                if (IsEnabled(CustomComboPreset.BLM_ST_Manaward) &&
                    ActionReady(Manaward) && PlayerHealthPercentageHp() < BLM_ST_Manaward_Threshold)
                    return Manaward;
            }

            // Low level movement
            if (IsEnabled(CustomComboPreset.BLM_ST_UseScathe) &&
                IsMoving() && !LevelChecked(Triplecast))
                return Scathe;

            // CRITICAL: Polyglot overcap protection
            if (IsEnabled(CustomComboPreset.BLM_ST_UsePolyglot) &&
                HasMaxPolyglotStacks && PolyglotTimer <= 5000)
                return LevelChecked(Xenoglossy) ? Xenoglossy : Foul;

            // Advanced Thunder management - DoT falling off based
            if (IsEnabled(CustomComboPreset.BLM_ST_Thunder) &&
                LevelChecked(Thunder) && HasStatusEffect(Buffs.Thunderhead))
            {
                float refreshTimer = BLM_ST_ThunderUptime_Threshold;
                int hpThreshold = BLM_ST_Thunder_SubOption == 1 || !InBossEncounter() ? BLM_ST_ThunderOption : 0;

                if (CanApplyStatus(CurrentTarget, ThunderList[OriginalHook(Thunder)]) &&
                    (ThunderDebuffST is null && ThunderDebuffAoE is null ||
                     ThunderDebuffST?.RemainingTime <= refreshTimer ||
                     ThunderDebuffAoE?.RemainingTime <= refreshTimer) &&
                    GetTargetHPPercent() > hpThreshold)
                    return OriginalHook(Thunder);
            }

            // Pre-Amplifier Polyglot usage
            if (IsEnabled(CustomComboPreset.BLM_ST_Amplifier) &&
                IsEnabled(CustomComboPreset.BLM_ST_UsePolyglot) &&
                LevelChecked(Amplifier) &&
                GetCooldownRemainingTime(Amplifier) < 5 &&
                HasMaxPolyglotStacks)
                return Xenoglossy;

            // Advanced movement system
            if (IsMoving() && InCombat())
            {
                foreach(int priority in BLM_ST_Movement_Priority.Items.OrderBy(x => x))
                {
                    int index = BLM_ST_Movement_Priority.IndexOf(priority);
                    if (CheckMovementConfigMeetsRequirements(index, out uint action))
                        return action;
                }
            }

            // OPTIMIZED FIRE PHASE - God Tier 6 Fire IV + Paradox rotation
            if (FirePhase)
            {
                // Smart Polyglot usage (avoid overcapping while saving for movement)
                if (IsEnabled(CustomComboPreset.BLM_ST_UsePolyglot) &&
                    ((BLM_ST_MovementOption[3] &&
                      PolyglotStacks > BLM_ST_Polyglot_Movement &&
                      PolyglotStacks > BLM_ST_Polyglot_Save) ||
                     (!BLM_ST_MovementOption[3] &&
                      PolyglotStacks > BLM_ST_Polyglot_Save)))
                    return LevelChecked(Xenoglossy) ? Xenoglossy : Foul;

                // Firestarter optimization - hold for Umbral Ice usage
                if ((LevelChecked(Paradox) && HasStatusEffect(Buffs.Firestarter) ||
                     TimeSinceFirestarterBuff >= 2) && AstralFireStacks < 3 ||
                    !LevelChecked(Fire4) && TimeSinceFirestarterBuff >= 2 && ActionReady(Fire3))
                    return Fire3;

                // Perfect Paradox timing in Astral Fire
                if (ActiveParadox &&
                    CurMp > 1600 &&
                    (AstralFireStacks < 3 ||
                     JustUsed(FlareStar, 5) ||
                     !LevelChecked(FlareStar) && ActionReady(Despair)))
                    return OriginalHook(Paradox);

                // Flare Star priority
                if (IsEnabled(CustomComboPreset.BLM_ST_FlareStar) &&
                    FlarestarReady)
                    return FlareStar;

                // Core Fire IV rotation
                if (ActionReady(FireSpam) && (LevelChecked(Despair) && CurMp - MP.FireI >= 800 || !LevelChecked(Despair)))
                    return FireSpam;

                // Despair finisher
                if (IsEnabled(CustomComboPreset.BLM_ST_Despair) &&
                    ActionReady(Despair))
                    return Despair;

                // Clean transition to Ice
                if (ActionReady(Blizzard3) &&
                    !HasStatusEffect(Role.Buffs.Swiftcast) && !HasStatusEffect(Buffs.Triplecast))
                    return Blizzard3;

                // Low level support
                if (IsEnabled(CustomComboPreset.BLM_ST_Transpose) &&
                    ActionReady(Transpose))
                    return Transpose;
            }

            // OPTIMIZED ICE PHASE - Quick recovery
            if (IcePhase)
            {
                // Perfect Paradox usage at max stacks
                if (UmbralHearts is 3 &&
                    UmbralIceStacks is 3 &&
                    ActiveParadox)
                    return OriginalHook(Paradox);

                // Fast transition back to Fire
                if (CurMp == MP.MaxMP)
                {
                    if (ActionReady(Fire3))
                        return Fire3;

                    if (IsEnabled(CustomComboPreset.BLM_ST_Transpose) &&
                        ActionReady(Transpose))
                        return Transpose;
                }

                // Rapid Umbral Ice stacking
                if (ActionReady(Blizzard3) && UmbralIceStacks < 3 &&
                    (JustUsed(Transpose, 5f) || JustUsed(Freeze, 10f)))
                    return Blizzard3;

                // Umbral Heart generation
                if (ActionReady(BlizzardSpam))
                    return BlizzardSpam;
            }

            // Fallback starting logic
            if (LevelChecked(Fire3))
                return CurMp >= 7500 ? Fire3 : Blizzard3;

            return actionID;
        }
    }

    // AoE rotations remain largely the same but with optimizations
    internal class BLM_AoE_SimpleMode : CustomCombo
    {
        protected internal override CustomComboPreset Preset => CustomComboPreset.BLM_AoE_SimpleMode;

        protected override uint Invoke(uint actionID)
        {
            if (actionID is not (Blizzard2 or HighBlizzard2))
                return actionID;

            if (Variant.CanCure(CustomComboPreset.BLM_Variant_Cure, BLM_VariantCure))
                return Variant.Cure;

            if (Variant.CanRampart(CustomComboPreset.BLM_Variant_Rampart))
                return Variant.Rampart;

            if (OccultCrescent.ShouldUsePhantomActions())
                return OccultCrescent.BestPhantomAction();

            // Optimized AoE OGCD usage
            if (CanSpellWeave() && !HasDoubleWeaved())
            {
                // High value Manafont for additional Flares
                if (ActionReady(Manafont) && EndOfFirePhase)
                    return Manafont;

                // Transpose optimization for AoE
                if (ActionReady(Transpose) && (EndOfFirePhase || EndOfIcePhaseAoEMaxLevel))
                    return Transpose;

                // Amplifier for Foul generation
                if (ActionReady(Amplifier) && PolyglotTimer >= 20000)
                    return Amplifier;

                // Ley Lines for AoE situations
                if (ActionReady(LeyLines) && !HasStatusEffect(Buffs.LeyLines) &&
                    GetRemainingCharges(LeyLines) > 1)
                    return LeyLines;
            }

            // Foul usage as filler
            if ((EndOfFirePhase || EndOfIcePhase || EndOfIcePhaseAoEMaxLevel) &&
                HasPolyglotStacks())
                return Foul;

            // Thunder II for AoE DoT
            if (HasStatusEffect(Buffs.Thunderhead) && LevelChecked(Thunder2) &&
                GetTargetHPPercent() > 1 &&
                CanApplyStatus(CurrentTarget, ThunderList[OriginalHook(Thunder2)]) &&
                (ThunderDebuffAoE is null && ThunderDebuffST is null ||
                 ThunderDebuffAoE?.RemainingTime <= 3 ||
                 ThunderDebuffST?.RemainingTime <= 3) &&
                (EndOfFirePhase || EndOfIcePhase || EndOfIcePhaseAoEMaxLevel))
                return OriginalHook(Thunder2);

            // Paradox filler
            if (ActiveParadox && EndOfIcePhaseAoEMaxLevel)
                return OriginalHook(Paradox);

            // FIRE PHASE AoE
            if (FirePhase)
            {
                // Flare Star priority
                if (FlarestarReady)
                    return FlareStar;

                // Low level Fire II
                if (ActionReady(Fire2) && !TraitLevelChecked(Traits.UmbralHeart))
                    return OriginalHook(Fire2);

                // Triplecast for Flare spam
                if (!HasStatusEffect(Buffs.Triplecast) && ActionReady(Triplecast) &&
                    GetRemainingCharges(Triplecast) > 1 && HasMaxUmbralHeartStacks &&
                    !ActionReady(Manafont))
                    return Triplecast;

                // Flare spam
                if (ActionReady(Flare))
                    return Flare;

                if (ActionReady(Transpose))
                    return Transpose;
            }

            // ICE PHASE AoE
            if (IcePhase)
            {
                // Quick transition when ready
                if ((CurMp == MP.MaxMP || HasMaxUmbralHeartStacks) &&
                    ActionReady(Transpose))
                    return Transpose;

                // Freeze vs Blizzard IV optimization
                if (ActionReady(Freeze))
                    return LevelChecked(Blizzard4) && HasBattleTarget() && NumberOfEnemiesInRange(Freeze, CurrentTarget) == 2
                        ? Blizzard4
                        : Freeze;

                // Low level Blizzard II
                if (!LevelChecked(Freeze) && ActionReady(Blizzard2))
                    return OriginalHook(Blizzard2);
            }

            return actionID;
        }
    }

    // Advanced AoE with same optimizations
    internal class BLM_AoE_AdvancedMode : CustomCombo
    {
        protected internal override CustomComboPreset Preset => CustomComboPreset.BLM_AoE_AdvancedMode;

        protected override uint Invoke(uint actionID)
        {
            if (actionID is not (Blizzard2 or HighBlizzard2))
                return actionID;

            if (Variant.CanCure(CustomComboPreset.BLM_Variant_Cure, BLM_VariantCure))
                return Variant.Cure;

            if (Variant.CanRampart(CustomComboPreset.BLM_Variant_Rampart))
                return Variant.Rampart;

            if (OccultCrescent.ShouldUsePhantomActions())
                return OccultCrescent.BestPhantomAction();

            // Advanced AoE OGCD management
            if (CanSpellWeave() && !HasDoubleWeaved())
            {
                if (IsEnabled(CustomComboPreset.BLM_AoE_Manafont) &&
                    ActionReady(Manafont) && EndOfFirePhase)
                    return Manafont;

                if (IsEnabled(CustomComboPreset.BLM_AoE_Transpose) &&
                    ActionReady(Transpose) && (EndOfFirePhase || EndOfIcePhaseAoEMaxLevel))
                    return Transpose;

                if (IsEnabled(CustomComboPreset.BLM_AoE_Amplifier) &&
                    ActionReady(Amplifier) && PolyglotTimer >= 20000)
                    return Amplifier;

                if (IsEnabled(CustomComboPreset.BLM_AoE_LeyLines) &&
                    ActionReady(LeyLines) && !HasStatusEffect(Buffs.LeyLines) &&
                    GetRemainingCharges(LeyLines) > BLM_AoE_LeyLinesCharges)
                    return LeyLines;
            }

            // Smart Foul usage
            if (IsEnabled(CustomComboPreset.BLM_AoE_UsePolyglot) &&
                (EndOfFirePhase || EndOfIcePhase || EndOfIcePhaseAoEMaxLevel) &&
                HasPolyglotStacks())
                return Foul;

            // Advanced Thunder II management
            if (IsEnabled(CustomComboPreset.BLM_AoE_Thunder) &&
                HasStatusEffect(Buffs.Thunderhead) && LevelChecked(Thunder2) &&
                CanApplyStatus(CurrentTarget, ThunderList[OriginalHook(Thunder2)]) &&
                (GetTargetHPPercent() > BLM_AoE_ThunderHP) &&
                (ThunderDebuffAoE is null && ThunderDebuffST is null ||
                 ThunderDebuffAoE?.RemainingTime <= 3 ||
                 ThunderDebuffST?.RemainingTime <= 3) &&
                (EndOfFirePhase || EndOfIcePhase || EndOfIcePhaseAoEMaxLevel))
                return OriginalHook(Thunder2);

            // Paradox filler
            if (IsEnabled(CustomComboPreset.BLM_AoE_ParadoxFiller) &&
                ActiveParadox && EndOfIcePhaseAoEMaxLevel)
                return OriginalHook(Paradox);

            // Advanced Fire Phase AoE
            if (FirePhase)
            {
                if (FlarestarReady)
                    return FlareStar;

                if (ActionReady(Fire2) && !TraitLevelChecked(Traits.UmbralHeart))
                    return OriginalHook(Fire2);

                if (IsEnabled(CustomComboPreset.BLM_AoE_Triplecast) &&
                    !HasStatusEffect(Buffs.Triplecast) && ActionReady(Triplecast) &&
                    GetRemainingCharges(Triplecast) > BLM_AoE_Triplecast_HoldCharges && HasMaxUmbralHeartStacks &&
                    !ActionReady(Manafont))
                    return Triplecast;

                if (ActionReady(Flare))
                    return Flare;

                if (IsNotEnabled(CustomComboPreset.BLM_AoE_Transpose) &&
                    ActionReady(Blizzard2) && TraitLevelChecked(Traits.AspectMasteryIII) && !TraitLevelChecked(Traits.UmbralHeart))
                    return OriginalHook(Blizzard2);

                if (IsEnabled(CustomComboPreset.BLM_AoE_Transpose) &&
                    ActionReady(Transpose))
                    return Transpose;
            }

            // Advanced Ice Phase AoE
            if (IcePhase)
            {
                if (HasMaxUmbralHeartStacks)
                {
                    if (IsNotEnabled(CustomComboPreset.BLM_AoE_Transpose) &&
                        CurMp == MP.MaxMP && ActionReady(Fire2) && TraitLevelChecked(Traits.AspectMasteryIII))
                        return OriginalHook(Fire2);

                    if (IsEnabled(CustomComboPreset.BLM_AoE_Transpose) &&
                        ActionReady(Transpose))
                        return Transpose;
                }

                if (ActionReady(Freeze))
                    return IsEnabled(CustomComboPreset.BLM_AoE_Blizzard4Sub) &&
                           LevelChecked(Blizzard4) && HasBattleTarget() && NumberOfEnemiesInRange(Freeze, CurrentTarget) == 2
                        ? Blizzard4
                        : Freeze;

                if (!LevelChecked(Freeze) && ActionReady(Blizzard2))
                    return OriginalHook(Blizzard2);
            }

            return actionID;
        }
    }

    // Helper combos remain the same
    internal class BLM_Variant_Raise : CustomCombo
    {
        protected internal override CustomComboPreset Preset => CustomComboPreset.BLM_Variant_Raise;

        protected override uint Invoke(uint actionID) =>
            actionID == Role.Swiftcast && Variant.CanRaise(CustomComboPreset.BLM_Variant_Raise)
                ? Variant.Raise
                : actionID;
    }

    internal class BLM_ScatheXeno : CustomCombo
    {
        protected internal override CustomComboPreset Preset => CustomComboPreset.BLM_ScatheXeno;

        protected override uint Invoke(uint actionID) =>
            actionID is Scathe && LevelChecked(Xenoglossy) && HasPolyglotStacks()
                ? Xenoglossy
                : actionID;
    }

    internal class BLM_Blizzard1to3 : CustomCombo
    {
        protected internal override CustomComboPreset Preset => CustomComboPreset.BLM_Blizzard1to3;

        protected override uint Invoke(uint actionID)
        {
            switch (actionID)
            {
                case Blizzard when LevelChecked(Blizzard3) && !IcePhase:
                    return Blizzard3;

                case Freeze when !LevelChecked(Freeze):
                    return Blizzard2;

                default:
                    return actionID;
            }
        }
    }

    internal class BLM_Fire1to3 : CustomCombo
    {
        protected internal override CustomComboPreset Preset { get; } = CustomComboPreset.BLM_Fire1to3;

        protected override uint Invoke(uint actionID) =>
            actionID is Fire &&
            (LevelChecked(Fire3) && !FirePhase ||
             HasStatusEffect(Buffs.Firestarter))
                ? Fire3
                : actionID;
    }

    internal class BLM_Between_The_LeyLines : CustomCombo
    {
        protected internal override CustomComboPreset Preset { get; } = CustomComboPreset.BLM_Between_The_LeyLines;

        protected override uint Invoke(uint actionID) =>
            actionID is LeyLines && HasStatusEffect(Buffs.LeyLines) && LevelChecked(BetweenTheLines)
                ? BetweenTheLines
                : actionID;
    }

    internal class BLM_Aetherial_Manipulation : CustomCombo
    {
        protected internal override CustomComboPreset Preset { get; } = CustomComboPreset.BLM_Aetherial_Manipulation;

        protected override uint Invoke(uint actionID) =>
            actionID is AetherialManipulation && ActionReady(BetweenTheLines) &&
            HasStatusEffect(Buffs.LeyLines) && !HasStatusEffect(Buffs.CircleOfPower) && !IsMoving()
                ? BetweenTheLines
                : actionID;
    }

    internal class BLM_UmbralSoul : CustomCombo
    {
        protected internal override CustomComboPreset Preset { get; } = CustomComboPreset.BLM_UmbralSoul;

        protected override uint Invoke(uint actionID) =>
            actionID is Transpose && IcePhase && LevelChecked(UmbralSoul)
                ? UmbralSoul
                : actionID;
    }

    internal class BLM_TriplecastProtection : CustomCombo
    {
        protected internal override CustomComboPreset Preset { get; } = CustomComboPreset.BLM_TriplecastProtection;

        protected override uint Invoke(uint actionID) =>
            actionID is Triplecast && HasStatusEffect(Buffs.Triplecast) && LevelChecked(Triplecast)
                ? All.SavageBlade
                : actionID;
    }

    internal class BLM_FireandIce : CustomCombo
    {
        protected internal override CustomComboPreset Preset { get; } = CustomComboPreset.BLM_FireandIce;

        protected override uint Invoke(uint actionID)
        {
            switch (actionID)
            {
                case Fire4 when FirePhase && LevelChecked(Fire4):
                    return Fire4;

                case Fire4 when IcePhase && LevelChecked(Blizzard4):
                    return Blizzard4;

                case Flare when FirePhase && LevelChecked(Flare):
                    return Flare;

                case Flare when IcePhase && LevelChecked(Freeze):
                    return Freeze;

                default:
                    return actionID;
            }
        }
    }

    internal class BLM_FireFlarestar : CustomCombo
    {
        protected internal override CustomComboPreset Preset { get; } = CustomComboPreset.BLM_FireFlarestar;

        protected override uint Invoke(uint actionID) =>
            actionID is Fire4 && FirePhase && FlarestarReady && LevelChecked(FlareStar) ||
            actionID is Flare && FirePhase && FlarestarReady && LevelChecked(FlareStar)
                ? FlareStar
                : actionID;
    }

    internal class BLM_Fire4to3 : CustomCombo
    {
        protected internal override CustomComboPreset Preset { get; } = CustomComboPreset.BLM_Fire4to3;
        protected override uint Invoke(uint actionID) =>
            actionID is Fire4 && !(FirePhase && LevelChecked(Fire4))
                ? Fire3
                : actionID;
    }

    internal class BLM_Blizzard4toDespair : CustomCombo
    {
        protected internal override CustomComboPreset Preset { get; } = CustomComboPreset.BLM_Blizzard4toDespair;
        protected override uint Invoke(uint actionID) =>
            actionID is Blizzard4 && FirePhase && LevelChecked(Despair)
                ? Despair
                : actionID;
    }

    internal class BLM_AmplifierXeno : CustomCombo
    {
        protected internal override CustomComboPreset Preset => CustomComboPreset.BLM_AmplifierXeno;
        protected override uint Invoke(uint actionID) =>
            actionID is Amplifier && HasMaxPolyglotStacks
                ? Xenoglossy
                : actionID;
    }
}
