using AmuletOfManyMinions.Projectiles.Squires;
using System.IO;
using Terraria;
using Terraria.ID;

namespace AmuletOfManyMinions.Core.Netcode.Packets
{
	public class RoyalDroppedPacket : PlayerPacket
	{
		protected readonly bool crown;
		protected readonly bool gown;
		protected readonly bool broadcast;

		//For reflection
		public RoyalDroppedPacket() { }

		public RoyalDroppedPacket(Player player, bool crown = false, bool gown = false, bool broadcast = false) : base(player)
		{
			this.crown = crown;
			this.gown = gown;
			this.broadcast = broadcast;
		}

		protected override void PostSend(BinaryWriter writer, Player player)
		{
			BitsByte bits = new BitsByte(crown, gown, broadcast);
			writer.Write(bits);
		}

		protected override void PostReceive(BinaryReader reader, int sender, Player player)
		{
			BitsByte bits = reader.ReadByte();
			bool crown = bits[0];
			bool gown = bits[1];
			bool broadcast = bits[2];

			player.GetModPlayer<SquireModPlayer>().SetRoyalDropped(crown, gown);

			if (Main.netMode == NetmodeID.Server && broadcast)
			{
				//Broadcast to all but original sender
				new RoyalDroppedPacket(player, crown, gown).Send(from: sender);
			}
		}
	}
}
