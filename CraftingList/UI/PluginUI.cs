﻿using CraftingList.Crafting;
using CraftingList.UI;
using CraftingList.Utility;
using Dalamud.Logging;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;


namespace CraftingList
{
    // It is good to have this be disposable in general, in case you ever need it
    // to do any cleanup
    unsafe class PluginUI : IDisposable
    {
        private List<ITab> Tabs;
        private const string newMacroEntryString = "New Macro...";
        private readonly CraftingList plugin;
        private readonly Configuration configuration;

        private string selectedMacroName = "";
        private string currMacroName = "";
        private int currMacroNum1 = 0;
        private int currMacroDur1 = 0;
        private int currMacroNum2 = 0;
        private int currMacroDur2 = 0;

        List<string> macroNames;

        // this extra bool exists for ImGui, since you can't ref a property
        private bool visible = false;
        public bool Visible
        {
            get { return this.visible; }
            set { this.visible = value; }
        }

        private bool settingsVisible = false;
        public bool SettingsVisible
        {
            get { return this.settingsVisible; }
            set { this.settingsVisible = value; }
        }

        public PluginUI(CraftingList plugin, Configuration configuration, Crafter crafter)
        {
            this.Tabs = new List<ITab>()
            {
                (ITab) new CraftingListTab(plugin),
            };
            this.plugin = plugin;
            this.configuration = configuration;
            macroNames = new List<string>
            {
                ""
            };


            var MealIndexes = DalamudApi.DataManager.GetExcelSheet<ItemFood>()!
                .Select(m => m.RowId);


            foreach (var macro in configuration.Macros)
            {
                macroNames.Add(macro.Name);
            }

        }

        public void Dispose()
        {
        }

        public void Draw()
        {
            // This is our only draw handler attached to UIBuilder, so it needs to be
            // able to draw any windows we might have open.
            // Each method checks its own visibility/state to ensure it only draws when
            // it actually makes sense.
            // There are other ways to do this, but it is generally best to keep the number of
            // draw delegates as low as possible.

            DrawSettingsWindow();
        }
                    
        public void DrawCraftingList()
        {
            Tabs[0].Draw();

        }

