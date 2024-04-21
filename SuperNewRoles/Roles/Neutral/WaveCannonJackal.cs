using System;
using System.Collections.Generic;
using AmongUs.GameOptions;
using Hazel;
using SuperNewRoles.Buttons;
using SuperNewRoles.Helpers;
using SuperNewRoles.Mode;
using SuperNewRoles.Patches;
using SuperNewRoles.Roles.Role;
using SuperNewRoles.Roles.RoleBases;
using SuperNewRoles.Roles.RoleBases.Interfaces;
using SuperNewRoles.WaveCannonObj;
using UnityEngine;
using static SuperNewRoles.Modules.CustomOption;
using static SuperNewRoles.Modules.CustomOptionHolder;
using static SuperNewRoles.Patches.PlayerControlFixedUpdatePatch;
using static SuperNewRoles.WaveCannonObj.WaveCannonObject;

namespace SuperNewRoles.Roles.Neutral;

public enum WCJackalSidekickType
{
    Sidekick,
    JackalFriends,
    WaveCannonSidekick,
    BulletSidekick,
}
public class WaveCannonJackal : RoleBase, INeutral, ICustomButton, ISaboAvailable, IImpostorVision, IJackal
{
    public static new RoleInfo Roleinfo = new(
        typeof(WaveCannonJackal),
        (p) => new WaveCannonJackal(p),
        RoleId.WaveCannonJackal,
        "WaveCannonJackal",
        RoleClass.JackalBlue,
        new(RoleId.WaveCannonJackal, TeamTag.Jackal,
            RoleTag.Killer, RoleTag.CanSidekick, RoleTag.SpecialKiller),
        TeamRoleType.Neutral,
        TeamType.Neutral
        );
    public static new OptionInfo Optioninfo =
        new(RoleId.WaveCannonJackal, 300120, false,
            CoolTimeOption: (20f, 2.5f, 180, 2.5f, false),
            VentOption: (true, false),
            SaboOption: (false, false),
            ImpostorVisionOption: (true, false),
            optionCreator: CreateOption);
    public static new IntroInfo Introinfo =
        new(RoleId.WaveCannonJackal, introSound: RoleTypes.Shapeshifter);

    public static CustomOption ChargeTime;
    public static CustomOption KillCooldown;
    public static CustomOption IsSyncKillCoolTime;
    
    // サイドキック系の設定
    public static CustomOption CanCreateSidekick;
    public static CustomOption CanCreateSidekickNewByNewJackal;
    public static CustomOption CreateSidekickCoolTime;
    public static CustomOption CreateSidekickType;

    // 弾の設定
    public static CustomOption CreateBulletToJackal;
    public static CustomOption CreatedSidekickHasWaveCannon;

    // 弾装填設定
    public static CustomOption BulletLoadBulletCooltime;
    public static CustomOption BulletLoadedChargeTime;


    public static CustomOption AnimationOptionType;

