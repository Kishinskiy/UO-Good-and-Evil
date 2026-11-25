using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Server;
using Server.Items;
using Server.Mobiles;

namespace Server.Mobiles
{
    public abstract class BaseMerchantBot : PlayerVendor
    {
        protected static readonly TimeSpan UpdateInterval = TimeSpan.FromHours(4.0);
        protected static readonly int MaxItemsOnSale = 15;
        protected static readonly FieldInfo SellItemsField =
            typeof(PlayerVendor).GetField("m_SellItems", BindingFlags.Instance | BindingFlags.NonPublic);

        private Timer _marketTimer;
        private bool _marketTimerStarted;

        public abstract string ProfessionName { get; }

        public BaseMerchantBot() : base(null, null)
        {
            Name = NameList.RandomName("male");
            Title = $"Traveling {ProfessionName}";
            CantWalk = false;
            this.HoldGold = 500000;
            this.Direction = (Direction)Utility.Random(8);

            DressUp();
        }

        // Методы, которые переопределяет каждый конкретный торговец
        protected abstract void DressUp();
        public abstract bool IsItemForProfession(Item item);
        protected abstract void GenerateCraftedStock(Hashtable sellItems);

        public void ProcessMarketCycle()
        {
            if (this.Backpack == null || this.Deleted || this.Map == null || this.Map == Map.Internal)
                return;

            Hashtable sellItems = SellItemsField?.GetValue(this) as Hashtable;
            if (sellItems == null) return;

            // 1. Чистка старого ассортимента
            List<Item> toRemove = new List<Item>();
            foreach (DictionaryEntry de in sellItems)
            {
                if (de.Key is Item item && !item.Deleted && Utility.RandomDouble() < 0.30)
                    toRemove.Add(item);
            }
            foreach (Item item in toRemove) item.Delete();

            // 2. ИИ-спекуляция (выкуп профильных товаров у других вендоров)
            if (Utility.RandomDouble() < 0.45 && sellItems.Count < MaxItemsOnSale)
                TrySpeculatePlayerVendors(sellItems);

            // 3. Пополнение витрины крафтовыми товарами
            if (sellItems.Count < MaxItemsOnSale)
            {
                int itemsToGenerate = Utility.RandomMinMax(1, 3);
                for (int i = 0; i < itemsToGenerate && sellItems.Count < MaxItemsOnSale; i++)
                    GenerateCraftedStock(sellItems);
            }

            this.InvalidateProperties();
        }

        public override bool OnDragDrop(Mobile from, Item item)
        {
            if (from == null || item == null || this.Deleted || this.Backpack == null || from.Backpack == null)
                return false;

            if (!IsItemForProfession(item))
            {
                this.SayTo(from, $"Я {ProfessionName}, мне не интересен этот товар. Обратитесь к другому мастеру!");
                return false;
            }

            Hashtable sellItems = SellItemsField?.GetValue(this) as Hashtable;
            if (sellItems != null && sellItems.Count >= MaxItemsOnSale)
            {
                this.SayTo(from, "Моя лавка переполнена, приходи позже!");
                return false;
            }

            int payoutGold = EvaluateItemWorth(item);
            if (!this.Backpack.TryDropItem(from, item, false))
                return false;

            Gold payout = payoutGold > 0 ? new Gold(payoutGold) : null;
            if (payout != null && !from.AddToBackpack(payout))
            {
                payout.Delete();
                from.Backpack.DropItem(item);
                this.SayTo(from, "Освободите место в рюкзаке для оплаты.");
                return false;
            }

            this.SayTo(from, $"Отличная работа! Я забираю это за {payoutGold} золотых.");
            this.PlaySound(0x2E6);

            AddItemToSale(sellItems, item, payoutGold * 3, $"[Куплено у мастера {from.Name}]");
            return true;
        }