        public void DrawMacroTab()
        {
            ImGui.SetWindowFontScale(1.1f);
            ImGui.Text("Select Macro");
            ImGui.SetWindowFontScale(1f);

            float availSize = ImGui.GetWindowContentRegionMax().X - ImGui.GetWindowContentRegionMin().X;

            ImGui.SetNextItemWidth(availSize * 0.75f);
            if (ImGui.BeginCombo("##Current Macro", selectedMacroName))
            {

                if (ImGui.Selectable(""))
                {
                    selectedMacroName = "";
                }
                if (ImGui.Selectable(newMacroEntryString))
                {
                    selectedMacroName = newMacroEntryString;
                    currMacroName = "";
                    currMacroNum1 = -1;
                    currMacroDur1 = 0;
                    currMacroNum2 = -1;
                    currMacroDur2 = 0;
                }
                foreach (var macro in macroNames)
                {
                    //We want "" to be a selectable macro name for the purposes of the crafting list, but we also want it to come first in this dropdown,
                    //So we put "" as a selectable manually and skip it in this loop
                    if (macro == "") continue; 

                    bool isSelected = (selectedMacroName == macro);
                    if (ImGui.Selectable(macro, isSelected))
                    {
                        selectedMacroName = macro;
                        if (selectedMacroName != "" && selectedMacroName != newMacroEntryString)
                        {
                            var mac = configuration.Macros.Where(m => m.Name == macro).First();
                            currMacroName = mac.Name;
                            currMacroNum1 = mac.Macro1Num;
                            currMacroDur1 = mac.Macro1DurationSeconds;
                            currMacroNum2 = mac.Macro2Num;
                            currMacroDur2 = mac.Macro2DurationSeconds;
                        }
                        if (selectedMacroName == "")
                        {
                            
                        }
                    }
                    if (isSelected)
                    {
                        ImGui.SetItemDefaultFocus();
                    }
                }
                ImGui.EndCombo();
            }

            if (selectedMacroName != "")
            {
                var currMacro = configuration.Macros.Where(m => m.Name == selectedMacroName);

                ImGui.Text("Name:");
                ImGui.SameLine();
                ImGui.SetNextItemWidth((availSize * 0.75f) - ImGui.CalcTextSize("Name:").X);
                ImGui.InputText("##CurrMacroName", ref currMacroName, 50);
      

                ImGui.PushItemWidth(availSize * 0.085f);

                ImGui.Text("Macro 1 Number: ");
                ImGui.SameLine();
                ImGui.InputInt("##CurrMacroNum1", ref currMacroNum1, 0);
                ImGui.SameLine();

                ImGui.Text("Macro 1 Duration (s): ");
                ImGui.SameLine();
                ImGui.InputInt("##CurrMacroDur1", ref currMacroDur1, 0);

                ImGui.Text("Macro 2 Number: ");
                ImGui.SameLine();
                ImGui.InputInt("##CurrMacroNum2", ref currMacroNum2, 0);
                ImGui.SameLine();

                ImGui.Text("Macro 2 Duration (s): ");
                ImGui.SameLine();
                ImGui.InputInt("##CurrMacroDur2", ref currMacroDur2, 0);

                ImGui.PopItemWidth();

                if (selectedMacroName == newMacroEntryString)
                {
                    if (ImGui.Button("+", new Vector2(25, 25)) &&
                        !macroNames.Contains(currMacroName) &&
                        currMacroName != newMacroEntryString &&
                        CraftingMacro.isValidMacro(new CraftingMacro(currMacroName, currMacroNum1, currMacroDur1, currMacroNum2, currMacroDur2)))
                    {
                        configuration.Macros.Add(new CraftingMacro(currMacroName, currMacroNum1, currMacroDur1, currMacroNum2, currMacroDur2));
                        macroNames.Add(currMacroName);

                        selectedMacroName = "";

                    }
                }
                else
                {
                    if (ImGui.Button("Save", new Vector2(60, 25)))
                    {
                        if (currMacroName != "" &&
                            currMacroName != newMacroEntryString &&
                            (currMacroName == selectedMacroName || !macroNames.Contains(currMacroName)) &&
                            CraftingMacro.isValidMacro(new CraftingMacro(currMacroName, currMacroNum1, currMacroDur1, currMacroNum2, currMacroDur2)))
                        {
                            currMacro.First().Name = currMacroName;
                            macroNames[macroNames.FindIndex(m => m == selectedMacroName)] = currMacroName;
                            selectedMacroName = currMacroName;
                            currMacro.First().Macro1Num = currMacroNum1;
                            currMacro.First().Macro2Num = currMacroNum2;
                            currMacro.First().Macro1DurationSeconds = currMacroDur1;
                            currMacro.First().Macro2DurationSeconds = currMacroDur2;
                            selectedMacroName = "";
                            plugin.PluginUi.OnConfigChange();
                        }
                    }
                    ImGui.SameLine();
                    if (ImGui.Button("Cancel", new Vector2(60f, 25f)))
                    {
                        selectedMacroName = "";
                    }
                    ImGui.SameLine();
                    if (ImGui.Button("Delete...", new Vector2(60, 25)))
                    {

                        configuration.Macros.RemoveAll(m => m.Name == selectedMacroName);
                        macroNames.Remove(selectedMacroName);
                        selectedMacroName = "";
                        plugin.PluginUi.OnConfigChange();
                    }
                }
            }
        }

