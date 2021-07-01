using DoffAndDonAgain.Common;
using DoffAndDonAgain.Common.Network;
using DoffAndDonAgain.Utility;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace DoffAndDonAgain.Server {
  public class SwapHandler {
    private DoffAndDonSystem System;

    public SwapHandler(DoffAndDonSystem system) {
      if (system.Side != EnumAppSide.Server) {
        system.Api.Logger.Warning("{0} is a server object instantiated on the client, ignoring.", nameof(DoffHandler));
        return;
      }
      System = system;

      if (System.Config.EnableSwap) {
        System.ServerChannel.SetMessageHandler<SwapArmorPacket>(OnSwapArmorPacket);
      }
      else {
        System.ServerChannel.SetMessageHandler<SwapArmorPacket>(OnSwapArmorPacketSwapDisabled);
      }
    }

    private void OnSwapArmorPacket(IServerPlayer player, SwapArmorPacket packet) {
      bool swapped = false;
      var armorStand = player.Entity.GetEntityArmorStandById(packet.ArmorStandEntityId);
      if (armorStand == null) {
        System.Error.TriggerFromServer(Constants.ERROR_TARGET_LOST, player);
        return;
      }
      else {
        swapped = SwapArmorWithStand(player, armorStand);
      }
      OnSwapcompleted(player, swapped);
    }

    private bool SwapArmorWithStand(IServerPlayer swapper, EntityArmorStand armorStand) {
      if (swapper == null || armorStand == null) { return false; }
      bool swapped = false;
      var playerArmorSlots = swapper.Entity.GetArmorSlots();
      var armorStandArmorSlots = armorStand.GetArmorSlots();

      for (int i = 0; i < playerArmorSlots.Count; i++) {
        if (playerArmorSlots[i].Empty && armorStandArmorSlots[i].Empty) { continue; }
        swapped = playerArmorSlots[i].TryFlipWith(armorStandArmorSlots[i]) || swapped;
      }
      if (swapped) {
        System.ArmorStandRerenderHandler?.UpdateRender(armorStand);
      }
      return swapped;
    }

    private void OnSwapcompleted(IServerPlayer player, bool successful) {
      if (successful) {
        OnSuccessfulSwap(player);
      }
      else {
        System.Error.TriggerFromServer(Constants.ERROR_COULD_NOT_SWAP, player);
      }
    }

    private void OnSuccessfulSwap(IServerPlayer player) {
      player.Entity.ConsumeSaturation(System.Config.SaturationCostPerSwap);
    }

    private void OnSwapArmorPacketSwapDisabled(IServerPlayer wouldBeSwapper, SwapArmorPacket packet) {
      System.Error.TriggerFromServer(Constants.ERROR_SWAP_DISABLED, wouldBeSwapper);
    }
  }
}
