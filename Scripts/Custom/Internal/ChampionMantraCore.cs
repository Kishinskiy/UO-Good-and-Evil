using System;
using Server;
using Server.Mobiles;
using Server.Network;
using Server.Items;
using Server.Custom.KarmaVirtueSystem;

namespace Server.Engines.CannedEvil
{
    public static class ChampionMantraCore
    {
        public static void Initialize()
        {
            EventSink.Speech += OnPlayerSpeech;
        }

        private static void OnPlayerSpeech(SpeechEventArgs e)
        {
            PlayerMobile pm = e.Mobile as PlayerMobile;
            if (!ChampionSystem.Enabled || pm == null || !pm.Alive || pm.NetState == null || string.IsNullOrWhiteSpace(e.Speech))
                return;

            string text = e.Speech.Trim();

            foreach (ChampionSpawn spawn in ChampionSystem.AllSpawns)
            {
                if (spawn == null || spawn.Deleted || spawn.Active || pm.Map != spawn.Map || !pm.InRange(spawn.Location, 5))
                    continue;

                string requiredMantra = GetRequiredMantra(spawn.Type);

                if (requiredMantra != null && string.Equals(text, requiredMantra, StringComparison.OrdinalIgnoreCase))
                {
                    e.Blocked = true;

                    // ИЗМЕНЕННАЯ ЛОГИКА:
                    // Если таймер запущен и текущее время МЕНЬШЕ, чем RestartTime (время ожидания еще не прошло)
                    if (spawn.RestartTime != DateTime.MinValue && DateTime.UtcNow < spawn.RestartTime)
                    {
                        pm.SendMessage(0x22, "Алтарь находится на стадии перезарядки. Сейчас пробудить его может только Рыцарь Доблести (Knight of Valor).");
                        continue;
                    }

                    if (spawn.Type == ChampionSpawnType.AvatarTrial || spawn.Type == ChampionSpawnType.MondainTrial)
                    {
	                    
	                    bool isDarkPath = KarmaVirtueManager.IsPredominantlyDark(pm);

	                    if (spawn.Type == ChampionSpawnType.AvatarTrial && isDarkPath)
	                    {
		                    pm.SendMessage(0x22, "Ваша душа осквернена Тьмой! Испытание Аватара доступно только идущим Путем Просветления.");
		                    continue;
	                    }

	                    if (spawn.Type == ChampionSpawnType.MondainTrial && !isDarkPath)
	                    {
		                    pm.SendMessage(0x22, "В вас слишком много Света! Испытание Мондейна покорится лишь адепту Книги Порока.");
		                    continue;
	                    }
	                    
	                    if (!ConsumeElementalRelics(pm))
	                    {
		                    pm.SendMessage(0x22, "Для пробуждения этого алтаря в вашем рюкзаке должны находиться все 4 Реликвии Пагана (Земли, Огня, Воды и Воздуха)!");
		                    continue;
	                    }
                    }
                    
                    // Если таймер вышел (DateTime.UtcNow >= spawn.RestartTime), алтарь перешел в режим ожидания мантры
                    spawn.Active = true;

                    pm.PlaySound(0x243);
                    Effects.SendLocationEffect(spawn.Location, spawn.Map, 0x3709, 30, 10, 1161, 0);
                    pm.FixedParticles(0x375A, 9, 20, 5027, EffectLayer.Waist);

                    spawn.PublicOverheadMessage(MessageType.Regular, 0x35, false, $"*Мантра принята древними силами*");
                    pm.SendMessage(0x58, $"[Алтарь {spawn.SpawnName}]: Заклинание сработало! Первая волна монстров пробуждается...");
                    break;
                }
            }
        }
        
        private static bool ConsumeElementalRelics(PlayerMobile pm)
        {
	        if (pm.Backpack == null)
		        return false;

	        Item earth = pm.Backpack.FindItemByType(typeof(HeartOfEarth));
	        Item flame = pm.Backpack.FindItemByType(typeof(TongueOfFlame));
	        Item seas  = pm.Backpack.FindItemByType(typeof(TearOfSeas));
	        Item wind  = pm.Backpack.FindItemByType(typeof(BreathOfWind));

	        // Проверяем, что все 4 реликвии найдены
	        if (earth == null || flame == null || seas == null || wind == null)
		        return false;

	        // Удаляем все 4 реликвии
	        earth.Delete();
	        flame.Delete();
	        seas.Delete();
	        wind.Delete();

	        return true;
        }

        
        

        public static string GetRequiredMantra(ChampionSpawnType type)
        {
            switch (type)
            {
                case ChampionSpawnType.UnholyTerror:   return "corp por";
                case ChampionSpawnType.VerminHorde:    return "des mani";
                case ChampionSpawnType.ColdBlood:      return "an ex por";
                case ChampionSpawnType.Abyss:          return "kal vas flam";
                case ChampionSpawnType.Arachnid:       return "an mani";
                case ChampionSpawnType.ForestLord:     return "vas corp sanct";
                case ChampionSpawnType.Infuse:         return "wis corp";
                case ChampionSpawnType.Terror:         return "an tym";
                case ChampionSpawnType.SleepingDragon: return "in san por";
                case ChampionSpawnType.Glade:          return "drak por";
                case ChampionSpawnType.Corrupt:        return "nox por";
                case ChampionSpawnType.DragonTurtle:   return "vas ort flam";
                case ChampionSpawnType.Khaldun:        return "in nox";
                case ChampionSpawnType.AvatarTrial:    return "in vas mani";
                case ChampionSpawnType.MondainTrial:   return "vas kal corp";
                default:                               return null;
            }
        }
    }
}
