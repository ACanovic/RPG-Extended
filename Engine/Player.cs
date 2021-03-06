﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Xml;

namespace Engine
{
    public class Player : LivingCreature
    {
        private int _gold;
        private int _experiencePoints;
        private Location _currentLocation;
        private int _mana;
        public int MaxMana { get; set; }

        public event EventHandler<MessageEventArgs> OnMessage;

        public int Mana
        {
            get { return _mana; }
            set
            {
                _mana = value;
                OnPropertyChanged("Mana");
            }
        }
        
        public int Gold
        {
            get { return _gold; }
            set
            {
                _gold = value;
                OnPropertyChanged("Gold");
            }
        }

        public int ExperiencePoints
        {
            get { return _experiencePoints; }
            private set
            {
                _experiencePoints = value;
                OnPropertyChanged("ExperiencePoints");
                OnPropertyChanged("Level");
            }
        }

        public int Level
        {
            get { return ((ExperiencePoints / 100) + 1); }
        }

        public Location CurrentLocation
        {
            get { return _currentLocation; }
            set
            {
                _currentLocation = value;
                OnPropertyChanged("CurrentLocation");
            }
        }

        public Weapon CurrentWeapon { get; set; }

        public BindingList<InventoryItem> Inventory { get; set; }

        public List<Weapon> Weapons
        {
            get { return Inventory.Where(x => x.Details is Weapon).Select(x => x.Details as Weapon).ToList(); }
        }

        public List<HealingPotion> Potions
        {
            get { return Inventory.Where(x => x.Details is HealingPotion).Select(x => x.Details as HealingPotion).ToList(); }
        }

        public BindingList<Spell> Spells { get; set; }

        public BindingList<PlayerQuest> Quests { get; set; }

        private Monster CurrentMonster { get; set; }

        private Player(int currentHitPoints, int maxHitPoints, int gold, int experiencePoints, int mana, int maxMana) : base(currentHitPoints, maxHitPoints)
        {
            Gold = gold;
            ExperiencePoints = experiencePoints;
            Mana = mana;
            MaxMana = maxMana;

            Spells = new BindingList<Spell>();
            Inventory = new BindingList<InventoryItem>();
            Quests = new BindingList<PlayerQuest>();
        }

        public static Player CreateDefaultPlayer()
        {
            Player player = new Player(10, 10, 20, 0, 10, 10);
            player.Inventory.Add(new InventoryItem(World.ItemByID(World.ITEM_ID_RUSTY_SWORD), 1));
            player.Spells.Add(World.SpellByID(World.SPELL_ID_FIREBALL));
            player.CurrentLocation = World.LocationByID(World.LOCATION_ID_HOME);

            return player;
        }

        public static Player CreatePlayerFromXmlstring(string xmlPlayerData)
        {
            try
            {
                XmlDocument playerData = new XmlDocument();

                playerData.LoadXml(xmlPlayerData);

                int currentHitPoints = Convert.ToInt32(playerData.SelectSingleNode("/Player/Stats/CurrentHitPoints").InnerText);
                int maxHitPoints = Convert.ToInt32(playerData.SelectSingleNode("/Player/Stats/MaxHitPoints").InnerText);
                int gold = Convert.ToInt32(playerData.SelectSingleNode("/Player/Stats/Gold").InnerText);
                int experiencePoints = Convert.ToInt32(playerData.SelectSingleNode("/Player/Stats/ExperiencePoints").InnerText);
                int mana = Convert.ToInt32(playerData.SelectSingleNode("/Player/Stats/Mana").InnerText);
                int maxMana = Convert.ToInt32(playerData.SelectSingleNode("/Player/Stats/MaxMana").InnerText);


                Player player = new Player(currentHitPoints, maxHitPoints, gold, experiencePoints, mana, maxMana);

                int currentLocationID = Convert.ToInt32(playerData.SelectSingleNode("/Player/Stats/CurrentLocation").InnerText);
                player.CurrentLocation = World.LocationByID(currentLocationID);

                if (playerData.SelectSingleNode("/Player/Stats/CurrentWeapon") != null)
                {
                    int currentWeaponID = Convert.ToInt32(playerData.SelectSingleNode("/Player/Stats/CurrentWeapon").InnerText);
                    player.CurrentWeapon = (Weapon)World.ItemByID(currentWeaponID);
                }

                foreach (XmlNode node in playerData.SelectNodes("/Player/InventoryItems/InventoryItem"))
                {
                    int id = Convert.ToInt32(node.Attributes["ID"].Value);
                    int quantity = Convert.ToInt32(node.Attributes["Quantity"].Value);

                    for (int i = 0; i < quantity; i++)
                    {
                        player.AddItemToInventory(World.ItemByID(id));
                    }
                }

                foreach (XmlNode node in playerData.SelectNodes("/Player/PlayerQuests/PlayerQuest"))
                {
                    int id = Convert.ToInt32(node.Attributes["ID"].Value);
                    bool isCompleted = Convert.ToBoolean(node.Attributes["IsCompleted"].Value);

                    PlayerQuest playerQuest = new PlayerQuest(World.QuestByID(id));
                    playerQuest.IsCompleted = isCompleted;

                    player.Quests.Add(playerQuest);
                }

                return player;
            }
            catch
            {
                //If there was an error with the document, return a default player object instead
                return Player.CreateDefaultPlayer();
            }
        }   
        
