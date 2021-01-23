using System;

namespace Rs317.Sharp
{
	public sealed class Player : Entity
	{
		public int rights;

		private long aLong1697;

		public EntityDefinition npcAppearance;

		public bool preventRotation { get; set; }

		public int[] bodyPartColour { get; private set; }

		public int team;

		private int gender;

		public String name;
		public static Cache mruNodes = new Cache(260);
		public int combatLevel;
		public int headIcon;
		public int skullIcon;
		public int modifiedAppearanceStartTime;
		public int modifiedAppearanceEndTime { get; set; }
		public int drawHeight2 { get; set; }
		public bool visible { get; set; }
		public int anInt1711 { get; set; }
		public int drawHeight { get; set; }
		public int anInt1713 { get; set; }
		public Model playerModel { get; set; }
		public int[] appearance;
		public long appearanceOffset;
		public int localX { get; set; }
		public int localY { get; set; }
		public int playerTileHeight { get; set; }
		public int playerTileWidth { get; set; }
		public int skill { get; private set; }

		public static IBaseClient clientInstance;

		public Player()
		{
			aLong1697 = -1L;
			preventRotation = false;
			bodyPartColour = new int[5];
			visible = false;
			appearance = new int[12];
		}

		private Model getAnimatedModel()
		{
			if (npcAppearance != null)
			{
				int frameId = -1;
				if (base.animation >= 0 && base.animationDelay == 0)
					frameId = AnimationSequence.animations[base.animation].primaryFrames[base.currentAnimationFrame];
				else if (base.queuedAnimationId >= 0)
					frameId = AnimationSequence.animations[base.queuedAnimationId].primaryFrames[base.queuedAnimationFrame];
				Model model = npcAppearance.getChildModel(-1, frameId, null);
				return model;
			}

			long l = appearanceOffset;
			int k = -1;
			int i1 = -1;
			int j1 = -1;
			int k1 = -1;
			if (base.animation >= 0 && base.animationDelay == 0)
			{
				AnimationSequence animation = AnimationSequence.animations[base.animation];
				k = animation.primaryFrames[base.currentAnimationFrame];
				if (base.queuedAnimationId >= 0 && base.queuedAnimationId != base.standAnimationId)
					i1 = AnimationSequence.animations[base.queuedAnimationId].primaryFrames[base.queuedAnimationFrame];
				if (animation.playerReplacementShield >= 0)
				{
					j1 = animation.playerReplacementShield;
					l += j1 - appearance[5] << 40;
				}

				if (animation.playerReplacementWeapon >= 0)
				{
					k1 = animation.playerReplacementWeapon;
					l += k1 - appearance[3] << 48;
				}
			}
			else if (base.queuedAnimationId >= 0)
				k = AnimationSequence.animations[base.queuedAnimationId].primaryFrames[base.queuedAnimationFrame];

			Model model_1 = (Model) mruNodes.get(l);
			if (model_1 == null)
			{
				bool flag = false;
				for (int i2 = 0; i2 < 12; i2++)
				{
					int k2 = appearance[i2];
					if (k1 >= 0 && i2 == 3)
						k2 = k1;
					if (j1 >= 0 && i2 == 5)
						k2 = j1;
					if (k2 >= 256 && k2 < 512 && !IdentityKit.cache[k2 - 256].bodyModelCached())
						flag = true;
					if (k2 >= 512 && !ItemDefinition.getDefinition(k2 - 512).equipModelCached(gender))
						flag = true;
				}

				if (flag)
				{
					if (aLong1697 != -1L)
						model_1 = (Model) mruNodes.get(aLong1697);
					if (model_1 == null)
						return null;
				}
			}

			if (model_1 == null)
			{
				Model[] models = new Model[12];
				int j2 = 0;
				for (int l2 = 0; l2 < 12; l2++)
				{
					int i3 = appearance[l2];
					if (k1 >= 0 && l2 == 3)
						i3 = k1;
					if (j1 >= 0 && l2 == 5)
						i3 = j1;
					if (i3 >= 256 && i3 < 512)
					{
						Model model_3 = IdentityKit.cache[i3 - 256].getBodyModel();
						if (model_3 != null)
							models[j2++] = model_3;
					}

					if (i3 >= 512)
					{
						Model model_4 = ItemDefinition.getDefinition(i3 - 512).getEquippedModel(gender);
						if (model_4 != null)
							models[j2++] = model_4;
					}
				}

				model_1 = new Model(j2, models);
				for (int part = 0; part < 5; part++)
					if (bodyPartColour[part] != 0)
					{
						model_1.recolour(ConstantData.GetAppearanceColor(part, 0),
							ConstantData.GetAppearanceColor(part, bodyPartColour[part]));
						if (part == 1)
							model_1.recolour(ConstantData.GetBeardColor(0), ConstantData.GetBeardColor(bodyPartColour[part]));
					}

				model_1.createBones();
				model_1.applyLighting(64, 850, -30, -50, -30, true);
				mruNodes.put(model_1, l);
				aLong1697 = l;
			}

			if (preventRotation)
				return model_1;
			Model model_2 = Model.aModel_1621;
			model_2.replaceWithModel(model_1, Animation.isNullFrame(k) & Animation.isNullFrame(i1));
			if (k != -1 && i1 != -1)
				model_2.mixAnimationFrames(AnimationSequence.animations[base.animation].flowControl, i1, k);
			else if (k != -1)
				model_2.applyTransformation(k);
			model_2.calculateDiagonals();
			model_2.triangleSkin = null;
			model_2.vertexSkin = null;
			return model_2;
		}

