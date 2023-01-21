﻿using CraftingList.Utility;
using Dalamud.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace CraftingList.Crafting.Macro
{
    internal class MacroManager
    {
        protected static readonly Random randomDelay = new(DateTime.Now.Millisecond);

        public static List<PluginMacro> PluginMacros
            => Service.Configuration.PluginMacros;

        public static List<IngameMacro> IngameMacros
            => Service.Configuration.IngameMacros;

        public static List<string> MacroNames { get; set; } = new() { "<Quick Synth>" };

        public static void InitializeMacros()
        {
            foreach (var macro in PluginMacros)
            {
                MacroNames.Add(macro.Name);
            }
            foreach (var macro in IngameMacros)
            {
                MacroNames.Add(macro.Name);
            }
        }
        public static CraftingMacro? GetMacro(string macroName)
        {
            return GetMacroFromList(PluginMacros, macroName) ?? GetMacroFromList(IngameMacros, macroName) ?? null;
        }

        public static CraftingMacro? GetMacroFromList(IEnumerable<CraftingMacro> macros, string macroName)
        {
            foreach (var macro in macros)
            {
                if (macro.Name == macroName)
                    return macro;
            }

            return null;
        }

        public static bool ExistsMacro(string macroName)
        {
            foreach (var name in MacroNames)
            {
                if (macroName == name)
                    return true;
            }
            return false;
        }

        public static bool ExistsMacroInList(IEnumerable<CraftingMacro> macroList, string macroName)
        {
            foreach (var macro in macroList)
            {
                if (macro.Name == macroName)
                    return true;
            }
            return false;
        }

        public static string GenerateUniqueName(IEnumerable<string> names, string baseNewName)
        {
            string modifier = "";
            int i = 1;
            while (names.Contains(baseNewName + modifier))
            {
                modifier = $" ({i++})";
            }

            return baseNewName + modifier;
        }

        public static void AddEmptyPluginMacro(string newName)
        {
            var newMacro = new PluginMacro(GenerateUniqueName(MacroNames, newName), 0, 0, "");
            
            PluginMacros.Add(newMacro);
            MacroNames.Add(newMacro.Name);
        }

        public static void AddEmptyIngameMacro(string newName)
        {
            var newMacro = new IngameMacro(newName, -1, -1);

            IngameMacros.Add(newMacro);
            MacroNames.Add(newMacro.Name);
        }

        public static void RemoveMacro(string macroName)
        {
            if (PluginMacros.RemoveAll(m => m.Name == macroName) > 0)
                MacroNames.Remove(macroName);

            else if (IngameMacros.RemoveAll(m => m.Name == macroName) > 0)
                MacroNames.Remove(macroName);
        }

        public static void RenameMacro(string currName, string newName)
        {
            var macro = GetMacro(currName);
            if (macro == null)
                return;
            
            macro.Name = GenerateUniqueName(MacroNames, newName);

            int index = MacroNames.IndexOf(currName);
            if (index == -1)
            {
                PluginLog.Error($"Name '{currName}' exists in Macro list, but not in MacroNames. Adding to MacroNames, but this should never happen.");
                MacroNames.Add(newName);
                return;
            }

            MacroNames[index] = newName;
            
            
        }

        public static IEnumerable<MacroCommand> Parse(string macroText)
        {
            var line = string.Empty;
            using var reader = new StringReader(macroText);

            while ((line = reader.ReadLine()) != null)
            {
                line = line.Trim();

                if (line.Length == 0)
                {
                    continue;
                }

                yield return MacroCommand.Parse(line);
            }
            yield break;
        }

        public static async Task<bool> ExecuteMacroCommands(IEnumerable<MacroCommand> commands)
        {
            foreach (var command in commands)
            {
                try
                {
                    if (!await command.Execute())
                    {
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    PluginLog.Error(ex.Message);
                    return false;
                }
            }

            var recipeNote = SeInterface.WaitForAddon("RecipeNote", true,
                Service.Configuration.MacroExtraTimeoutMs);

            try { recipeNote.Wait(); }
            catch {
                PluginLog.Error("RecipeNote wait timed out.");
                return false;
            }

            await Task.Delay(randomDelay.Next((int)Service.Configuration.ExecuteMacroDelayMinSeconds * 1000,
                                              (int)Service.Configuration.ExecuteMacroDelayMaxSeconds * 1000)
            );
            await Task.Delay(Service.Configuration.WaitDurations.AfterOpenCloseMenu);
            return true;
        }
    }
}
