using System;
using UnityEngine;

namespace KeyboardEscapeGameplay
{
    public sealed class World1ShopCatalog : ScriptableObject
    {
        public Item[] items;
        public Sprite activeTab;

        [Serializable]
        public sealed class Item
        {
            public string id, title;
            public bool aura, daily, iap;
            public double price;
            public float multiplier;
            public Sprite frame, icon;
            public GameObject effect;
            public Vector3 auraPreviewOffset;
        }
    }
}