		public Model getHeadModel()
		{
			if (!visible)
				return null;
			if (npcAppearance != null)
				return npcAppearance.getHeadModel();
			bool flag = false;
			for (int i = 0; i < 12; i++)
			{
				int j = appearance[i];
				if (j >= 256 && j < 512 && !IdentityKit.cache[j - 256].headModelCached())
					flag = true;
				if (j >= 512 && !ItemDefinition.getDefinition(j - 512).isDialogueModelCached(gender))
					flag = true;
			}

			if (flag)
				return null;
			Model[] models = new Model[12];
			int k = 0;
			for (int l = 0; l < 12; l++)
			{
				int i1 = appearance[l];
				if (i1 >= 256 && i1 < 512)
				{
					Model model_1 = IdentityKit.cache[i1 - 256].getHeadModel();
					if (model_1 != null)
						models[k++] = model_1;
				}

				if (i1 >= 512)
				{
					Model model_2 = ItemDefinition.getDefinition(i1 - 512).getDialogueModel(gender);
					if (model_2 != null)
						models[k++] = model_2;
				}
			}

			Model model = new Model(k, models);
			for (int j1 = 0; j1 < 5; j1++)
				if (bodyPartColour[j1] != 0)
				{
					model.recolour(ConstantData.GetAppearanceColor(j1, 0), ConstantData.GetAppearanceColor(j1, bodyPartColour[j1]));
					if (j1 == 1)
						model.recolour(ConstantData.GetBeardColor(0), ConstantData.GetBeardColor(bodyPartColour[j1]));
				}

			return model;
		}

		public override Model getRotatedModel()
		{
			if (!visible)
				return null;
			Model appearanceModel = getAnimatedModel();
			if (appearanceModel == null)
				return null;
			base.height = appearanceModel.modelHeight;
			appearanceModel.singleTile = true;
			if (preventRotation)
				return appearanceModel;
			if (base.graphicId != -1 && base.currentAnimationId != -1)
			{
				SpotAnimation animation = SpotAnimation.cache[base.graphicId];
				Model graphicModel = animation.getModel();
				if (graphicModel != null)
				{
					Model model = new Model(true, Animation.isNullFrame(base.currentAnimationId), false, graphicModel);
					model.translate(0, -base.graphicHeight, 0);
					model.createBones();
					model.applyTransformation(animation.sequences.primaryFrames[base.currentAnimationId]);
					model.triangleSkin = null;
					model.vertexSkin = null;
					if (animation.scaleXY != 128 || animation.scaleZ != 128)
						model.scaleT(animation.scaleXY, animation.scaleXY, animation.scaleZ);
					model.applyLighting(64 + animation.modelLightFalloff, 850 + animation.modelLightAmbient, -30, -50, -30,
						true);
					Model[] models = {appearanceModel, model};
					appearanceModel = new Model(models);
				}
			}

			if (playerModel != null)
			{
				if (clientInstance.CurrentTick >= modifiedAppearanceEndTime)
					playerModel = null;
				if (clientInstance.CurrentTick >= modifiedAppearanceStartTime && clientInstance.CurrentTick < modifiedAppearanceEndTime)
				{
					Model model = playerModel;
					model.translate(anInt1711 - base.x, drawHeight - drawHeight2, anInt1713 - base.y);
					if (base.turnDirection == 512)
					{
						model.rotate90Degrees();
						model.rotate90Degrees();
						model.rotate90Degrees();
					}
					else if (base.turnDirection == 1024)
					{
						model.rotate90Degrees();
						model.rotate90Degrees();
					}
					else if (base.turnDirection == 1536)
						model.rotate90Degrees();

					Model[] models = {appearanceModel, model};
					appearanceModel = new Model(models);
					if (base.turnDirection == 512)
						model.rotate90Degrees();
					else if (base.turnDirection == 1024)
					{
						model.rotate90Degrees();
						model.rotate90Degrees();
					}
					else if (base.turnDirection == 1536)
					{
						model.rotate90Degrees();
						model.rotate90Degrees();
						model.rotate90Degrees();
					}

					model.translate(base.x - anInt1711, drawHeight2 - drawHeight, base.y - anInt1713);
				}
			}

			appearanceModel.singleTile = true;
			return appearanceModel;
		}