        private void TrySpeculatePlayerVendors(Hashtable botSellItems)
        {
            if (SellItemsField == null) return;

            List<PlayerVendor> vendors = new List<PlayerVendor>();
            foreach (Mobile m in World.Mobiles.Values)
            {
                if (m is PlayerVendor pv && pv.Backpack != null && !pv.Deleted && pv.Map == this.Map && pv != this)
                    vendors.Add(pv);
            }

            if (vendors.Count == 0) return;

            PlayerVendor targetVendor = vendors[Utility.Random(vendors.Count)];
            Hashtable playerSellItems = SellItemsField.GetValue(targetVendor) as Hashtable;
            if (playerSellItems == null || playerSellItems.Count == 0) return;

            List<Item> validItems = new List<Item>();
            foreach (DictionaryEntry de in playerSellItems)
            {
                if (de.Key is Item it && !it.Deleted && IsItemForProfession(it))
                    validItems.Add(it);
            }

            if (validItems.Count == 0) return;

            Item itemToBuy = validItems[Utility.Random(validItems.Count)];
            VendorItem playerVi = playerSellItems[itemToBuy] as VendorItem;
            if (playerVi == null || playerVi.Price <= 0) return;

            int targetPrice = playerVi.Price;
            if (targetPrice <= (EvaluateItemWorth(itemToBuy) * 2.5) && HoldGold >= targetPrice)
            {
                HoldGold -= targetPrice;
                if (targetVendor is BaseMerchantBot)
                    targetVendor.HoldGold += targetPrice;
                else if (targetVendor.Owner != null)
                {
                    targetVendor.Owner.BankBox.DropItem(new Gold(targetPrice));
                    targetVendor.Owner.SendMessage(0x58, $"[Рынок]: {this.Title} {this.Name} выкупил ваш товар '{itemToBuy.Name}' за {targetPrice} gp!");
                }

                playerVi.Invalidate();
                playerSellItems.Remove(itemToBuy);
                this.Backpack.DropItem(itemToBuy);

                AddItemToSale(botSellItems, itemToBuy, (int)(targetPrice * 1.5), "[Перекуплено на рынке]");
                targetVendor.InvalidateProperties();
            }
        }

        protected void AddItemToSale(Hashtable sellItems, Item item, int price, string fallbackDesc)
        {
            if (item == null || sellItems == null) return;

            if (string.IsNullOrEmpty(item.Name))
                item.Name = !string.IsNullOrEmpty(item.ItemData.Name) ? item.ItemData.Name : item.GetType().Name;

            if (item.Parent != this.Backpack)
                this.Backpack.DropItem(item);

            if (sellItems[item] is VendorItem previous)
                previous.Invalidate();

            string desc = GetItemDescription(item) ?? fallbackDesc;
            sellItems[item] = new VendorItem(item, price, desc, DateTime.UtcNow);
            item.InvalidateProperties();
            this.InvalidateProperties();
        }

        protected virtual int EvaluateItemWorth(Item item)
        {
            if (item == null) return 0;
            int worth = (int)(item.Weight * 50) + 250;
            if (item is BaseWeapon bw && bw.Quality == ItemQuality.Exceptional) worth += 1500;
            if (item is BaseArmor ba && ba.Quality == ItemQuality.Exceptional) worth += 1500;
            if (item is BaseContainer) worth += 2000;
            return Math.Max(300, Math.Min(45000, worth));
        }

        private static string GetItemDescription(Item item)
        {
            if (item is BaseWeapon bw) return $"Урон: {bw.MinDamage}-{bw.MaxDamage}, Скорость: {bw.Speed}";
            if (item is BaseArmor ba) return $"Броня: {ba.ArmorRating}";
            if (item is BaseContainer) return "Контейнер / Мебель";
            return null;
        }

        public BaseMerchantBot(Serial serial) : base(serial) { }

        public override void OnAfterSpawn()
        {
            base.OnAfterSpawn();
            StartMarketTimer();
            ProcessMarketCycle();
        }

        private void StartMarketTimer()
        {
            if (Deleted || _marketTimerStarted) return;
            _marketTimerStarted = true;
            _marketTimer = Timer.DelayCall(TimeSpan.FromSeconds(5.0), UpdateInterval, ProcessMarketCycle);
        }

        public override void OnDelete()
        {
            _marketTimer?.Stop();
            base.OnDelete();
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
            if (this.HoldGold < 100000) this.HoldGold = 500000;
            Timer.DelayCall(TimeSpan.FromSeconds(5.0), StartMarketTimer);
        }
    }
}
