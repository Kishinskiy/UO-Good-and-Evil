using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Server;
using Server.Engines.Quests;
using Server.Mobiles;
using Server.Items;
using Server.Regions;
using Server.Network;
using Server.Engines.CityLoyalty;

namespace Server.Custom.SmartNPCs
{
    public static class SmartNpcSystem
    {
        private static Dictionary<Mobile, DateTime> m_Cooldowns = new Dictionary<Mobile, DateTime>();

        private static readonly string[] DynamicGreetings_Normal = new string[]
        {
            "Приветствую тебя, благородный путник!",
            "Хороший день для приключений, не так ли?",
            "Добро пожаловать! Если ищешь кров или товары — ты в правильном месте."
        };

        private static readonly string[] DynamicGreetings_Rain = new string[]
        {
            "Ну и погодка... Промок до нитки! Хоть бы плащ от этой сырости спасал.",
            "Этот ледяной дождь погубит весь урожай! Заходи скорее под крышу.",
            "Путник, под таким ливнем только простуду ловить! Рад видеть тебя в сухом месте."
        };

        private static readonly string[] DynamicGreetings_SummerHeat = new string[]
        {
            "Какая невыносимая жара! Мозги буквально плавятся на этом солнце.",
            "В такую сушь только и думаешь, как бы сделать глоток ледяной воды...",
            "Жара душит... Даже у фонтана дышать нечем. Сними тяжелый плащ, путник!"
        };

        private static readonly string[] DynamicGreetings_Night = new string[]
        {
            "Тссс... Не шуми, путник. Ночью за стенами города бродят жуткие твари.",
            "Уже поздно, порядочные горожане давно спят. Стража настороже.",
            "Ночной воздух так холоден... Тебе лучше переждать темноту в таверне."
        };

        private static readonly string[] DynamicGreetings_BloodMoon = new string[]
        {
            "Спаси нас Лорд Бритиш... Всходит Кровавая Луна! Мы все обречены!",
            "Нечисть уже прорывается к центру города! Оружие к бою, если хочешь выжить!",
            "Проклятие... Луна окрасилась кровью! Древнее зло восстало в эту ночь!"
        };

        private static readonly string[] DynamicGoodbyes = new string[]
        {
            "Всего доброго! Береги себя на дорогах.",
            "Хорошего дня и удачной охоты!",
            "Рад был помочь, путник! Заходи еще.",
            "Да хранят тебя боги! Доброго пути."
        };

        // Все мантры строго на английской латинице для нативной активации шрайнов
        private static readonly Dictionary<City, Tuple<string, string>> VirtueMap = new Dictionary<City, Tuple<string, string>>()
        {
            { City.Britain,     new Tuple<string, string>("Compassion (Сострадание)", "MU") },
            { City.Yew,         new Tuple<string, string>("Justice (Справедливость)", "BEH") },
            { City.Minoc,       new Tuple<string, string>("Sacrifice (Жертвенность)", "СAH") },
            { City.Moonglow,    new Tuple<string, string>("Honesty (Честность)", "AHM") },
            { City.Trinsic,     new Tuple<string, string>("Honor (Честь)", "SUMM") },
            { City.SkaraBrae,   new Tuple<string, string>("Spirituality (Духовность)", "OM") },
            { City.Jhelom,      new Tuple<string, string>("Valor (Доблесть)", "RA") },
            { City.NewMagincia, new Tuple<string, string>("Humility (Смирение)", "LUM") }
        };

        private static readonly Dictionary<string, Point3D> m_StaticCities = new Dictionary<string, Point3D>(StringComparer.OrdinalIgnoreCase)
        {
            { "Britain", new Point3D(1495, 1629, 10) },
            { "Minoc", new Point3D(2466, 544, 0) },
            { "Yew", new Point3D(546, 992, 0) },
            { "Moonglow", new Point3D(4442, 1172, 0) },
            { "Magincia", new Point3D(3714, 2220, 20) },
            { "Trinsic", new Point3D(1867, 2780, 0) },
            { "Skara Brae", new Point3D(632, 2233, 0) },
            { "Jhelom", new Point3D(1383, 3815, 0) },
            { "Vesper", new Point3D(2899, 676, 0) },
            { "Cove", new Point3D(2275, 1210, 0) },
            { "New Haven", new Point3D(3503, 2574, 14) },
            { "Wind", new Point3D(5223, 190, 5) },
            { "Serpent's Hold", new Point3D(3010, 3371, 15) },
            { "Nujel'm", new Point3D(3732, 1279, 0) },
            { "Buccaneer's Den", new Point3D(2706, 2163, 0) },
            { "Delucia", new Point3D(5228, 3978, 37) },
            { "Papua", new Point3D(5769, 3176, 0) },
            { "Heartwood", new Point3D(6984, 337, 0) },
            { "Ocllo", new Point3D(3649, 2682, 0) }    
        };

    

