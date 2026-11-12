using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KeyboardEscapeGameplay
{
    public sealed partial class World1Session
    {
        readonly HashSet<string> ownedCosmetics = new HashSet<string>();
        GameObject trailEffect, auraEffect;
        bool auraUsesSourceSlot;
        internal World1CosmeticMaterials CosmeticMaterials { get; private set; }

        public World1ShopCatalog Catalog { get; private set; }
        public World1ShopCatalog.Item EquippedTrail { get; private set; }
        public World1ShopCatalog.Item EquippedAura { get; private set; }
        public bool ShopOpen { get; internal set; }
        public float SpeedLimit { get; private set; } = 1;
        public float PlayerSpeed => Mathf.Lerp(World1Rules.MinimumMoveSpeed, World1Rules.MoveSpeedAtLevel(Level), SpeedLimit);
        public double CosmeticMultiplier => (EquippedTrail?.multiplier ?? 1) * (EquippedAura?.multiplier ?? 1);

        public void SetSpeedLimit(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value)) return;
            SpeedLimit = Mathf.Clamp01(value);
        }

        public bool Owns(World1ShopCatalog.Item item) => item != null && ownedCosmetics.Contains(item.id);
        public bool IsEquipped(World1ShopCatalog.Item item) => item != null && (item.aura ? EquippedAura : EquippedTrail) == item;

        public string UseCosmetic(World1ShopCatalog.Item item)
        {
            if (item == null || !Catalog.items.Contains(item)) return "Vật phẩm không có trong shop.";
            if (!Owns(item))
            {
                if (item.daily) return "Nhận vật phẩm này từ phần thưởng đăng nhập ngày.";
                if (item.iap) return "Vật phẩm này cần giao dịch IAP trong game gốc.";
                if (Entropy < item.price) return "Bạn chưa đủ cúp.";
                Entropy -= item.price;
                ownedCosmetics.Add(item.id);
            }

            SetEquipped(item.aura, IsEquipped(item) ? null : item);
            Save();
            return null;
        }

        void LoadShopProgress()
        {
            CosmeticMaterials = new World1CosmeticMaterials();
            Catalog = Resources.Load<World1ShopCatalog>("World1ShopCatalog");
            if (Catalog == null) throw new InvalidOperationException("World 1 shop catalog is missing.");
            if (!persistProgress) return;

            SetSpeedLimit(PlayerPrefs.GetFloat(SaveKey + "SpeedLimit", 1));
            string json = PlayerPrefs.GetString(SaveKey + "Cosmetics", "");
            if (string.IsNullOrEmpty(json)) return;
            CosmeticSave data;
            try { data = JsonUtility.FromJson<CosmeticSave>(json); }
            catch (ArgumentException) { return; }
            if (data?.owned == null) return;

            foreach (var item in Catalog.items)
            {
                if (Array.IndexOf(data.owned, item.id) < 0) continue;
                ownedCosmetics.Add(item.id);
                if (item.id == (item.aura ? data.aura : data.trail)) SetEquipped(item.aura, item);
            }
        }

        void SaveShopProgress()
        {
            PlayerPrefs.SetFloat(SaveKey + "SpeedLimit", SpeedLimit);
            var data = new CosmeticSave
            {
                owned = ownedCosmetics.ToArray(),
                trail = EquippedTrail?.id,
                aura = EquippedAura?.id
            };
            PlayerPrefs.SetString(SaveKey + "Cosmetics", JsonUtility.ToJson(data));
        }

        void SetEquipped(bool aura, World1ShopCatalog.Item item)
        {
            if (aura)
            {
                EquippedAura = item;
                SetAuraVisual(item);
            }
            else
            {
                EquippedTrail = item;
                ReplaceEffect(ref trailEffect, item);
            }
        }

        void SetAuraVisual(World1ShopCatalog.Item item)
        {
            if (auraEffect != null)
            {
                auraEffect.SetActive(false);
                if (!auraUsesSourceSlot) Destroy(auraEffect);
            }
            auraEffect = null;
            auraUsesSourceSlot = false;
            if (item?.effect == null) return;

            // The W1 Aura_Provider maps each asset to its existing visual under this transform.
            var provider = player.transform.Find("Root/Aura_Provider");
            var slot = provider == null ? null : provider.Find(item.effect.name);
            auraUsesSourceSlot = slot != null;
            if (auraUsesSourceSlot) auraEffect = slot.gameObject;
            else
            {
                auraEffect = Instantiate(item.effect, player.transform, false);
                auraEffect.transform.localPosition = Vector3.up * .75f;
            }
            CosmeticMaterials.ApplyAura(auraEffect);
            auraEffect.SetActive(true);
        }

        void ReplaceEffect(ref GameObject effect, World1ShopCatalog.Item item)
        {
            if (effect != null)
            {
                effect.SetActive(false);
                Destroy(effect);
            }
            effect = item?.effect == null ? null : Instantiate(item.effect, player.transform, false);
            if (effect != null)
            {
                effect.name = item.id;
                CosmeticMaterials.Apply(effect);
            }
        }

        internal void ClearCosmeticTrail()
        {
            if (trailEffect == null) return;
            foreach (var trail in trailEffect.GetComponentsInChildren<TrailRenderer>()) trail.Clear();
        }

        [Serializable]
        sealed class CosmeticSave
        {
            public string[] owned;
            public string trail, aura;
        }
    }
}