    private static void CreateOption()
    {
        ChargeTime = Create(Optioninfo.OptionId, false, CustomOptionType.Neutral, "WaveCannonChargeTime", 3f, 0.5f, 15f, 0.5f, Optioninfo.RoleOption); Optioninfo.OptionId++;
        KillCooldown = Create(Optioninfo.OptionId, false, CustomOptionType.Neutral, "JackalCooldownSetting", 30f, 2.5f, 60f, 2.5f, Optioninfo.RoleOption, format: "unitSeconds"); Optioninfo.OptionId++;
        IsSyncKillCoolTime = Create(Optioninfo.OptionId, false, CustomOptionType.Neutral, "IsSyncKillCoolTime", false, Optioninfo.RoleOption); Optioninfo.OptionId++;
        // サイドキック系の設定
        CanCreateSidekick = Create(Optioninfo.OptionId, false, CustomOptionType.Neutral, "JackalCreateSidekickSetting", false, Optioninfo.RoleOption); Optioninfo.OptionId++;
        CanCreateSidekickNewByNewJackal = Create(Optioninfo.OptionId, false, CustomOptionType.Neutral, "JackalNewJackalCreateSidekickSetting", false, CanCreateSidekick); Optioninfo.OptionId++;
        CreateSidekickCoolTime = Create(Optioninfo.OptionId, false, CustomOptionType.Neutral, "PavlovsownerCreateDogCoolTime", 30f, 2.5f, 60f, 2.5f, CanCreateSidekick, format: "unitSeconds"); Optioninfo.OptionId++;

        List<string> SidekickTypeTexts = [];
        foreach (WCJackalSidekickType type in System.Enum.GetValues(typeof(WCJackalSidekickType)))
        {
            SidekickTypeTexts.Add(ModTranslation.GetString("WaveCannonJackalSidekickType" + type));
        }
        CreateSidekickType = Create(Optioninfo.OptionId, false, CustomOptionType.Neutral, "WaveCannonJackalSidekickType", SidekickTypeTexts.ToArray(), CanCreateSidekick); Optioninfo.OptionId++;

        // Bullet
        CreateBulletToJackal = Create(Optioninfo.OptionId, false, CustomOptionType.Neutral, "WaveCannonJackalCreateBulletToJackal", false, CreateSidekickType, openSelection: 3); Optioninfo.OptionId++;
        CreatedSidekickHasWaveCannon = Create(Optioninfo.OptionId, false, CustomOptionType.Neutral, "WaveCannonJackalNewJackalHaveWaveCannon", false, CreateSidekickType, openSelection: 3); Optioninfo.OptionId++;

        BulletLoadBulletCooltime = Create(Optioninfo.OptionId, false, CustomOptionType.Neutral, "WaveCannonJackalLoadBulletCoolTime", 30f, 2.5f, 60f, 2.5f, CreateSidekickType, openSelection: 3, format: "unitSeconds"); Optioninfo.OptionId++;
        BulletLoadedChargeTime = Create(Optioninfo.OptionId, false, CustomOptionType.Neutral, "WaveCannonJackalBulletLoadedChargeTime", 10f, 1f, 30f, 1f, CreateSidekickType, openSelection: 3); Optioninfo.OptionId++;
        
        // AnimTypes
        string[] AnimTypeTexts = new string[WCCreateAnimHandlers.Count];
        int index = 0;
        foreach (string TypeName in WCCreateAnimHandlers.Keys)
        {
            if (TypeName == WCAnimType.None.ToString())
                break;
            AnimTypeTexts[index] = ModTranslation.GetString("WaveCannonAnimType" + TypeName);
            index++;
        }
        AnimationOptionType = Create(Optioninfo.OptionId, false, CustomOptionType.Neutral, "WaveCannonAnimationType", AnimTypeTexts, Optioninfo.RoleOption); Optioninfo.OptionId++;
    }

    public bool CanUseSabo => Optioninfo.CanUseSabo;
    public bool CanUseVent => Optioninfo.CanUseVent;
    public bool IsImpostorVision => Optioninfo.IsImpostorVision;

    public bool IwasSidekicked { get; private set; }

    public CustomButtonInfo WaveCannonButtonInfo;
    public CustomButtonInfo[] CustomButtonInfos { get; }

    public bool CanSidekick { get; private set; }

    public ISidekick CreatedSidekick;

