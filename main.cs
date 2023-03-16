using System;
using System.Xml.Serialization;
using Rocket.API;
using Rocket.Core.Plugins;
using Rocket.Unturned.Events;
using Rocket.Unturned.Player;
using UnityEngine;
using SDG.Unturned;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Rocket.Unturned;
using Rocket.Core;
using Steamworks;
using Rocket.API.Collections;
using Rocket.Unturned.Chat;
using System.Linq;

namespace intrcptn {
    public class db_player {
        public ulong steam_id;
        public string name;
        public string surname;
        public string dob_d;
        public string dob_m;
        public string dob_y;

        public static int get_index(ulong sid) {
            for (int i = 0; i < main.cfg.players.Count; i++) 
                if (main.cfg.players[i].steam_id == sid) return i;
            return -1;
        }
    }
    
    public class character_class {
        public string name;
        public ushort hat;
        public ushort glasses;
        public ushort mask;
        public ushort shirt;
        public ushort vest;
        public ushort pants;
        public string image_url;
    }
    
    public class info {
        public string n;
        public string sn;
        public string d;
        public string m;
        public string y;
        public int ci;
    }

    public static class globals {
        public static Dictionary<ulong, info> dict = new Dictionary<ulong, info>();
    }

    public static class util {
        public static void raycast(UnturnedPlayer player, float distance, Action<RaycastHit> callback) {
            var _r = Physics.RaycastAll(player.Player.look.aim.position, player.Player.look.aim.forward, distance);
            for (int i = 0; i < _r.Length; i++) {
                callback(_r[i]);
            }
        }

        public static RaycastHit raycast(UnturnedPlayer player, float distance, int masks) {
            Physics.Raycast(player.Player.look.aim.position, player.Player.look.aim.forward, out var raycastHit, distance, masks);
            return raycastHit;
        }
    }

    internal class cmd_passport : IRocketCommand {
        public void Execute(IRocketPlayer caller, params string[] command) {
            UnturnedPlayer p = (UnturnedPlayer)caller;
            bool object_found = false;
            util.raycast(p, main.cfg.pass_max_distance, delegate(RaycastHit hit) {
                if (object_found) return;
                RaycastInfo raycastInfo = new RaycastInfo(hit);
                raycastInfo.limb = ELimb.SPINE;
                if (raycastInfo.transform != null) {
                    if (raycastInfo.transform.CompareTag("Enemy")) {
                        raycastInfo.player = DamageTool.getPlayer(raycastInfo.transform);
                        raycastInfo.limb = DamageTool.getLimb(raycastInfo.transform);
                    }
                    Player _p2 = raycastInfo.player;
                    if (_p2 == null) return;
                    object_found = true;
                    UnturnedPlayer p2 = UnturnedPlayer.FromPlayer(_p2);
                    if (p2 == null || p2.Player == null) return;
                    int index = db_player.get_index(p2.CSteamID.m_SteamID);
                    if (index == -1) return;
                    p2.Player.setPluginWidgetFlag(EPluginWidgetFlags.Modal, true);
                    EffectManager.sendUIEffect(main.cfg.pass_effect_id, main.cfg.pass_effect_key, p2.Player.channel.owner.transportConnection, true);
                    if (p2.SteamProfile != null && p2.SteamProfile.AvatarFull != null && p2.SteamProfile.AvatarFull.AbsoluteUri != null)
                        EffectManager.sendUIEffectImageURL(main.cfg.pass_effect_key, p2.Player.channel.owner.transportConnection, true, "avatar", p2.SteamProfile.AvatarFull.AbsoluteUri);
                    EffectManager.sendUIEffectText(main.cfg.pass_effect_key, p2.Player.channel.owner.transportConnection, true, "name_text", main.cfg.players[index].name);
                    EffectManager.sendUIEffectText(main.cfg.pass_effect_key, p2.Player.channel.owner.transportConnection, true, "surname_text", main.cfg.players[index].surname);
                    EffectManager.sendUIEffectText(main.cfg.pass_effect_key, p2.Player.channel.owner.transportConnection, true, "dob_text", main.instance.Translate("dob_format", main.cfg.players[index].dob_d, main.cfg.players[index].dob_m, main.cfg.players[index].dob_y));
                }
            });
        }

        public AllowedCaller AllowedCaller => AllowedCaller.Player;
        public string Name => "passport";
        public string Help => "null";
        public string Syntax => "null";
        public List<string> Aliases => new List<string>();
        public List<string> Permissions => new List<string> { "interception.signup.passport" };
    }