		public sealed override bool isVisible()
		{
			return visible;
		}

		public override void SetName(string name)
		{
			if (string.IsNullOrEmpty(name)) throw new ArgumentException("Value cannot be null or empty.", nameof(name));

			this.name = name;
		}

		public override void SetLevel(int level)
		{
			if (level <= 0) throw new ArgumentOutOfRangeException(nameof(level));

			combatLevel = level;
		}

		public void updatePlayerAppearance(Default317Buffer stream)
		{
			stream.position = 0;
			gender = stream.getUnsignedByte();
			headIcon = stream.getUnsignedByte();
			skullIcon = stream.getUnsignedByte();

			npcAppearance = null;
			this.team = 0;
			for (int slot = 0; slot < 12; slot++)
			{
				int itemId1 = stream.getUnsignedByte();
				if (itemId1 == 0)
				{
					appearance[slot] = 0;
					continue;
				}

				int itemId2 = stream.getUnsignedByte();
				appearance[slot] = (itemId1 << 8) + itemId2;
				if (slot == 0 && appearance[0] == 65535)
				{
					npcAppearance = EntityDefinition.getDefinition(stream.getUnsignedLEShort());
					break;
				}

				if (appearance[slot] >= 512 && appearance[slot] - 512 < ItemDefinition.itemCount)
				{
					int team = ItemDefinition.getDefinition(appearance[slot] - 512).teamId;
					if (team != 0)
						this.team = team;
				}
			}

			for (int bodyPart = 0; bodyPart < 5; bodyPart++)
			{
				int colour = stream.getUnsignedByte();
				if (colour < 0 || colour >= ConstantData.GetAppearanceColorRowLength(bodyPart))
					colour = 0;
				bodyPartColour[bodyPart] = colour;
			}

			base.standAnimationId = stream.getUnsignedLEShort();
			if (base.standAnimationId == 65535)
				base.standAnimationId = -1;
			base.standTurnAnimationId = stream.getUnsignedLEShort();
			if (base.standTurnAnimationId == 65535)
				base.standTurnAnimationId = -1;
			base.walkAnimationId = stream.getUnsignedLEShort();
			if (base.walkAnimationId == 65535)
				base.walkAnimationId = -1;
			base.turnAboutAnimationId = stream.getUnsignedLEShort();
			if (base.turnAboutAnimationId == 65535)
				base.turnAboutAnimationId = -1;
			base.turnRightAnimationId = stream.getUnsignedLEShort();
			if (base.turnRightAnimationId == 65535)
				base.turnRightAnimationId = -1;
			base.turnLeftAnimationId = stream.getUnsignedLEShort();
			if (base.turnLeftAnimationId == 65535)
				base.turnLeftAnimationId = -1;
			base.runAnimationId = stream.getUnsignedLEShort();
			if (base.runAnimationId == 65535)
				base.runAnimationId = -1;
			name = TextClass.formatName(TextClass.longToName(stream.getLong()));
			combatLevel = stream.getUnsignedByte();
			skill = stream.getUnsignedLEShort();
			visible = true;
			appearanceOffset = 0L;
			for (int slot = 0; slot < 12; slot++)
			{
				appearanceOffset <<= 4;
				if (appearance[slot] >= 256)
					appearanceOffset += appearance[slot] - 256;
			}

			if (appearance[0] >= 256)
				appearanceOffset += appearance[0] - 256 >> 4;
			if (appearance[1] >= 256)
				appearanceOffset += appearance[1] - 256 >> 8;
			for (int bodyPart = 0; bodyPart < 5; bodyPart++)
			{
				appearanceOffset <<= 3;
				appearanceOffset += bodyPartColour[bodyPart];
			}

			appearanceOffset <<= 1;
			appearanceOffset += gender;
		}
	}
}