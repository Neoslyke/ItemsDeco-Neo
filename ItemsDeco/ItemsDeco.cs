using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.Drawing;
using Terraria.ID;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.Hooks;

namespace ItemsDeco;

[ApiVersion(2, 1)]
public class ItemsDeco : TerrariaPlugin
{
    public override string Name => "ItemsDeco";
    public override string Author => "Neoslyke, FrankV22, Soofa, 少司命";
    public override Version Version => new Version(3, 2, 0);
    public override string Description => "Shows item decoration when switching items and in chat.";

    private readonly Dictionary<int, int> _lastSelectedItem = new();

    public static Configuration Config { get; private set; } = new();

    public ItemsDeco(Main game) : base(game)
    {
        Order = -100;
    }

    public override void Initialize()
    {
        Config = Configuration.Load();

        ServerApi.Hooks.ServerLeave.Register(this, OnLeave);
        ServerApi.Hooks.ServerChat.Register(this, OnServerChat, int.MinValue);
        On.OTAPI.Hooks.MessageBuffer.InvokeGetData += OnGetData;
        GeneralHooks.ReloadEvent += OnReload;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            ServerApi.Hooks.ServerLeave.Deregister(this, OnLeave);
            ServerApi.Hooks.ServerChat.Deregister(this, OnServerChat);
            On.OTAPI.Hooks.MessageBuffer.InvokeGetData -= OnGetData;
            GeneralHooks.ReloadEvent -= OnReload;
        }
        base.Dispose(disposing);
    }

    private void OnReload(ReloadEventArgs args)
    {
        Config = Configuration.Load();
        args.Player?.SendSuccessMessage("[ItemsDeco] Configuration reloaded.");
    }

    private void OnLeave(LeaveEventArgs args)
    {
        _lastSelectedItem.Remove(args.Who);
    }

    private void OnServerChat(ServerChatEventArgs args)
    {
        if (args.Handled) return;

        var player = TShock.Players[args.Who];
        if (player == null) return;

        var text = args.Text;

        if (string.IsNullOrWhiteSpace(text)) return;
        if (text.StartsWith(TShock.Config.Settings.CommandSpecifier)) return;
        if (text.StartsWith(TShock.Config.Settings.CommandSilentSpecifier)) return;

        var message = FormatChatMessage(player, text);

        TShock.Utils.Broadcast(
            string.Format(
                TShock.Config.Settings.ChatFormat,
                player.Group.Name,
                player.Group.Prefix,
                player.Name,
                player.Group.Suffix,
                message),
            player.Group.R, player.Group.G, player.Group.B);

        args.Handled = true;
    }

    private bool OnGetData(
        On.OTAPI.Hooks.MessageBuffer.orig_InvokeGetData orig,
        MessageBuffer instance,
        ref byte packetId,
        ref int readOffset,
        ref int start,
        ref int length,
        ref int messageType,
        int maxPackets)
    {
        try
        {
            if (packetId == 13)
            {
                using var ms = new MemoryStream(instance.readBuffer);
                ms.Position = readOffset;
                using var reader = new BinaryReader(ms);

                var playerIndex = reader.ReadByte();
                var player = TShock.Players[playerIndex];

                if (player == null || !player.Active || player.Dead)
                    return orig(instance, ref packetId, ref readOffset, ref start, ref length, ref messageType, maxPackets);

                reader.BaseStream.Seek(4, SeekOrigin.Current);
                var selectedSlot = reader.ReadByte();

                if (player.TPlayer.selectedItem != selectedSlot)
                {
                    var item = player.TPlayer.inventory[selectedSlot];
                    HandleItemSwitch(player, item);
                }
            }
        }
        catch (Exception ex)
        {
            TShock.Log.ConsoleError($"[ItemsDeco] Error: {ex.Message}");
        }

        return orig(instance, ref packetId, ref readOffset, ref start, ref length, ref messageType, maxPackets);
    }

    private void HandleItemSwitch(TSPlayer player, Item item)
    {
        if (item == null || item.type == ItemID.None) return;

        if (Config.ItemText.Enabled)
        {
            ShowItemText(player, item);
        }

        if (Config.ItemAboveHead.Enabled)
        {
            ShowItemAboveHead(player, item);
        }
    }

    private void ShowItemText(TSPlayer player, Item item)
    {
        var parts = new List<string>();

        if (Config.ItemText.ShowName)
        {
            parts.Add(item.Name);
        }

        if (Config.ItemText.ShowDamage && item.damage > 0)
        {
            parts.Add($"{Config.ItemText.DamageText}: {item.damage}");
        }

        if (parts.Count == 0) return;

        var message = string.Join(" - ", parts);
        var color = GetRarityColor(item.rare);

        player.SendData(PacketTypes.CreateCombatTextExtended, message,
            (int)color.PackedValue,
            player.TPlayer.Center.X,
            player.TPlayer.Center.Y - 32);
    }

    private void ShowItemAboveHead(TSPlayer player, Item item)
    {
        if (!_lastSelectedItem.TryGetValue(player.Index, out int lastItem) || lastItem != item.type)
        {
            _lastSelectedItem[player.Index] = item.type;

            var settings = new ParticleOrchestraSettings
            {
                IndexOfPlayerWhoInvokedThis = (byte)player.Index,
                MovementVector = new Vector2(0, -24),
                PositionInWorld = player.TPlayer.Center + new Vector2(0, -24),
                UniqueInfoPiece = item.type
            };

            ParticleOrchestrator.BroadcastParticleSpawn(ParticleOrchestraType.ItemTransfer, settings);
        }
    }

    private string FormatChatMessage(TSPlayer player, string message)
    {
        if (!Config.ItemChat.Enabled)
            return message;

        var item = player.TPlayer.inventory[player.TPlayer.selectedItem];

        if (item == null || item.type <= 0)
            return message;

        var parts = new List<string>();

        if (Config.ItemChat.ShowName)
        {
            parts.Add($"[i:{item.type}]");
        }

        if (Config.ItemChat.ShowDamage && item.damage > 0)
        {
            var hex = Config.ItemChat.DamageColor.ToHex();
            parts.Add($"[c/{hex}:{item.damage}]");
        }

        if (parts.Count == 0)
            return message;

        return $"[ {string.Join(" ", parts)} ] {message}";
    }

    private Color GetRarityColor(int rarity)
    {
        if (Config.ItemText.RarityColors.TryGetValue(rarity, out var color))
        {
            return new Color(color.R, color.G, color.B);
        }

        return new Color(
            Config.ItemText.DefaultColor.R,
            Config.ItemText.DefaultColor.G,
            Config.ItemText.DefaultColor.B);
    }
}