        public bool HasRequiredItemToEnterThisLocation(Location location)
        {
            if(location.ItemRequiredToEnter == null)
            {
                //No item required
                return true;
            }

            //Check inventory for item
            return Inventory.Any(ii => ii.Details.ID == location.ItemRequiredToEnter.ID);
        }

        private void SetCurrentMonsterForCurrentLocation(Location location)
        {
            CurrentMonster = location.NewInstanceOfMonsterLivingHere();

            if(CurrentMonster != null)
            {
                RaiseMessage("You see a " + location.MonsterLivingHere.Name);
            }
        }

        private bool PlayerDoesNotHaveRequiredItem(Location location)
        {
            return !HasRequiredItemToEnterThisLocation(location);
        }

        private bool PlayerDoesNotHaveThisQuest(Quest quest)
        {
            return Quests.All(pq => pq.Details.ID != quest.ID);
        }

        private bool PlayerHasNotCompleted(Quest quest)
        {
            return Quests.Any(pq => pq.Details.ID == quest.ID && !pq.IsCompleted);
        }

        private void GiveQuestToPlayer(Quest quest)
        {
            RaiseMessage("You receive the " + quest.Name + " quest.");
            RaiseMessage(quest.Description);
            RaiseMessage("To complete it, return with: ");

            foreach(QuestCompletionItem qci in quest.QuestCompletionItems)
            {
                RaiseMessage(string.Format("{0} {1}", qci.Quantity, qci.Quantity == 1 ? qci.Details.Name : qci.Details.NamePlural));
            }

            RaiseMessage("");

            Quests.Add(new PlayerQuest(quest));
        }

        public bool PlayerHasAllQuestCompletionItemsFor(Quest quest)
        {
            //See if player has all items needed
            foreach(QuestCompletionItem qci in quest.QuestCompletionItems)
            {
                if(!Inventory.Any(ii => ii.Details.ID == qci.Details.ID && ii.Quantity >= qci.Quantity))
                {
                    return false;
                }
            }

            //Can only reach here if player has all items needed, and sufficient quantity
            return true;
        }

        public void RemoveQuestCompletionItems(Quest quest)
        {
            foreach(QuestCompletionItem qci in quest.QuestCompletionItems)
            {
                InventoryItem item = Inventory.SingleOrDefault(ii => ii.Details.ID == qci.Details.ID);

                if(item != null)
                {
                    RemoveItemFromInventory(item.Details, qci.Quantity);
                }
            }
        }

        public void AddExperiencePoints(int experiencePointsToAdd)
        {
            ExperiencePoints += experiencePointsToAdd;
            MaxHitPoints = (Level * 10);
        }

        private void GivePlayerQuestRewards(Quest quest)
        {
            RaiseMessage("");
            RaiseMessage("You completed the " + quest.Name + " quest.");
            RaiseMessage("You receive; ");
            RaiseMessage(quest.RewardExperiencePoints + " experience points");
            RaiseMessage(quest.RewardGold + " gold");
            RaiseMessage(quest.RewardItem.Name, true);

            AddExperiencePoints(quest.RewardExperiencePoints);
            Gold += quest.RewardGold;

            RemoveQuestCompletionItems(quest);
            AddItemToInventory(quest.RewardItem);

            MarkQuestCompleted(quest);
        }

        public void MarkQuestCompleted(Quest quest)
        {
            PlayerQuest playerQuest = Quests.SingleOrDefault(pq => pq.Details.ID == quest.ID);

            if (playerQuest != null)
            {
                playerQuest.IsCompleted = true;
            }
        }