        public void DrawOptionsTab()
        {
            ImGui.SetWindowFontScale(1.15f);
            ImGui.Text("Options");
            ImGui.SetWindowFontScale(1f);
            ImGui.Columns(2);
            ImGui.SetColumnWidth(1, 350);
            ImGui.SetColumnWidth(2, 350);
            float availWidth = ImGui.GetColumnWidth();

            ImGui.SetNextItemWidth(340 - ImGui.CalcTextSize("Repair threshold ").X);
            ImGui.SliderInt("Repair Threshold ", ref configuration.RepairThresholdPercent, 0, 99);
            ImGui.Checkbox("Only repair if durability is below 99?", ref configuration.OnlyRepairIfBelow99);
            ImGui.NewLine();

            // auxillary variables to allow for error checking
            int completeSoundEffect = configuration.SoundEffectListComplete;
            int cancelSoundEffect = configuration.SoundEffectListCancel;
            ImGui.Checkbox("Play Sound effect when crafting terminates?", ref configuration.AlertOnTerminate);
            if (configuration.AlertOnTerminate)
            {
                ImGui.PushItemWidth(60);
                ImGui.Dummy(new Vector2(50f, 0));
                ImGui.SameLine();
                if (ImGui.InputInt("List Complete Sound Effect", ref completeSoundEffect, 0))
                {
                    if (completeSoundEffect >= 1 && completeSoundEffect <= 16) configuration.SoundEffectListComplete = completeSoundEffect;
                }

                ImGui.Dummy(new Vector2(50f, 0));
                ImGui.SameLine();
                if (ImGui.InputInt("List Cancelled Sound Effect", ref cancelSoundEffect, 0))
                {
                    if (cancelSoundEffect >= 1 && cancelSoundEffect <= 16) configuration.SoundEffectListCancel = cancelSoundEffect;
                }
                ImGui.PopItemWidth();
            }
            ImGui.NewLine();

            int extraTimeout = configuration.MacroExtraTimeoutMs;
            ImGui.SetNextItemWidth(ImGui.CalcTextSize("0000000").X);
            if (ImGui.InputInt("Extra Timeout on Macros (ms)", ref extraTimeout, 0))
            {
                if (extraTimeout > 0) configuration.MacroExtraTimeoutMs = extraTimeout;
            }

            int addonTimeout = configuration.AddonTimeout;
            ImGui.SetNextItemWidth(ImGui.CalcTextSize("0000000").X);
            if (ImGui.InputInt("Timeout on Waiting for Menus (ms)", ref addonTimeout, 0))
            {
                if (addonTimeout > 0) configuration.AddonTimeout = addonTimeout;
            }
            ImGui.NextColumn();
            ImGui.Checkbox("Flash window when prompting to select HQ Materials?", ref configuration.FlashWindowOnHQPrompt);
            ImGui.Columns(1);
        }

        public void DrawExperimentalTab()
        {
            ImGui.Text("Wait durations (ms)");
            ImGui.PushItemWidth(ImGui.CalcTextSize("0000000").X);

            object box = configuration.WaitDurations;
            foreach (var field in typeof(WaitDurationHelper).GetFields())
            {
                int toref = (int)(field.GetValue(box) ?? 2000);
                if (ImGui.InputInt(field.Name, ref toref, 0))
                {
                    field.SetValue(box, toref);
                    configuration.WaitDurations = (WaitDurationHelper)box;
                }
            }
            ImGui.PopItemWidth();
        }
        public void DrawSettingsWindow()
        {
            if (!Visible)
            {
                return;
            }

            ImGui.SetNextWindowSizeConstraints(new Vector2(725f, 375f), new Vector2(float.PositiveInfinity, float.PositiveInfinity));
            if (ImGui.Begin("Crafting List", ref this.visible,
                 ImGuiWindowFlags.None))
            {
                ImGui.BeginTabBar("##ConfigTab");
                if (ImGui.BeginTabItem("Crafting List"))
                {
                    DrawCraftingList();
                    ImGui.EndTabItem();
                }
                if (ImGui.BeginTabItem("Macros"))
                {
                    DrawMacroTab();
                    //ImGui.NewLine();
                    //DrawNewMacro();

                    ImGui.EndTabItem();
                }
                if (ImGui.BeginTabItem("Other"))
                {
                    DrawOptionsTab();
                    ImGui.EndTabItem();
                }
                if (ImGui.BeginTabItem("Experimental"))
                {
                    DrawExperimentalTab();
                    ImGui.EndTabItem();
                }
                ImGui.EndTabBar();




            }
            configuration.Save();
            ImGui.End();
        }

        public void OnConfigChange()
        {
            foreach (var tab in Tabs)
            {
                tab.OnConfigChange();
            }
        }


    }
}
