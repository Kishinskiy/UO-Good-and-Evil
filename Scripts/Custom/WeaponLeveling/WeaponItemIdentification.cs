using System;
using System.Linq;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Targeting;

namespace Server.Custom.WeaponLeveling
{
    public static class WeaponItemIdentification
    {
        public static void Initialize()
        {
            // Откладываем на 1 тик, чтобы гарантированно перезаписать ванильный обработчик после загрузки всех скриптов
            Timer.DelayCall(TimeSpan.Zero, () =>
            {
                SkillInfo.Table[(int)SkillName.ItemID].Callback = OnUse;
            });
        }

        public static TimeSpan OnUse(Mobile from)
        {
            from.SendLocalizedMessage(500343); // What do you wish to identify?
            from.Target = new InternalTarget();

            return TimeSpan.FromSeconds(1.0);
        }

        private class InternalTarget : Target
        {
            public InternalTarget() : base(8, false, TargetFlags.None)
            {
                AllowNonlocal = true;
            }

            protected override void OnTarget(Mobile from, object o)
            {
                if (o is Item item)
                {
                    if (from.CheckTargetSkill(SkillName.ItemID, o, 0, 100))
                    {
                        if (item is Food)
                        {
                            from.SendLocalizedMessage(1041368); // You're fairly sure this is edible.
                        }
                        else
                        {
                            item.OnSingleClick(from);
                        }

                        if (item is BaseWeapon weapon)
                        {
                            ShowWeaponLevelInfo(from, weapon);
                        }
                    }
                    else
                    {
                        from.SendLocalizedMessage(500353); // You are not certain...
                    }
                }
                else if (o is Mobile mob)
                {
                    mob.OnSingleClick(from);
                }
                else
                {
                    from.SendLocalizedMessage(500353); // You are not certain...
                }
            }
        }

        private static void ShowWeaponLevelInfo(Mobile from, BaseWeapon weapon)
        {
            var data = WeaponLevelSystem.GetData(weapon, createIfMissing: false);

            if (data == null || !data.IsAwakened)
            {
                from.SendMessage(0x3B2, "Это обычное оружие. Его потенциал можно пробудить свитком закалки у кузнеца.");
                return;
            }

            double itemIdSkill = from.Skills[SkillName.ItemID].Value;

            // 1. Базовая информация доступна при любом успешном осмотре
            from.SendMessage(0x59, $"[Анализ оружия] Уровень закалки: {data.SkillValue:F1} / {data.SkillCap:F1}");

            // 2. Прогресс первого Slayer (требуется Item Identification >= 50.0)
            if (weapon.Slayer == SlayerName.None && data.SlayerKills.Count > 0)
            {
                if (itemIdSkill >= 50.0)
                {
                    var topSlayer = data.SlayerKills.OrderByDescending(kvp => kvp.Value).First();
                    int req = WeaponLevelSystem.KillsRequiredForSlayer;
                    from.SendMessage(0x35, $"Прогресс пробуждения [{topSlayer.Key}]: {topSlayer.Value} / {req} убийств.");
                }
                else
                {
                    from.SendMessage(0x3B2, "Вы чувствуете в оружии скрытую жажду крови, но не хватает навыка Item Identification (50.0), чтобы распознать её природу.");
                }
            }
            // 3. Прогресс второго Slayer (требуется Item Identification >= 80.0)
            else if (weapon.Slayer != SlayerName.None && weapon.Slayer2 == SlayerName.None)
            {
                var nextSlayer = data.SlayerKills
                    .Where(kvp => kvp.Key != weapon.Slayer)
                    .OrderByDescending(kvp => kvp.Value)
                    .FirstOrDefault();

                if (nextSlayer.Value > 0)
                {
                    if (itemIdSkill >= 80.0)
                    {
                        int req2 = WeaponLevelSystem.KillsRequiredForSlayer * 3;
                        from.SendMessage(0x35, $"Потенциал второй силы [{nextSlayer.Key}]: {nextSlayer.Value} / {req2} (требуется закалка 100.0).");
                    }
                    else
                    {
                        from.SendMessage(0x3B2, "В оружии зарождается вторая сущность, но для её анализа требуется мастерство Item Identification (80.0).");
                    }
                }
            }
        }
    }
}
