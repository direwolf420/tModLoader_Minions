using AmuletOfManyMinions.Items.Accessories.SquireBat;
using AmuletOfManyMinions.Items.Accessories.SquireSkull;
using AmuletOfManyMinions.Items.Armor.RoyalArmor;
using AmuletOfManyMinions.Items.Materials;
using AmuletOfManyMinions.Projectiles.Minions.BalloonBuddy;
using AmuletOfManyMinions.Projectiles.Minions.BeeQueen;
using AmuletOfManyMinions.Projectiles.Minions.BoneSerpent;
using AmuletOfManyMinions.Projectiles.Minions.CharredChimera;
using AmuletOfManyMinions.Projectiles.Minions.GoblinGunner;
using AmuletOfManyMinions.Projectiles.Squires;
using AmuletOfManyMinions.Projectiles.Squires.AncientCobaltSquire;
using AmuletOfManyMinions.Projectiles.Squires.ArmoredBoneSquire;
using AmuletOfManyMinions.Projectiles.Squires.BoneSquire;
using AmuletOfManyMinions.Projectiles.Squires.GuideSquire;
using AmuletOfManyMinions.Projectiles.Squires.PottedPal;
using AmuletOfManyMinions.Projectiles.Squires.Squeyere;
using AmuletOfManyMinions.Projectiles.Squires.VikingSquire;
using Microsoft.Xna.Framework;
using System;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using static Terraria.ModLoader.ModContent;

namespace AmuletOfManyMinions.NPCs
{
	class LootTable : GlobalNPC
	{
		public static void SpawnRoyalArmor(Player lootRecipient, bool fromBag, NPC npc)
		{
			// always give the player the part of the set they're missing, if they're missing a part
			var mPlayer = lootRecipient.GetModPlayer<SquireModPlayer>();
			bool crown = mPlayer.RoyalCrownDropped;
			int crownType = ItemType<RoyalCrown>();
			int gownType = ItemType<RoyalGown>();
			bool gown = mPlayer.RoyalGownDropped;

			int itemType = ItemID.None;

			if (mPlayer.RoyalDroppedNone || mPlayer.RoyalDroppedFull && Main.rand.NextFloat() < 0.1f)
			{
				itemType = Main.rand.NextBool() ? crownType : gownType;
			}
			else if (!crown)
			{
				crown = true;
				itemType = crownType;
			}
			else if (!gown)
			{
				gown = true;
				itemType = gownType;
			}

			if (itemType == ItemID.None) return;

			if (fromBag)
			{
				lootRecipient.QuickSpawnItem(itemType);
				mPlayer.SetRoyalDropped(crown, gown, clientWantsBroadcast: true);
			}
			else if (npc != null)
			{
				DropItemInstanced(npc, npc.position, npc.Hitbox.Size(), itemType,
					onDropped: delegate (Player player, Item item)
					{
						player.GetModPlayer<SquireModPlayer>().SetRoyalDropped(crown, gown);
					}
				);
			}
		}

		/// <summary>
		/// Alternative, static version of npc.DropItemInstanced. Checks the npCondition delegate before syncing/spawning the item
		/// </summary>
		public static void DropItemInstanced(NPC npc, Vector2 Position, Vector2 HitboxSize, Func<int> itemTypeFunc, int itemStack = 1, Func<NPC, Player, bool> npCondition = null, Action<Player, Item> onDropped = null, Action afterAtleastOneDropped = null, bool interactionRequired = true)
		{
			bool atleastOneDropped = false;
			if (Main.netMode == NetmodeID.Server)
			{
				for (int p = 0; p < Main.maxPlayers; p++)
				{
					Player player = Main.player[p];
					if ((npc.playerInteraction[player.whoAmI] || !interactionRequired) && (npCondition?.Invoke(npc, player) ?? true))
					{
						int itemType = itemTypeFunc();
						int i = Item.NewItem((int)Position.X, (int)Position.Y, (int)HitboxSize.X, (int)HitboxSize.Y, itemType, itemStack, true);
						Item item = Main.item[i];
						onDropped?.Invoke(player, item);
						Main.itemLockoutTime[i] = 54000;
						NetMessage.SendData(MessageID.InstancedItem, player.whoAmI, -1, null, i);
						item.active = false;
						atleastOneDropped = true;
					}
				}
			}
			else if (Main.netMode == NetmodeID.SinglePlayer)
			{
				Player player = Main.LocalPlayer;
				if (npCondition?.Invoke(npc, player) ?? true)
				{
					int i = Item.NewItem((int)Position.X, (int)Position.Y, (int)HitboxSize.X, (int)HitboxSize.Y, itemTypeFunc(), itemStack);
					Item item = Main.item[i];
					onDropped?.Invoke(player, item);
					atleastOneDropped = true;
				}
			}

			if (atleastOneDropped)
			{
				afterAtleastOneDropped?.Invoke();
			}
		}

		public static void DropItemInstanced(NPC npc, Vector2 Position, Vector2 HitboxSize, int itemType, int itemStack = 1, Func<NPC, Player, bool> npCondition = null, Action<Player, Item> onDropped = null, Action afterAtleastOneDropped = null, bool interactionRequired = true)
		{
			DropItemInstanced(npc, Position, HitboxSize, () => itemType, itemStack, npCondition, onDropped, afterAtleastOneDropped, interactionRequired);
		}

