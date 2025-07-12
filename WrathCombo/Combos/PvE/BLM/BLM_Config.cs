using Dalamud.Interface.Colors;
using ImGuiNET;
using WrathCombo.CustomComboNS.Functions;
using WrathCombo.Extensions;
using static WrathCombo.Window.Functions.UserConfig;
namespace WrathCombo.Combos.PvE;

internal partial class BLM
{
    internal static class Config
    {
        public static UserInt
            BLM_SelectedOpener = new("BLM_SelectedOpener", 0),
            BLM_Balance_Content = new("BLM_Balance_Content", 1),
            BLM_ST_LeyLinesCharges = new("BLM_ST_LeyLinesCharges", 0), // More aggressive Ley Lines usage
            BLM_ST_ThunderOption = new("BLM_ST_ThunderOption", 5),     // Lower HP threshold for more DoT uptime
            BLM_ST_Thunder_SubOption = new("BLM_ST_Thunder_SubOption", 0),
            BLM_ST_Triplecast_SubOption = new("BLM_ST_Triplecast_SubOption", 0), // Always use (Patch 7.2 change)
            BLM_ST_ThunderUptime_Threshold = new("BLM_ST_ThunderUptime_Threshold", 3), // Tighter refresh timing
            BLM_ST_Triplecast_Movement = new("BLM_ST_Triplecast_Movement", 0), // More aggressive usage
            BLM_ST_Polyglot_Movement = new("BLM_ST_Polyglot_Movement", 0),     // Use for movement freely
            BLM_ST_Polyglot_Save = new("BLM_ST_Polyglot_Save", 0),             // Don't save, use for DPS
            BLM_ST_Manaward_Threshold = new("BLM_ST_Manaward_Threshold", 30),   // More proactive usage
            BLM_AoE_Triplecast_HoldCharges = new("BLM_AoE_Triplecast_HoldCharges", 0), // Use all charges
            BLM_AoE_LeyLinesCharges = new("BLM_AoE_LeyLinesCharges", 0),       // Use all charges for AoE
            BLM_AoE_ThunderHP = new("BLM_AoE_ThunderHP", 10),                   // Lower threshold for AoE DoT
            BLM_VariantCure = new("BLM_VariantCure", 50),
            // New optimized settings
            BLM_ST_FirestarterHold = new("BLM_ST_FirestarterHold", 1),          // Hold Firestarter for Ice phase
            BLM_ST_PolyglotAggression = new("BLM_ST_PolyglotAggression", 1),    // Aggressive Polyglot usage
            BLM_ST_MovementPriority = new("BLM_ST_MovementPriority", 0),        // Xenoglossy first for movement
            BLM_ST_RotationAlignment = new("BLM_ST_RotationAlignment", 1),      // Force proper rotation alignment
            BLM_ST_OpenerOptimization = new("BLM_ST_OpenerOptimization", 1);    // Enhanced opener logic

        public static UserBoolArray
            BLM_ST_MovementOption = new("BLM_ST_MovementOption"),
            BLM_ST_AdvancedOptions = new("BLM_ST_AdvancedOptions");

        public static UserIntArray
            BLM_ST_Movement_Priority = new("BLM_ST_Movement_Priority");

