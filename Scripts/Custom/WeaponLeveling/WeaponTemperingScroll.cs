using System;
using Server;
using Server.Engines.Craft;
using Server.Items;
using Server.Mobiles;
using Server.Targeting;

namespace Server.Custom.WeaponLeveling
{
    public class WeaponTemperingScroll : Item
    {
        private const double MinBlacksmithy = 60.0;

        [Constructable]
        public WeaponTemperingScroll() : base(0x14F0)
        {
            Name = "Свиток древней закалки оружия";
            Hue = 0x489;
            Weight = 1.0;
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);
            list.Add(1060658, "Эффект\tПробуждает потенциал роста оружия");
            list.Add(1060659, "Требования\tBlacksmithy 60.0–115.0, горн и наковальня");
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
                return;
            }

            if (!CheckForgeAndBaseSkill(from, out string failReason))
            {
                from.SendMessage(0x22, failReason);
                return;
            }

            from.SendMessage(0x59, "Выберите оружие в рюкзаке, которое хотите закалить в пламени горна:");
            from.Target = new InternalTarget(this);
        }

        private static bool CheckForgeAndBaseSkill(Mobile from, out string failReason)
        {
            failReason = null;

            if (from.Skills[SkillName.Blacksmith].Value < MinBlacksmithy)
            {
                failReason = $"Для работы со свитком закалки требуется навык кузнечного дела (Blacksmithy) не ниже {MinBlacksmithy:F1}!";
                return false;
            }

            DefBlacksmithy.CheckAnvilAndForge(from, 3, out bool anvil, out bool forge);
            if (!anvil || !forge)
            {
                failReason = "Для закалки оружия вы должны стоять рядом с наковальней и кузнечным горном (Forge)!";
                return false;
            }

            return true;
        }

        // Расчёт требуемого навыка Blacksmithy в зависимости от ранга и магии оружия
        public static double GetRequiredSkillForWeapon(BaseWeapon weapon, out string tierName)
        {
            // 1. Легендарные артефакты (ArtifactRarity >= 10) -> требуется 115.0 (Power Scroll)
            if (weapon.ArtifactRarity >= 10)
            {
                tierName = "Легендарный артефакт";
                return 115.0;
            }

            // 2. Малые и обычные артефакты (ArtifactRarity 1..9 или IsArtifact) -> требуется 105.0 (Power Scroll)
            if (weapon.ArtifactRarity > 0 || weapon.IsArtifact)
            {
                tierName = "Артефактное оружие";
                return 105.0;
            }

            // 3. Магическое/зачарованное оружие (есть бонусы атрибутов, урон, скорость, слеер и т.д.) -> требуется 85.0
            if (HasMagicalProperties(weapon))
            {
                tierName = "Магическое оружие";
                return 85.0;
            }

            // 4. Обычное или исключительное (Exceptional) оружие -> достаточно базовых 60.0
            tierName = "Обычное оружие";
            return 60.0;
        }

        private static bool HasMagicalProperties(BaseWeapon weapon)
        {
            if (weapon.Slayer != SlayerName.None || weapon.Slayer2 != SlayerName.None)
                return true;

            if (weapon.ExtendedWeaponAttributes != null && !weapon.ExtendedWeaponAttributes.IsEmpty)
                return true;

            if (weapon.Attributes != null && !weapon.Attributes.IsEmpty)
                return true;

            if (weapon.WeaponAttributes != null && !weapon.WeaponAttributes.IsEmpty)
                return true;

            return false;
        }

        public static double GetSuccessChance(double playerSkill, double reqSkill)
        {
            if (playerSkill < reqSkill)
                return 0.0;

            double chance = 0.05 + ((playerSkill - reqSkill) * 0.19);
            return Math.Min(1.0, Math.Max(0.0, chance));
        }

        private class InternalTarget : Target
        {
            private readonly WeaponTemperingScroll _scroll;

            public InternalTarget(WeaponTemperingScroll scroll) : base(-1, false, TargetFlags.None)
            {
                _scroll = scroll;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (_scroll == null || _scroll.Deleted)
                    return;

                if (!_scroll.IsChildOf(from.Backpack))
                {
                    from.SendLocalizedMessage(1042001);
                    return;
                }

                if (!CheckForgeAndBaseSkill(from, out string failReason))
                {
                    from.SendMessage(0x22, failReason);
                    return;
                }

                if (!(targeted is BaseWeapon weapon) || weapon is Fists)
                {
                    from.SendMessage(0x22, "Этот свиток можно применить только к оружию.");
                    return;
                }

                if (!weapon.IsChildOf(from.Backpack))
                {
                    from.SendMessage(0x22, "Оружие должно находиться в вашем рюкзаке (снимите его с рук перед закалкой).");
                    return;
                }

                var data = WeaponLevelSystem.GetData(weapon, createIfMissing: true);
                if (data.IsAwakened)
                {
                    from.SendMessage(0x22, "Это оружие уже прошло ритуал закалки!");
                    return;
                }

                // Проверяем конкретный порог навыка под выбранный клинок
                double reqSkill = GetRequiredSkillForWeapon(weapon, out string tierName);
                double playerSkill = from.Skills[SkillName.Blacksmith].Value;

                if (playerSkill < reqSkill)
                {
                    from.SendMessage(0x22, $"Категория предмета: [{tierName}]. Для его закалки требуется навык Blacksmithy {reqSkill:F1} (ваш навык: {playerSkill:F1}).");
                    return;
                }

                // Считаем шанс успеха и тратим свиток при попытке
                double successChance = GetSuccessChance(playerSkill, reqSkill);
                _scroll.Delete();

                if (Utility.RandomDouble() < successChance)
                {
                    // Успешная закалка
                    data.IsAwakened = true;

                    from.PlaySound(0x2A);
                    from.FixedParticles(0x373A, 10, 15, 5018, EffectLayer.Waist);
                    from.SendMessage(0x35, $"Вы успешно закалили [{tierName}] в пламени горна (шанс был {successChance * 100:F0}%)! Теперь оружие будет развиваться в боях.");
                }
                else
                {
                    // Неудача: свиток сгорел, оружие осталось целым
                    from.PlaySound(0x54);
                    from.FixedParticles(0x3735, 1, 30, 9503, EffectLayer.Waist);
                    from.SendMessage(0x22, $"Ритуал сорвался (шанс успеха был {successChance * 100:F0}%)! Магия свитка рассеялась в пламени горна, но само оружие не пострадало.");
                }
            }
        }

        public WeaponTemperingScroll(Serial serial) : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            reader.ReadInt();
        }
    }
}
