using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace ShakeItDoff {
  public class ShakeItDoffMod : ModSystem {
    public const string DOFF_CHANNEL_NAME = "shakeitdoff";
    private const GlKeys DEFAULT_DOFF_KEY = GlKeys.U;
    private const string DOFF_CODE = "doffarmor";
    private const string DOFF_DESC = "Remove all armor";
    private const string DOFF_ERROR_HANDS = "needbothhandsfree";
    private const string DOFF_ERROR_HANDS_DESC = "Need both hands free.";
    public override void Start(ICoreAPI api) {
      base.Start(api);

      api.Network.RegisterChannel(DOFF_CHANNEL_NAME)
        .RegisterMessageType(typeof(DoffArmorPacket));
    }

    public override void StartClientSide(ICoreClientAPI capi) {
      base.StartClientSide(capi);

      capi.Input.RegisterHotKey(DOFF_CODE, DOFF_DESC, DEFAULT_DOFF_KEY, HotkeyType.CharacterControls);
      capi.Input.SetHotKeyHandler(DOFF_CODE, (KeyCombination kc) => { return TryToDoff(capi); });
    }

    public override void StartServerSide(ICoreServerAPI sapi) {
      base.StartServerSide(sapi);

      sapi.Network.GetChannel(DOFF_CHANNEL_NAME).SetMessageHandler<DoffArmorPacket>((IServerPlayer doffer, DoffArmorPacket packet) => { Doff(doffer, packet); });
    }

    private bool TryToDoff(ICoreClientAPI capi) {
      var doffer = capi.World.Player;
      if (HasBothHandsEmpty(doffer)) {
        var doffArmorPacket = new DoffArmorPacket();
        var armorStand = GetTargetedArmorStandEntity(doffer);
        doffArmorPacket.ArmorStandEntityId = armorStand?.EntityId;
        capi.Network.GetChannel(DOFF_CHANNEL_NAME).SendPacket(doffArmorPacket);
        Doff(doffer, armorStand);
        return true;
      }
      else {
        capi.TriggerIngameError(this, DOFF_ERROR_HANDS, Lang.GetIfExists($"shakeitdoff:ingameerror-{DOFF_ERROR_HANDS}") ?? DOFF_ERROR_HANDS_DESC);
        return false;
      }
    }

    private bool HasBothHandsEmpty(IPlayer doffer) {
      return doffer.Entity.RightHandItemSlot.Empty && doffer.Entity.LeftHandItemSlot.Empty;
    }

    private EntityArmorStand GetTargetedArmorStandEntity(IClientPlayer player) {
      return player.CurrentEntitySelection?.Entity as EntityArmorStand;
    }

    private void Doff(IServerPlayer doffer, DoffArmorPacket packet) {
      EntityArmorStand armorStand = doffer.Entity.World.GetNearestEntity(doffer.Entity.Pos.AsBlockPos.ToVec3d(), 10, 10, (Entity entity) => {
        return entity.EntityId == packet.ArmorStandEntityId;
      }) as EntityArmorStand;
      Doff(doffer, armorStand);
    }

    private void Doff(IPlayer doffer, EntityArmorStand armorStand) {
      bool gaveToArmorStand = false;
      foreach (var slot in doffer.Entity.GetArmorSlots().Values) {
        if (!(slot?.Empty ?? true)) {
          var sinkSlot = armorStand?.GearInventory?.GetBestSuitedSlot(slot);
          if (sinkSlot?.slot != null && sinkSlot.weight > 0) {
            if (slot.TryPutInto(doffer.Entity.World, sinkSlot.slot) > 0) {
              gaveToArmorStand = true;
              sinkSlot.slot.MarkDirty();
            }
            gaveToArmorStand = true;
          }
          else {
            doffer.InventoryManager.DropItem(slot, true);
          }
          slot.MarkDirty();
        }
      }
      if (gaveToArmorStand) {
        armorStand.WatchedAttributes.MarkAllDirty();
      }
    }
  }
}
