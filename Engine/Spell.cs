namespace Engine
{
    public class Spell
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public int ManaCost { get; set; }
        public int Strength { get; set; }
        public string SpellType { get; set; }

        public Spell(int id, string name, int manaCost, int strength, string spellType)
        {
            ID = id;
            Name = name;
            ManaCost = manaCost;
            Strength = strength;
            SpellType = spellType;
        }
    }
}
