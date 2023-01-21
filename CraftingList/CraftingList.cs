﻿using CraftingList.Crafting;
using CraftingList.Crafting.Macro;
using CraftingList.SeFunctions;
using CraftingList.Utility;
using Dalamud.Game.Command;
using Dalamud.Logging;
using Dalamud.Plugin;
using Dalamud.Utility.Signatures;
using System.Threading.Tasks;

namespace CraftingList
{


    public sealed class CraftingList : IDalamudPlugin
    {
        public string Name => "Crafting List";

        internal Configuration Configuration { get; init; }
        internal PluginUI PluginUi { get; init; }

        internal Crafter Crafter { get; init; }

        public static void InitializeSingletons()
        {
            Singleton<OpenRecipebyRecipeId>.Set(Service.SigScanner);
            Singleton<GetBaseUiObject>.Set(Service.SigScanner);
            Singleton<GetUiObjectByName>.Set(Service.SigScanner);
            Singleton<OpenRecipeByItemId>.Set(Service.SigScanner);
            Singleton<UseAction>.Set(Service.SigScanner);
            Singleton<OpenContextMenuForAddon>.Set(Service.SigScanner);
            Singleton<AgentRepairReceiveEvent>.Set(Service.SigScanner);
            Singleton<AgentRecipeNoteReceiveEvent>.Set(Service.SigScanner);
            Singleton<AddonRepairReceiveEvent>.Set(Service.SigScanner);
            Singleton<AddonRecipeNoteReceiveEvent>.Set(Service.SigScanner);
            Singleton<AgentRecipeNoteHide>.Set(Service.SigScanner);
            Singleton<AgentRecipeMaterialListReceiveEvent>.Set(Service.SigScanner);
        }

        public CraftingList(DalamudPluginInterface pluginInterface)
        {
            Configuration = pluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            Configuration.Initialize(pluginInterface);

            Service.Initialize(pluginInterface, Configuration);
            Module.Initialize();
            SignatureHelper.Initialise(this);

            InitializeSingletons();
            MacroManager.InitializeMacros();

            Crafter = new Crafter();


            this.PluginUi = new PluginUI(this);


            Service.CommandManager.AddHandler("/craftinglist", new CommandInfo(OnCraftingList)
            {
                HelpMessage = "Create a list of items to craft"
            });
            Service.CommandManager.AddHandler("/clist", new CommandInfo(OnCraftingList)
            {
                HelpMessage = "Abbreviation for /craftinglist"
            });

            Service.CommandManager.AddHandler("/craftallitems", new CommandInfo(OnCraftAllItems)
            {
                HelpMessage = "Craft all items in your list"
            });

            Service.CommandManager.AddHandler("/clcancel", new CommandInfo(OnCancel)
            {
                HelpMessage = "Cancel current craft."
            });
            Service.CommandManager.AddHandler("/testcommand", new CommandInfo(OnCommand)
            {
                ShowInHelp = false
            });
            Service.CommandManager.AddHandler("/closerecipenote", new CommandInfo(OnCloseRecipeNote)
            {
                ShowInHelp = false
            });
        
            Service.PluginInterface.UiBuilder.Draw += DrawUI;
            //DalamudApi.PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;
        }

        public void Dispose()
        {
            SeInterface.Dispose();
            PluginUi.Dispose();
            Service.CommandManager.RemoveHandler("/craftinglist");
            Service.CommandManager.RemoveHandler("/craftallitems");
            Service.CommandManager.RemoveHandler("/clist");
            Service.CommandManager.RemoveHandler("/clcancel");
            Service.CommandManager.RemoveHandler("/testcommand");
            Service.CommandManager.RemoveHandler("/closerecipenote");

            //DalamudApi.CommandManager.RemoveHandler("/clconfig");
        }

        private void OnCommand(string command, string args)
        {
            Configuration.CanaryTestFlag = !Configuration.CanaryTestFlag;
            PluginLog.Debug($"Canary: {Configuration.CanaryTestFlag}");

            SeInterface.Repair().ClickRepairAll();
        }

        private void OnCraftingList(string command, string args)
        {

            this.PluginUi.Visible = true;
        }

        private void OnCraftAllItems(string command, string args)
        {
            Crafter.CraftAllItems();
        }

        private void OnCancel(string command, string args)
        {
            Crafter.Cancel("Cancelling craft...", false);
        }

        private void OnOpenConfig(string command, string args)
        {
            DrawConfigUI();
        }

        //I could not ever explain why, but closing the recipe note directly
        //crashes the game in some circumstances, but directing a call through 
        //the macro system doesn't.
        //I dunno.
        private void OnCloseRecipeNote(string command, string args)
        {
            SeInterface.RecipeNote().Close();
        }
        private void DrawUI()
        {
            this.PluginUi.Draw();
        }

        private void DrawConfigUI()
        {
            this.PluginUi.SettingsVisible = true;
        }
    }
}