    public class config : IRocketPluginConfiguration, IDefaultable {
        public ushort reg_effect_id;
        public short reg_effect_key;
        public ushort pass_effect_id;
        public short pass_effect_key;
        public float pass_max_distance;
        public int min_name_len;
        public List<string> names_blacklist;
        public int min_surname_len;
        public List<string> surnames_blacklist;
        public int min_dob_day;
        public int max_dob_day;
        public int min_dob_month;
        public int max_dob_month;
        public int min_dob_year;
        public int max_dob_year;

        public List<character_class> classes;
        public List<db_player> players;
       
        public void LoadDefaults() {
            reg_effect_id = 24955;
            reg_effect_key = -24955;
            pass_effect_id = 24956;
            pass_effect_key = -24956;
            pass_max_distance = 10f;
            min_name_len = 1;
            names_blacklist = new List<string>() { 
                "something"
            };
            min_surname_len = 4;
            surnames_blacklist = new List<string>() {
                "something"
            };
            min_dob_day = 1;
            max_dob_day = 31;
            min_dob_month = 1;
            max_dob_month = 12;
            min_dob_year = 1900;
            max_dob_year = 2010;
            classes = new List<character_class>() {
                new character_class() {
                    name = "farmer",
                    hat = 244,
                    glasses = 0,
                    mask = 0,
                    shirt = 242,
                    vest = 0,
                    pants = 243,
                    image_url = "https://st2.depositphotos.com/1480128/7947/i/600/depositphotos_79472762-stock-photo-farmer-in-soybean-fields.jpg"
                }
            };
            players = new List<db_player>();
        }
    }

    public class main : RocketPlugin<config> {
        internal static main instance;
        internal static config cfg;

