using System.IO;

namespace TrProtocol.Models
{
    public partial class ItemData
    {
        public ItemData(BinaryBufferReader br)
        {
            Read(br);
        }
        public static ItemData Get(Item item) { return item; }
        public ItemData() { }
        public override string ToString()
        {
            return $"[{ItemID}, {Prefix}, {Stack}]";
        }
        public void Write(BinaryWriter bw)
        {
            bw.Write(ItemID);
            bw.Write(Prefix);
            bw.Write(Stack);
        }
        public ItemData Read(BinaryBufferReader br)
        {
            ItemID = br.ReadInt16();
            Prefix = br.ReadByte();
            Stack = br.ReadInt16();
            return this;
        }
        public short ItemID { get; set; }
        public byte Prefix { get; set; }
        public short Stack { get; set; }
        public static implicit operator Item(ItemData item)
        {
            var i = new Item();
            if (item is null)
                return i;
            i.SetDefaults(item.ItemID);
            i.prefix = item.Prefix;
            i.stack = item.Stack;
            return i;
        }
        public static implicit operator ItemData(Item item)
        {
            return new ItemData()
            {
                ItemID = (short)item.type,
                Prefix = item.prefix,
                Stack = (short)item.stack
            };
        }
        public Item ToItem()
        {
            return this;
        }
    }
}
