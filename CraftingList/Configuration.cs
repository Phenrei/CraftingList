﻿using CraftingList.Crafting;
using CraftingList.Crafting.Macro;
using Dalamud.Configuration;
using Dalamud.Plugin;
using System;
using System.Collections.Generic;

namespace CraftingList
{
    [Serializable]
    public struct WaitDurationHelper
    {
        public int AfterChangeJobs = 1500;
        public int AfterEatFood = 3000;
        public int AfterCompleteMacroHQ = 3500;
        public int AfterCompleteMacroCollectible = 1800;
        public int AfterExitCrafting = 2500;
        public int AfterOpenCloseMenu = 500;
        public int AfterRepairConfirm = 3250;
        public int AfterClickOffFood = 1000;
        public int QuickSynthPerItem = 3000;

        public WaitDurationHelper() { }
    }

    [Serializable]
    public class Configuration : IPluginConfiguration
    {
        public int Version { get; set; } = 1;

        public List<IngameMacro> IngameMacros { get; set; } = new();
        public List<PluginMacro> PluginMacros { get; set; } = new();

        public bool UsePluginMacros = true;

        public List<CListEntry> EntryList { get; set; } = new();

        public bool FlashWindowOnHQPrompt = true;

        public int RepairThresholdPercent = 99;

        public bool HasCraftTimeout = false;
        public int CraftTimeoutMinutes = 0;
        
        public bool OnlyRepairIfBelow99 = true;

        public WaitDurationHelper WaitDurations = new();
        public int AddonTimeout = 3000;
        public int MacroExtraTimeoutMs = 5000;

        public int SoundEffectListComplete = 6;
        public int SoundEffectListCancel = 4;
        public bool AlertOnTerminate = true;

        public float ExecuteMacroDelayMinSeconds = 1;
        public float ExecuteMacroDelayMaxSeconds = 5;

        public float ClickSynthesizeDelayMinSeconds = 1;
        public float ClickSynthesizeDelayMaxSeconds = 5;

        public bool SmartWait = false;

        public bool AcknowledgedMacroChange = false;

        public IEnumerable<CraftingMacro> GetMacros()
        {
            return UsePluginMacros ? PluginMacros : IngameMacros;
        }

        // the below exist just to make saving less cumbersome
        [NonSerialized]
        private DalamudPluginInterface? pluginInterface;

        public void Initialize(DalamudPluginInterface pluginInterface)
        {
            this.pluginInterface = pluginInterface;
        }

        public void Save()
        {
            pluginInterface!.SavePluginConfig(this);
        }
    }
}