        public static void Initialize()
        {
            EventSink.Movement += new MovementEventHandler(OnPlayerMovement);
        }

        private static void OnPlayerMovement(MovementEventArgs e)
        {
            Mobile player = e.Mobile;
            if (player == null || !player.Player || !player.Alive)
                return;

            IPooledEnumerable eable = player.Map.GetMobilesInRange(player.Location, 3);
            foreach (Mobile m in eable)
            {
                if (m is BaseVendor && m.Alive && m.Combatant == null)
                {
                    DateTime lastSpoken;

                    if (m_Cooldowns.TryGetValue(m, out lastSpoken) && DateTime.UtcNow - lastSpoken < TimeSpan.FromSeconds(30.0))
                        continue;

                    if (Utility.RandomDouble() < 0.15)
                    {
                        m_Cooldowns[m] = DateTime.UtcNow;
                        m.Direction = m.GetDirectionTo(player);

                        string speech = GetContextualSpeech(m);

                        m.PublicOverheadMessage(Network.MessageType.Regular, 0x3B2, false, speech);
                        break;
                    }
                }
            }
            eable.Free();
        }

        public static bool HandleNpcSpeech(Mobile npc, Mobile player, string speechText)
        {
            if (npc == null || player == null || string.IsNullOrEmpty(speechText))
                return false;

            if (!player.InRange(npc.Location, 3))
                return false;

            string text = speechText.ToLower().Trim();
            string reply = "";

            int uoHours, uoMinutes;
            Clock.GetTime(npc.Map, npc.X, npc.Y, out uoHours, out uoMinutes);
            MoonPhase currentMoonPhase = Clock.GetMoonPhase(npc.Map, npc.X, npc.Y);
            bool isNight = (uoHours >= 20 || uoHours < 4);

            string currentTownName = "";
            Region r = npc.Region;
            if (r != null && !string.IsNullOrEmpty(r.Name) && !r.Name.Equals(npc.Map.Name, StringComparison.OrdinalIgnoreCase) && !r.Name.Equals("Default", StringComparison.OrdinalIgnoreCase))
            {
                currentTownName = r.Name;
            }

            if (text.Contains("спасибо") || text.Contains("благодарю") || text.Contains("thanks") || text.Contains("ty"))
            {
                reply = DynamicGoodbyes[Utility.Random(DynamicGoodbyes.Length)];
            }

            // 1. ИНТЕРАКТИВНАЯ ТОРГОВЛЯ 💰
            else if (text.Contains("торговать") || text.Contains("торговля") || text.Contains("trade"))
            {
                reply = "Ты хочешь что-то 'купить', 'продать' или ты ищешь 'министр'а торговли?";
            }
            else if (text.Equals("купить") || text.Contains("хочу купить") || text.Contains("buy"))
            {
                if (npc is BaseVendor vendor) { vendor.Direction = vendor.GetDirectionTo(player); vendor.VendorBuy(player); return true; }
            }
            else if (text.Equals("продать") || text.Contains("хочу продать") || text.Contains("sell"))
            {
                if (npc is BaseVendor vendor) { vendor.Direction = vendor.GetDirectionTo(player); vendor.VendorSell(player); return true; }
            }

            // =========================================================================
            // КОМАНДА «НАНЯТЬ» (ДЛЯ МОДА НАЁМНИКОВ) 🍻⚔️
            // =========================================================================
            else if (text.Contains("нанять") || text.Contains("hire") || text.Contains("наемник"))
            {
                if (npc is MercenaryCaptain mercVendor)
                {
                    npc.Direction = npc.GetDirectionTo(player);
                    mercVendor.VendorBuy(player);
                    return true;
                }
                else
                {
                    reply = "Я честный горожанин, путник, а не наёмный убийца. Ступай в таверну к Капитану Наёмников, он продаст тебе военные контракты.";
                }
            }

            // =========================================================================
            // КОМАНДА «РАБОТА» / «КВЕСТ» 📜🛡️
            // =========================================================================
            else if (text.Contains("работа") || text.Contains("квест") || text.Contains("quest") || text.Contains("задание"))
            {
                if (npc is BaseEscort escortNpc)
                {
                    npc.Direction = npc.GetDirectionTo(player);
                    escortNpc.OnDoubleClick(player);
                    return true;
                }
                else
                {
                    reply = "Нынче у меня нет свободных поручений. Но ты всегда можешь помочь Торговому Министру с развозкой караванов!";
                }
            }
            // =========================================================================
            // КОМАНДА «ПРОФЕССИЯ» 🧑‍я💎
            // =========================================================================
            else if (text.Contains("профессия") || text.Contains("кто ты") || text.Contains("проф"))
            {
                if (npc is BaseVendor vendorNpc)
                {
                    reply = $"Я — местный {vendorNpc.Name}. Занимаюсь торговлей и обучаю путников мастерству. Напиши мне слово 'тренировать', чтобы увидеть мои уроки!";
                }
                else
                {
                    reply = $"Моё имя {npc.Name}. Я простой житель этого прекрасного региона.";
                }
            }
            // =========================================================================
            // КОМАНДА «ТРЕНИРОВАТЬ» (СИСТЕМА СКИЛЛОВ С ЧЕТЫРЬМЯ ПАРАМЕТРАМИ) ⚔️🎓
            // =========================================================================
            else if (text.StartsWith("тренировать") || text.StartsWith("train"))
            {
                if (npc is BaseVendor trainer)
                {
                    npc.Direction = npc.GetDirectionTo(player);
                    if (text.Equals("тренировать") || text.Equals("train"))
                    {
                        List<string> teachableSkills = new List<string>();
                        foreach (SkillName skName in Enum.GetValues(typeof(SkillName)))
                        {
                            if (trainer.Skills[skName] != null && trainer.Skills[skName].Base > 0)
                                teachableSkills.Add(skName.ToString());
                        }
                        if (teachableSkills.Count > 0)
                            reply = "Я могу обучить тебя следующим навыкам за золото: " + string.Join(", ", teachableSkills.ToArray()) + ". Напиши, например: 'тренировать " + teachableSkills[0] + "'";
                        else
                            reply = "Увы, мне нечему тебя научить, путник.";
                    }
                    else
                    {
                        string skillToTrain = text.Replace("тренировать", "").Replace("train", "").Trim().ToLower();
                        bool foundSkill = false;
                        foreach (SkillName skName in Enum.GetValues(typeof(SkillName)))
                        {
                            if (skName.ToString().ToLower().Contains(skillToTrain) && trainer.Skills[skName] != null && trainer.Skills[skName].Base > 0)
                            {
                                foundSkill = true;
                                trainer.Teach(skName, player, 0, true);  // ИСПРАВЛЕНО: Полная сигнатура из 4 параметров! 🎉⚙️
                                break;
                            }
                        }
                        if (!foundSkill)
                            reply = "Я не обучаю такому навыку. Напиши просто 'тренировать', чтобы увидеть список моих умений.";
                        return true;
                    }
                }
            }
            // =========================================================================
            // 🔥 ДИНАМИЧЕСКИЕ СЛУХИ И ОНЛАЙН-РАДАР ПОЛУБОГОВ ВМЕСТЕ! 📡🗣️👑
            // =========================================================================
            else if (text.Contains("слухи") || text.Contains("новости") || text.Contains("аватар") || text.Contains("зло") || text.Contains("мондейн") || text.Contains("полубог") || text.Contains("сплетни"))
            {
                List<PlayerMobile> onlineAvatars = new List<PlayerMobile>();
                
                // Сканируем всех активных игроков в сети по новым флагам Полубога!
                foreach (NetState ns in NetState.Instances)
                {
                    if (ns.Mobile is PlayerMobile pm && pm.Alive && pm.AccessLevel == AccessLevel.Player)
                    {
                        if (pm.IsDemigod || pm.EpicGood || pm.EpicEvil)
                        {
                            onlineAvatars.Add(pm);
                        }
                    }
                }

                if (onlineAvatars.Count == 0)
                {
                    string[] staticRumors = new string[]
                    {
                        "О, нынче на дорогах затишье. Высшие Полубоги скрылись в своих чертогах. Но поговаривают, Алтарь Justice сокрыт на самом севере Felucca, среди льдов!",
                        "Старейшины шепчут, что Алтарь Honor (Чести) находится на изолированном острове. Без верных наёмников туда соваться — верная смерть.",
                        "Охотники поговаривают, что в глубинах лесов Yew спрятан древний сундук с сокровищами пиратов... Но точные координаты знают лишь звёзды.",
                        "В тавернах поговаривают, что если развивать караваны городов до 2 уровня, жители вспомнят утерянные английские мантры Добродетелей!",
                        "Контрабандисты шепчутся, что Алтарь Доблести (Valor) в Felucca позволяет принудительно вызывать Чемпионов, если ты истинный Рыцарь!"
                    };
                    reply = staticRumors[Utility.Random(staticRumors.Length)];
                }
                else
                {
                    PlayerMobile av = onlineAvatars[Utility.Random(onlineAvatars.Count)];
                    string regName = (av.Region != null && !string.IsNullOrEmpty(av.Region.Name)) ? av.Region.Name : "Диких землях";

                    if (av.EpicEvil)
                    {
                        // Слухи, если в сети Владыка Тьмы (EpicEvil)
                        string[] darkRumors = new string[]
                        {
                            $"Шепотом, путник... Владыка Тьмы по имени {av.Name} сейчас ступает по земле! Его видели в районе {regName} ({av.Map.Name}). Храни нас Свет!",
                            $"Говорят, адепт Хаоса {av.Name}, обретя мощь Полубога, сейчас рыщет где-то в {regName}. Небеса сгустились над теми краями!",
                            $"В тавернах все дрожат от страха: Владыка Тьмы {av.Name} вышел на охоту в {regName}. Силам Света пора объединиться!"
                        };
                        reply = darkRumors[Utility.Random(darkRumors.Length)];
                    }
                    else
                    {
                        // Слухи, если в сети Аватар Света (EpicGood)
                        string classTitle = "Великий Аватар";
                        if (av.Skills[SkillName.Swords].Value >= 100.0) classTitle = "Аватар-Паладин";
                        else if (av.Skills[SkillName.Magery].Value >= 100.0) classTitle = "Арканический Аватар";

                        string[] lightRumors = new string[]
                        {
                            $"Говорят, истинный Аватар Света по имени {av.Name} сейчас в мире! Его видели как {classTitle} в районе {regName} ({av.Map.Name}). Люди молятся на него!",
                            $"Шепчутся, что {av.Name} — живое воплощение Добродетелей — сейчас проезжал через {regName}. Сияние его ауры озарило небосвод!",
                            $"В тавернах только и разговоров, что {av.Name} ведет за собой силы Света где-то в {regName}. Охотники, у вас есть шанс помочь ему!"
                        };
                        reply = lightRumors[Utility.Random(lightRumors.Length)];
                    }
                }
            }
            // =========================================================================
            // МАНТРЫ ДОБРОДЕТЕЛЕЙ ОТ ЖИВЫХ NPC
            // =========================================================================
            else if (text.Contains("мантра") || text.Contains("mantra"))
            {
                City currentCity = City.Britain;
                bool insideValidCity = false;
                foreach (City c in Enum.GetValues(typeof(City)))
                {
                    string checkName = (c == City.NewMagincia) ? "magincia" : c.ToString().ToLower();
                    if (!string.IsNullOrEmpty(currentTownName) && currentTownName.ToLower().Contains(checkName))
                    {
                        currentCity = c;
                        insideValidCity = true;
                        break;
                    }
                }
                if (!insideValidCity)
                {
                    reply = "Дивные слова ты шепчешь, путник... Мантры Добродетелей ведомы лишь жителям Великих Городов. Ступай в Британию, Минок или Тринсик.";
                }
                else
                {
                    Tuple<string, string> virtueInfo;
                    if (!VirtueMap.TryGetValue(currentCity, out virtueInfo))
                    {
                        reply = $"В городе {currentCity} нет святыни Добродетели с известной мне мантрой.";
                    }
                    else if (!CityLoyaltySystem.Enabled || CityLoyaltySystem.Cities == null)
                    {
                        reply = "Городская система любви сейчас недоступна, поэтому я не могу определить твою репутацию.";
                    }
                    else
                    {
                        CityLoyaltySystem citySystem =
                            CityLoyaltySystem.Cities.FirstOrDefault(c => c.City == currentCity);

                        if (citySystem == null)
                        {
                            reply = $"Не удалось определить городскую репутацию в {currentCity}.";
                        }
                        else
                        {
                            CityLoyaltyEntry loyaltyEntry =
                                citySystem.GetPlayerEntry<CityLoyaltyEntry>(player);
                            int lovePoints = loyaltyEntry != null ? loyaltyEntry.Love : 0;
                            LoyaltyRating loyaltyRating =
                                citySystem.GetLoyaltyRating(player, loyaltyEntry);

                            if (loyaltyRating >= LoyaltyRating.Respected)
                            {
                                reply = $"Слушай внимательно, путник! Твоя репутация в {currentCity} — {loyaltyRating} ({lovePoints} очков любви). Древнее слово силы для Алтаря {virtueInfo.Item1} — {virtueInfo.Item2}. Произнеси его у святыни!";
                            }
                            else
                            {
                                reply = $"Я открою тебе мантру {virtueInfo.Item1}, когда твоя репутация в {currentCity} достигнет уровня Respected. Сейчас твой уровень — {loyaltyRating}, очков любви — {lovePoints}; для Respected нужно не менее 1000 очков любви и чтобы любовь к городу преобладала над нейтралитетом и неприязнью.";
                            }
                        }
                    }
                }
            }

            // 2. ДИНАМИЧЕСКИЙ ПОИСК КОНКРЕТНОГО ГОРОДА 🗺️
            if (string.IsNullOrEmpty(reply))
            {
                foreach (var pair in m_StaticCities)
                {
                    if (text.Contains(pair.Key.ToLower()))
                    {
                        Point3D targetLoc = pair.Value;
                        Direction dir = npc.GetDirectionTo(targetLoc);
                        reply = $"Путь к городу {pair.Key} лежит на {GetDirectionName(dir)} отсюда. Координаты центра: {targetLoc.X}, {targetLoc.Y}.";
                        break;
                    }
                }
            }
                        // 3. ПОИСК МИНИСТРА ⚖️
            if (string.IsNullOrEmpty(reply) && (text.Equals("министр") || text.Contains("министр торговли") || text.Contains("minister")))
            {
                Mobile minister = FindCityOfficial(npc, searchGovernor: false);
                if (minister != null)
                {
                    Direction dir = npc.GetDirectionTo(minister.Location);
                    string townLabel = !string.IsNullOrEmpty(currentTownName) ? $"города {currentTownName} " : "";
                    reply = $"Торговый министр {townLabel}находится на {GetDirectionName(dir)} отсюда. Точные координаты: {minister.X}, {minister.Y}.";
                }
                else
                {
                    reply = "В этом поселении сейчас нет Торгового Министра. Ищи его в крупных городах.";
                }
            }

            // 4. ПОИСК ГУБЕРНАТОРА 👑
            if (string.IsNullOrEmpty(reply) && (text.Contains("губернатор") || text.Contains("мэр") || text.Contains("governor")))
            {
                Mobile governor = FindCityOfficial(npc, searchGovernor: true);
                if (governor != null)
                {
                    Direction dir = npc.GetDirectionTo(governor.Location);
                    string townLabel = !string.IsNullOrEmpty(currentTownName) ? $"города {currentTownName} " : "";
                    reply = $"Губернатор {townLabel}сейчас находится на {GetDirectionName(dir)} отсюда. Координаты: {governor.X}, {governor.Y}.";
                }
                else
                {
                    reply = "В этом городе сейчас нет действующего Губернатора или он отсутствует на месте.";
                }
            }

            // 5. РЕАКЦИЯ НА ПОГОДУ 🌧️
            if (string.IsNullOrEmpty(reply) && (text.Contains("погода") || text.Contains("дождь") || text.Contains("weather") || text.Contains("rain")))
            {
                if (IsRainingOverNpc(npc))
                    reply = "Льет как из ведра! Пронизывающий cold. Если промочит до нитки, бег будет заблокирован, пока не согреешься у костра или внутри таверны.";
                else if (npc.Map.Season == 1)
                    reply = "Погода нынче жаркая, настоящее лето! Главное — не перегреваться в тяжелых доспехах под палящим солнцем.";
                else
                    reply = "Погода нынче благосклонна к путешественникам, небосвод чист.";
            }
            // 6. РЕАКЦИЯ НА НОЧЬ 🌙
            if (string.IsNullOrEmpty(reply) && (text.Contains("ночь") || text.Contains("осада") || text.Contains("штурм") || text.Contains("ночные монстры")))
            {
                if (isNight)
                    reply = "Осада идет прямо сейчас! Слышишь скрежет за воротами? Городская защита отключена, вся надежда только на героев!";
                else
                    reply = string.Format("Сейчас на часах {0}:{1:D2}. Всё спокойно. Но как только пробьет 20:00 вечера, чудовища пойдут на штурм.", uoHours, uoMinutes);
            }
            // 7. РЕАКЦИЯ НА КРОВАТУЮ ЛУНУ 🩸
            if (string.IsNullOrEmpty(reply) && (text.Contains("луна") || text.Contains("полнолуние") || text.Contains("moon")))
            {
                if (currentMoonPhase == MoonPhase.Full)
                    reply = "Посмотри на небо... Луна полностью багровая. Это проклятое Полнолуние! В эту ночь вместо обычных зомби город будут штурмовать Личи и Мумии!";
                else
                    reply = string.Format("Небосвод чист. Фаза луны сейчас — {0}. До Кровавой Луны еще есть время, так что ночные набеги будут терпимыми.", currentMoonPhase);
            }
            // 8. Текущее местоположение 🧭
            if (string.IsNullOrEmpty(reply) && (text.Contains("где я") || text.Contains("какой город") || text.Contains("город") || text.Contains("где мы")))
            {
                string locationCoords = $"[Координаты: {player.X}, {player.Y}]";
                if (!string.IsNullOrEmpty(currentTownName))
                    reply = $"Мы находимся в поселении {currentTownName}. {locationCoords}";
                else
                    reply = $"Мы сейчас на диких землях мира {npc.Map.Name}. {locationCoords}";
            }
            // 9. Абстрактный запрос дороги
            if (string.IsNullOrEmpty(reply) && (text.Contains("дорога") || text.Contains("куда идти") || text.Contains("где")))
            {
                if (!string.IsNullOrEmpty(currentTownName))
                    reply = $"Ты уже находишься в черте города {currentTownName}. Банк и лавки торговцев прямо в центре поселения.";
                else
                {
                    Point3D britainLoc = m_StaticCities["Britain"];
                    Direction dir = npc.GetDirectionTo(britainLoc);
                    reply = $"Ты заплутал в диких землях. Столица Britain находится на {GetDirectionName(dir)} отсюда. Координаты ворот: {britainLoc.X}, {britainLoc.Y}.";
                }
            }
            // 10. Базовое приветствие
            if (string.IsNullOrEmpty(reply) && (text.Contains("привет") || text.Contains("здравствуй") || text.Contains("хай") || text.Contains("hello") || text.Contains("hi")))
            {
                reply = string.Format("Здравствуй, {0}. Чем я могу помочь?", player.Name);
            }
            if (!string.IsNullOrEmpty(reply))
            {
                npc.Direction = npc.GetDirectionTo(player);
                npc.PublicOverheadMessage(Network.MessageType.Regular, 0x3B2, false, reply);
                player.SendMessage(0x5D, $"{npc.Name}: {reply}");
                return true;
            }
            return false;
        }
        private static string GetContextualSpeech(Mobile npc)
        {
            Map map = npc.Map;
            if (map == null || map == Map.Internal)
                return DynamicGreetings_Normal[Utility.Random(DynamicGreetings_Normal.Length)];
            int uoHours, uoMinutes;
            Clock.GetTime(map, npc.X, npc.Y, out uoHours, out uoMinutes);
            MoonPhase currentMoonPhase = Clock.GetMoonPhase(map, npc.X, npc.Y);
            bool isNight = (uoHours >= 20 || uoHours < 4);
            bool isNpcInDoors = IsNpcInDoors(npc);
            if (isNight && currentMoonPhase == MoonPhase.Full)
                return DynamicGreetings_BloodMoon[Utility.Random(DynamicGreetings_BloodMoon.Length)];
            if (isNight)
                return DynamicGreetings_Night[Utility.Random(DynamicGreetings_Night.Length)];
            if (!isNpcInDoors && IsRainingOverNpc(npc))
                return DynamicGreetings_Rain[Utility.Random(DynamicGreetings_Rain.Length)];
            if (!isNpcInDoors && map.Season == 1)
                return DynamicGreetings_SummerHeat[Utility.Random(DynamicGreetings_SummerHeat.Length)];
            return DynamicGreetings_Normal[Utility.Random(DynamicGreetings_Normal.Length)];
        }
        private static bool IsRainingOverNpc(Mobile npc)
        {
            var weatherList = Server.Misc.Weather.GetWeatherList(npc.Map);
            if (weatherList != null)
            {
                for (int j = 0; j < weatherList.Count; ++j)
                {
                    Server.Misc.Weather w = weatherList[j];
                    bool isActive = false;
                    System.Reflection.FieldInfo activeField = typeof(Server.Misc.Weather).GetField("m_Active", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                    if (activeField != null) isActive = (bool)activeField.GetValue(w);
                    if (isActive && w.Temperature > 0 && w.IntersectsWith(new Rectangle2D(npc.X, npc.Y, 1, 1)))
                        return true;
                }
            }
            return false;
        }
        private static bool IsNpcInDoors(Mobile npc)
        {
            Region reg = npc.Region;
            if (reg == null) return false;
            if (reg.IsPartOf(typeof(Server.Regions.HouseRegion)) || reg.Name == "Inns" || reg.Name == "Taverns")
                return true;
            return false;
        }
        private static string GetDirectionName(Direction dir)
        {
            int value = (int)(dir & Direction.Mask);
            switch (value)
            {
                case 0: return "Север";
                case 1: return "Северо-Восток";
                case 2: return "Восток";
                case 3: return "Юго-Восток";
                case 4: return "Юг";
                case 5: return "Юго-Запад";
                case 6: return "Запад";
                case 7: return "Северо-Запад";
                default: return "прямо";
            }
        }

        private static Mobile FindCityOfficial(Mobile npc, bool searchGovernor)
        {
            if (npc == null || npc.Map == null)
                return null;

            Region townRegion = npc.Region?.GetRegion(typeof(TownRegion));
            string cityName = townRegion != null ? townRegion.Name : npc.Region?.Name;

            // 1. Поиск через систему лояльности города (CityLoyaltySystem)
            if (!string.IsNullOrEmpty(cityName) && CityLoyaltySystem.Cities != null)
            {
                foreach (var sys in CityLoyaltySystem.Cities)
                {
                    string sysName = sys.City == City.NewMagincia ? "Magincia" : sys.City.ToString();
                    if (cityName.IndexOf(sysName, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        if (searchGovernor && sys.Governor != null && !sys.Governor.Deleted && sys.Governor.Map == npc.Map)
                            return sys.Governor;

                        if (!searchGovernor && sys.Minister != null && !sys.Minister.Deleted && sys.Minister.Map == npc.Map)
                            return sys.Minister;
                    }
                }
            }

            // 2. Поиск министра напрямую через статический список TradeMinister.Ministers (в том же городе и карте)
            if (!searchGovernor && TradeMinister.Ministers != null && !string.IsNullOrEmpty(cityName))
            {
                foreach (var m in TradeMinister.Ministers)
                {
                    if (m != null && !m.Deleted && m.Map == npc.Map)
                    {
                        string mCityName = m.City == City.NewMagincia ? "Magincia" : m.City.ToString();
                        if (cityName.IndexOf(mCityName, StringComparison.OrdinalIgnoreCase) >= 0)
                            return m;
                    }
                }
            }
            return null;
        }
    }
}
