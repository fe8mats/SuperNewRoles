using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SuperNewRoles.Replay.ReplayActions;
public class ReplayActionReportDeadBody : ReplayAction
{
    public byte sourcePlayer;
    public byte targetPlayer;
    public override void ReadReplayFile(BinaryReader reader) {
        ActionTime = reader.ReadSingle();
        //ここにパース処理書く
        sourcePlayer = reader.ReadByte();
        targetPlayer = reader.ReadByte();
    }
    public override void WriteReplayFile(BinaryWriter writer)
    {
        writer.Write(ActionTime);
        //ここにパース処理書く
        writer.Write(sourcePlayer);
        writer.Write(targetPlayer);
    }
    public override ReplayActionId GetActionId() => ReplayActionId.ReportDeadBody;
    //アクション実行時の処理
    public override void OnAction() {
        //ここに処理書く
        PlayerControl source = ModHelpers.PlayerById(sourcePlayer);
        GameData.PlayerInfo target = GameData.Instance.GetPlayerById(targetPlayer);
        if (source == null)
        {
            Logger.Info("エラー");
            return;
        }
        source.ReportDeadBody(target);
    }
    //試合内でアクションがあったら実行するやつ
    public static ReplayActionReportDeadBody Create(byte sourcePlayer, byte targetPlayer)
    {
        ReplayActionReportDeadBody action = new();
        if (!CheckAndCreate(action)) return null;
        //ここで初期化(コレは仮処理だから消してね)
        action.sourcePlayer = sourcePlayer;
        action.targetPlayer = targetPlayer;
        return action;
    }
}