        internal static void Draw(CustomComboPreset preset)
        {
            switch (preset)
            {
                case CustomComboPreset.BLM_ST_Opener:
                    ImGui.TextColored(ImGuiColors.DalamudOrange, "God Tier Opener Selection:");
                    
                    DrawHorizontalRadioButton(BLM_SelectedOpener,
                        "Standard \"5+7\" Opener", 
                        "Uses the optimal Standard opener from the rotation guide.\nThis is the recommended opener for maximum DPS.",
                        0);

                    DrawHorizontalRadioButton(BLM_SelectedOpener,
                        "Alternative Flare Opener", 
                        "Uses the Alternative Flare opener with more Paradox flexibility.\nSlightly weaker but more adaptable to fight mechanics.",
                        1);

                    DrawBossOnlyChoice(BLM_Balance_Content);

                    ImGui.Spacing();
                    if (DrawCheckboxGameConfig(BLM_ST_OpenerOptimization, "Enhanced Opener Logic", 
                        "Enables advanced opener optimizations including perfect cooldown alignment and movement handling."))
                    {
                        ImGui.TextColored(ImGuiColors.DalamudGrey, 
                            "This ensures optimal opener execution even with fight mechanics.");
                    }
                    break;

                case CustomComboPreset.BLM_ST_LeyLines:
                    ImGui.TextColored(ImGuiColors.DalamudOrange, "Ley Lines Optimization (God Tier):");
                    
                    DrawSliderInt(0, 2, BLM_ST_LeyLinesCharges,
                        "Charges to keep ready (0 = Use all for maximum uptime)");

                    ImGui.TextColored(ImGuiColors.DalamudGrey, 
                        "Setting to 0 maximizes Ley Lines uptime for optimal DPS.\n" +
                        "Higher values provide more safety for mechanics.");
                    break;

                case CustomComboPreset.BLM_ST_Movement:
                    ImGui.TextColored(ImGuiColors.DalamudOrange, "Movement Priority System (Patch 7.2 Optimized):");
                    
                    DrawHorizontalMultiChoice(BLM_ST_MovementOption,
                        $"Use {Xenoglossy.ActionName()}", 
                        "Instant cast, highest priority for movement", 4, 0);
                    DrawPriorityInput(BLM_ST_Movement_Priority, 4, 0, 
                        $"{Xenoglossy.ActionName()} Priority (Recommended: 1): ");

                    DrawHorizontalMultiChoice(BLM_ST_MovementOption,
                        $"Use {Paradox.ActionName()}", 
                        "Good for movement in Fire phase", 4, 1);
                    DrawPriorityInput(BLM_ST_Movement_Priority, 4, 1, 
                        $"{Paradox.ActionName()} Priority (Recommended: 2): ");

                    DrawHorizontalMultiChoice(BLM_ST_MovementOption,
                        $"Use {Triplecast.ActionName()}", 
                        "Multiple instant casts (Patch 7.2: mainly for movement)", 4, 2);
                    DrawPriorityInput(BLM_ST_Movement_Priority, 4, 2, 
                        $"{Triplecast.ActionName()} Priority (Recommended: 3): ");

                    DrawHorizontalMultiChoice(BLM_ST_MovementOption,
                        $"Use {Role.Swiftcast.ActionName()}", 
                        "Single instant cast", 4, 3);
                    DrawPriorityInput(BLM_ST_Movement_Priority, 4, 3, 
                        $"{Role.Swiftcast.ActionName()} Priority (Recommended: 4): ");

                    ImGui.Spacing();
                    ImGui.TextColored(ImGuiColors.DalamudGrey, 
                        "Patch 7.2 Note: Triplecast/Swiftcast no longer save time on casting,\n" +
                        "so they're primarily used for movement now.");
                    break;

                case CustomComboPreset.BLM_ST_UsePolyglot:
                    ImGui.TextColored(ImGuiColors.DalamudOrange, "Polyglot Management (God Tier):");
                    
                    if (DrawSliderInt(0, 3, BLM_ST_Polyglot_Save,
                        "Charges to save for manual use (Recommended: 0 for max DPS)"))
                        if (BLM_ST_Polyglot_Movement > 3 - BLM_ST_Polyglot_Save)
                            BLM_ST_Polyglot_Movement.Value = 3 - BLM_ST_Polyglot_Save;

                    if (DrawSliderInt(0, 3, BLM_ST_Polyglot_Movement,
                        "Charges to save for movement (Recommended: 0-1)"))
                        if (BLM_ST_Polyglot_Save > 3 - BLM_ST_Polyglot_Movement)
                            BLM_ST_Polyglot_Save.Value = 3 - BLM_ST_Polyglot_Movement;

                    ImGui.Spacing();
                    DrawCheckboxGameConfig(BLM_ST_PolyglotAggression, "Aggressive Polyglot Usage",
                        "Uses Polyglot more aggressively to avoid overcapping and maximize DPS.");

                    ImGui.TextColored(ImGuiColors.DalamudGrey, 
                        "Xenoglossy is your strongest spell - don't let charges go to waste!\n" +
                        "Use them for movement, weaving, and damage optimization.");
                    break;

                case CustomComboPreset.BLM_ST_Triplecast:
                    ImGui.TextColored(ImGuiColors.DalamudOrange, "Triplecast Usage (Patch 7.2 Updated):");
                    
                    DrawHorizontalRadioButton(BLM_ST_Triplecast_SubOption,
                        "Always", "Use always for maximum mobility (Recommended).", 0);

                    DrawHorizontalRadioButton(BLM_ST_Triplecast_SubOption,
                        "Not under Leylines", "Conservative usage (not recommended for god tier play).", 1);

                    if (BLM_ST_MovementOption[0])
                        DrawSliderInt(0, 2, BLM_ST_Triplecast_Movement,
                            "Charges to save for movement (Recommended: 0)");

                    ImGui.Spacing();
                    ImGui.TextColored(ImGuiColors.DalamudGrey, 
                        "Patch 7.2: Triplecast no longer saves casting time.\n" +
                        "Use freely for movement and mechanics.");
                    break;

                case CustomComboPreset.BLM_ST_Thunder:
                    ImGui.TextColored(ImGuiColors.DalamudOrange, "Thunder DoT Optimization:");

                    DrawSliderInt(0, 50, BLM_ST_ThunderOption, 
                        "Stop using at Enemy HP % (Recommended: 5 for maximum DoT uptime)");

                    ImGui.Indent();
                    ImGui.TextColored(ImGuiColors.DalamudYellow, "HP check application:");

                    DrawHorizontalRadioButton(BLM_ST_Thunder_SubOption,
                        "Non-Bosses Only", "Recommended: Only skip DoT on trash mobs when low HP.", 0);

                    DrawHorizontalRadioButton(BLM_ST_Thunder_SubOption,
                        "All Enemies", "Less optimal: Applies HP check to all enemies.", 1);

                    DrawSliderInt(0, 10, BLM_ST_ThunderUptime_Threshold, 
                        "Refresh when DoT has X seconds remaining (Recommended: 3)");

                    ImGui.Unindent();

                    ImGui.Spacing();
                    ImGui.TextColored(ImGuiColors.DalamudGrey, 
                        "Use Thunder when DoT is falling off, not on a fixed schedule.\n" +
                        "This maximizes DoT uptime while maintaining rotation flow.");
                    break;

                case CustomComboPreset.BLM_ST_Manaward:
                    DrawSliderInt(0, 100, BLM_ST_Manaward_Threshold,
                        $"{Manaward.ActionName()} HP threshold (Recommended: 30% for proactive use)");

                    ImGui.TextColored(ImGuiColors.DalamudGrey,
                        "Lower values mean more proactive shield usage for survivability.");
                    break;

                case CustomComboPreset.BLM_AoE_LeyLines:
                    DrawSliderInt(0, 2, BLM_AoE_LeyLinesCharges,
                        $"Charges to keep ready (Recommended: 0 for max AoE DPS)");

                    ImGui.TextColored(ImGuiColors.DalamudGrey,
                        "Use all charges for maximum AoE damage in multi-target scenarios.");
                    break;

                case CustomComboPreset.BLM_AoE_Triplecast:
                    DrawSliderInt(0, 2, BLM_AoE_Triplecast_HoldCharges,
                        $"Charges to keep ready (Recommended: 0 for Flare spam)");

                    ImGui.TextColored(ImGuiColors.DalamudGrey,
                        "Use all charges to enable smooth Flare rotations in AoE.");
                    break;

                case CustomComboPreset.BLM_AoE_Thunder:
                    DrawSliderInt(0, 50, BLM_AoE_ThunderHP,
                        $"Stop using {Thunder2.ActionName()} at HP% (Recommended: 10)");

                    ImGui.TextColored(ImGuiColors.DalamudGrey,
                        "Lower threshold maintains DoT uptime longer in AoE scenarios.");
                    break;

                case CustomComboPreset.BLM_Variant_Cure:
                    DrawSliderInt(1, 100, BLM_VariantCure,
                        "HP% threshold for Variant Cure", 200);
                    break;

                // New advanced configuration section
                case CustomComboPreset.BLM_ST_AdvancedMode:
                    ImGui.TextColored(ImGuiColors.DalamudOrange, "Advanced God Tier Options:");
                    
                    DrawCheckboxGameConfig(BLM_ST_FirestarterHold, "Optimize Firestarter Usage",
                        "Holds Firestarter for Ice phase to optimize Fire III usage after Transpose.");

                    DrawCheckboxGameConfig(BLM_ST_RotationAlignment, "Force Rotation Alignment", 
                        "Ensures perfect 6 Fire IV + Paradox rotation alignment for maximum DPS.");

                    ImGui.Spacing();
                    ImGui.TextColored(ImGuiColors.DalamudGrey,
                        "These options implement advanced optimizations from the rotation guide.\n" +
                        "Enable for maximum theoretical DPS output.");
                    break;
            }
        }
    }
}