		public override void NPCLoot(NPC npc)
		{
			base.NPCLoot(npc);
			// make all spawn chances more likely on expert mode
			float spawnChance = Main.rand.NextFloat() * (Main.expertMode ? 0.67f : 1);

			if(npc.type == NPCID.Guide)
			{
				if (Main.npc.Any(n => n.active && NPCSets.lunarBosses.Contains(n.type)))
				{
					Item.NewItem(npc.getRect(), ItemType<GuideHair>(), 1);
				} else if (NPC.downedBoss1 || NPC.downedSlimeKing)
				{
					Item.NewItem(npc.getRect(), ItemType<GuideSquireMinionItem>(), 1, prefixGiven: -1);
				}
			}

			if(spawnChance < 0.05f && NPCSets.preHardmodeIceEnemies.Contains(npc.netID))
			{
				Item.NewItem(npc.getRect(), ItemType<VikingSquireMinionItem>(), 1);
			}


			if (spawnChance < 0.12f && npc.type == NPCID.ManEater)
			{
				Item.NewItem(npc.getRect(), ItemType<AncientCobaltSquireMinionItem>(), 1, prefixGiven: -1);
			}

			if (spawnChance < 0.04f && NPCSets.hornets.Contains(npc.netID))
			{
				Item.NewItem(npc.getRect(), ItemType<AncientCobaltSquireMinionItem>(), 1, prefixGiven: -1);
			}

			if (spawnChance < 0.015f && NPCSets.angryBones.Contains(npc.netID))
			{
				Item.NewItem(npc.getRect(), ItemType<BoneSquireMinionItem>(), 1, prefixGiven: -1);
			}

			if (spawnChance < 0.05f && npc.type == NPCID.GiantBat)
			{
				Item.NewItem(npc.getRect(), ItemType<SquireBatAccessory>(), 1, prefixGiven: -1);
			}

			if (spawnChance < 0.33f && npc.type == NPCID.GoblinSummoner)
			{
				Item.NewItem(npc.getRect(), ItemType<GoblinGunnerMinionItem>(), 1, prefixGiven: -1);
			}

			if (spawnChance < 0.10f && npc.type == NPCID.Eyezor)
			{
				Item.NewItem(npc.getRect(), ItemType<SqueyereMinionItem>(), 1, prefixGiven: -1);
			}

			if (spawnChance < 0.03f && NPCSets.blueArmoredBones.Contains(npc.netID))
			{
				Item.NewItem(npc.getRect(), ItemType<ArmoredBoneSquireMinionItem>(), 1, prefixGiven: -1);
			}

			if (spawnChance < 0.025f && NPCSets.hellArmoredBones.Contains(npc.netID))
			{
				Item.NewItem(npc.getRect(), ItemType<CharredChimeraMinionItem>(), 1, prefixGiven: -1);
			}

			if(!Main.expertMode)
			{
				if(npc.type == NPCID.KingSlime)
				{
					Player lootRecipient = Main.player[Player.FindClosest(npc.position, npc.width, npc.height)];
					SpawnRoyalArmor(lootRecipient, false, npc);
				}

				if (spawnChance < 0.33f && npc.type == NPCID.Plantera)
				{
					Item.NewItem(npc.getRect(), ItemType<PottedPalMinionItem>(), 1, prefixGiven: -1);
				}

				if (spawnChance < 0.33f && npc.type == NPCID.QueenBee)
				{
					Item.NewItem(npc.getRect(), ItemType<BeeQueenMinionItem>(), 1, prefixGiven: -1);
				}

				if(spawnChance < 0.5f && npc.type == NPCID.SkeletronHead)
				{
					Item.NewItem(npc.getRect(), ItemType<SquireSkullAccessory>(), 1, prefixGiven: -1);
				}

				if (spawnChance < 0.25f && npc.type == NPCID.WallofFlesh)
				{
					Item.NewItem(npc.getRect(), ItemType<BoneSerpentMinionItem>(), 1, prefixGiven: -1);
				}
			}
		}

		public override void SetupShop(int type, Chest shop, ref int nextSlot)
		{
			if (type == NPCID.PartyGirl)
			{
				shop.item[nextSlot].SetDefaults(ItemType<BalloonBuddyMinionItem>());
				nextSlot++;
			}

			if(type == NPCID.Clothier)
			{
				shop.item[nextSlot].SetDefaults(ItemID.AncientCloth);
				nextSlot++;
			}
		}
	}

	public class BossBagGlobalItem: GlobalItem
	{

		public override void OpenVanillaBag(string context, Player player, int arg)
		{
			float spawnChance = Main.rand.NextFloat();
			switch(arg)
			{
				case ItemID.KingSlimeBossBag:
					LootTable.SpawnRoyalArmor(player, true, null);
					break;
				case ItemID.QueenBeeBossBag:
					if(spawnChance < 0.67f)
					{
						player.QuickSpawnItem(ItemType<BeeQueenMinionItem>());
					}
					break;
				case ItemID.SkeletronBossBag:
					player.QuickSpawnItem(ItemType<SquireSkullAccessory>());
					break;
				case ItemID.WallOfFleshBossBag:
					if (spawnChance < 0.67f)
					{
						player.QuickSpawnItem(ItemType<BoneSerpentMinionItem>());
					}
					break;
				case ItemID.PlanteraBossBag:
					if (spawnChance < 0.67f)
					{
						player.QuickSpawnItem(ItemType<PottedPalMinionItem>());
					}
					break;
				default:
					break;
			}
		}

	}
}