    private void ButtonOnClick()
    {
        var pos = CachedPlayer.LocalPlayer.transform.position;
        MessageWriter writer = RPCHelper.StartRPC(CustomRPC.WaveCannon);
        writer.Write((byte)WaveCannonObject.RpcType.Spawn);
        writer.Write((byte)0);
        writer.Write(CachedPlayer.LocalPlayer.PlayerPhysics.FlipX);
        writer.Write(CachedPlayer.LocalPlayer.PlayerId);
        writer.Write(pos.x);
        writer.Write(pos.y);
        writer.Write((byte)WaveCannonJackal.AnimationOptionType.GetSelection());
        writer.EndRPC();
        RPCProcedure.WaveCannon((byte)WaveCannonObject.RpcType.Spawn, 0, CachedPlayer.LocalPlayer.PlayerPhysics.FlipX, CachedPlayer.LocalPlayer.PlayerId, pos, (WaveCannonObject.WCAnimType)WaveCannonJackal.AnimationOptionType.GetSelection());
    }
    private void OnEffectEnds()
    {
        WaveCannonObject obj = Objects.Values.FirstOrDefault(x => x.Owner != null && x.Owner.PlayerId == CachedPlayer.LocalPlayer.PlayerId && x.Id == WaveCannonObject.Ids[CachedPlayer.LocalPlayer.PlayerId] - 1);
        if (obj == null)
        {
            Logger.Info("nullなのでreturnしました", "WaveCannonButton");
            return;
        }
        var pos = CachedPlayer.LocalPlayer.transform.position;
        byte[] buff = new byte[sizeof(float) * 2];
        Buffer.BlockCopy(BitConverter.GetBytes(pos.x), 0, buff, 0 * sizeof(float), sizeof(float));
        Buffer.BlockCopy(BitConverter.GetBytes(pos.y), 0, buff, 1 * sizeof(float), sizeof(float));
        MessageWriter writer = RPCHelper.StartRPC(CustomRPC.WaveCannon);
        writer.Write((byte)RpcType.Shoot);
        writer.Write((byte)obj.Id);
        writer.Write(CachedPlayer.LocalPlayer.PlayerPhysics.FlipX);
        writer.Write(CachedPlayer.LocalPlayer.PlayerId);
        writer.Write(pos.x);
        writer.Write(pos.y);
        writer.Write((byte)AnimationOptionType.GetSelection());
        writer.EndRPC();
        RPCProcedure.WaveCannon((byte)RpcType.Shoot, (byte)obj.Id, CachedPlayer.LocalPlayer.PlayerPhysics.FlipX, CachedPlayer.LocalPlayer.PlayerId, pos, (WCAnimType)AnimationOptionType.GetSelection());
    }


    public WaveCannonJackal(PlayerControl p) : base(p, Roleinfo, Optioninfo, Introinfo)
    {
        IwasSidekicked = false;
        CanSidekick = CanCreateSidekick.GetBool();
        CreatedSidekick = null;

        WaveCannonButtonInfo = new(null, this, ButtonOnClick, (isAlive) => isAlive, CustomButtonCouldType.CanMove, null,
            ModHelpers.LoadSpriteFromResources("SuperNewRoles.Resources.WaveCannonButton.png", 115f),
            () => Optioninfo.CoolTime, new Vector3(-2f, 1, 0),
            ModTranslation.GetString("WaveCannonButtonName"), KeyCode.F,
            DurationTime: () => ChargeTime.GetFloat(), OnEffectEnds: OnEffectEnds);
        CustomButtonInfos = [WaveCannonButtonInfo];
    }
    public static void ResetCooldowns()
    {
        HudManagerStartPatch.JackalKillButton.MaxTimer = KillCooldown.GetFloat();
        HudManagerStartPatch.JackalKillButton.Timer = HudManagerStartPatch.JackalKillButton.MaxTimer;
        if (!IsSyncKillCoolTime.GetBool())
            return;
        WaveCannonJackal wcjackal = RoleBaseManager.GetLocalRoleBase<WaveCannonJackal>();
        if (wcjackal != null)
            wcjackal.WaveCannonButtonInfo.ResetCoolTime();
    }
    public static void EndMeeting() => ResetCooldowns();

