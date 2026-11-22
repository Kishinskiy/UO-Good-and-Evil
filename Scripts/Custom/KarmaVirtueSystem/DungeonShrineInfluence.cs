using System;
using System.Collections.Generic;
using Server;
using Server.Mobiles;

namespace Server.Custom.KarmaVirtueSystem
{
    public static class DungeonShrineInfluence
    {
        // Храним базовые параметры спавнеров до модификации, чтобы корректно откатывать их назад
        private class SpawnerBackup
        {
            public TimeSpan BaseMinDelay;
            public TimeSpan BaseMaxDelay;
            public int BaseMaxCount;
            public List<int> BaseObjectCounts = new List<int>();
            public int AppliedState; // 0 = нейтрально, 1 = свет, 2 = тьма
        }

        private static readonly Dictionary<Spawner, SpawnerBackup> _backups = new Dictionary<Spawner, SpawnerBackup>();
        private static Timer _syncTimer;

        public static void Initialize()
        {
            // Проверяем и синхронизируем спавнеры данжей раз в 60 секунд
            // (также можно вызывать UpdateAllDungeons() сразу в ProcessShrineActivation)
            _syncTimer = Timer.DelayCall(TimeSpan.FromSeconds(15), TimeSpan.FromMinutes(1), UpdateAllDungeons);
        }

        public static void UpdateAllDungeons()
        {
            // 1. Собираем текущее состояние по названиям подземелий
            var dungeonStates = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            foreach (ShrineData shrine in KarmaVirtueManager.Shrines)
            {
                if (!string.IsNullOrEmpty(shrine.DungeonRegionName))
                {
                    dungeonStates[shrine.DungeonRegionName] = KarmaVirtueManager.GetShrineWorldState(shrine.ID);
                }
            }

            // 2. Проходим по всем спавнерам в мире
            foreach (Item item in World.Items.Values)
            {
                if (item is Spawner spawner && !spawner.Deleted && spawner.Map != null && spawner.Map != Map.Internal)
                {
                    Region reg = Region.Find(spawner.Location, spawner.Map);
                    if (reg == null)
                        continue;

                    int targetState = 0;
                    foreach (var kvp in dungeonStates)
                    {
                        if (KarmaVirtueManager.IsRegionWithin(reg, kvp.Key))
                        {
                            targetState = kvp.Value;
                            break;
                        }
                    }

                    // Если спавнер не в данже-антагонисте и не был изменён ранее — пропускаем
                    if (targetState == 0 && !_backups.ContainsKey(spawner))
                        continue;

                    ApplyStateToSpawner(spawner, targetState);
                }
            }
        }

        private static void ApplyStateToSpawner(Spawner spawner, int state)
        {
            if (!_backups.TryGetValue(spawner, out SpawnerBackup backup))
            {
                // Сохраняем оригинальные настройки спавнера перед первым изменением
                backup = new SpawnerBackup
                {
                    BaseMinDelay = spawner.MinDelay,
                    BaseMaxDelay = spawner.MaxDelay,
                    BaseMaxCount = spawner.MaxCount,
                    AppliedState = 0
                };

                foreach (var so in spawner.SpawnObjects)
                {
                    backup.BaseObjectCounts.Add(so.MaxCount);
                }

                _backups[spawner] = backup;
            }

            // Если состояние не изменилось с прошлого раза — ничего не делаем
            if (backup.AppliedState == state)
                return;

            double delayMult = 1.0;
            double countMult = 1.0;

            if (state == 1) // Светлый алтарь: реже респаун (1.5x), меньше мобов (0.7x)
            {
                delayMult = KarmaVirtueManager.DungeonRespawnTimeLightMultiplier;
                countMult = KarmaVirtueManager.DungeonSpawnCountLightMultiplier;
            }
            else if (state == 2) // Тёмный алтарь: чаще респаун (0.7x), больше мобов (1.5x)
            {
                delayMult = KarmaVirtueManager.DungeonRespawnTimeDarkMultiplier;
                countMult = KarmaVirtueManager.DungeonSpawnCountDarkMultiplier;
            }

            // Применяем задержку респауна от базового значения
            spawner.MinDelay = TimeSpan.FromSeconds(Math.Max(5, backup.BaseMinDelay.TotalSeconds * delayMult));
            spawner.MaxDelay = TimeSpan.FromSeconds(Math.Max(10, backup.BaseMaxDelay.TotalSeconds * delayMult));

            // Применяем лимит количества мобов (минимум 1, если изначально был >= 1)
            spawner.MaxCount = backup.BaseMaxCount > 0
                ? Math.Max(1, (int)Math.Round(backup.BaseMaxCount * countMult))
                : 0;

            for (int i = 0; i < spawner.SpawnObjects.Count && i < backup.BaseObjectCounts.Count; i++)
            {
                int baseObjCount = backup.BaseObjectCounts[i];
                spawner.SpawnObjects[i].MaxCount = baseObjCount > 0
                    ? Math.Max(1, (int)Math.Round(baseObjCount * countMult))
                    : 0;
            }

            backup.AppliedState = state;

            // Если эффект алтаря закончился и вернулся в 0 — удаляем запись из кэша
            if (state == 0)
            {
                _backups.Remove(spawner);
            }
        }
    }
}
