using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using InfinityScript;

namespace DeadliestTag
{
    public class DeadliestTag : BaseScript
    {
        public Entity[] dogtags = new Entity[10];
        public int vanish;
        private int _mapCount = 0;

        public DeadliestTag()
        {
            vanish = Call<int>("loadfx", "impacts/small_snowhit");
            Call(294, "prop_dogtags_foe");
            Call(294, "prop_dogtags_friend");
            Call(297, "waypoint_dogtags");
            PlayerConnected += new Action<Entity>(entity =>
                {
                    entity.SpawnedPlayer += () => OnPlayerSpawned(entity);
                    entity.SetClientDvar("cg_objectiveText", "Avoid enemy tags and pick up your own team's tags for points!");
                });
            for (int i = 0; i < 10; i++)
            {
                int curObjID = 31 - _mapCount++;
                dogtags[i] = Call<Entity>("spawn", "script_model", new Vector3(0, 0, 0));
                dogtags[i].SetField("isReset", 1);
                dogtags[i].SetField("objId", curObjID);
                Call(431, dogtags[i].GetField<int>("objId"), "invisible", new Vector3(0, 0, 0));
                Call(434, dogtags[i].GetField<int>("objId"), "waypoint_dogtags");
                Entity[] tagVisuals = new Entity[2];
                tagVisuals[0] = Call<Entity>("spawn", "script_model", new Vector3(0, 0, 0));
                tagVisuals[1] = Call<Entity>("spawn", "script_model", new Vector3(0, 0, 0));
                tagVisuals[0].Call("setmodel", "prop_dogtags_friend");
                tagVisuals[1].Call("setmodel", "prop_dogtags_foe");
                tagVisuals[0].Call("hide");
                tagVisuals[1].Call("hide");
                dogtags[i].SetField("visuals", new Parameter(tagVisuals));
            }
        }

        public void OnPlayerSpawned(Entity player)
        {
            dogtags[player.EntRef].SetField("team", player.GetField<string>("sessionteam"));
            player.SetClientDvar("cg_objectiveText", "Avoid enemy tags and pick up your own team's tags for points!");
        }

        public override void OnSay(Entity player, string name, string message)
        {
            if (message == "tag")
                SpawnTag(player);
        }

        public override void OnPlayerKilled(Entity player, Entity inflictor, Entity attacker, int damage, string mod, string weapon, Vector3 dir, string hitLoc)
        {
            AfterDelay(150, () => SpawnTag(player));
        }

        public void SpawnTag(Entity player)
        {
            Call("playfx", vanish, dogtags[player.EntRef].Origin);
            dogtags[player.EntRef].Origin =  player.Origin;
            dogtags[player.EntRef].SetField("isReset", 0);
            Call(435, dogtags[player.EntRef].GetField<int>("objId"), dogtags[player.EntRef].Origin);
            Call(433, dogtags[player.EntRef].GetField<int>("objId"), "active");
            Call(358, dogtags[player.EntRef].GetField<int>("objId"), player.GetField<string>("sessionteam"));
            dogtags[player.EntRef].GetField<Entity[]>("visuals")[0].Origin = player.Origin + new Vector3(0, 0, 14);
            dogtags[player.EntRef].GetField<Entity[]>("visuals")[1].Origin = player.Origin + new Vector3(0, 0, 14);
            WatchTagTeams(player);
            Call(377, dogtags[player.EntRef].Origin, "mp_killconfirm_tags_drop");
                OnInterval(200, () =>
                    {
                        if (dogtags[player.EntRef].GetField<int>("isReset") == 0)
                        {
                            WatchTagPickup(player.EntRef);
                            return true;
                        }
                        else return false;
                    });
            OnInterval(1000, () =>
                {
                    if (dogtags[player.EntRef].GetField<int>("isReset") == 0)
                    {
                        Bounce(dogtags[player.EntRef], dogtags[player.EntRef].GetField<Entity[]>("visuals")[0]);
                        Bounce(dogtags[player.EntRef], dogtags[player.EntRef].GetField<Entity[]>("visuals")[1]);
                        return true;
                    }
                    else return false;
                });
        }