        private void LetMonsterAttack()
        {
            int damageToPlayer = RNG.NumberBetween(0, CurrentMonster.MaxDamage);

            RaiseMessage("The " + CurrentMonster.Name + " did " + damageToPlayer + " points of damage.");

            CurrentHitPoints -= damageToPlayer;

            if (IsDead)
            {
                RaiseMessage("The " + CurrentMonster.Name + " killed you.");

                MoveHome();
            }
        }

        private void HealPlayer(int hitPointsToHeal)
        {
            CurrentHitPoints = Math.Min(CurrentHitPoints + hitPointsToHeal, MaxHitPoints);
        }

        private void CompletelyHeal()
        {
            CurrentHitPoints = MaxHitPoints;
            Mana = MaxMana;
        }

        public void MoveTo(Location location)
        {
            if (PlayerDoesNotHaveRequiredItem(location))
            {
                RaiseMessage("You must have a " + location.ItemRequiredToEnter.Name + " to enter this location.");
                return;
            }

            //Otherwise player can enter
            CurrentLocation = location;

            CompletelyHeal();

            if (location.HasAQuest)
            {
                if (PlayerDoesNotHaveThisQuest(location.QuestAvailableHere))
                {
                    GiveQuestToPlayer(location.QuestAvailableHere);
                }
                else
                {
                    if (PlayerHasNotCompleted(location.QuestAvailableHere) && PlayerHasAllQuestCompletionItemsFor(location.QuestAvailableHere))
                    {
                        GivePlayerQuestRewards(location.QuestAvailableHere);
                    }
                }
            }

            SetCurrentMonsterForCurrentLocation(location);
        }

        public void MoveNorth()
        {
            if (CurrentLocation.LocationToNorth != null)
            {
                MoveTo(CurrentLocation.LocationToNorth);
            }
        }

        public void MoveEast()
        {
            if (CurrentLocation.LocationToEast != null)
            {
                MoveTo(CurrentLocation.LocationToEast);
            }
        }

        public void MoveSouth()
        {
            if (CurrentLocation.LocationToSouth != null)
            {
                MoveTo(CurrentLocation.LocationToSouth);
            }
        }

        public void MoveWest()
        {
            if (CurrentLocation.LocationToWest != null)
            {
                MoveTo(CurrentLocation.LocationToWest);
            }
        }

        private void MoveHome()
        {
            MoveTo(World.LocationByID(World.LOCATION_ID_HOME));
        }

        private void CreateNewXmlChildNode(XmlDocument document, XmlNode parentNode, string elementName, object value)
        {
            XmlNode node = document.CreateElement(elementName);
            node.AppendChild(document.CreateTextNode(value.ToString()));
            parentNode.AppendChild(node);
        }

        private void AddXmlAttributeToNode(XmlDocument document, XmlNode node, string attributeName, object value)
        {
            XmlAttribute attribute = document.CreateAttribute(attributeName);
            attribute.Value = value.ToString();
            node.Attributes.Append(attribute);
        }

        private void RaiseInventoryChangedEvent(Item item)
        {
            if (item is Weapon)
            {
                OnPropertyChanged("Weapons");
            }

            if (item is HealingPotion)
            {
                OnPropertyChanged("Potions");
            }
        }

        public void AddItemToInventory(Item itemToAdd, int quantity = 1)
        {
            InventoryItem item = Inventory.SingleOrDefault(ii => ii.Details.ID == itemToAdd.ID);

            if(item == null)
            {
                //No copy exists, add to inventory
                Inventory.Add(new InventoryItem(itemToAdd, quantity));
            }
            else
            {
                item.Quantity += quantity;
            }

            RaiseInventoryChangedEvent(itemToAdd);
        }

        public void RemoveItemFromInventory(Item itemToRemove, int quantity = 1)
        {
            InventoryItem item = Inventory.SingleOrDefault(ii => ii.Details.ID == itemToRemove.ID);

            if (item == null)
            {
                //Might want to raise an error here, currently ignore if player doesn't have item
            }
            else
            {
                item.Quantity -= quantity;

                //Might want to raise error for attempted negative inventory
                if (item.Quantity < 0)
                {
                    item.Quantity = 0;
                }

                //Remove item from list if quantity 0
                if (item.Quantity == 0)
                {
                    Inventory.Remove(item);
                }

                //Notify UI
                RaiseInventoryChangedEvent(itemToRemove);
            }
        }

        public void UseWeapon(Weapon weapon)
        {
            int damage = RNG.NumberBetween(weapon.MinDamage, weapon.MaxDamage);

            if (damage == 0)
            {
                RaiseMessage("You missed the " + CurrentMonster.Name);
            }
            else
            {
                CurrentMonster.CurrentHitPoints -= damage;
                RaiseMessage("You hit the" + CurrentMonster.Name + " for " + damage + " points.");

            }

            //Check if the monster died
            if (CurrentMonster.IsDead)
            {
                LootCurrentMonster();

                //Refresh location
                MoveTo(CurrentLocation);
            }
            else
            {
                LetMonsterAttack();
            }
        }