        protected override void Load() {
            instance = this;
            cfg = instance.Configuration.Instance;
            Provider.onCheckValidWithExplanation += delegate (ValidateAuthTicketResponse_t callback, ref bool isValid, ref string explanation) {
                int index = db_player.get_index(callback.m_SteamID.m_SteamID);
                if (index == -1) return;
                SteamPending steamPending = Provider.pending.FirstOrDefault((SteamPending x) => x.playerID.steamID == callback.m_SteamID);
                if (steamPending == null) return;
                steamPending.playerID.characterName = cfg.players[index].name + " " + cfg.players[index].surname;
            };
            U.Events.OnPlayerConnected += delegate (UnturnedPlayer p) {
                int index = db_player.get_index(p.CSteamID.m_SteamID);
                if (index != -1) return;
                globals.dict.Add(p.CSteamID.m_SteamID, new info() { n = string.Empty, sn = string.Empty, d = string.Empty, m = string.Empty, y = string.Empty, ci = 0 });
                p.Player.setPluginWidgetFlag(EPluginWidgetFlags.Modal, true);
                EffectManager.sendUIEffect(main.cfg.reg_effect_id, main.cfg.reg_effect_key, p.Player.channel.owner.transportConnection, true);
                EffectManager.sendUIEffectImageURL(main.cfg.reg_effect_key, p.Player.channel.owner.transportConnection, true, "background_image", main.cfg.classes[0].image_url);
                EffectManager.sendUIEffectText(main.cfg.reg_effect_key, p.Player.channel.owner.transportConnection, true, "character", main.cfg.classes[0].name);
            };
            EffectManager.onEffectTextCommitted += delegate (Player player, string buttonName, string text) {
                var sid = player.channel.owner.playerID.steamID.m_SteamID;
                if (!globals.dict.ContainsKey(sid)) return;
                if (buttonName == "name_input") globals.dict[sid].n = text;
                else if (buttonName == "surname_input") globals.dict[sid].sn = text;
                else if (buttonName == "dob_input_d") globals.dict[sid].d = text;
                else if (buttonName == "dob_input_m") globals.dict[sid].m = text;
                else if (buttonName == "dob_input_y") globals.dict[sid].y = text;
                else return;
            };
            EffectManager.onEffectButtonClicked += delegate (Player player, string buttonName) {
                var sid = player.channel.owner.playerID.steamID.m_SteamID;
                if (buttonName == "accept_button") {
                    if (!globals.dict.ContainsKey(sid)) return;
                    if (globals.dict[sid].n.Length < cfg.min_name_len || globals.dict[sid].sn.Length < cfg.min_surname_len) return;
                    if (cfg.names_blacklist.Contains(globals.dict[sid].n.ToLower()) || cfg.surnames_blacklist.Contains(globals.dict[sid].sn.ToLower())) return;
                    int day;
                    if (!int.TryParse(globals.dict[sid].d, out day)) return;
                    if (day < cfg.min_dob_day || day > cfg.max_dob_day) return;
                    int month;
                    if (!int.TryParse(globals.dict[sid].m, out month)) return;
                    if (month < cfg.min_dob_month || month > cfg.max_dob_month) return;
                    int year;
                    if (!int.TryParse(globals.dict[sid].y, out year)) return;
                    if (year < cfg.min_dob_year || year > cfg.max_dob_year) return;
                    string str_day = string.Empty;
                    if (day < 10) str_day = "0" + day.ToString();
                    string str_month = string.Empty;
                    if (month < 10) str_day = "0" + month.ToString();
                    main.cfg.players.Add(new db_player() {
                        steam_id = sid,
                        name = globals.dict[sid].n,
                        surname = globals.dict[sid].sn,
                        dob_d = str_day,
                        dob_m = str_month,
                        dob_y = year.ToString()
                    });
                    if (main.cfg.classes[globals.dict[sid].ci].hat > 0) player.inventory.forceAddItemAuto(new Item(main.cfg.classes[globals.dict[sid].ci].hat, true), false, false, true);
                    if (main.cfg.classes[globals.dict[sid].ci].glasses > 0) player.inventory.forceAddItemAuto(new Item(main.cfg.classes[globals.dict[sid].ci].glasses, true), false, false, true);
                    if (main.cfg.classes[globals.dict[sid].ci].mask > 0) player.inventory.forceAddItemAuto(new Item(main.cfg.classes[globals.dict[sid].ci].mask, true), false, false, true);
                    if (main.cfg.classes[globals.dict[sid].ci].shirt > 0) player.inventory.forceAddItemAuto(new Item(main.cfg.classes[globals.dict[sid].ci].shirt, true), false, false, true);
                    if (main.cfg.classes[globals.dict[sid].ci].vest > 0) player.inventory.forceAddItemAuto(new Item(main.cfg.classes[globals.dict[sid].ci].vest, true), false, false, true);
                    if (main.cfg.classes[globals.dict[sid].ci].pants > 0) player.inventory.forceAddItemAuto(new Item(main.cfg.classes[globals.dict[sid].ci].pants, true), false, false, true);
                    Provider.kick(player.channel.owner.playerID.steamID, main.instance.Translate("kick_reason"));
                }
                else if (buttonName == "close_pass_button") {
                    EffectManager.askEffectClearByID(main.cfg.pass_effect_id, player.channel.owner.transportConnection);
                    player.setPluginWidgetFlag(EPluginWidgetFlags.Modal, false);
                }
                else if (buttonName == "left_arrow") {
                    if (!globals.dict.ContainsKey(sid)) return;
                    globals.dict[sid].ci -= 1;
                    if (globals.dict[sid].ci < 0) globals.dict[sid].ci = main.cfg.classes.Count - 1;
                    EffectManager.sendUIEffectImageURL(main.cfg.reg_effect_key, player.channel.owner.transportConnection, true, "background_image", main.cfg.classes[globals.dict[sid].ci].image_url);
                    EffectManager.sendUIEffectText(main.cfg.reg_effect_key, player.channel.owner.transportConnection, true, "character", main.cfg.classes[globals.dict[sid].ci].name);
                }
                else if (buttonName == "right_arrow") {
                    if (!globals.dict.ContainsKey(sid)) return;
                    globals.dict[sid].ci += 1;
                    if (globals.dict[sid].ci > main.cfg.classes.Count - 1) globals.dict[sid].ci = 0;
                    EffectManager.sendUIEffectImageURL(main.cfg.reg_effect_key, player.channel.owner.transportConnection, true, "background_image", main.cfg.classes[globals.dict[sid].ci].image_url);
                    EffectManager.sendUIEffectText(main.cfg.reg_effect_key, player.channel.owner.transportConnection, true, "character", main.cfg.classes[globals.dict[sid].ci].name);
                }
                else return;
            };
            SaveManager.onPostSave += delegate () {
                Configuration.Save();
            };
            GC.Collect();
        }

        protected override void Unload() {
            cfg = null;
            instance = null;
            GC.Collect();
        }

        public override TranslationList DefaultTranslations => new TranslationList
        {
            { "kick_reason", "Регистрация на проекте, перезайдите на сервер" },
            { "dob_format", "{0}/{1}/{2}" },
        };
    }
}

