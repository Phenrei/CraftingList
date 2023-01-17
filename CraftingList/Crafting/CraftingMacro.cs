﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CraftingList.Crafting
{
    public abstract class CraftingMacro
    {
        protected static readonly Random randomDelay = new(DateTime.Now.Millisecond);

        public string Name = "";
        public uint FoodID = 0;
        public uint MedicineID = 0;

        public CraftingMacro(string name, uint foodID, uint medicineID)
        {
            this.Name = name;
            this.FoodID = foodID;
            this.MedicineID = medicineID;
        }

        public abstract Task<bool> Execute(bool collectible);
    }
}