    public void SetAmSidekicked()
    {
        CanSidekick = CanCreateSidekickNewByNewJackal.GetBool();
        IwasSidekicked = true;
    }
}
/*
class WaveCannonJackal
{
    // RoleClass Start
    public static List<PlayerControl> WaveCannonJackalPlayer;
    public static List<PlayerControl> FakeSidekickWaveCannonPlayer;
    public static List<PlayerControl> SidekickWaveCannonPlayer;
    public static Color32 color = RoleClass.JackalBlue;
    public static List<int> CreatePlayers;
    public static bool CanCreateFriend;
    public static void ClearAndReload()
    {
        WaveCannonJackalPlayer = new();
        FakeSidekickWaveCannonPlayer = new();
        SidekickWaveCannonPlayer = new();
        CreatePlayers = new();
        CanCreateSidekick = WaveCannonJackalCreateSidekick.GetBool();
        CanCreateFriend = WaveCannonJackalCreateFriend.GetBool();
    }
    // RoleClass End

    // Button Start
    public static CustomButton WaveCannonJackalSidekickButton;

    public static void MakeButtons(HudManager hm)
    {
        WaveCannonJackalSidekickButton = new(
            () =>
            {
                var target = JackalSetTarget();
                if (target && RoleHelpers.IsAlive(PlayerControl.LocalPlayer) && PlayerControl.LocalPlayer.CanMove && CanCreateSidekick)
                {
                    if (target.IsRole(RoleId.SideKiller)) // サイドキック相手がマッドキラーの場合
                    {
                        if (!RoleClass.SideKiller.IsUpMadKiller) // サイドキラーが未昇格の場合
                        {
                            var sidePlayer = RoleClass.SideKiller.GetSidePlayer(target); // targetのサイドキラーを取得
                            if (sidePlayer != null && sidePlayer.IsAlive()) // null(作っていない)ならば処理しない
                            {
                                sidePlayer.RPCSetRoleUnchecked(RoleTypes.Impostor);
                                RoleClass.SideKiller.IsUpMadKiller = true;
                            }
                        }
                    }
                    if (CanCreateFriend)
                    {
                        Jackal.CreateJackalFriends(target); //クルーにして フレンズにする
                    }
                    else
                    {
                        bool IsFakeWaveCannonJackal = EvilEraser.IsBlockAndTryUse(EvilEraser.BlockTypes.WaveCannonJackalSidekick, target);
                        MessageWriter killWriter = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.CreateSidekickWaveCannon, SendOption.Reliable, -1);
                        killWriter.Write(target.PlayerId);
                        killWriter.Write(IsFakeWaveCannonJackal);
                        AmongUsClient.Instance.FinishRpcImmediately(killWriter);
                        RPCProcedure.CreateSidekickWaveCannon(target.PlayerId, IsFakeWaveCannonJackal);
                    }
                    CanCreateSidekick = false;
                }
            },
            (bool isAlive, RoleId role) => { return isAlive && role is RoleId.WaveCannonJackal && ModeHandler.IsMode(ModeId.Default) && CanCreateSidekick; },
            () =>
            {
                return JackalSetTarget() && PlayerControl.LocalPlayer.CanMove;
            },
            () => { EndMeeting(); },
            RoleClass.Jackal.GetButtonSprite(),
            new Vector3(-2.925f, -0.06f, 0),
            hm,
            hm.AbilityButton,
            null,
            0,
            () => { return false; }
        )
        {
            buttonText = ModTranslation.GetString("JackalCreateSidekickButtonName"),
            showButtonText = true
        };
    }
    public static void EndMeeting() => EndMeetingResetCooldown();

    // Button Start

    public class WaveCannonJackalFixedPatch
    {
        public static void WaveCannonJackalPlayerOutLineTarget()
        {
            JackalSeer.SetPlayerOutline(JackalSetTarget(), color);
        }
        /// <summary>
        /// SK昇格処理を行うかの判定、追放処理時・キル発生時に判定される
        /// </summary>
        /// <param name="role">死亡者の役職</param>
        public static void Postfix(PlayerControl __instance, RoleId role)
        {
            if (AmongUsClient.Instance.AmHost)
            {
                if (WaveCannonJackal.SidekickWaveCannonPlayer.Count > 0)
                {
                    var upflag = true;
                    foreach (PlayerControl p in WaveCannonJackalPlayer)
                    {
                        if (p.IsAlive())
                        {
                            upflag = false;
                        }
                    }
                    if (upflag)
                    {
                        byte jackalId = (byte)RoleId.WaveCannonJackal;
                        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SidekickPromotes, SendOption.Reliable, -1);
                        writer.Write(jackalId);
                        AmongUsClient.Instance.FinishRpcImmediately(writer);
                        RPCProcedure.SidekickPromotes(jackalId);
                    }
                }
            }
            if (role == RoleId.WaveCannonJackal)
            {
                WaveCannonJackalPlayerOutLineTarget();
            }
        }
    }
}*/