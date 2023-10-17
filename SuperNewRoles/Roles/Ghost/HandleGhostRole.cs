using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using SuperNewRoles.Mode;

namespace SuperNewRoles.Roles;

class HandleGhostRole
{
    [HarmonyPatch(typeof(RoleManager), nameof(RoleManager.AssignRoleOnDeath))]
    class AssignRole
    {
        public static bool Prefix([HarmonyArgument(0)] PlayerControl player)
        {
            if (player.IsAlive()) return false; //生存者は弾く
            if (!ModeHandler.IsMode(ModeId.Default, ModeId.Werewolf, ModeId.SuperHostRoles)) return true;

            return true;
        }

        public static void Postfix([HarmonyArgument(0)] PlayerControl player)
        {
            if (!ModeHandler.IsMode(ModeId.Default, ModeId.Werewolf, ModeId.SuperHostRoles)) return;

            if (player.IsAlive() || !player.IsGhostRole(RoleId.DefaultRole)) return; //生存者と割り当て済みの人は弾く
            if (player.IsRole(AmongUs.GameOptions.RoleTypes.GuardianAngel)) return; //幽霊役職がアサインされていたら守護天使をアサインしない

            bool isAssign = HandleAssign(player);
            if (isAssign && ModeHandler.IsMode(ModeId.SuperHostRoles)) // 幽霊役職が配布された非導入者の役職を守護天使に変更する
            {
                if (!player.IsMod()) player.RpcSetRole(AmongUs.GameOptions.RoleTypes.GuardianAngel);
            }
        }
    }

    public static bool HandleAssign(PlayerControl player)
    {
        //各役職にあったアサインをする
        var Team = TeamRoleType.Error;
        Team = player.IsCrew() ? TeamRoleType.Crewmate : player.IsNeutral() ? TeamRoleType.Neutral : TeamRoleType.Impostor;
        List<IntroData> GhostRoles = new();
        foreach (IntroData intro in IntroData.GhostRoleData)
        {
            if (intro.Team != Team) continue;
            GhostRoles.Add(intro);
        }
        var assignrole = Assing(GhostRoles);
        if (assignrole == RoleId.DefaultRole) return false;
        switch (Team)
        {
            case TeamRoleType.Impostor:
                if (AllRoleSetClass.ImpostorGhostRolePlayerNum <= 0)
                    return false;
                AllRoleSetClass.ImpostorGhostRolePlayerNum--;
                break;
            case TeamRoleType.Neutral:
                if (AllRoleSetClass.NeutralGhostRolePlayerNum <= 0)
                    return false;
                AllRoleSetClass.NeutralGhostRolePlayerNum--;
                break;
            case TeamRoleType.Crewmate:
                if (AllRoleSetClass.CrewmateGhostRolePlayerNum <= 0)
                    return false;
                AllRoleSetClass.CrewmateGhostRolePlayerNum--;
                break;

        }
        player.SetRoleRPC(assignrole);
        return true;
    }

    //アサインする役職を決める
    public static RoleId Assing(List<IntroData> introData)
    {
        List<RoleId> Assigns = new();
        List<RoleId> Assignnos = new();
        ModeId mode = ModeHandler.GetMode();
        foreach (IntroData data in introData)
        {
            //その役職のプレイヤー数を取得
            var count = AllRoleSetClass.GetPlayerCount(data.RoleId);
            //設定を取得
            var option = IntroData.GetOption(data.RoleId);
            //確率を取得
            var selection = option.GetSelection();

            //確率が0%ではないかつ、
            //もう割り当てきられてないか(最大人数まで割り当てられていないか)
            if ((option.isSHROn || mode != ModeId.SuperHostRoles) && selection != 0 && count > CachedPlayer.AllPlayers.ToArray().ToList().Count((CachedPlayer pc) => pc.PlayerControl.IsGhostRole(data.RoleId)))
            {
                //100%なら100%アサインListに入れる
                if (selection == 10)
                {
                    Assigns.Add(data.RoleId);
                    //100%アサインリストの中身が0だったら処理しない(100%アサインリストのほうがアサインされるため)
                }
                else if (Assigns.Count <= 0)
                {
                    //確率分だけRoleIdを入れる
                    for (int i = 0; i < selection; i++)
                    {
                        Assignnos.Add(data.RoleId);
                    }
                }
            }
        }
        //100%アサインリストの中身が0ではなかったらランダムに選んでアサイン
        if (Assigns.Count > 0)
        {
            return ModHelpers.GetRandom(Assigns);
            //100%ではない、アサインリストの中身が0ではなかったらランダムに選んでアサイン
        }
        else if (Assignnos.Count > 0)
        {
            return ModHelpers.GetRandom(Assignnos);
        }
        //どっちも中身が0だったら通常の役職(DefaultRole)を返す
        return RoleId.DefaultRole;
    }
}