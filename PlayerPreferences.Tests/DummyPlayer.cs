using System;
using System.Collections.Generic;
using Smod2.API;

namespace PlayerPreferences.Tests
{
    public class DummyPlayer : Player
    {
        public DummyPlayer(string steamId)
        {
            SteamId = steamId;
        }

        public override void Kill(DamageType type = DamageType.NUKE)
        {
            throw new NotImplementedException();
        }

        public override int GetHealth()
        {
            throw new NotImplementedException();
        }

        public override void AddHealth(int amount)
        {
            throw new NotImplementedException();
        }

        public override void Damage(int amount, DamageType type = DamageType.NUKE)
        {
            throw new NotImplementedException();
        }

        public override void SetHealth(int amount, DamageType type = DamageType.NUKE)
        {
            throw new NotImplementedException();
        }

        public override int GetAmmo(AmmoType type)
        {
            throw new NotImplementedException();
        }

        public override void SetAmmo(AmmoType type, int amount)
        {
            throw new NotImplementedException();
        }

        public override Vector GetPosition()
        {
            throw new NotImplementedException();
        }

        public override void Teleport(Vector pos, bool unstuck = false)
        {
            throw new NotImplementedException();
        }

        public override void SetRank(string color = "", string text = "", string @group = "")
        {
            throw new NotImplementedException();
        }

        public override string GetRankName()
        {
            throw new NotImplementedException();
        }

        public override void Disconnect()
        {
            throw new NotImplementedException();
        }

        public override void Disconnect(string message)
        {
            throw new NotImplementedException();
        }

        public override void Ban(int duration)
        {
            throw new NotImplementedException();
        }

        public override void Ban(int duration, string message)
        {
            throw new NotImplementedException();
        }

        public override Item GiveItem(ItemType type)
        {
            throw new NotImplementedException();
        }

        public override List<Item> GetInventory()
        {
            throw new NotImplementedException();
        }

        public override Item GetCurrentItem()
        {
            throw new NotImplementedException();
        }

        public override void SetCurrentItem(ItemType type)
        {
            throw new NotImplementedException();
        }

        public override int GetCurrentItemIndex()
        {
            throw new NotImplementedException();
        }

        public override void SetCurrentItemIndex(int index)
        {
            throw new NotImplementedException();
        }

        public override bool HasItem(ItemType type)
        {
            throw new NotImplementedException();
        }

        public override int GetItemIndex(ItemType type)
        {
            throw new NotImplementedException();
        }

        public override bool IsHandcuffed()
        {
            throw new NotImplementedException();
        }

        public override void ChangeRole(Role role, bool full = true, bool spawnTeleport = true, bool spawnProtect = true,
            bool removeHandcuffs = false)
        {
            throw new NotImplementedException();
        }

        public override object GetGameObject()
        {
            throw new NotImplementedException();
        }

        public override UserGroup GetUserGroup()
        {
            throw new NotImplementedException();
        }

        public override string[] RunCommand(string command, string[] args)
        {
            throw new NotImplementedException();
        }

        public override bool GetGodmode()
        {
            throw new NotImplementedException();
        }

        public override void SetGodmode(bool godmode)
        {
            throw new NotImplementedException();
        }

        public override Vector GetRotation()
        {
            throw new NotImplementedException();
        }

        public override void SendConsoleMessage(string message, string color = "green")
        {
            throw new NotImplementedException();
        }

        public override void Infect(float time)
        {
            throw new NotImplementedException();
        }

        public override void ThrowGrenade(ItemType grenadeType, bool isCustomDirection, Vector direction, bool isEnvironmentallyTriggered,
            Vector position, bool isCustomForce, float throwForce, bool slowThrow = false)
        {
            throw new NotImplementedException();
        }

        public override bool GetBypassMode()
        {
            throw new NotImplementedException();
        }

        public override string GetAuthToken()
        {
            throw new NotImplementedException();
        }

        public override void HideTag(bool enable)
        {
            throw new NotImplementedException();
        }

        public override void PersonalBroadcast(uint duration, string message, bool isMonoSpaced)
        {
            throw new NotImplementedException();
        }

        public override void PersonalClearBroadcasts()
        {
            throw new NotImplementedException();
        }

        public override Vector Get106Portal()
        {
            throw new NotImplementedException();
        }

        public override void SetRadioBattery(int battery)
        {
            throw new NotImplementedException();
        }

        public override void HandcuffPlayer(Player playerToHandcuff)
        {
            throw new NotImplementedException();
        }

        public override void RemoveHandcuffs()
        {
            throw new NotImplementedException();
        }

        public override bool GetGhostMode()
        {
            throw new NotImplementedException();
        }

        public override void SetGhostMode(bool ghostMode, bool visibleToSpec = true, bool visibleWhenTalking = true)
        {
            throw new NotImplementedException();
        }

        public override TeamRole TeamRole { get; set; }
        public override string Name { get; }
        public override string IpAddress { get; }
        public override int PlayerId { get; }
        public override string SteamId { get; }
        public override RadioStatus RadioStatus { get; set; }
        public override bool OverwatchMode { get; set; }
        public override bool DoNotTrack { get; }
    }
}
