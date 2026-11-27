using System;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Network;

namespace Server.Custom.VirtueQuests
{
    public class AncientLoreTome : Item
    {
        private bool m_IsDecoded;
        private string m_LoreTitle;
        private string m_LoreText;
        private Timer m_StudyTimer;

        [CommandProperty(AccessLevel.GameMaster)]
        public bool IsDecoded
        {
            get => m_IsDecoded;
            set { m_IsDecoded = value; UpdateAppearance(); InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string LoreText
        {
            get => m_LoreText;
            set { m_LoreText = value; InvalidateProperties(); }
        }

        [Constructable]
        public AncientLoreTome() : base(0x2254) // Графика древнего фолианта
        {
            Weight = 2.0;
            UpdateAppearance();
        }

        public AncientLoreTome(Serial serial) : base(serial)
        {
        }

        private void UpdateAppearance()
        {
            if (m_IsDecoded)
            {
                Name = "Расшифрованный фолиант древних знаний";
                Hue = 1153; // Светло-голубой/магический оттенок изученной книги
            }
            else
            {
                Name = "Древний неизученный фолиант";
                Hue = 2418; // Старый бронзовый/пыльный оттенок
            }
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            if (m_IsDecoded)
            {
                list.Add(1060658, "Статус\tРасшифрован (Готов к чтению)");
                if (!string.IsNullOrEmpty(m_LoreTitle))
                    list.Add(1060659, "Раздел\t{0}", m_LoreTitle);
            }
            else
            {
                list.Add(1060658, "Статус\tЗапечатан древними рунами");
                list.Add(1060659, "Требование\tEvaluating Intelligence (от 40.0)");
            }
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001); // Книга должна находиться в вашем рюкзаке.
                return;
            }

            // Если книга уже изучена — сразу открываем гамп с сохраненной подсказкой
            if (m_IsDecoded)
            {
                from.PlaySound(0x55); // Звук перелистывания страниц
                from.SendGump(new VirtueLoreBookGump(m_LoreTitle ?? "Древний манускрипт", m_LoreText));
                return;
            }

            // Если еще не изучена — запускаем процесс расшифровки
            StartStudying(from);
        }

        public void StartStudying(Mobile from)
        {
            if (m_StudyTimer != null && m_StudyTimer.Running)
            {
                from.SendMessage(0x22, "Вы уже сосредоточены на изучении этой книги!");
                return;
            }

            double evalSkill = from.Skills[SkillName.EvalInt].Value;

            if (evalSkill < 40.0)
            {
                from.SendMessage(0x22, "Ваш навык Evaluating Intelligence слишком низок (требуется минимум 40.0), чтобы понять эти руны.");
                return;
            }

            // Расчет скорости изучения: от 12 секунд (на 40 навыка) до 3 секунд (на 120 навыка)
            double delaySeconds = Math.Max(3.0, 15.0 - (evalSkill / 10.0));

            from.SendMessage(0x35, $"Вы начинаете расшифровывать древние символы... (Время изучения: {delaySeconds:F1} сек.)");
            from.PlaySound(0x249);
            from.FixedParticles(0x375A, 9, 20, 5016, 2418, 0, EffectLayer.Head);

            // Запускаем таймер исследования
            m_StudyTimer = Timer.DelayCall(TimeSpan.FromSeconds(delaySeconds), () => CompleteStudy(from));
        }

        private void CompleteStudy(Mobile from)
        {
            m_StudyTimer = null;

            if (from == null || from.Deleted || !from.Alive || Deleted)
                return;

            if (!IsChildOf(from.Backpack))
            {
                from.SendMessage(0x22, "Вы прервали изучение, убрав книгу из рюкзака.");
                return;
            }

            double evalSkill = from.Skills[SkillName.EvalInt].Value;

            // Прокачка навыка EvalInt при попытке изучения
            from.CheckSkill(SkillName.EvalInt, 40.0, 120.0);

            // Расчет шанса успеха: 25% на 40 навыка -> 65% на 100 навыка -> 85% на 120 навыка
            double successChance = Math.Max(0.20, Math.Min(0.90, (evalSkill - 15.0) / 120.0));

            if (Utility.RandomDouble() <= successChance)
            {
                // Получаем текст из вашего VirtueLoreResearchMod
                string fullLore = VirtueLoreResearchMod.GetSeparatedLoreMessage(evalSkill);
                
                // Выделяем заголовок из первой строки для красивого отображения
                int lineBreak = fullLore.IndexOf('\n');
                m_LoreTitle = lineBreak > 0 ? fullLore.Substring(0, lineBreak).Trim() : "Тайное знание";
                m_LoreText = fullLore;
                m_IsDecoded = true;

                UpdateAppearance();
                InvalidateProperties();

                from.PlaySound(0x1F7);
                from.FixedParticles(0x373A, 10, 15, 5018, EffectLayer.Waist);
                from.SendMessage(0x44, $"Успех! Вы расшифровали фолиант (Шанс был: {(successChance * 100):F0}%).");
                
                // Сразу открываем расшифрованную книгу
                from.SendGump(new VirtueLoreBookGump(m_LoreTitle, m_LoreText));
            }
            else
            {
                from.PlaySound(0x5C); // Звук неудачи (fizzle)
                from.SendMessage(0x22, $"Вам не удалось сложить смысл рун воедино. Попробуйте снова (Шанс успеха: {(successChance * 100):F0}%).");
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);

            writer.Write(m_IsDecoded);
            writer.Write(m_LoreTitle);
            writer.Write(m_LoreText);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();

            m_IsDecoded = reader.ReadBool();
            m_LoreTitle = reader.ReadString();
            m_LoreText = reader.ReadString();

            UpdateAppearance();
        }
    }
}
