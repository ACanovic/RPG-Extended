using System.Collections.Generic;
using System.Linq;

namespace Engine
{
    public class Monster : LivingCreature
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public int MaxDamage { get; set; }
        public int RewardExperiencePoints { get; set; }
        public int RewardGold { get; set; }

        //All item drop rates for this type of monster
        public List<LootItem> LootTable { get; set; }

        //Items this instance of monster has in inventory
        internal List<InventoryItem> LootItems { get; }

        public Monster(int id, string name, int maxDamage, int rewardExperiencePoints, int rewardGold, int currentHitPoints, int maxHitPoints) : base (currentHitPoints, maxHitPoints)
        {
            ID = id;
            Name = name;
            MaxDamage = maxDamage;
            RewardExperiencePoints = rewardExperiencePoints;
            RewardGold = rewardGold;

            LootTable = new List<LootItem>();

            LootItems = new List<InventoryItem>();
        }

        internal Monster NewInstanceOfMonster()
        {
            Monster newMonster = new Monster(ID, Name, MaxDamage, RewardExperiencePoints, RewardGold, CurrentHitPoints, MaxHitPoints);

            //Add items to looted item list based on drop rates
            foreach(LootItem lootItem in LootTable.Where(lootitem => RNG.NumberBetween(1, 100) <= lootitem.DropPercentage))
            {
                newMonster.LootItems.Add(new InventoryItem(lootItem.Details, 1));
            }

            //If no items were dropped, return the default drops
            if(newMonster.LootItems.Count == 0)
            {
                foreach(LootItem lootItem in LootTable.Where(x => x.IsDefaultItem))
                {
                    newMonster.LootItems.Add(new InventoryItem(lootItem.Details, 1));
                }
            }

            return newMonster;
        }
    }
}
