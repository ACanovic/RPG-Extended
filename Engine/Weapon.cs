namespace Engine
{
    public class Weapon : Item
    {
        public int MinDamage { get; set; }
        public int MaxDamage { get; set; }

        public Weapon(int id, string name, string namePlural, int minDamage, int maxDamage, int price) : base(id, name, namePlural, price)
        {
            MinDamage = minDamage;
            MaxDamage = maxDamage;
        }
    }
}