using System;
using Server;
using Server.Items;
using Server.Custom.ItemSockets;

namespace Server.Custom.JewelryCrafting
{
    public class JewelryGem : Item
    {
        private string m_GemEffect;
        private double m_GemQuality = 1.0;

        [CommandProperty(AccessLevel.GameMaster)]
        public string GemEffect { get { return m_GemEffect; } set { m_GemEffect = value; InvalidateProperties(); } }

        [CommandProperty(AccessLevel.GameMaster)]
        public double GemQuality { get { return m_GemQuality; } set { m_GemQuality = value; InvalidateProperties(); } }

        [Constructable]
        public JewelryGem(string gemEffect, double quality, int itemID, int hue) : base(itemID)
        {
            Weight = 0.1;
            m_GemEffect = gemEffect;
            m_GemQuality = quality;
            Hue = hue;

            UpdateGemNameAndTier();
        }

        private void UpdateGemNameAndTier()
        {
            string tier = "Обычный";
            if (m_GemQuality >= 3.0) tier = "Легендарный";
            else if (m_GemQuality >= 2.0) tier = "Совершенный";
            else if (m_GemQuality >= 1.5) tier = "Безупречный";
            else if (m_GemQuality < 1.0) tier = "Треснувший";

            Name = $"{tier} {GetGemBaseName(m_GemEffect)}";
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            int percent = (int)Math.Round(m_GemQuality * 100);
            list.Add($"Чистота огранки: {percent}%");
            list.Add(0x35, $"Эффект: {GetGemBonusDescription(m_GemEffect, m_GemQuality)}");
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001);
                return;
            }

            SocketChisel chisel = from.Backpack.FindItemByType<SocketChisel>();
            if (chisel == null || chisel.Deleted || chisel.UsesRemaining <= 0)
            {
                from.SendMessage(0x22, "Для инкрустации этого камня вам необходимо иметь в рюкзаке Ювелирное зубило кузнеца (SocketChisel) и стоять у горна с наковальней.");
                return;
            }

            from.SendMessage(0x59, $"Выберите оружие, броню или украшение в рюкзаке, чтобы вставить [{Name}]:");
            from.Target = new SocketChisel.SelectEquipmentTarget(chisel, this);
        }

        private string GetGemBaseName(string effect)
        {
            switch (effect)
            {
                // Базовые (Ванильное соответствие)
                case "sapphire": return "Сапфир Разума";
                case "emerald": return "Изумруд Точности";
                case "diamond": return "Алмаз Защиты";
                case "citrine": return "Цитрин Выносливости";
                case "amber": return "Янтарь Снабжения";

                // Продвинутые (Ванильное соответствие)
                case "ruby": return "Рубин Урона";
                case "amethyst": return "Аметист Экономии";
                case "tourmaline": return "Турмалин Скорости";
                case "starsapphire": return "Звёздный Сапфир Жизни";

                // Высшие / Редкие ресурсные камни (Ванильное соответствие)
                case "perfectemerald": return "Совершенный Изумруд Регенерации";
                case "darksapphire": return "Тёмный Сапфир Потока";
                case "bluediamond": return "Голубой Алмаз Времени";
                case "turquoise": return "Бирюза Грома";
                case "ecrucitrine": return "Экру-Цитрин Знахаря";
                case "whitepearl": return "Белая Жемчужина Стремительности";
                case "brilliantamber": return "Сияющий Янтарь Чародея";

                // Особые и резисты
                case "vamp_shard": return "Осколок Вампиризма";
                case "firerubyslayer": return "Огненный Рубин Истребителя";
                case "physical_resist": return "Жемчужина Физической Стойкости";
                case "fire_resist": return "Огненный Рубин Стойкости";
                case "cold_resist": return "Сапфир Ледяной Стойкости";
                case "poison_resist": return "Изумруд Ядовитой Стойкости";
                case "energy_resist": return "Бирюза Энергетической Стойкости";
                default: return "Драгоценный Камень";
            }
        }

        private string GetGemBonusDescription(string effect, double quality)
        {
            switch (effect)
            {
                case "sapphire":
                    return $"+{(int)Math.Round(8.0 * quality)} Макс. Мана (Mana)";
                case "emerald":
                    return $"+{(int)Math.Round(5.0 * quality)}% HCI (Шанс попадания)";
                case "diamond":
                    return $"+{(int)Math.Round(5.0 * quality)}% DCI (Шанс защиты)";
                case "citrine":
                    return $"+{(int)Math.Round(5.0 * quality)} Стамина-Реген и +{(int)Math.Round(30.0 * quality)} Luck";
                case "amber":
                    return $"+{(int)Math.Round(10.0 * quality)}% LRC (Экономия реагентов)";
                case "ruby":
                    return $"+{(int)Math.Round(15.0 * quality)}% DI (Увеличение урона)";
                case "amethyst":
                    return $"-{(int)Math.Round(5.0 * quality)}% LMC (Снижение расхода маны)";
                case "tourmaline":
                    return $"+{(int)Math.Round(5.0 * quality)}% SSI (Скорость атаки)";
                case "starsapphire":
                    return $"+{(int)Math.Round(8.0 * quality)} Макс. Здоровье (Hits)";
                case "perfectemerald":
                    return $"+{(int)Math.Round(3.0 * quality)} ХП-Реген (Regen Hits)";
                case "darksapphire":
                    return $"+{(int)Math.Round(3.0 * quality)} Мана-Реген (Regen Mana)";
                case "bluediamond":
                    return $"+{(int)Math.Round(1.0 * quality)} Faster Casting (FC)";
                case "turquoise":
                    return $"+{(int)Math.Round(35.0 * quality)}% Шанс удара молнией (или +SDI)";
                case "ecrucitrine":
                    return $"+{(int)Math.Round(50.0 * quality)}% Усиление лечебных зелий";
                case "whitepearl":
                    return $"+{(int)Math.Round(10.0 * quality)}% SSI (Высшая скорость атаки)";
                case "brilliantamber":
                    return $"+{(int)Math.Round(1.0 * quality)} FCR и +{(int)Math.Round(10.0 * quality)}% SDI (Урон заклинаний)";
                case "vamp_shard":
                    return $"+{(int)Math.Round(20.0 * quality)}% Вампиризм (или ХП-Реген)";
                case "firerubyslayer":
                    return $"+{(int)Math.Round(20.0 * quality)}% DI (Урон) и +{(int)Math.Round(10.0 * quality)}% HCI (Точность)";
                case "physical_resist":
                    return "Повышает физический резист";
                case "fire_resist":
                    return "Повышает огненный резист";
                case "cold_resist":
                    return "Повышает резист холода";
                case "poison_resist":
                    return "Повышает ядовитый резист";
                case "energy_resist":
                    return "Повышает энергетический резист";
                default:
                    return "Магический резонанс не настроен";
            }
        }

        public JewelryGem(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); writer.Write(m_GemEffect); writer.Write(m_GemQuality); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int version = reader.ReadInt(); m_GemEffect = reader.ReadString(); m_GemQuality = reader.ReadDouble(); UpdateGemNameAndTier(); }
    }
}
