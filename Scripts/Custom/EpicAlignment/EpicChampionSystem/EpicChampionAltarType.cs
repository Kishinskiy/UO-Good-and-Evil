// **********
// ServUO - EpicChampionAltarType.cs
// **********
using Server.Mobiles;
using System;

namespace Server.Custom.EpicAlignment.EpicChampionSystem
{
	public enum EpicChampionAltarType
	{
		AvatarTrial,
		MondainTrial,
	}

	public class ChampionSpawnInfo
	{
		public string Name { get; }
		public Type Champion { get; }
		public Type[][] SpawnTypes { get; }
		public string[] LevelNames { get; }

		public ChampionSpawnInfo(string name, Type champion, string[] levelNames, Type[][] spawnTypes)
		{
			Name = name;
			Champion = champion;
			LevelNames = levelNames;
			SpawnTypes = spawnTypes;
		}

		public static ChampionSpawnInfo[] Table => m_Table;

		private static readonly ChampionSpawnInfo[] m_Table =
		{
			new ChampionSpawnInfo("Avatar Trial", typeof(Semidar), new string[] { "Искра", "Пламя", "Аватар" }, new Type[][]
			{ 
				// Волна 1: Первые прихвостни Скверны
				new Type[] { typeof(GreaterMongbat), typeof(Imp) }, 
				// Волна 2: Тёмные горгульи и гарпии
				new Type[] { typeof(Gargoyle), typeof(Harpy) }, 
				// Волна 3: Проклятые мертвецы и лорды личей
				new Type[] { typeof(Lich), typeof(BoneKnight) }, 
				// Волна 4: Высшие демоны и суккубы
				new Type[] { typeof(Daemon), typeof(Succubus) }
			}),

			new ChampionSpawnInfo("Mondain Trial", typeof(Semidar), new string[] { "Святость", "Праведность", "Мондейн" }, new Type[][]
			{ 
				// Волна 1: Монахи, послушники и святые огоньки
				new Type[] { typeof(Pixie), typeof(Wisp) }, 
				// Волна 2: Храмовники, кавалерия и воины орденов
				new Type[] { typeof(Paladin), typeof(Centaur) }, 
				// Волна 3: Призванные небесные защитники и кирины
				new Type[] { typeof(Kirin), typeof(Unicorn) }, 
				// Волна 4: Высшие Архангелы (Единороги/Ангелы в зависимости от сборки, заменим на Серебряных Серпентов и Серых магов)
				new Type[] { typeof(SilverSerpent), typeof(HolyMage) } 
			}),
		};
		
		public static ChampionSpawnInfo GetInfo(EpicChampionAltarType type)
		{
			int v = (int)type;

			if (v < 0 || v >= m_Table.Length)
				v = 0;

			return m_Table[v];
		}
	}
}