        public void WatchTagTeams(Entity player)
        {
            dogtags[player.EntRef].GetField<Entity[]>("visuals")[0].Call("hide");
            dogtags[player.EntRef].GetField<Entity[]>("visuals")[1].Call("hide");
            foreach (Entity players in Players)
            {
                if (players.GetField<string>("sessionteam") == player.GetField<string>("sessionteam"))
                    dogtags[player.EntRef].GetField<Entity[]>("visuals")[1].Call("showtoplayer", player);
                else dogtags[player.EntRef].GetField<Entity[]>("visuals")[0].Call("showtoplayer", player);
            }
        }

        public void WatchTagPickup(int tagID)
        {
            foreach (Entity player in Players)
            {
                if (player.Origin.DistanceTo(dogtags[tagID].Origin) < 50 && player.IsAlive)
                {
                    if (player.GetField<string>("sessionteam") != dogtags[tagID].GetField<string>("team"))
                    {
                        Call(315, dogtags[tagID].GetField<string>("team"), Call<int>(314, dogtags[tagID].GetField<string>("team")) + 1000);
                        Call(377, dogtags[tagID].Origin, "mp_killconfirm_tags_deny");
                        scoreHUD(player, -1000);
                    }
                    else
                    {
                        Call(315, dogtags[tagID].GetField<string>("team"), Call<int>(314, dogtags[tagID].GetField<string>("team")) + 100);
                        Call(377, dogtags[tagID].Origin, "mp_killconfirm_tags_pickup");
                        scoreHUD(player, 100);
                    }
                    resetTag(tagID);
                }
            }
        }

        public void Bounce(Entity tag, Entity visual)
        {
                Vector3 bottomPos = tag.Origin;
                Vector3 topPos = tag.Origin + new Vector3(0, 0, 12);

                visual.Call("moveto", topPos, .5f, .15f, .15f);
                visual.Call("rotateyaw", 180, .5f);

                AfterDelay(500, () =>
                    {
                        visual.Call("moveto", bottomPos, .5f, .15f, .15f);
                        visual.Call("rotateyaw", 180, .5f);
                    });
        }

        public void resetTag(int tagID)
        {
            //Call("playfx", vanish, dogtags[tagID].Origin);
            dogtags[tagID].Origin = new Vector3(0, 0, 0);
            dogtags[tagID].GetField<Entity[]>("visuals")[0].Call("hide");
            dogtags[tagID].GetField<Entity[]>("visuals")[1].Call("hide");
            dogtags[tagID].GetField<Entity[]>("visuals")[0].Origin = new Vector3(0, 0, 0);
            dogtags[tagID].GetField<Entity[]>("visuals")[1].Origin = new Vector3(0, 0, 0);
            Call(435, dogtags[tagID].GetField<int>("objId"), new Vector3(0, 0, 0));
            Call(433, dogtags[tagID].GetField<int>("objId"), "invisible");
        }

        public void scoreHUD(Entity player, int amount)
        {
            HudElem score = HudElem.CreateFontString(player, "hudsmall", 1.2f);
            score.SetPoint("CENTER", "CENTER");
            if (amount < 0)
            {
                score.Color = new Vector3(.6f, .2f, .2f);
                score.SetText("-" + amount);
            }
            else
            {
                score.Color = new Vector3(.2f, .6f, .2f);
                score.SetText("+" + amount);
            }
            score.Alpha = 0;
            score.Call("fadeovertime", .1f);
            score.Alpha = 1;
            AfterDelay(2000, () =>
                {
                    score.Call("fadeovertime", .1f);
                    score.Alpha = 0;
                    AfterDelay(200, () =>
                        score.Call("destroy"));
                });
        }
    }
}
