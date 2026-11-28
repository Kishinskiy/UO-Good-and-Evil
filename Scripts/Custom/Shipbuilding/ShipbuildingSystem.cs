using System;
using Server;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Multis;
using Server.Network;
using Server.Targeting;

namespace Server.Custom.Shipbuilding
{
    public class ShipbuildingSite : Item
    {
        private Type m_TargetBoatDeed;
        private string m_ShipName;
        private Mobile m_Builder;
        private double m_RequiredCarpentry;

        private CraftResource m_WoodResource = CraftResource.None;
        private CraftResource m_MetalResource = CraftResource.None;
        private int m_WoodHue = 0;

        [CommandProperty(AccessLevel.GameMaster)]
        public CraftResource WoodResource
        {
            get => m_WoodResource;
            set { m_WoodResource = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public CraftResource MetalResource
        {
            get => m_MetalResource;
            set { m_MetalResource = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int RequiredBoards { get; set; }
        [CommandProperty(AccessLevel.GameMaster)]
        public int CurrentBoards { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int RequiredCloth { get; set; }
        [CommandProperty(AccessLevel.GameMaster)]
        public int CurrentCloth { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int RequiredIngots { get; set; }
        [CommandProperty(AccessLevel.GameMaster)]
        public int CurrentIngots { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int RequiredNails { get; set; }
        [CommandProperty(AccessLevel.GameMaster)]
        public int CurrentNails { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public double RequiredCarpentry
        {
            get => m_RequiredCarpentry;
            set => m_RequiredCarpentry = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile Builder
        {
            get => m_Builder;
            set => m_Builder = value;
        }

        public bool IsComplete =>
            CurrentBoards >= RequiredBoards &&
            CurrentCloth >= RequiredCloth &&
            CurrentIngots >= RequiredIngots &&
            CurrentNails >= RequiredNails;

        public ShipbuildingSite(Mobile builder, string shipName, Type deedType, double reqSkill, int boards, int cloth, int ingots, int nails)
            : base(0x14F3)
        {
            Name = $"Верфь: {shipName} [Строится]";
            Movable = false;
            Hue = 2413;

            m_Builder = builder;
            m_ShipName = shipName;
            m_TargetBoatDeed = deedType;
            m_RequiredCarpentry = reqSkill;

            RequiredBoards = boards;
            RequiredCloth = cloth;
            RequiredIngots = ingots;
            RequiredNails = nails;
        }

        public ShipbuildingSite(Serial serial) : base(serial)
        {
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            if (m_Builder != null)
                list.Add(1060658, "Судостроитель\t{0}", m_Builder.Name);

            string woodName = m_WoodResource == CraftResource.None ? "Не выбран" : CraftResources.GetName(m_WoodResource);
            string metalName = m_MetalResource == CraftResource.None ? "Не выбран" : CraftResources.GetName(m_MetalResource);

            list.Add(1060660, "Корпус (дерево)\t{0}", woodName);
            list.Add(1060661, "Обшивка (металл)\t{0}", metalName);

            int totalReq = RequiredBoards + RequiredCloth + RequiredIngots + RequiredNails;
            int totalCur = CurrentBoards + CurrentCloth + CurrentIngots + CurrentNails;
            int pct = totalReq > 0 ? (int)((totalCur / (double)totalReq) * 100.0) : 0;

            list.Add(1060659, "Готовность\t{0}%", pct);
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!from.InRange(GetWorldLocation(), 4))
            {
                from.SendLocalizedMessage(500446);
                return;
            }

            from.CloseGump(typeof(ShipbuildingGump));
            from.SendGump(new ShipbuildingGump(from, this));
        }

        public override bool OnDragDrop(Mobile from, Item dropped)
        {
            if (!from.InRange(GetWorldLocation(), 4))
            {
                from.SendLocalizedMessage(500446);
                return false;
            }

            if (TryAddSingleItem(from, dropped))
            {
                from.PlaySound(0x23D);
                InvalidateProperties();

                from.CloseGump(typeof(ShipbuildingGump));
                from.SendGump(new ShipbuildingGump(from, this));

                if (IsComplete)
                    CompleteShip(from);
            }

            return false;
        }

        public bool TryAddSingleItem(Mobile from, Item item)
        {
            // 1. Обработка древесины (доски и бревна)
            if (item is BaseBoard || item is BaseLog)
            {
                CraftResource res = item is BaseLog log
                    ? log.Resource
                    : CraftResources.GetFromType(item.GetType());

                if (res == CraftResource.None)
                    res = CraftResource.RegularWood;

                if (m_WoodResource != CraftResource.None && m_WoodResource != res)
                {
                    from.SendMessage(0x22, $"Корпус уже заложен из {CraftResources.GetName(m_WoodResource)}. Нельзя смешивать породы дерева!");
                    return false;
                }

                if (ConsumeResource(from, item, RequiredBoards - CurrentBoards, val => CurrentBoards += val, "досок/бревен"))
                {
                    if (m_WoodResource == CraftResource.None)
                    {
                        m_WoodResource = res;
                        m_WoodHue = CraftResources.GetHue(res);
                        if (m_WoodHue > 0)
                            Hue = m_WoodHue; // Стапель меняет цвет под выбранное дерево
                    }
                    return true;
                }
                return false;
            }

            // 2. Обработка ткани
            if (item is Cloth)
            {
                return ConsumeResource(from, item, RequiredCloth - CurrentCloth, val => CurrentCloth += val, "ткани");
            }
            if (item is BoltOfCloth)
            {
                int needed = RequiredCloth - CurrentCloth;
                if (needed <= 0)
                {
                    from.SendMessage("Ткань уже полностью уложена.");
                    return false;
                }

                int boltsNeeded = (int)Math.Ceiling(needed / 50.0);
                int toTake = Math.Min(boltsNeeded, item.Amount);
                int addedCloth = Math.Min(needed, toTake * 50);

                item.Consume(toTake);
                CurrentCloth += addedCloth;
                from.SendMessage(0x44, $"Вы добавили {addedCloth} ед. ткани из рулонов.");
                return true;
            }

            // 3. Обработка металла (слитки)
            if (item is BaseIngot ingot)
            {
                CraftResource res = ingot.Resource;
                if (res == CraftResource.None)
                    res = CraftResource.Iron;

                if (m_MetalResource != CraftResource.None && m_MetalResource != res)
                {
                    from.SendMessage(0x22, $"Обшивка уже делается из {CraftResources.GetName(m_MetalResource)}. Нельзя смешивать разные металлы!");
                    return false;
                }

                if (ConsumeResource(from, item, RequiredIngots - CurrentIngots, val => CurrentIngots += val, "слитков"))
                {
                    if (m_MetalResource == CraftResource.None)
                        m_MetalResource = res;
                    return true;
                }
                return false;
            }

            // 4. Обработка гвоздей
            if (item is CraftNails)
            {
                return ConsumeResource(from, item, RequiredNails - CurrentNails, val => CurrentNails += val, "гвоздей");
            }

            from.SendMessage(0x22, "Этот материал не подходит для судостроения.");
            return false;
        }

        private bool ConsumeResource(Mobile from, Item item, int needed, Action<int> addAction, string resName)
        {
            if (needed <= 0)
            {
                from.SendMessage($"Запас {resName} на верфи уже полностью укомплектован.");
                return false;
            }

            int toTake = Math.Min(needed, item.Amount);
            item.Consume(toTake);
            addAction(toTake);
            from.SendMessage(0x44, $"Вы добавили {toTake} {resName} в конструкцию судна.");
            return true;
        }

        public void PullResourcesFromBackpack(Mobile from)
        {
            if (from.Backpack == null)
                return;

            bool anyAdded = false;
            Item[] items = from.Backpack.FindItemsByType(new Type[] { typeof(BaseBoard), typeof(BaseLog), typeof(Cloth), typeof(BoltOfCloth), typeof(BaseIngot), typeof(CraftNails) });

            foreach (Item item in items)
            {
                if (IsComplete)
                    break;

                if (TryAddSingleItem(from, item))
                    anyAdded = true;
            }

            if (anyAdded)
            {
                from.PlaySound(0x23D);
                InvalidateProperties();

                if (IsComplete)
                    CompleteShip(from);
            }
            else
            {
                from.SendMessage(0x22, "В вашем рюкзаке нет подходящих материалов (проверьте соответствие породы дерева и металла).");
            }
        }

        public void CompleteShip(Mobile from)
        {
            if (from.Skills[SkillName.Carpentry].Value < m_RequiredCarpentry)
            {
                from.SendMessage(0x22, $"Для финального спуска судна на воду требуется навык плотничества {m_RequiredCarpentry:F1}!");
                return;
            }
            from.PlaySound(0x5B5);
            from.FixedParticles(0x373A, 10, 15, 5018, EffectLayer.Waist);
            from.SendMessage(0x44, $"Поздравляем! Постройка судна «{m_ShipName}» завершена!");
            if (m_TargetBoatDeed != null)
            {
                BaseBoatDeed deed = Activator.CreateInstance(m_TargetBoatDeed) as BaseBoatDeed;
                if (deed != null)
                {
                    // Передаем цвет древесины диду
                    deed.Hue = m_WoodHue;
                    // Сохраняем характеристики материалов в таблицу свойств корабля
                    ShipMaterialRegistry.RegisterDeed(deed, m_WoodResource, m_MetalResource, m_WoodHue);
                    string wName = CraftResources.GetName(m_WoodResource);
                    string mName = CraftResources.GetName(m_MetalResource);
                    deed.Name = $"{m_ShipName} [{wName} / {mName}]";
                    if (!from.AddToBackpack(deed))
                        deed.MoveToWorld(from.Location, from.Map);
                    from.SendMessage(0x35, $"Судовой дид ({wName} / {mName}) помещен в ваш рюкзак.");
                }
            }
            Delete();
        }
        public void CancelAndRefund(Mobile from)
        {
            if (from != m_Builder && from.AccessLevel < AccessLevel.GameMaster)
            {
                from.SendMessage(0x22, "Только основатель верфи может свернуть строительство.");
                return;
            }
            if (CurrentBoards > 1)
            {
                Item boards = CraftResources.GetInfo(m_WoodResource)?.ResourceTypes?.Length > 0
                ? Activator.CreateInstance(CraftResources.GetInfo(m_WoodResource).ResourceTypes[0], CurrentBoards / 2) as Item
                : new Board(CurrentBoards / 2);
                if (boards != null) from.AddToBackpack(boards);
            }
            if (CurrentCloth > 1) from.AddToBackpack(new Cloth(CurrentCloth / 2));
            if (CurrentIngots > 1)
            {
                Item ingots = CraftResources.GetInfo(m_MetalResource)?.ResourceTypes?.Length > 0
                ? Activator.CreateInstance(CraftResources.GetInfo(m_MetalResource).ResourceTypes[0], CurrentIngots / 2) as Item
                : new IronIngot(CurrentIngots / 2);
                if (ingots != null) from.AddToBackpack(ingots);
            }
            if (CurrentNails > 1) from.AddToBackpack(new CraftNails(CurrentNails / 2));
            from.SendMessage(0x22, "Строительство отменено. Половина вложенных материалов возвращена в рюкзак.");
            Delete();
        }
        public string ShipName => m_ShipName;
        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(1); // version 1
            writer.Write((int)m_WoodResource);
            writer.Write((int)m_MetalResource);
            writer.Write(m_WoodHue);
            writer.Write(m_ShipName);
            writer.Write(m_TargetBoatDeed?.FullName);
            writer.Write(m_Builder);
            writer.Write(m_RequiredCarpentry);
            writer.Write(RequiredBoards);
            writer.Write(CurrentBoards);
            writer.Write(RequiredCloth);
            writer.Write(CurrentCloth);
            writer.Write(RequiredIngots);
            writer.Write(CurrentIngots);
            writer.Write(RequiredNails);
            writer.Write(CurrentNails);
        }
        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
            if (version >= 1)
            {
                m_WoodResource = (CraftResource)reader.ReadInt();
                m_MetalResource = (CraftResource)reader.ReadInt();
                m_WoodHue = reader.ReadInt();
            }
            m_ShipName = reader.ReadString();
            string typeName = reader.ReadString();
            if (!string.IsNullOrEmpty(typeName))
                m_TargetBoatDeed = ScriptCompiler.FindTypeByFullName(typeName);
            m_Builder = reader.ReadMobile();
            m_RequiredCarpentry = reader.ReadDouble();
            RequiredBoards = reader.ReadInt();
            CurrentBoards = reader.ReadInt();
            RequiredCloth = reader.ReadInt();
            CurrentCloth = reader.ReadInt();
            RequiredIngots = reader.ReadInt();
            CurrentIngots = reader.ReadInt();
            RequiredNails = reader.ReadInt();
            CurrentNails = reader.ReadInt();
        }
    }
    // --- Интерфейс верфи (Gump) ---
    public class ShipbuildingGump : Gump
    {
        private readonly Mobile m_From;
        private readonly ShipbuildingSite m_Site;
        public ShipbuildingGump(Mobile from, ShipbuildingSite site) : base(100, 100)
        {
            m_From = from;
            m_Site = site;
            AddPage(0);
            AddBackground(0, 0, 440, 380, 9270);
            AddAlphaRegion(15, 15, 410, 350);
            AddHtml(20, 22, 400, 25, $"ВЕРФЬ: {site.ShipName.ToUpper()}", false, false);
            string woodStr = site.WoodResource == CraftResource.None ? "Любое (первое задаст тип)" : CraftResources.GetName(site.WoodResource);
            string metalStr = site.MetalResource == CraftResource.None ? "Любой (первый задаст тип)" : CraftResources.GetName(site.MetalResource);
            AddLabel(30, 52, 0x35, $"Древесина корпуса: {woodStr}");
            AddLabel(30, 72, 0x35, $"Металл обшивки: {metalStr}");
            int y = 105;
            AddResourceRow(y, "Доски / Брёвна:", site.CurrentBoards, site.RequiredBoards); y += 40;
            AddResourceRow(y, "Ткань (паруса):", site.CurrentCloth, site.RequiredCloth); y += 40;
            AddResourceRow(y, "Слитки металла:", site.CurrentIngots, site.RequiredIngots); y += 40;
            AddResourceRow(y, "Железные гвозди:", site.CurrentNails, site.RequiredNails); y += 45;
            AddLabel(30, y, 0x480, $"Требуемый навык Carpentry: {site.RequiredCarpentry:F1}");
            AddButton(30, 315, 4005, 4007, 1, GumpButtonType.Reply, 0);
            AddLabel(65, 317, 0x44, "Внести материалы из рюкзака");
            AddButton(300, 315, 4017, 4019, 2, GumpButtonType.Reply, 0);
            AddLabel(335, 317, 0x22, "Свернуть");
        }
        private void AddResourceRow(int y, string label, int current, int required)
        {
            AddLabel(30, y, 0x480, label);
            AddLabel(280, y, current >= required ? 0x44 : 0x35, $"{current} / {required}");
            AddImageTiled(30, y + 20, 370, 10, 2624);
            double ratio = required > 0 ? Math.Min(1.0, (double)current / required) : 1.0;
            int barWidth = (int)(366 * ratio);
            if (barWidth > 0)
                AddImageTiled(32, y + 22, barWidth, 6, 9354);
        }
        public override void OnResponse(NetState sender, RelayInfo info)
        {
            if (m_Site == null || m_Site.Deleted || !m_From.InRange(m_Site.GetWorldLocation(), 4))
                return;
            if (info.ButtonID == 1)
            {
                m_Site.PullResourcesFromBackpack(m_From);
                if (!m_Site.Deleted)
                    m_From.SendGump(new ShipbuildingGump(m_From, m_Site));
            }
            else if (info.ButtonID == 2)
            {
                m_Site.CancelAndRefund(m_From);
            }
        }
    }
    // --- Базовый класс дида на закладку верфи и проекты (остаются прежними) ---
    public abstract class BaseShipyardDeed : Item
    {
        public abstract string ShipTitle { get; }
        public abstract Type BoatDeedType { get; }
        public abstract double ReqCarpentry { get; }
        public abstract int ReqBoards { get; }
        public abstract int ReqCloth { get; }
        public abstract int ReqIngots { get; }
        public abstract int ReqNails { get; }
        public BaseShipyardDeed(string name) : base(0x14F0)
        {
            Name = name;
            Weight = 5.0;
            Hue = 2413;
        }
        public BaseShipyardDeed(Serial serial) : base(serial) { }
        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);
            list.Add(1060658, "Проект\t{0}", ShipTitle);
            list.Add(1060659, "Материалы\t{0} досок, {1} ткани, {2} слитков, {3} гвоздей", ReqBoards, ReqCloth, ReqIngots, ReqNails);
        }
        public override void OnDoubleClick(Mobile from)
        {
            if (!IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001);
                return;
            }
            from.SendMessage(0x35, "Выберите место на берегу рядом с водой для закладки стапеля.");
            from.Target = new PlaceSiteTarget(this);
        }
        private class PlaceSiteTarget : Target
        {
            private readonly BaseShipyardDeed m_Deed;
            public PlaceSiteTarget(BaseShipyardDeed deed) : base(6, true, TargetFlags.None)
            {
                m_Deed = deed;
            }
            protected override void OnTarget(Mobile from, object targeted)
            {
                if (m_Deed.Deleted || !m_Deed.IsChildOf(from.Backpack))
                    return;
                IPoint3D p = targeted as IPoint3D;
                if (p == null)
                    return;
                Point3D loc = new Point3D(p);
                Map map = from.Map;
                if (map == null || !map.CanFit(loc.X, loc.Y, loc.Z, 16, false, false))
                {
                    from.SendMessage(0x22, "Здесь нельзя разместить строительную площадку.");
                    return;
                }
                if (!IsNearWater(map, loc))
                {
                    from.SendMessage(0x22, "Верфь можно заложить только на берегу (в пределах 5 тайлов от воды).");
                    return;
                }
                ShipbuildingSite site = new ShipbuildingSite(
                from,
                m_Deed.ShipTitle,
                m_Deed.BoatDeedType,
                m_Deed.ReqCarpentry,
                m_Deed.ReqBoards,
                m_Deed.ReqCloth,
                m_Deed.ReqIngots,
                m_Deed.ReqNails
                );
                site.MoveToWorld(loc, map);
                m_Deed.Delete();
                from.PlaySound(0x23D);
                from.SendMessage(0x44, "Стапель заложен! Дважды кликните по нему, чтобы начать строительство.");
            }
            private bool IsNearWater(Map map, Point3D loc)
            {
                for (int x = -5; x <= 5; x++)
                {
                    for (int y = -5; y <= 5; y++)
                    {
                        LandTile land = map.Tiles.GetLandTile(loc.X + x, loc.Y + y);
                        if ((land.ID >= 168 && land.ID <= 171) || (land.ID >= 310 && land.ID <= 311))
                            return true;
                        StaticTile[] statics = map.Tiles.GetStaticTiles(loc.X + x, loc.Y + y, true);
                        foreach (StaticTile tile in statics)
                        {
                            if ((tile.ID >= 0x1796 && tile.ID <= 0x17B2))
                                return true;
                        }
                    }
                }
                return false;
            }
        }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); }
    }
    public class SmallShipyardDeed : BaseShipyardDeed
    {
        public override string ShipTitle => "Малый шлюп (Small Boat)";
        public override Type BoatDeedType => typeof(SmallBoatDeed);
        public override double ReqCarpentry => 65.0;
        public override int ReqBoards => 600;
        public override int ReqCloth => 200;
        public override int ReqIngots => 150;
        public override int ReqNails => 50;
        [Constructable]
        public SmallShipyardDeed() : base("Чертеж закладки: Малый шлюп") { }
        public SmallShipyardDeed(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); }
    }
    public class MediumShipyardDeed : BaseShipyardDeed
    {
        public override string ShipTitle => "Средний торговый корабль (Medium Boat)";
        public override Type BoatDeedType => typeof(MediumBoatDeed);
        public override double ReqCarpentry => 80.0;
        public override int ReqBoards => 1200;
        public override int ReqCloth => 400;
        public override int ReqIngots => 300;
        public override int ReqNails => 100;
        [Constructable]
        public MediumShipyardDeed() : base("Чертеж закладки: Средний корабль") { }
        public MediumShipyardDeed(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); }
    }
    public class LargeDragonShipyardDeed : BaseShipyardDeed
    {
        public override string ShipTitle => "Большой боевой дракар (Large Dragon Boat)";
        public override Type BoatDeedType => typeof(LargeDragonBoatDeed);
        public override double ReqCarpentry => 95.0;
        public override int ReqBoards => 2500;
        public override int ReqCloth => 800;
        public override int ReqIngots => 600;
        public override int ReqNails => 200;
        [Constructable]
        public LargeDragonShipyardDeed() : base("Чертеж закладки: Большой дракар") { }
        public LargeDragonShipyardDeed(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); }
    }
}