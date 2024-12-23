﻿using CraftingList.Utility;
using System.Collections.Generic;
using Dalamud.Game.ClientState.Conditions;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CraftingList.Crafting.Macro
{
    public partial class MacroCommand(string text, int wait, string actionName)
    {
        private static readonly Regex WaitRegex = WaitTimeRegexFunc();
        private static readonly Regex ActionNameRegex = ActionNameRegexFunc();

        private static readonly HashSet<string> CraftingActionNames = [];
        private static readonly HashSet<string> CraftingQualityActionNames = [];

        static MacroCommand()
        {
            PopulateCraftingNames();
            PopulateCraftingQualityActionNames();
        }

        public string Text { get; } = text;

        public int WaitMS { get; } = wait;

        private string actionName = actionName.ToLowerInvariant();

        public static MacroCommand Parse(string text)
        {
            var wait = 0;
            var waitMatch = WaitRegex.Match(text);
            if (waitMatch.Success)
            {
                var waitValue = waitMatch.Groups["wait"].Value;
                wait = (int)(float.Parse(waitValue) * 1000);
            }
            text = text.Remove(waitMatch.Groups["modifier"].Index, waitMatch.Groups["modifier"].Length);

            string actionName = "";
            var nameMatch = ActionNameRegex.Match(text);
            if (nameMatch.Success)
            {
                actionName = ExtractAndUnquote(nameMatch, "name");
            }

            return new MacroCommand(text, wait, actionName);
        }

        protected static string ExtractAndUnquote(Match match, string groupName)
        {
            var group = match.Groups[groupName];
            var groupValue = group.Value;

            if (groupValue.StartsWith('"') && groupValue.EndsWith('"'))
                groupValue = groupValue.Trim('"');

            return groupValue;
        }

        public async Task<bool> Execute()
        {
            Service.PluginLog.Verbose($"[MacroCommand.Execute()] Executing '{Text}'");

            if (!IsCraftingAction(actionName))
            {
                Service.PluginLog.Verbose("Not a crafting action: " + actionName);
                Service.ChatManager.SendMessage(Text);
                await Task.Delay(WaitMS);
                return true;
            }

            if (!CraftHelper.IsCrafting() && Service.Configuration.SkipCraftingActionsWhenNotCrafting)
            {
                Service.PluginLog.Debug("[MacroCommand.Execute()] Skipping action because player is not crafting.");
                return true;
            }

            Service.ChatManager.SendMessage(Text);

            await Task.Delay(200); // Give the game time to enter crafting state before looking for changes
            if (Service.Configuration.SmartWait)
            {
                if (!Service.GameEventManager.WaitTillReady(Service.Configuration.WaitDurations.CraftingActionMaxDelay))
                {
                    Service.PluginLog.Error("[MacroCommand.Execute()] Didn't receive a response after using action.");
                    return false;
                }

                //Don't check output because it can't fail (failing would entail the crafting data updating, but the action not leaving animation lock)
                await Service.WaitForCondition(ConditionFlag.Crafting40, false, Service.Configuration.WaitDurations.CraftingActionMaxDelay);

                return true;
            }

            await Task.Delay(WaitMS);
            
            return true;
        }

        private static bool IsCraftingAction(string name)
            => CraftingActionNames.Contains(name);

        private static bool IsCraftingQualityAction(string name)
            => CraftingQualityActionNames.Contains(name);

        private static void PopulateCraftingNames()
        {
            var actions = Service.DataManager.GetExcelSheet<Lumina.Excel.Sheets.Action>()!;
            foreach (var row in actions)
            {
                var job = row.ClassJob.ValueNullable?.ClassJobCategory.ValueNullable;
                if (job == null || !job.Value.CRP)
                    continue;

                var name = row.Name.ToString().ToLowerInvariant();
                if (name.Length == 0)
                    continue;

                CraftingActionNames.Add(name);           
            }

            var craftActions = Service.DataManager.GetExcelSheet<Lumina.Excel.Sheets.CraftAction>()!;
            foreach (var row in craftActions)
            {
                if (row.ClassJob.ValueNullable == null) {
                    continue;
                }

                var name = row.Name.ToString().ToLowerInvariant();
                if (name.Length == 0)
                    continue;
                CraftingActionNames.Add(name);
            }
        }
        private static void PopulateCraftingQualityActionNames()
        {
            var craftIDs = new uint[]
            {
                100002, 100016, 100031, 100046, 100061, 100076, 100091, 100106, // Basic Touch
                100004, 100018, 100034, 100048, 100064, 100078, 100093, 100109, // Standard Touch
                100411, 100412, 100413, 100414, 100415, 100416, 100417, 100418, // Advanced Touch
                100128, 100129, 100130, 100131, 100132, 100133, 100134, 100135, // Precise Touch
                100227, 100228, 100229, 100230, 100231, 100232, 100233, 100234, // Prudent Touch
                100243, 100244, 100245, 100246, 100247, 100248, 100249, 100250, // Focused Touch
                100283, 100284, 100285, 100286, 100287, 100288, 100289, 100290, // Trained Eye
                100299, 100300, 100301, 100302, 100303, 100304, 100305, 100306, // Preparatory Touch
                100339, 100340, 100341, 100342, 100343, 100344, 100345, 100346, // Byregot's Blessing
                100355, 100356, 100357, 100358, 100359, 100360, 100361, 100362, // Hasty Touch
                100435, 100436, 100437, 100438, 100439, 100440, 100441, 100442, // Trained Finesse
                // 100387, 100388, 100389, 100390, 100391, 100392, 100393, 100394, // Reflect
                // 100323, 100324, 100325, 100326, 100327, 100328, 100329, 100330, // Delicate Synthesis
            };
            

            var actionIDs = new uint[]
            {
            19004, 19005, 19006, 19007, 19008, 19009, 19010, 19011, // Innovation
            260, 261, 262, 263, 264, 265, 266, 267, // Great Strides
            };

            var actions = Service.DataManager.GetExcelSheet<Lumina.Excel.Sheets.Action>()!;
            foreach (var actionID in actionIDs)
            {
                var name = actions.GetRow(actionID)!.Name.ToString().ToLowerInvariant();
                CraftingQualityActionNames.Add(name);
            }

            var craftActions = Service.DataManager.GetExcelSheet<Lumina.Excel.Sheets.CraftAction>()!;
            foreach (var craftID in craftIDs)
            {
                var name = craftActions.GetRow(craftID)!.Name.ToString().ToLowerInvariant();
                CraftingQualityActionNames.Add(name);
            }
        }

        [GeneratedRegex("^/(?:ac|action)\\s+(?<name>.*?)\\s*$", RegexOptions.IgnoreCase | RegexOptions.Compiled, "en-US")]
        private static partial Regex ActionNameRegexFunc();
        [GeneratedRegex("(?<modifier><wait\\.(?<wait>\\d+(?:\\.\\d+)?)>)", RegexOptions.IgnoreCase | RegexOptions.Compiled, "en-US")]
        private static partial Regex WaitTimeRegexFunc();
    }
}