        private void LootCurrentMonster()
        {
            RaiseMessage("");
            RaiseMessage("You defeated the " + CurrentMonster.Name);
            RaiseMessage("You receive " + CurrentMonster.RewardExperiencePoints + " experience points");
            RaiseMessage("You receive " + CurrentMonster.RewardGold + " gold");

            AddExperiencePoints(CurrentMonster.RewardExperiencePoints);
            Gold += CurrentMonster.RewardGold;

            // Give monster's loot items to the player
            foreach (InventoryItem inventoryItem in CurrentMonster.LootItems)
            {
                AddItemToInventory(inventoryItem.Details);

                RaiseMessage(string.Format("You loot {0} {1}", inventoryItem.Quantity, inventoryItem.Description));
            }

            RaiseMessage("");
        }

        public void UsePotion(HealingPotion potion)
        {
            RaiseMessage("You drink a " + potion.Name);

            HealPlayer(potion.AmountToHeal);

            RemoveItemFromInventory(potion);

            LetMonsterAttack();
        }

        public string ToXmlString()
        {
            XmlDocument playerData = new XmlDocument();

            //Create top level XML node
            XmlNode player = playerData.CreateElement("Player");
            playerData.AppendChild(player);

            //Create stats node to hold other player statistic nodes
            XmlNode stats = playerData.CreateElement("Stats");
            player.AppendChild(stats);

            //Create child nodes for Stats
            CreateNewXmlChildNode(playerData, stats, "CurrentHitPoints", CurrentHitPoints);
            CreateNewXmlChildNode(playerData, stats, "MaximumHitPoints", MaxHitPoints);
            CreateNewXmlChildNode(playerData, stats, "Gold", Gold);
            CreateNewXmlChildNode(playerData, stats, "ExperiencePoints", ExperiencePoints);
            CreateNewXmlChildNode(playerData, stats, "Level", Level);

            if (CurrentWeapon != null)
            {
                CreateNewXmlChildNode(playerData, stats, "CurrentWeapon", CurrentWeapon);
            }

            //Create the InventoryItems node to hold each item
            XmlNode inventoryItems = playerData.CreateElement("InventoryItems");
            player.AppendChild(inventoryItems);

            //Create a node for each item in the inventory
            foreach(InventoryItem item in this.Inventory)
            {
                XmlNode inventoryItem = playerData.CreateElement("InventoryItem");

                AddXmlAttributeToNode(playerData, inventoryItem, "ID", item.Details.ID);
                AddXmlAttributeToNode(playerData, inventoryItem, "Quantity", item.Quantity);

                inventoryItems.AppendChild(inventoryItem);
            }

            //Create the PlayerQuests node to hold each quest
            XmlNode playerQuests = playerData.CreateElement("PlayerQuests");
            player.AppendChild(playerQuests);

            //Create a PlayerQuest node for each quest
            foreach(PlayerQuest quest in this.Quests)
            {
                XmlNode playerQuest = playerData.CreateElement("PlayerQuest");

                AddXmlAttributeToNode(playerData, playerQuest, "ID", quest.Details.ID);
                AddXmlAttributeToNode(playerData, playerQuest, "IsCompleted", quest.IsCompleted);

                playerQuests.AppendChild(playerQuest);
            }

            return playerData.InnerXml; 
        }

        private void RaiseMessage(string message, bool addExtraNewLine = false)
        {
            if (OnMessage != null)
            {
                OnMessage(this, new MessageEventArgs(message, addExtraNewLine));
            }
        }

        public void CastSpell(Spell spell)
        {
            if(Mana >= spell.ManaCost)
            {
                Mana -= spell.ManaCost;

                RaiseMessage("You cast " + spell.Name);

                int damage = spell.Strength;

                CurrentMonster.CurrentHitPoints -= damage;
                RaiseMessage("You hit the" + CurrentMonster.Name + " for " + damage + " points.");

                //Check if the monster died
                if (CurrentMonster.IsDead)
                {
                    LootCurrentMonster();

                    //Refresh location
                    MoveTo(CurrentLocation);
                }
                else
                {
                    LetMonsterAttack();
                }
            }
            else
            {
                RaiseMessage("You do not have enough mana to cast " + spell.Name);
            }
        }
    }
}
